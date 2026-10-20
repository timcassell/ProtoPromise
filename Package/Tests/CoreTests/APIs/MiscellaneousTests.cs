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
                maxPreviousDepth = System.Math.Max(maxPreviousDepth, GetCausalityTraceDepth(previousTrace, depths));
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
                AssertCausalityTraceIsPrintedCorrectly(traceX, text);
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

#if NET6_0_OR_GREATER // Building a stack trace from frames requires .Net 5+, and the runtime's StackTraceHiddenAttribute requires .Net 6+.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static System.Diagnostics.StackFrame GetVisibleFrameForTraceTest() => new System.Diagnostics.StackFrame(0, true);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining), System.Diagnostics.StackTraceHidden]
        private static System.Diagnostics.StackFrame GetHiddenFrameForTraceTest() => new System.Diagnostics.StackFrame(0, true);

        [Test]
        public void CausalityTraceFormatStackTrace_HidesTheOutermostFrameIfItIsAnnotated()
        {
            // Unlike the runtime's StackTrace.ToString(), the outermost frame is hidden like the other frames.
            var stackTrace = new System.Diagnostics.StackTrace(new[] { GetVisibleFrameForTraceTest(), GetHiddenFrameForTraceTest() });
            Assert.AreEqual(1, CausalityTrace.GetVisibleFrames(stackTrace).Length);
            var text = CausalityTrace.FormatStackTrace(stackTrace);
            StringAssert.Contains(nameof(GetVisibleFrameForTraceTest), text);
            StringAssert.DoesNotContain(nameof(GetHiddenFrameForTraceTest), text);
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
        public void ChannelClose_PendingReadContinuationsRunInsideTheChannelScope()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var channel = Proto.Promises.Channels.Channel<int>.NewUnbounded();
                try
                {
                    var channelTrace = Internal.GetTrace(channel._ref);
                    CausalityTrace continuationTrace = null;
                    channel.Reader.ReadAsync(false)
                        .Then(_ => { continuationTrace = CausalityTrace.Capture(); })
                        .Forget();

                    channel.Writer.TryClose();

                    Assert.IsNotNull(channelTrace);
                    CollectionAssert.Contains(continuationTrace.PreviousTraces, channelTrace);
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

        // Asserts that the callback ran inside the scope of the operation that was started by the method (one of the ...ForTraceTest methods).
        private static void AssertRanInsideTheScopeStartedBy(CausalityTrace callbackTrace, string methodName)
        {
            Assert.IsNotNull(callbackTrace);
            Assert.Greater(callbackTrace.PreviousTraces.Count, 0);
            StringAssert.Contains(methodName, callbackTrace.PreviousTraces[0].StackTrace.ToString());
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void RegisterForTraceTest(CancelationToken cancelationToken, CausalityTrace[] callbackTrace)
            => cancelationToken.Register(() => callbackTrace[0] = CausalityTrace.Capture());

        [Test]
        public void CancelationCallback_RunsInsideTheScopeOfWhereItWasRegistered()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var cancelationSource = CancelationSource.New();
                var callbackTrace = new CausalityTrace[1];
                RegisterForTraceTest(cancelationSource.Token, callbackTrace);

                cancelationSource.Cancel();
                cancelationSource.Dispose();

                AssertRanInsideTheScopeStartedBy(callbackTrace[0], nameof(RegisterForTraceTest));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        // This is how other async method builders (e.g. async Task) await a promise.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void OnCompletedForTraceTest(Promise promise, bool configured, CausalityTrace[] continuationTrace)
        {
            if (configured)
            {
                var awaiter = promise.ConfigureAwait(ContinuationOptions.Foreground).GetAwaiter();
                awaiter.OnCompleted(() =>
                {
                    continuationTrace[0] = CausalityTrace.Capture();
                    awaiter.GetResult();
                });
            }
            else
            {
                var awaiter = promise.GetAwaiter();
                awaiter.OnCompleted(() =>
                {
                    continuationTrace[0] = CausalityTrace.Capture();
                    awaiter.GetResult();
                });
            }
        }

        [Test]
        public void AwaiterContinuation_RunsInsideTheScopeOfWhereItWasAwaited(
            [Values] bool configured)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                var continuationTrace = new CausalityTrace[1];
                OnCompletedForTraceTest(deferred.Promise, configured, continuationTrace);

                deferred.Resolve();
                TestHelper.ExecuteForegroundCallbacks();

                AssertRanInsideTheScopeStartedBy(continuationTrace[0], nameof(OnCompletedForTraceTest));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Progress NewProgressForTraceTest(CausalityTrace[] handlerTrace)
            => Progress.New(_ => handlerTrace[0] = CausalityTrace.Capture(), SynchronizationOption.Synchronous);

        [Test]
        public void ProgressHandler_RunsInsideTheScopeOfWhereTheProgressWasCreated()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var handlerTrace = new CausalityTrace[1];
                var progress = NewProgressForTraceTest(handlerTrace);

                progress.Token.Report(0.5d);
                progress.DisposeAsync()
                    .WaitWithTimeoutWhileExecutingForegroundContext(System.TimeSpan.FromSeconds(1));

                AssertRanInsideTheScopeStartedBy(handlerTrace[0], nameof(NewProgressForTraceTest));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise<int> GetLazyPromiseForTraceTest(AsyncLazy<int> lazy)
            => lazy.Promise;

        [Test]
        public void AsyncLazyFactory_RunsInsideTheScopeOfWhereTheResultWasFirstRequested(
            [Values] bool withProgress)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                CausalityTrace factoryTrace = null;
                var lazy = withProgress
                    ? new AsyncLazy<int>(_ =>
                    {
                        factoryTrace = CausalityTrace.Capture();
                        return Promise.Resolved(1);
                    })
                    : new AsyncLazy<int>(() =>
                    {
                        factoryTrace = CausalityTrace.Capture();
                        return Promise.Resolved(1);
                    });

                Assert.AreEqual(1, GetLazyPromiseForTraceTest(lazy)
                    .WaitWithTimeoutWhileExecutingForegroundContext(System.TimeSpan.FromSeconds(1)));

                AssertRanInsideTheScopeStartedBy(factoryTrace, nameof(GetLazyPromiseForTraceTest));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise ForEachForTraceTest(bool asyncEnumerable, List<CausalityTrace> bodyTraces)
        {
            // The body runs on the foreground context, so that it runs on the test thread.
            var options = new Proto.Promises.Threading.ParallelAsyncOptions() { SynchronizationContext = Promise.Config.ForegroundContext };
            System.Func<int, CancelationToken, Promise> body = (_, __) =>
            {
                bodyTraces.Add(CausalityTrace.Capture());
                return Promise.Resolved();
            };
            return asyncEnumerable
                ? Proto.Promises.Threading.ParallelAsync.ForEach(Proto.Promises.Linq.AsyncEnumerable.Range(0, 3), options, body)
                : Proto.Promises.Threading.ParallelAsync.ForEach(Enumerable.Range(0, 3), options, body);
        }

        [Test]
        public void ParallelAsyncBody_RunsInsideTheScopeOfWhereTheLoopWasStarted(
            [Values] bool asyncEnumerable)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var bodyTraces = new List<CausalityTrace>();
                ForEachForTraceTest(asyncEnumerable, bodyTraces)
                    .WaitWithTimeoutWhileExecutingForegroundContext(System.TimeSpan.FromSeconds(1));

                Assert.AreEqual(3, bodyTraces.Count);
                foreach (var bodyTrace in bodyTraces)
                {
                    AssertRanInsideTheScopeStartedBy(bodyTrace, nameof(ForEachForTraceTest));
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
        public void UnhandledRejection_SameExceptionAfterTracingWasDisabled_DoesNotHaveTheOldTrace()
        {
            var exception = new System.InvalidOperationException("Bang!");
            CausalityTrace.IsEnabled = true;
            try
            {
                var firstTrace = RejectAndReportLater(exception).CausalityTrace;
                Assert.IsNotNull(firstTrace);

                CausalityTrace.IsEnabled = false;
                Assert.IsNull(RejectAndReportLater(exception).CausalityTrace);

                CausalityTrace.IsEnabled = true;
                var secondTrace = RejectAndReportLater(exception).CausalityTrace;
                Assert.IsNotNull(secondTrace);
                Assert.AreNotSame(firstTrace, secondTrace);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
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
                // Each level's traces refer to the previous level twice (the callback and the deferred), so printing every path would grow exponentially.
                // Both are deeper than MaxDepth, so they're truncated the same way.
                var text200 = GetTraceFromRecursivelyScheduledContinuations(200).ToString();
                var trace400 = GetTraceFromRecursivelyScheduledContinuations(400);
                var text400 = trace400.ToString();
                StringAssert.IsMatch(@"--- \[#\d+ repeated \d+ times?: Thread \d+ ", text400);
                // The output doesn't grow with the number of iterations, other than the threads and times of the repeats (which are bounded by MaxDepth).
                Assert.Less(WithoutThreadsAndTimes(text400).Length, WithoutThreadsAndTimes(text200).Length + 100);
                AssertCausalityTraceIsPrintedCorrectly(trace400, text400);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static CausalityTrace GetTraceFromRecursivelyScheduledContinuations(int iterations)
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
            try
            {
                for (int i = 0; i < iterations; ++i)
                {
                    deferred.Resolve();
                }
            }
            finally
            {
                // Complete the last scheduled continuation without scheduling another.
                schedule = () => { };
                deferred.Resolve();
            }
            return callbackTrace;
        }

        [Test]
        public void CausalityTraceToString_AsyncLoop_RepeatsArePrintedOnce()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var text16 = GetTraceFromAsyncLoop(16).ToString();
                var trace64 = GetTraceFromAsyncLoop(64);
                var text64 = trace64.ToString();
                StringAssert.IsMatch(@"--- \[#\d+ repeated \d+ times?: Thread \d+ ", text64);
                // The output doesn't grow with the number of iterations, other than the threads and times of the repeats (which are bounded by MaxDepth).
                Assert.Less(WithoutThreadsAndTimes(text64).Length, WithoutThreadsAndTimes(text16).Length + 100);
                AssertCausalityTraceIsPrintedCorrectly(trace64, text64);
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
                AssertCausalityTraceIsPrintedCorrectly(trace[0], text);
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
                AssertCausalityTraceIsPrintedCorrectly(trace[0], text);
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
                AssertCausalityTraceIsPrintedCorrectly(traceX, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureFromHelper()
            => CausalityTrace.Capture();

        [Test]
        public void CausalityTraceCapture_NegativeSkipFrames_Throws()
        {
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => CausalityTrace.Capture(-1));
        }

        [Test]
        public void CausalityTrace_CapturesInTheSameTooDeepScope_ShareTheTruncatedTraces()
        {
            const int maxDepth = 4;
            int originalMaxDepth = CausalityTrace.MaxDepth;
            CausalityTrace.MaxDepth = maxDepth;
            CausalityTrace.IsEnabled = true;
            try
            {
                // Build a scope whose trace is at the max depth, so that every capture inside it is truncated.
                var trace = CausalityTrace.Capture();
                for (int i = 1; i < maxDepth; ++i)
                {
                    var scope = CausalityTrace.EnterScope(trace);
                    trace = CausalityTrace.Capture();
                    scope.Dispose();
                }
                var deepScope = CausalityTrace.EnterScope(trace);
                var trace1 = CausalityTrace.Capture();
                var trace2 = CausalityTrace.Capture();
                deepScope.Dispose();

                Assert.AreNotSame(trace, trace1.PreviousTraces[0], "The previous trace was expected to be truncated.");
                Assert.AreSame(trace1.PreviousTraces[0], trace2.PreviousTraces[0]);
            }
            finally
            {
                CausalityTrace.MaxDepth = originalMaxDepth;
                CausalityTrace.IsEnabled = false;
            }
        }

        // Collects the exceptions that are reported to the UncaughtRejectionHandler, such as scopes that were not disposed correctly.
        private sealed class ReportedExceptions : System.IDisposable
        {
            private readonly System.Action<UnhandledException> _previousHandler = Promise.Config.UncaughtRejectionHandler;
            private readonly List<System.Exception> _exceptions = new List<System.Exception>();
            private readonly List<CausalityTrace> _traces = new List<CausalityTrace>();

            internal ReportedExceptions()
            {
                Promise.Config.UncaughtRejectionHandler = ex =>
                {
#if PROTO_PROMISE_DEVELOPER_MODE
                    // Developer mode wraps the report in an exception that is traced where it was reported.
                    ex = (UnhandledException) ex.InnerException;
#endif
                    lock (_exceptions)
                    {
                        _exceptions.Add((System.Exception) ex.Value);
                        _traces.Add(ex.CausalityTrace);
                    }
                };
            }

            // The causality traces of every report of the exception.
            internal CausalityTrace[] GetTraces(System.Exception exception)
            {
                lock (_exceptions)
                {
                    return Enumerable.Range(0, _exceptions.Count).Where(i => _exceptions[i] == exception).Select(i => _traces[i]).ToArray();
                }
            }

            // The causality trace of the report of the exception.
            internal CausalityTrace GetTrace(System.Exception exception)
            {
                lock (_exceptions)
                {
                    return _traces[_exceptions.IndexOf(exception)];
                }
            }

            internal System.Exception[] Exceptions
            {
                get
                {
                    lock (_exceptions)
                    {
                        return _exceptions.ToArray();
                    }
                }
            }

            // Asserts that each reported exception is a misused scope with the message, in order.
            internal void AssertScopeMisuses(params string[] messages)
            {
                var exceptions = Exceptions;
                Assert.AreEqual(messages.Length, exceptions.Length, string.Join("\n", exceptions.Select(e => e.ToString())));
                for (int i = 0; i < messages.Length; i++)
                {
                    Assert.IsInstanceOf<System.InvalidOperationException>(exceptions[i]);
                    StringAssert.Contains("CausalityTrace.Scope", exceptions[i].Message);
                    StringAssert.Contains(messages[i], exceptions[i].Message);
                }
            }

            public void Dispose()
                => Promise.Config.UncaughtRejectionHandler = _previousHandler;
        }

        // Records what the callbacks that are posted to it throw, by running them immediately.
        private sealed class ThrowRecordingContext : System.Threading.SynchronizationContext, System.IDisposable
        {
            private readonly System.Threading.SynchronizationContext _previousContext = Promise.Config.ForegroundContext;
            private readonly List<System.Exception> _thrown = new List<System.Exception>();

            internal ThrowRecordingContext()
                => Promise.Config.ForegroundContext = this;

            public override void Post(System.Threading.SendOrPostCallback d, object state)
            {
                System.Exception thrown = null;
                try
                {
                    d(state);
                }
                catch (System.Exception e)
                {
                    thrown = e;
                }
                lock (_thrown)
                {
                    _thrown.Add(thrown);
                }
            }

            // Asserts that each posted callback threw the exception.
            internal void AssertThrown(System.Exception expected, int count)
            {
                System.Exception[] thrown;
                lock (_thrown)
                {
                    thrown = _thrown.ToArray();
                }
                Assert.AreEqual(count, thrown.Length);
                foreach (var e in thrown)
                {
                    Assert.IsNotNull(e);
                    TestHelper.AssertRejection(expected, e);
                }
            }

            public void Dispose()
                => Promise.Config.ForegroundContext = _previousContext;
        }

        [Test]
        public void UncaughtRejectionHandler_Throws_ItsExceptionIsThrownInTheContext()
        {
            var previousHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                using (var context = new ThrowRecordingContext())
                {
                    var handlerException = new System.InvalidOperationException("Handler");
                    Promise.Config.UncaughtRejectionHandler = _ => throw handlerException;

                    // The handler's exception doesn't escape into the code that reported the rejection.
                    Promise.Rejected(new System.Exception("Rejected")).Forget();
                    context.AssertThrown(handlerException, 1);
                }
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = previousHandler;
            }
        }

        // Returns the exception that disposing the scope throws, or null. A ref struct can't be used in a lambda, so we don't use Assert.Throws.
        // Copies of a scope are disposed the same way as the scope.
        private static System.Exception GetDisposeException(CausalityTrace.Scope scope)
        {
            try
            {
                scope.Dispose();
                return null;
            }
            catch (System.Exception e)
            {
                return e;
            }
        }

        // Disposing a scope that was already exited (by disposing it or its outer scope) is improper, so it throws.
        private static void AssertDisposeThrows(CausalityTrace.Scope scope, bool isAlreadyDisposed)
        {
            var exception = GetDisposeException(scope);
            Assert.IsInstanceOf<System.InvalidOperationException>(exception);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            // DEBUG mode knows whether the scope was already disposed.
            Assert.AreEqual(isAlreadyDisposed, exception is System.ObjectDisposedException, exception.ToString());
#endif
        }

        [Test]
        public void CausalityTraceScope_DisposedTwice_ThrowsAndDoesNotExitOtherScopes()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var outerTrace = CausalityTrace.Capture();
                    var innerTrace = CausalityTrace.Capture();
                    var outerScope = CausalityTrace.EnterScope(outerTrace);
                    var innerScope = CausalityTrace.EnterScope(innerTrace);
                    innerScope.Dispose();
                    AssertDisposeThrows(innerScope, true);

                    // A scope that was entered in the same place on the stack is not exited either.
                    var laterTrace = CausalityTrace.Capture();
                    var laterScope = CausalityTrace.EnterScope(laterTrace);
                    AssertDisposeThrows(innerScope, true);
                    CollectionAssert.AreEqual(new[] { laterTrace, outerTrace }, CausalityTrace.Capture().PreviousTraces);
                    laterScope.Dispose();

                    // The outer scope is still the current scope.
                    var trace = CausalityTrace.Capture();
                    outerScope.Dispose();
                    Assert.AreSame(outerTrace, trace.PreviousTraces[0]);
                    reports.AssertScopeMisuses();
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceScope_OuterDisposedBeforeInner_IsReportedAndExitsTheInnerScope_InnerDisposeThrows()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var outerTrace = CausalityTrace.Capture();
                    var innerTrace = CausalityTrace.Capture();
                    var outerScope = CausalityTrace.EnterScope(outerTrace);
                    var innerScope = CausalityTrace.EnterScope(innerTrace);
                    outerScope.Dispose();
                    reports.AssertScopeMisuses("before its nested");
                    Assert.AreEqual(0, CausalityTrace.Capture().PreviousTraces.Count);

                    // Disposing the inner scope later throws, and doesn't exit a scope that was entered after the outer scope was disposed.
                    var laterTrace = CausalityTrace.Capture();
                    var laterScope = CausalityTrace.EnterScope(laterTrace);
                    AssertDisposeThrows(innerScope, false);
                    var trace = CausalityTrace.Capture();
                    laterScope.Dispose();
                    CollectionAssert.AreEqual(new[] { laterTrace }, trace.PreviousTraces);
                    reports.AssertScopeMisuses("before its nested");
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !(PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE) || PROTO_PROMISE_TEST_GC_ENABLED
        // In DEBUG mode, the scope that is not disposed is also reported when it's garbage collected, so this requires the GC.
        [Test]
        public void CausalityTraceScope_NotDisposedInCallback_IsReportedAndTheCallbackCompletes()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var deferred = Promise.NewDeferred();
                    bool isContinued = false;
                    ThenWithScopeNotDisposedForTraceTest(deferred.Promise)
                        .Then(() => isContinued = true)
                        .Forget();
                    deferred.Resolve();

                    Assert.IsTrue(isContinued);
                    // The callback's scope also exited the scope that was not disposed.
                    Assert.AreEqual(0, CausalityTrace.Capture().PreviousTraces.Count);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // The scope that was not disposed is also reported when it's collected, which may happen at any time.
                    TestHelper.GcCollectAndWaitForFinalizers();
                    var exceptions = reports.Exceptions;
                    Assert.AreEqual(2, exceptions.Length, string.Join("\n", exceptions.Select(e => e.ToString())));
                    Assert.AreEqual(1, exceptions.OfType<UnreleasedObjectException>().Count());
                    Assert.AreEqual(1, exceptions.OfType<System.InvalidOperationException>().Count(e => e.Message.Contains("before its nested")));
#else
                    reports.AssertScopeMisuses("before its nested");
#endif
                    // The report's causality trace is where the callback that didn't dispose its scope was registered.
                    var reportTrace = reports.GetTrace(GetNestedScopesNotDisposedReport(reports));
                    Assert.IsNotNull(reportTrace);
                    StringAssert.Contains(nameof(ThenWithScopeNotDisposedForTraceTest), reportTrace.StackTrace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise ThenWithScopeNotDisposedForTraceTest(Promise promise)
            => promise.Then(() => { CausalityTrace.EnterScope(CausalityTrace.Capture()); });

        private static System.Exception GetNestedScopesNotDisposedReport(ReportedExceptions reports)
            => reports.Exceptions.Single(e => e is System.InvalidOperationException && e.Message.Contains("before its nested"));

        [Test]
        public void CausalityTraceScope_NotDisposedInCallback_HandlerThrows_TheCallbackCompletes()
        {
            var previousHandler = Promise.Config.UncaughtRejectionHandler;
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var context = new ThrowRecordingContext())
                {
                    var handlerException = new System.InvalidOperationException("Handler");
                    Promise.Config.UncaughtRejectionHandler = _ => throw handlerException;

                    var deferred = Promise.NewDeferred();
                    bool isContinued = false;
                    deferred.Promise
                        .Then(() => { CausalityTrace.EnterScope(CausalityTrace.Capture()); })
                        .Then(() => isContinued = true)
                        .Forget();
                    deferred.Resolve();

                    Assert.IsTrue(isContinued);
                    Assert.AreEqual(0, CausalityTrace.Capture().PreviousTraces.Count);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // The scope that was not disposed is also reported when it's collected, which may happen at any time.
                    TestHelper.GcCollectAndWaitForFinalizers();
                    context.AssertThrown(handlerException, 2);
#else
                    context.AssertThrown(handlerException, 1);
#endif
                }
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = previousHandler;
                CausalityTrace.IsEnabled = false;
            }
        }
#endif

#if PROTO_PROMISE_TEST_GC_ENABLED && (PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE)
        [Test]
        public void CausalityTraceScope_NotDisposed_IsReportedWithWhereItWasEntered([Values] bool isTraceNull)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            UnhandledException unhandledException = null;
            Promise.Config.UncaughtRejectionHandler = ex =>
            {
#if PROTO_PROMISE_DEVELOPER_MODE
                // Developer mode wraps the report in an exception that is traced where it was reported.
                ex = (UnhandledException) ex.InnerException;
#endif
                unhandledException = ex;
            };
            CausalityTrace.IsEnabled = true;
            try
            {
                // The scope is entered on another thread, so that it's not left on this thread's scopes.
                var thread = new Thread(() => EnterAndAbandonScope(isTraceNull));
                thread.Start();
                thread.Join();

                TestHelper.GcCollectAndWaitForFinalizers();
                Assert.IsNotNull(unhandledException);
                Assert.IsInstanceOf<UnreleasedObjectException>(unhandledException.Value);
                StringAssert.Contains("CausalityTrace.Scope", ((UnreleasedObjectException) unhandledException.Value).Message);
                StringAssert.Contains(nameof(EnterAndAbandonScope), unhandledException.StackTrace);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void EnterAndAbandonScope(bool isTraceNull)
            => CausalityTrace.EnterScope(isTraceNull ? null : CausalityTrace.Capture());
#endif

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        [Test]
        public void CausalityTraceScope_NullTrace_IsCheckedForBeingDisposedCorrectly()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var outerScope = CausalityTrace.EnterScope(CausalityTrace.Capture());
                    var nullScope = CausalityTrace.EnterScope(null);
                    outerScope.Dispose();
                    Assert.AreEqual(0, CausalityTrace.Capture().PreviousTraces.Count);
                    AssertDisposeThrows(nullScope, false);
                    AssertDisposeThrows(nullScope, true);
                    reports.AssertScopeMisuses("before its nested");
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceScope_NullTraceDisposedBeforeInner_IsReportedAndExitsTheInnerScope_InnerDisposeThrows()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var nullScope = CausalityTrace.EnterScope(null);
                    var innerScope = CausalityTrace.EnterScope(CausalityTrace.Capture());
                    nullScope.Dispose();
                    reports.AssertScopeMisuses("before its nested");
                    Assert.AreEqual(0, CausalityTrace.Capture().PreviousTraces.Count);
                    AssertDisposeThrows(innerScope, false);
                    reports.AssertScopeMisuses("before its nested");
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }
#endif

        [Test]
        public void CausalityTraceScope_EnteredWhileDisabled_OuterDisposedBeforeInner_IsReportedAndExitsTheInnerScope_InnerDisposeThrows()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var outerTrace = CausalityTrace.Capture();
                    var innerTrace = CausalityTrace.Capture();
                    var outerScope = CausalityTrace.EnterScope(outerTrace);
                    // Scopes that are entered while causality traces are disabled must also be disposed in order.
                    CausalityTrace.IsEnabled = false;
                    var innerScope = CausalityTrace.EnterScope(innerTrace);
                    CausalityTrace.IsEnabled = true;
                    outerScope.Dispose();
                    Assert.AreEqual(0, CausalityTrace.Capture().PreviousTraces.Count);
                    AssertDisposeThrows(innerScope, false);
                    reports.AssertScopeMisuses("before its nested");
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceScope_OuterDisposedBeforeInner_ExitsTheInnerScopeBeforeItIsReported()
        {
            var previousHandler = Promise.Config.UncaughtRejectionHandler;
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var context = new ThrowRecordingContext())
                {
                    // The handler runs with the restored traces, and if it throws, its exception is thrown in the context instead of from Dispose.
                    var handlerException = new System.InvalidOperationException("Handler");
                    CausalityTrace handlerTrace = null;
                    Promise.Config.UncaughtRejectionHandler = _ =>
                    {
                        handlerTrace = CausalityTrace.Capture();
                        throw handlerException;
                    };
                    var outerScope = CausalityTrace.EnterScope(CausalityTrace.Capture());
                    var innerScope = CausalityTrace.EnterScope(CausalityTrace.Capture());

                    Assert.IsNull(GetDisposeException(outerScope));
                    Assert.AreEqual(0, handlerTrace.PreviousTraces.Count);
                    Assert.AreEqual(0, CausalityTrace.Capture().PreviousTraces.Count);
                    context.AssertThrown(handlerException, 1);
                    AssertDisposeThrows(innerScope, false);
                }
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = previousHandler;
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceEnterScope_NullTrace_DoesNothing()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var outerTrace = CausalityTrace.Capture();
                var outerScope = CausalityTrace.EnterScope(outerTrace);
                var nullScope = CausalityTrace.EnterScope(null);
                var trace = CausalityTrace.Capture();
                nullScope.Dispose();
                outerScope.Dispose();
                Assert.AreEqual(1, trace.PreviousTraces.Count);
                Assert.AreSame(outerTrace, trace.PreviousTraces[0]);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_OuterScopeWithTheSameStackTraceAsAnOuterScopeInItsHistory_PrintsEveryStackTrace()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // O and E have the same stack trace, and O is in E's history through T. O must not be printed as E,
                // because E's history only reaches O's cause through O.
                var root = CaptureTraceGraph(
                    (CaptureAtUnique, new int[0]),      // 0
                    (CaptureAtLoop, new[] { 0 }),       // 1: O
                    (CaptureAtLeaf, new int[0]),        // 2
                    (CaptureAtStep, new[] { 2, 1 }),    // 3: T, in O's scope
                    (CaptureAtLoop, new[] { 3 }),       // 4: E
                    (CaptureAtOther, new[] { 3 }),      // 5
                    (CaptureAtRoot, new[] { 5, 4 }));   // 6, in E's scope

                var text = root.ToString();
                StringAssert.Contains(nameof(CaptureAtUnique), text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_RepeatsWhoseOuterScopesAreOnlyInTheirOwnHistories_PrintsEveryStackTrace()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each loop iteration runs in the scope of the previous iteration, and the newest iteration's cause was registered in an earlier iteration.
                var root = CaptureTraceGraph(
                    (CaptureAtLeaf, new int[0]),        // 0
                    (CaptureAtUnique, new int[0]),      // 1
                    (CaptureAtLoop, new[] { 1, 0 }),    // 2
                    (CaptureAtOther, new int[0]),       // 3
                    (CaptureAtLoop, new[] { 3, 2 }),    // 4
                    (CaptureAtStep, new[] { 4 }),       // 5
                    (CaptureAtLoop, new[] { 5, 4 }));   // 6

                var text = root.ToString();
                StringAssert.Contains(nameof(CaptureAtUnique), text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_TruncatedTraceWithTheSameStackTraceAsAnotherOuterScope_PrintsTheTruncation()
        {
            int originalMaxDepth = CausalityTrace.MaxDepth;
            CausalityTrace.IsEnabled = true;
            CausalityTrace.MaxDepth = 4;
            try
            {
                // The root's history is deeper than MaxDepth, so its previous traces are truncated to 1 level.
                // 4 is copied without its history, so its copy has the same stack trace as 0, but it must not be printed as 0.
                var root = CaptureTraceGraph(
                    (CaptureAtLeaf, new int[0]),        // 0
                    (CaptureAtOther, new int[0]),       // 1
                    (CaptureAtOther, new[] { 1 }),      // 2
                    (CaptureAtOther, new[] { 2 }),      // 3
                    (CaptureAtLeaf, new[] { 3 }),       // 4
                    (CaptureAtRoot, new[] { 0, 0, 4 })); // 5

                var text = root.ToString();
                StringAssert.Contains("Older traces truncated", text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.MaxDepth = originalMaxDepth;
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_OuterScopeWhoseHistoryIsOnlyMissingTheTruncation_PrintsTheTruncation()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // 6 has the same stack trace as the outer scope 1, and 1's history has every stack trace in 6's history,
                // but it only reaches 6's truncation through 6 itself, so printing 6 as 1 would hide the truncation.
                // The traces are copies of two traces, so that they have the same two stack traces, and the truncation can be set directly
                // (it can only be reproduced with a much larger history).
                var traceS0 = CaptureAtLeaf();
                var traceS1 = CaptureAtOther();
                var trace6 = CopyTrace(traceS0, false, CopyTrace(traceS1, true), CopyTrace(traceS1, true));
                var trace3 = CopyTrace(traceS0, false, CopyTrace(traceS1, false), trace6, trace6, trace6);
                var trace1 = CopyTrace(traceS0, false, trace3, CopyTrace(traceS0, false));
                var root = CopyTrace(traceS1, false, trace1, trace1, CopyTrace(traceS0, false));

                var text = root.ToString();
                StringAssert.Contains("Older traces truncated", text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't skip frames.
        [Test]
        public void CausalityTraceToString_TracesWithoutVisibleFrames_ArePrintedAsHidden()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // A trace without frames in the chain, as an outer scope, and as the root.
                var root = CaptureTraceGraph(
                    (CaptureAtLeaf, new int[0]),            // 0
                    (CaptureWithoutFrames, new[] { 0 }),    // 1
                    (CaptureWithoutFrames, new int[0]),     // 2
                    (CaptureWithoutFrames, new[] { 1, 2 })); // 3

                var text = root.ToString();
                Assert.AreEqual(3, CountOccurrences(text, "--- [All frames are hidden] ---"), text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        // Skips every frame, like a trace whose frames are all hidden.
        private static CausalityTrace CaptureWithoutFrames() => new CausalityTrace(1000);
#endif

        [Test]
        public void CausalityTrace_HasNoStaticConstructor()
        {
            // A static constructor would make IL2CPP check that the class is initialized every time IsEnabled is read on hot paths.
            Assert.IsNull(typeof(CausalityTrace).TypeInitializer);
        }

        [Test]
        public void PromiseConfig_HasNoStaticConstructor()
        {
            // A static constructor would make IL2CPP check that the class is initialized every time a setting is read on hot paths.
            Assert.IsNull(typeof(Promise.Config).TypeInitializer);
        }

        [Test]
        public void PromiseConfigDefaultTimerFactory_CanBeSetAndRestored()
        {
            var original = Promise.Config.DefaultTimerFactory;
#if !UNITY_WEBGL // The WebGL player replaces the default with a Unity timer factory.
            Assert.AreSame(Proto.Timers.TimerFactory.System, original);
#endif
            var timerFactory = new FakeTimerFactoryForConfigTest();
            Promise.Config.DefaultTimerFactory = timerFactory;
            try
            {
                Assert.AreSame(timerFactory, Promise.Config.DefaultTimerFactory);
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => Promise.Config.DefaultTimerFactory = null);
                Assert.AreSame(timerFactory, Promise.Config.DefaultTimerFactory);
            }
            finally
            {
                Promise.Config.DefaultTimerFactory = original;
            }
            Assert.AreSame(original, Promise.Config.DefaultTimerFactory);
        }

        private sealed class FakeTimerFactoryForConfigTest : Proto.Timers.TimerFactory
        {
            public override Proto.Timers.Timer CreateTimer(System.Threading.TimerCallback callback, object state, System.TimeSpan dueTime, System.TimeSpan period)
                => throw new System.NotImplementedException();
        }

        [Test]
        public void CausalityTraceMaxDepth_CanBeSetAndRestored()
        {
            int defaultMaxDepth = CausalityTrace.MaxDepth;
            // The default, unless another test leaked a change.
            Assert.AreEqual(128, defaultMaxDepth);
            try
            {
                CausalityTrace.MaxDepth = 1;
                Assert.AreEqual(1, CausalityTrace.MaxDepth);
                CausalityTrace.MaxDepth = int.MaxValue;
                Assert.AreEqual(int.MaxValue, CausalityTrace.MaxDepth);
            }
            finally
            {
                CausalityTrace.MaxDepth = defaultMaxDepth;
            }
            Assert.AreEqual(defaultMaxDepth, CausalityTrace.MaxDepth);
        }

        [Test]
        public void CausalityTraceCapture_RecordsTheThreadAndTime()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var thread = System.Threading.Thread.CurrentThread;
                long before = System.TimeProvider.System.GetTimestamp();
                var trace = CausalityTrace.Capture();
                long after = System.TimeProvider.System.GetTimestamp();

                Assert.AreEqual(thread.ManagedThreadId, trace.ThreadId);
                Assert.AreEqual(thread.Name, trace.ThreadName);
                Assert.AreEqual(thread.IsThreadPoolThread, trace.IsThreadPoolThread);
                Assert.AreEqual(thread.IsBackground, trace.IsBackgroundThread);
                Assert.That(trace.Timestamp, Is.InRange(before, after));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !UNITY_WEBGL
        [Test]
        public void CausalityTraceToString_PrintsTheThreadAndTimeOfEachTrace()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Traces from the same frames are not next to each other, so that they're not folded as repeats.
                var foreground = CaptureOnNewThread(false, null);
                var named = CaptureOnThreadHelper(foreground);
                var background = CaptureOnNewThread(true, named);
                var pooled = CaptureOnThreadPool(background);
                var root = CaptureInScopeForTraceTest(pooled);
                var text = root.ToString();

                Assert.IsNull(foreground.ThreadName);
                Assert.IsFalse(foreground.IsBackgroundThread);
                Assert.IsNotNull(named.ThreadName);
                Assert.IsNull(background.ThreadName);
                Assert.IsTrue(background.IsBackgroundThread);
                Assert.IsTrue(pooled.IsThreadPoolThread);
                Assert.That(named.Timestamp, Is.InRange(foreground.Timestamp, background.Timestamp));
                Assert.That(pooled.Timestamp, Is.InRange(background.Timestamp, root.Timestamp));

                // The printed trace has no time, and the earlier traces show how long before it they were captured.
                var firstLine = text.Split('\n')[0].TrimEnd('\r');
                StringAssert.StartsWith($"   --- [Thread {root.ThreadId} ", firstLine);
                StringAssert.DoesNotContain("earlier", firstLine);
                const string time = @", \d+\.\d{3} ms earlier\] ---";
                StringAssert.IsMatch($@"--- \[Thread {foreground.ThreadId} \(foreground\){time}", text);
                StringAssert.IsMatch($@"--- \[{ThreadPattern(named)}{time}", text);
                StringAssert.IsMatch($@"--- \[Thread {background.ThreadId} \(background\){time}", text);
                // Newer runtimes name thread pool threads.
                var poolThread = pooled.ThreadName == null ? $@"Thread {pooled.ThreadId} \(thread pool\)" : ThreadPattern(pooled);
                StringAssert.IsMatch($@"--- \[{poolThread}{time}", text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_RepeatsOnOtherThreads_AreFoldedWithTheirThreadsAndTimes()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The same code ran on thread A twice, then on thread B, then on thread A twice more, each time inside the scope of the previous time,
                // like a loop that moved between threads. They're all in a scope, so that the first can be folded with the others.
                var last = TurnCapturer.Capture(CaptureAtOther(), 2, 0, 0, 1, 0, 0);
                var root = CaptureInScopeForTraceTest(last);
                var text = root.ToString();

                // The last repeat is printed, and the others are folded into it, with their threads and times from the latest to the earliest
                // (consecutive repeats on the same thread are grouped).
                var threadA = ThreadPattern(last);
                var threadB = ThreadPattern(last.PreviousTraces[0].PreviousTraces[0]);
                StringAssert.IsMatch($@"--- \[{threadA}, \d+\.\d{{3}} ms earlier\] ---", text);
                const string time = @"(\d+\.\d{3})";
                var match = System.Text.RegularExpressions.Regex.Match(text, $@"--- \[#\d+ repeated 4 times: {threadA} at {time} ms earlier; "
                    + $@"{threadB} at {time} ms earlier; {threadA} at {time}, {time} ms earlier\] ---");
                Assert.IsTrue(match.Success, text);
                var times = Enumerable.Range(1, 4).Select(i => double.Parse(match.Groups[i].Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                Assert.That(times, Is.Ordered, text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static string ThreadPattern(CausalityTrace namedThreadTrace)
            => $@"Thread {namedThreadTrace.ThreadId} ""{System.Text.RegularExpressions.Regex.Escape(namedThreadTrace.ThreadName)}""";
#endif // !UNITY_WEBGL

        [Test]
        public void CausalityTraceToString_RepeatsReferredToAgain_DoNotRepeatTheirThreadsAndTimes()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The main chain prints the last repeat and refers to the others, then the outer scope's history starts with one of the others,
                // so it refers to the rest of the repeats again. The repeats are all in a scope, so that the first can be folded with the others.
                var repeats = CaptureAtOther();
                for (int i = 0; i < 4; ++i)
                {
                    repeats = CaptureInScopeForTraceTest(repeats);
                }
                var main = CopyTrace(CaptureAtStep(), false, repeats);
                var outerScope = CopyTrace(CaptureAtLoop(), false, repeats.PreviousTraces[0]);
                var root = CopyTrace(CaptureAtRoot(), false, main, outerScope);
                var text = root.ToString();

                // Only the first reference lists the threads and times.
                var references = System.Text.RegularExpressions.Regex.Matches(text, @"--- \[#\d+ repeated 3 times(:[^\]]*)?\] ---");
                Assert.AreEqual(2, references.Count, text);
                Assert.IsTrue(references[0].Groups[1].Success, text);
                Assert.IsFalse(references[1].Groups[1].Success, text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_OuterScopeReferredToAgain_PrintsItsThreadAndTimeEachTime()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // 2 and 3 have the same stack trace, so 3 is printed as 2. Both 4 and 5 refer to 3, and each reference must show 3's own time.
                System.Func<CausalityTrace> captureLater = () =>
                {
                    long timestamp = System.TimeProvider.System.GetTimestamp();
                    while (System.TimeProvider.System.GetTimestamp() == timestamp) { }
                    return CaptureAtLeaf();
                };
                var root = CaptureTraceGraph(
                    (CaptureAtOther, new int[0]),           // 0
                    (CaptureAtUnique, new int[0]),          // 1
                    (captureLater, new[] { 0 }),            // 2
                    (captureLater, new[] { 0 }),            // 3
                    (CaptureAtStep, new[] { 1, 3 }),        // 4
                    (CaptureAtLoop, new[] { 4, 3 }),        // 5
                    (CaptureAtRoot, new[] { 5, 2 }));       // 6
                var text = root.ToString();

                var outerScopeReferences = System.Text.RegularExpressions.Regex.Matches(text, @"--- \[Outer causality scope [A-Z/]+, see (?:above|below)(; [^\]]*)?\] ---");
                Assert.AreEqual(3, outerScopeReferences.Count, text);
                // 2 is referred to without a time, and both references to 3 have the same time.
                var detailed = outerScopeReferences.Cast<System.Text.RegularExpressions.Match>().Where(m => m.Groups[1].Success).Select(m => m.Groups[1].Value).ToArray();
                Assert.AreEqual(2, detailed.Length, text);
                Assert.AreEqual(detailed[0], detailed[1], text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_SameOuterScopeEnteredTwice_ListsItsTimeOnce()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                System.Func<CausalityTrace> captureLater = () =>
                {
                    long timestamp = System.TimeProvider.System.GetTimestamp();
                    while (System.TimeProvider.System.GetTimestamp() == timestamp) { }
                    return CaptureAtLeaf();
                };
                var root = CaptureTraceGraph(
                    (CaptureAtOther, new int[0]),           // 0
                    (captureLater, new[] { 0 }),            // 1
                    (captureLater, new[] { 0 }),            // 2: printed as 1
                    (CaptureAtOther, new[] { 0, 1, 2, 2 })); // 3: 2 was entered twice
                var text = root.ToString();

                StringAssert.IsMatch(@"--- \[Outer causality scope A, see below; Thread \d+ .* at \d+\.\d{3} ms earlier\] ---", text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_OuterScopesPrintedAsAnother_PrintTheirThreadsAndTimes()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The traces are captured at different times, otherwise they would be copies of the same capture (which have no time of their own).
                System.Func<CausalityTrace> captureLater = () =>
                {
                    long timestamp = System.TimeProvider.System.GetTimestamp();
                    while (System.TimeProvider.System.GetTimestamp() == timestamp) { }
                    return CaptureAtLeaf();
                };
                var root = CaptureTraceGraph(
                    (CaptureAtOther, new int[0]),           // 0
                    (captureLater, new[] { 0 }),            // 1
                    (captureLater, new[] { 0 }),            // 2: the same stack trace and history as 1, so it's printed as 1
                    (CaptureAtOther, new[] { 0, 1, 2 }));   // 3
                var text = root.ToString();

                var outerScope = root.PreviousTraces[2];
                StringAssert.IsMatch($@"--- \[Outer causality scope A, see below; Thread {outerScope.ThreadId} .* at \d+\.\d{{3}} ms earlier\] ---", text);
                Assert.AreEqual(1, CountOccurrences(text, "Outer causality scope A, see below"), text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        // Captures a trace inside the scope of the previous trace, from the same frames every time.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureInScopeForTraceTest(CausalityTrace previous)
        {
            var scope = CausalityTrace.EnterScope(previous);
            try
            {
                return CausalityTrace.Capture();
            }
            finally
            {
                scope.Dispose();
            }
        }

#if !UNITY_WEBGL
        // Captures a trace on a new unnamed thread (the threads of ThreadHelper have names).
        private static CausalityTrace CaptureOnNewThread(bool isBackground, CausalityTrace previous)
        {
            CausalityTrace trace = null;
            System.Runtime.ExceptionServices.ExceptionDispatchInfo exception = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    trace = CaptureInScopeForTraceTest(previous);
                }
                catch (System.Exception e)
                {
                    exception = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e);
                }
            });
            thread.IsBackground = isBackground;
            thread.Start();
            thread.Join();
            exception?.Throw();
            return trace;
        }

        private static CausalityTrace CaptureOnThreadHelper(CausalityTrace previous)
        {
            CausalityTrace trace = null;
            new ThreadHelper().ExecuteSingleAction(() => trace = CaptureInScopeForTraceTest(previous));
            return trace;
        }

        private static CausalityTrace CaptureOnThreadPool(CausalityTrace previous)
        {
            CausalityTrace trace = null;
            System.Runtime.ExceptionServices.ExceptionDispatchInfo exception = null;
            using (var captured = new System.Threading.ManualResetEventSlim())
            {
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        trace = CaptureInScopeForTraceTest(previous);
                    }
                    catch (System.Exception e)
                    {
                        exception = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e);
                    }
                    finally
                    {
                        captured.Set();
                    }
                });
                captured.Wait();
            }
            exception?.Throw();
            return trace;
        }

        // Captures traces on the threads of ThreadHelper in turns, each inside the scope of the previous one, with the same frames on every thread.
        private sealed class TurnCapturer
        {
            private readonly System.Threading.SemaphoreSlim[] _turns;
            private readonly System.Threading.SemaphoreSlim _captured = new System.Threading.SemaphoreSlim(0);
            private CausalityTrace _previous;
            private volatile bool _isDone;

            private TurnCapturer(int threadCount)
            {
                _turns = new System.Threading.SemaphoreSlim[threadCount];
                for (int i = 0; i < threadCount; ++i)
                {
                    _turns[i] = new System.Threading.SemaphoreSlim(0);
                }
            }

            // Captures a trace on the thread of each turn (an index of the threads), and returns the last trace.
            internal static CausalityTrace Capture(CausalityTrace previous, int threadCount, params int[] turns)
            {
                var capturer = new TurnCapturer(threadCount) { _previous = previous };
                var threadHelper = new ThreadHelper();
                for (int i = 0; i < threadCount; ++i)
                {
                    int index = i;
                    threadHelper.AddParallelAction(() => capturer.Run(index));
                }
                // The turns are given from another thread, because this thread waits for all of the actions.
                threadHelper.AddParallelAction(() => capturer.GiveTurns(turns));
                threadHelper.ExecutePendingParallelActions();
                return capturer._previous;
            }

            private void GiveTurns(int[] turns)
            {
                try
                {
                    foreach (int turn in turns)
                    {
                        _turns[turn].Release();
                        _captured.Wait();
                        if (_isDone)
                        {
                            // A capture failed.
                            break;
                        }
                    }
                }
                finally
                {
                    _isDone = true;
                    foreach (var turn in _turns)
                    {
                        turn.Release();
                    }
                }
            }

            private void Run(int threadIndex)
            {
                var turn = _turns[threadIndex];
                while (true)
                {
                    turn.Wait();
                    if (_isDone)
                    {
                        return;
                    }
                    try
                    {
                        _previous = CaptureInScopeForTraceTest(_previous);
                    }
                    catch
                    {
                        _isDone = true;
                        throw;
                    }
                    finally
                    {
                        _captured.Release();
                    }
                }
            }
        }
#endif // !UNITY_WEBGL

        [Test]
        public void CausalityTraceCapture_InContinuationOnAnotherContext_IncludesWhereTheContinuationWasScheduled()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                CaptureAfterAwaitOnContextForTraceTest(deferred.Promise, context, captured => trace = captured).Forget();
                ResolveForTraceTest(deferred);
                Assert.IsNull(trace);
                context.Execute();

                // The code that resolved the deferred isn't on the stack when the continuation runs on the context, but the trace includes it.
                Assert.IsFalse(trace.GetFrames().Any(frame => frame.GetMethod().Name == nameof(ResolveForTraceTest)), trace.ToString());
                Assert.IsTrue(HistoryContainsMethod(trace, nameof(ResolveForTraceTest)), trace.ToString());
                // The async function is still the main cause.
                Assert.IsTrue(trace.PreviousTraces[0].GetFrames().Any(frame => frame.GetMethod().Name == nameof(CausalityTraceCapture_InContinuationOnAnotherContext_IncludesWhereTheContinuationWasScheduled)), trace.ToString());
                AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_AfterSwitchingToAnotherContext_IncludesWhereTheSwitchWasScheduled()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                CaptureAfterSwitchToContextForTraceTest(deferred.Promise, context, captured => trace = captured).Forget();
                // The async function continues in ResolveForTraceTest, then switches to the context.
                ResolveForTraceTest(deferred);
                Assert.IsNull(trace);
                context.Execute();

                Assert.IsFalse(trace.GetFrames().Any(frame => frame.GetMethod().Name == nameof(ResolveForTraceTest)), trace.ToString());
                Assert.IsTrue(HistoryContainsMethod(trace, nameof(ResolveForTraceTest)), trace.ToString());
                AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_ExceptionThrownInCallback_TraceIncludesWhatInvokedTheCallback()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Thrown in callback");
                    var deferred = Promise.NewDeferred();
                    deferred.Promise
                        .Then(() => throw exception)
                        .Forget();
                    ResolveForTraceTest(deferred);

                    var trace = reports.GetTrace(exception);
                    // Where the exception was caught, which is inside the call that resolved the deferred.
                    Assert.IsTrue(trace.GetFrames().Any(frame => frame.GetMethod().Name == nameof(ResolveForTraceTest)), trace.ToString());
                    // The callback's operation is still the main cause.
                    Assert.IsTrue(trace.PreviousTraces[0].GetFrames().Any(frame => frame.GetMethod().Name == nameof(UnhandledRejection_ExceptionThrownInCallback_TraceIncludesWhatInvokedTheCallback)), trace.ToString());
                    AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_ExceptionThrownInAsyncFunction_TraceIncludesWhatContinuedTheFunction()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Thrown in async function");
                    var deferred = Promise.NewDeferred();
                    ThrowAfterAwaitForTraceTest(deferred.Promise, exception).Forget();
                    ResolveForTraceTest(deferred);

                    var trace = reports.GetTrace(exception);
                    Assert.IsTrue(trace.GetFrames().Any(frame => frame.GetMethod().Name == nameof(ResolveForTraceTest)), trace.ToString());
                    // Where the async function was called is still the main cause.
                    Assert.IsTrue(trace.PreviousTraces[0].GetFrames().Any(frame => frame.GetMethod().Name == nameof(UnhandledRejection_ExceptionThrownInAsyncFunction_TraceIncludesWhatContinuedTheFunction)), trace.ToString());
                    AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_ExceptionThrownInCatchContinueWithOrFinally_TraceIncludesWhatInvokedTheCallback(
            [Values("Catch", "ContinueWith", "Finally")] string callbackType)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Thrown in " + callbackType);
                    var deferred = Promise.NewDeferred();
                    Promise promise = callbackType == "Catch" ? deferred.Promise.Then(() => throw new System.Exception("Caught")).Catch(() => throw exception)
                        : callbackType == "ContinueWith" ? deferred.Promise.ContinueWith(_ => throw exception)
                        : deferred.Promise.Finally(() => throw exception);
                    promise.Forget();
                    ResolveForTraceTest(deferred);

                    var trace = reports.GetTrace(exception);
                    Assert.IsTrue(trace.GetFrames().Any(frame => frame.GetMethod().Name == nameof(ResolveForTraceTest)), trace.ToString());
                    Assert.IsTrue(trace.PreviousTraces[0].GetFrames().Any(frame => frame.GetMethod().Name == nameof(UnhandledRejection_ExceptionThrownInCatchContinueWithOrFinally_TraceIncludesWhatInvokedTheCallback)), trace.ToString());
                    AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_ExceptionThrownInPromiseNewOnAContext_TraceIncludesWhatRanTheCallback()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var context = new QueuedContext();
                    var exception = new System.InvalidOperationException("Thrown in Promise.New");
                    Promise.New(_ => throw exception, context, forceAsync: true).Forget();
                    ExecuteForTraceTest(context);

                    var trace = reports.GetTrace(exception);
                    Assert.IsTrue(trace.GetFrames().Any(frame => frame.GetMethod().Name == nameof(ExecuteForTraceTest)), trace.ToString());
                    Assert.IsTrue(trace.PreviousTraces[0].GetFrames().Any(frame => frame.GetMethod().Name == nameof(UnhandledRejection_ExceptionThrownInPromiseNewOnAContext_TraceIncludesWhatRanTheCallback)), trace.ToString());
                    AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_ExceptionThrownInPromiseNewImmediately_TraceIsThePromisesTrace()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Thrown in Promise.New");
                    Promise.New(_ => throw exception).Forget();

                    // The callback ran inside Promise.New, so a trace captured where the exception was caught would print the same stack again.
                    var trace = reports.GetTrace(exception);
                    Assert.AreEqual(0, trace.PreviousTraces.Count, trace.ToString());
                    AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledException_ThrownInProgressHandlerOnAContext_TraceIncludesWhatReportedTheProgress()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var context = new QueuedContext();
                    var exception = new System.InvalidOperationException("Thrown in progress handler");
                    var progress = Progress.New(_ => throw exception, context);
                    ReportProgressForTraceTest(progress.Token);
                    context.Execute();
                    progress.DisposeAsync().Forget();
                    context.Execute();

                    var trace = reports.GetTrace(exception);
                    Assert.IsTrue(HistoryContainsMethod(trace, nameof(ReportProgressForTraceTest)), trace.ToString());
                    AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_PromiseRejected_TraceIsWhereItWasCreated(
            [Values("Rejected", "ThenOnResolved")] string rejectionType)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException(rejectionType);
                    var promise = rejectionType == "Rejected"
                        ? Promise.Rejected(exception)
                        // The callback runs synchronously inside Then, which rejects the promise it returns with Promise.Rejected.
                        : Promise.Resolved().Then(() => throw exception);
                    promise.Forget();

                    // The trace isn't captured again where the promise was rejected, which would print the same stack twice.
                    var trace = reports.GetTrace(exception);
                    Assert.AreEqual(0, trace.PreviousTraces.Count, trace.ToString());
                    Assert.IsTrue(trace.GetFrames().Any(frame => frame.GetMethod().Name == nameof(UnhandledRejection_PromiseRejected_TraceIsWhereItWasCreated)), trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledException_RethrownInProgressHandler_KeepsItsOriginalTrace()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Rethrown");
                    Promise.Rejected(exception).Forget();
                    var originalTrace = reports.GetTrace(exception);

                    var context = new QueuedContext();
                    var progress = Progress.New(_ => throw exception, context);
                    ReportProgressForTraceTest(progress.Token);
                    context.Execute();
                    progress.DisposeAsync().Forget();
                    context.Execute();

                    var traces = reports.GetTraces(exception);
                    Assert.AreEqual(2, traces.Length);
                    Assert.AreSame(originalTrace, traces[1]);
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_InCallbackThatAdoptedACompletedConfiguredPromise_IncludesWhatRanTheCallback()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                deferred.Promise
                    // The callback returns a configured promise that is already complete, so the callback promise adopts it while the callback runs.
                    .Then(() => Promise.Resolved().ConfigureContinuation(new ContinuationOptions(context, CompletedContinuationBehavior.Asynchronous)))
                    .Then(() => { trace = CausalityTrace.Capture(); })
                    .Forget();
                ResolveForTraceTest(deferred);
                context.Execute();

                Assert.IsTrue(HistoryContainsMethod(trace, nameof(ResolveForTraceTest)), trace.ToString());
                AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_AfterAwaitingACompletedConfiguredPromiseAsValueTask_IncludesWhatRanTheAwait()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                // The value task's source has no trace of its own, so the await relies on the trace captured where it was scheduled.
                deferred.Promise
                    .Then(() => { _ = CaptureAfterAwaitingValueTaskForTraceTest(Promise.Resolved().ConfigureContinuation(new ContinuationOptions(context, CompletedContinuationBehavior.Asynchronous)), captured => trace = captured); })
                    .Forget();
                ResolveForTraceTest(deferred);
                context.Execute();

                Assert.IsTrue(HistoryContainsMethod(trace, nameof(ResolveForTraceTest)), trace.ToString());
                AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async System.Threading.Tasks.Task CaptureAfterAwaitingValueTaskForTraceTest(Promise promise, System.Action<CausalityTrace> onCaptured)
        {
            // ConfigureAwait(false), so that the continuation runs where the value task is completed instead of on the test's context.
            await promise.AsValueTask().ConfigureAwait(false);
            onCaptured(CausalityTrace.Capture());
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ExecuteForTraceTest(QueuedContext context)
            => context.Execute();

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ReportProgressForTraceTest(ProgressToken token)
            => token.Report(0.5);

        private static async Promise ThrowAfterAwaitForTraceTest(Promise promise, System.Exception exception)
        {
            await promise;
            throw exception;
        }

        [Test]
        public void CausalityTraceCapture_InPromiseRun_DoesNotIncludeWhereItWasScheduledTwice()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                CausalityTrace trace = null;
                Promise.Run(() => { trace = CausalityTrace.Capture(); }, context).Forget();
                context.Execute();

                // The run promise's trace is the only cause; scheduling the callback when the promise was created doesn't add another.
                Assert.AreEqual(1, trace.PreviousTraces.Count, trace.ToString());
                AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_InCallbackOfCompletedConfiguredPromise_DoesNotIncludeWhereItWasScheduledTwice()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                var configured = deferred.Promise.ConfigureContinuation(new ContinuationOptions(context, CompletedContinuationBehavior.Asynchronous));
                deferred.Resolve();
                CausalityTrace trace = null;
                // The configured promise is already complete, so the callback is scheduled on the context when Then is called.
                configured
                    .Then(() => { trace = CausalityTrace.Capture(); })
                    .Forget();
                context.Execute();

                // The callback promise's trace is the only cause; scheduling the callback when it was hooked up doesn't add another.
                Assert.AreEqual(1, trace.PreviousTraces.Count, trace.ToString());
                AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async Promise CaptureAfterSwitchToContextForTraceTest(Promise promise, System.Threading.SynchronizationContext context, System.Action<CausalityTrace> onCaptured)
        {
            await promise;
            await Promise.SwitchToContextAwait(context, forceAsync: true);
            onCaptured(CausalityTrace.Capture());
        }

        private static async Promise CaptureAfterAwaitOnContextForTraceTest(Promise promise, System.Threading.SynchronizationContext context, System.Action<CausalityTrace> onCaptured)
        {
            await promise.ConfigureAwait(new ContinuationOptions(context));
            onCaptured(CausalityTrace.Capture());
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ResolveForTraceTest(Promise.Deferred deferred)
            => deferred.Resolve();

        private static bool HistoryContainsMethod(CausalityTrace trace, string methodName)
        {
            var visited = new HashSet<CausalityTrace>();
            var traces = new Stack<CausalityTrace>();
            traces.Push(trace);
            while (traces.Count > 0)
            {
                var current = traces.Pop();
                if (!visited.Add(current))
                {
                    continue;
                }
                if (current.GetFrames().Any(frame => frame.GetMethod().Name == methodName))
                {
                    return true;
                }
                foreach (var previous in current.PreviousTraces)
                {
                    traces.Push(previous);
                }
            }
            return false;
        }

        // Runs the posted callbacks when Execute is called, like a UI thread's message loop.
        private sealed class QueuedContext : System.Threading.SynchronizationContext
        {
            private readonly Queue<(System.Threading.SendOrPostCallback callback, object state)> _queue = new Queue<(System.Threading.SendOrPostCallback, object)>();

            public override void Post(System.Threading.SendOrPostCallback d, object state)
            {
                lock (_queue)
                {
                    _queue.Enqueue((d, state));
                }
            }

            internal void Execute()
            {
                while (true)
                {
                    (System.Threading.SendOrPostCallback callback, object state) item;
                    lock (_queue)
                    {
                        if (_queue.Count == 0)
                        {
                            return;
                        }
                        item = _queue.Dequeue();
                    }
                    item.callback.Invoke(item.state);
                }
            }
        }

        // Copies the trace with the previous traces (innermost first), so that the copy has the same stack trace.
        private static CausalityTrace CopyTrace(CausalityTrace trace, bool isTruncated, params CausalityTrace[] previousTraces)
            => new CausalityTrace(trace, previousTraces, isTruncated);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtLeaf() => CausalityTrace.Capture();

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtLoop() => CausalityTrace.Capture();

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtStep() => CausalityTrace.Capture();

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtRoot() => CausalityTrace.Capture();

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtOther() => CausalityTrace.Capture();

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtUnique() => CausalityTrace.Capture();

        // Captures each trace with the earlier traces at the indices as its previous traces (innermost first), and returns the last trace.
        // The traces are captured from one line, so that traces from the same capture method have the same stack trace,
        // which is why the scopes' traces are set directly instead of entering nested scopes.
        private static CausalityTrace CaptureTraceGraph(params (System.Func<CausalityTrace> capture, int[] previousIndices)[] specs)
        {
            var traces = new List<CausalityTrace>();
            var currentTraces = CausalityTrace.ts_traces;
            try
            {
                foreach (var spec in specs)
                {
                    var scopeTraces = new Stack<CausalityTrace>();
                    for (int i = spec.previousIndices.Length - 1; i >= 0; --i)
                    {
                        scopeTraces.Push(traces[spec.previousIndices[i]]);
                    }
                    CausalityTrace.ts_traces = scopeTraces;
                    traces.Add(spec.capture());
                }
            }
            finally
            {
                CausalityTrace.ts_traces = currentTraces;
            }
            return traces[traces.Count - 1];
        }

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

        private static CausalityTrace GetTraceFromAsyncLoop(int iterations)
        {
            var pending = new Queue<Promise.Deferred>();
            var trace = new CausalityTrace[1];
            LoopCallingAsyncMethod(pending, trace, iterations).Forget();
            while (pending.Count > 0)
            {
                pending.Dequeue().Resolve();
            }
            return trace[0];
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

        // Every outer scope that is referred to must be labeled once, in the direction that the reference says, and every label must be referred to.
        // Repeated blocks must be labeled once, and printed before they are repeated.
        // Every stack trace in the history must be printed (as a whole), and the truncation must be printed if the history is truncated.
        private static void AssertCausalityTraceIsPrintedCorrectly(CausalityTrace trace, string text)
        {
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text, @"Outer causality scope ([A-Z]+(?:/[A-Z]+)*), see (above|below)"))
            {
                int labelIndex = text.IndexOf("--- [Outer causality scope " + match.Groups[1].Value + "] ---");
                Assert.GreaterOrEqual(labelIndex, 0, "Missing label for " + match.Value + ": " + text);
                Assert.AreEqual(match.Groups[2].Value == "below", labelIndex > match.Index, match.Value + ": " + text);
            }
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text, @"--- \[Outer causality scope ([A-Z]+(?:/[A-Z]+)*)\] ---"))
            {
                Assert.AreEqual(1, CountOccurrences(text, match.Value), "Duplicate " + match.Value + ": " + text);
                StringAssert.Contains("Outer causality scope " + match.Groups[1].Value + ", see", text, "Unreferenced " + match.Value + ": " + text);
            }
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text, @"\[#(\d+) repeated (\d+) times?(?:: [^\]]*)?\]"))
            {
                var number = match.Groups[1].Value;
                int beginIndex = text.IndexOf("--- [#" + number + " Begin] ---");
                int endIndex = text.IndexOf("--- [#" + number + " End] ---");
                Assert.GreaterOrEqual(beginIndex, 0, "Missing begin label for " + match.Value + ": " + text);
                Assert.Greater(endIndex, beginIndex, "Missing end label for " + match.Value + ": " + text);
                Assert.Less(endIndex, match.Index, match.Value + " must be after its block: " + text);
                Assert.AreEqual(1, CountOccurrences(text, "--- [#" + number + " Begin] ---"), "Duplicate begin label for " + match.Value + ": " + text);
                Assert.AreNotEqual("0", match.Groups[2].Value, text);
            }
            var openLabels = new Stack<string>();
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text, @"--- \[#(\d+) (Begin|End)\] ---"))
            {
                if (match.Groups[2].Value == "Begin")
                {
                    openLabels.Push(match.Groups[1].Value);
                }
                else
                {
                    Assert.AreEqual(openLabels.Count > 0 ? openLabels.Pop() : null, match.Groups[1].Value, "Misnested " + match.Value + ": " + text);
                }
            }
            Assert.AreEqual(0, openLabels.Count, "Unclosed labels: " + text);

            // The stack traces are printed in the format of the visible frames (see CausalityTrace.FormatStackTrace).
            var textLines = text.Split(new[] { System.Environment.NewLine }, System.StringSplitOptions.None);
            var visited = new HashSet<CausalityTrace>();
            var traces = new Stack<CausalityTrace>();
            bool isTruncated = false;
            traces.Push(trace);
            while (traces.Count > 0)
            {
                var current = traces.Pop();
                if (!visited.Add(current))
                {
                    continue;
                }
                isTruncated |= current.IsTruncated;
                var stackTraceText = string.Join(System.Environment.NewLine,
                    current.GetFrames().Select(frame => new System.Diagnostics.StackTrace(frame).ToString().TrimEnd('\r', '\n')));
                var stackTraceLines = stackTraceText.Length == 0
                    ? new[] { "   --- [All frames are hidden] ---" }
                    : stackTraceText.Split(new[] { System.Environment.NewLine }, System.StringSplitOptions.None);
                Assert.IsTrue(ContainsBlock(textLines, stackTraceLines), "Missing stack trace:" + System.Environment.NewLine + stackTraceText + System.Environment.NewLine + "in:" + System.Environment.NewLine + text);
                foreach (var previousTrace in current.PreviousTraces)
                {
                    traces.Push(previousTrace);
                }
            }
            Assert.AreEqual(isTruncated, text.Contains("Older traces truncated"), text);

            // Each trace's thread and time is printed with its stack trace (at most once in a chain and once in its own section), once where it's folded as a repeat,
            // and once in each reference to it as an outer scope that is printed as another, so the output can't grow faster than the number of traces and their previous traces.
            int timeCount = System.Text.RegularExpressions.Regex.Matches(text, @"\d+\.\d{3}(?=,| ms earlier)").Count;
            Assert.LessOrEqual(timeCount, visited.Count * 3 + visited.Sum(current => current.PreviousTraces.Count), text);
        }

        // Whether the lines are printed as a whole block, between markers (or blank lines, or the start or end of the text),
        // so that a stack trace that is only printed as part of a longer stack trace is not found.
        private static bool ContainsBlock(string[] textLines, string[] blockLines)
        {
            for (int start = 0; start + blockLines.Length <= textLines.Length; ++start)
            {
                int end = start + blockLines.Length;
                if ((start == 0 || IsMarkerLine(textLines[start - 1]))
                    && (end == textLines.Length || IsMarkerLine(textLines[end]))
                    && Enumerable.Range(0, blockLines.Length).All(i => textLines[start + i] == blockLines[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsMarkerLine(string line)
            => line.Length == 0 || line.TrimStart().StartsWith("---");

        private static int CountOccurrences(string text, string value)
            => System.Text.RegularExpressions.Regex.Matches(text, System.Text.RegularExpressions.Regex.Escape(value)).Count;

        // Removes the threads and times that follow references, and replaces the times of the stack traces.
        private static string WithoutThreadsAndTimes(string text)
        {
            text = System.Text.RegularExpressions.Regex.Replace(text, @"[:;] Thread [^\]]*\]", "]");
            return System.Text.RegularExpressions.Regex.Replace(text, @"\d+\.\d{3} ms earlier", "0.000 ms earlier");
        }
    }
}