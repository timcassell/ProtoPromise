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
            // Some tests continue on the thread pool, which may still be releasing objects after the test completes.
            TestHelper.Cleanup(spinForThreadPool: true);

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

        private static int GetCausalityTraceDepth(CausalityTrace trace)
        {
            int depth = 0;
            for (; trace != null; trace = trace.Previous)
            {
                ++depth;
            }
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
        public void CausalityTraceToString_PrintsTheChainOfPreviousTraces()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The traces are captured in different methods, because optimized code may report the same location for calls in the same method,
                // which would make them repeats of each other (see CausalityTrace.Repeats).
                var traceA = CaptureAtOther();
                var scopeA = CausalityTrace.EnterScope(traceA);
                var traceB = CaptureAtStep();
                // B's callback runs synchronously inside A's callback.
                var scopeB = CausalityTrace.EnterScope(traceB);
                var traceX = CaptureAtLoop();
                scopeB.Dispose();
                scopeA.Dispose();

                // The previous trace is the innermost scope. A's callback is already on the stack.
                Assert.AreSame(traceB, traceX.Previous);
                Assert.AreSame(traceA, traceB.Previous);
                Assert.IsNull(traceA.Previous);
                Assert.IsNull(OuterTrace(traceX));
                CollectionAssert.IsEmpty(traceX.Repeats);

                var text = traceX.ToString();
                var lines = text.Split(new[] { System.Environment.NewLine }, System.StringSplitOptions.None);
                // The chain is X -> B -> A.
                Assert.AreEqual(2, lines.Count(l => l == "--- End of stack trace from the previous location ---"), text);
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
                System.Func<System.Diagnostics.StackFrame, bool> isInternal = f => f.GetMethod()?.DeclaringType?.Namespace?.StartsWith("Proto.") == true;
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

        // Not inlined, so that its frame is in the stack trace, but marked for aggressive inlining, which the runtime's StackTrace.ToString() hides.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining | System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static CausalityTrace CaptureInAMethodMarkedForAggressiveInliningForTraceTest() => CausalityTrace.Capture();

        [Test]
        public void CausalityTraceGetFrames_ExcludesFramesOfMethodsMarkedForAggressiveInlining()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var trace = CaptureInAMethodMarkedForAggressiveInliningForTraceTest();

                const string methodName = nameof(CaptureInAMethodMarkedForAggressiveInliningForTraceTest);
#if !ENABLE_IL2CPP // IL2CPP's C++ compiler may inline it anyway.
                Assert.IsTrue(trace.StackTrace.GetFrames().Any(f => f.GetMethod()?.Name == methodName), trace.StackTrace.ToString());
#endif
                Assert.IsFalse(ContainsMethod(trace, methodName), trace.ToString());
                StringAssert.DoesNotContain(methodName, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceDebuggerDisplay_IsTheThreadAndTheInnermostFrame()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var trace = CaptureAtLeaf();
#if PROTO_PROMISE_DEVELOPER_MODE
                // Developer mode doesn't hide internal frames.
                string innermostMethod = $"{typeof(CausalityTrace).FullName}.{nameof(CausalityTrace.Capture)}";
#else
                string innermostMethod = $"{typeof(MiscellaneousTests).FullName}.{nameof(CaptureAtLeaf)}";
#endif

                StringAssert.StartsWith($"Thread {System.Threading.Thread.CurrentThread.ManagedThreadId} ", trace.DebuggerDisplay);
                StringAssert.EndsWith($" at {innermostMethod}", trace.DebuggerDisplay);
                // Unlike ToString(), it doesn't print the stack trace.
                StringAssert.DoesNotContain(System.Environment.NewLine, trace.DebuggerDisplay);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void RejectForReportTraceTest(Promise.Deferred deferred, System.Exception exception)
        {
            deferred.Reject(exception);
            Volatile.Write(ref s_frameKeeper, 1);
        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void DisposeForReportTraceTest(Promise.Retainer retainer)
        {
            retainer.Dispose();
            Volatile.Write(ref s_frameKeeper, 2);
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
                AssertPreviousTrace(deferredTrace, trace);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledExceptionMessage_Traced_SaysTheStackTraceIsTheCausalityTrace()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var exception = new System.InvalidOperationException("Bang!");
                var unhandledException = CaptureUnhandledException(exception, () => Promise.Rejected(exception).Forget());

                Assert.IsNotNull(unhandledException.CausalityTrace);
                StringAssert.EndsWith(" -- This exception's StackTrace contains the causality trace of the rejection (the async operations that led to it).", unhandledException.Message);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledExceptionMessage_TracingDisabled_SaysHowToEnableIt()
        {
            var exception = new System.InvalidOperationException("Bang!");
            var unhandledException = CaptureUnhandledException(exception, () => Promise.Rejected(exception).Forget());

            Assert.IsNull(unhandledException.CausalityTrace);
            StringAssert.EndsWith(" -- Set Proto.Diagnostics.CausalityTrace.IsEnabled = true to get a causality trace.", unhandledException.Message);
        }

        [Test]
        public void UnhandledExceptionMessage_TracingEnabledAfterTheRejection_SaysNoTraceWasFound()
        {
            var exception = new System.InvalidOperationException("Bang!");
            var promise = Promise.Rejected(exception);
            CausalityTrace.IsEnabled = true;
            try
            {
                var unhandledException = CaptureUnhandledException(exception, () => promise.Forget());

                Assert.IsNull(unhandledException.CausalityTrace);
#if PROMISE_DEBUG
                StringAssert.EndsWith(" -- No causality trace was captured. CausalityTrace.IsEnabled may have been set after the operation was started.", unhandledException.Message);
#else
                StringAssert.EndsWith(" -- No causality trace was found. Causality traces are not always available in RELEASE mode (for example, objects reported from finalizers on some runtimes). Enable DEBUG mode for reliable causality traces.", unhandledException.Message);
#endif
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void DeferredResolve_ContinuationsInvokedSynchronously_TheirTraceIsTheCause()
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
                // The deferred's scope is outside the continuation's scope, so its trace is an outer trace, and what completed the deferred is on the stack.
                AssertPreviousTrace(thenTrace, callbackTrace);
                CollectionAssert.AreEqual(OuterTracesFromTheSameMethod(deferredTrace), callbackTrace.OuterTraces, callbackTrace.ToString());
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

                AssertPreviousTrace(thenTrace, callbackTrace);
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
                AssertPreviousTrace(finallyTrace, callbackTrace);
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
                AssertPreviousTrace(catchTrace, callbackTrace);
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
                AssertPreviousTrace(thenTrace, trace);
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
                int scopeCount = 0;
                var trace = CaptureUnhandledException(exception, () =>
                {
                    Promise.New(deferred =>
                    {
                        // The continuation runs while the deferred is rejected, inside its own scope.
                        deferred.Promise
                            .Catch(() =>
                            {
                                scopeCount = CausalityTrace.ts_traces.Count;
                                throw Promise.Rethrow;
                            })
                            .Forget();
                        RejectForReportTraceTest(deferred, exception);
                    });
                }).CausalityTrace;

                // The scope of Promise.New's callback, and the scope of the continuation.
                Assert.AreEqual(2, scopeCount);

#if PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't hide internal frames, so the promise's trace is not contained by the trace.
                // The promise's trace is the cause, and it has no cause of its own.
                Assert.IsNotNull(trace.Previous);
                Assert.IsNull(trace.Previous.Previous);
#else
                // The promise's trace is skipped, because the call to Promise.New is on the stack, and it has no cause of its own.
                Assert.IsNull(trace.Previous);
#endif
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
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            // Enabled covers the ExecutionContext and ConfiguredAwaitDualContext paths.
            bool wasFlowExecutionContextEnabled = Promise.Config.s_asyncFlowExecutionContextEnabled;
            Promise.Config.s_asyncFlowExecutionContextEnabled = flowExecutionContext;
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

                var deferred1Trace = deferred1.Promise._ref.Trace;
                var deferred2Trace = deferred2.Promise._ref.Trace;
                var promise = Func();
                var asyncTrace = promise._ref.Trace;
                promise.Forget();

                deferred1.Resolve();
                TestHelper.ExecuteForegroundCallbacks();
                deferred2.Resolve();

                Assert.IsNotNull(asyncTrace);
                Assert.AreSame(asyncTrace, traceAfterFirstAwait.Previous);
                Assert.AreSame(asyncTrace, traceAfterSecondAwait.Previous);
                // Only the continuation that was scheduled on the context has where it was scheduled.
                // The context runs it from this method (see KeepsOuterTracesFromTheSameMethod).
                // A continuation that the deferred invoked synchronously runs in the deferred's scope, so its outer trace is the deferred's trace instead.
                if (!configureFirstAwaitToForeground)
                {
                    CollectionAssert.AreEqual(OuterTracesFromTheSameMethod(deferred1Trace), traceAfterFirstAwait.OuterTraces, traceAfterFirstAwait.ToString());
                }
                else if (KeepsOuterTracesFromTheSameMethod)
                {
                    Assert.IsNotNull(OuterTrace(traceAfterFirstAwait), traceAfterFirstAwait.ToString());
                }
                // Otherwise it's dropped, unless IL2CPP's C++ linker folded Deferred.Resolve with an identical function,
                // whose name is printed in its place, so its frame is visible and it's not printed like the outer frames of the trace.
                CollectionAssert.AreEqual(OuterTracesFromTheSameMethod(deferred2Trace), traceAfterSecondAwait.OuterTraces, traceAfterSecondAwait.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.s_asyncFlowExecutionContextEnabled = wasFlowExecutionContextEnabled;
            }
        }

#if PROMISE_DEBUG
        [Test]
        public void AsyncPromise_ResumedOnAContextWhileTracingIsDisabled_ContinuationsRunInsideTheAsyncPromiseScopeAfterItIsEnabledAgain()
        {
            // In DEBUG mode, the async promise keeps its trace while causality traces are disabled, so it has its trace again after they're enabled again.
            // Resuming on a context clears the field that marks it as traced, so this checks that the mark is restored while they're disabled.
            // (When the ExecutionContext flows, the field is the ExecutionContext instead, which is captured again at every await.)
            bool wasFlowExecutionContextEnabled = Promise.Config.s_asyncFlowExecutionContextEnabled;
            Promise.Config.s_asyncFlowExecutionContextEnabled = false;
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred1 = Promise.NewDeferred();
                var deferred2 = Promise.NewDeferred();
                CausalityTrace traceAfterSecondAwait = null;

                async Promise Func()
                {
                    await deferred1.Promise.ConfigureAwait(ContinuationOptions.Foreground);
                    await deferred2.Promise;
                    traceAfterSecondAwait = CausalityTrace.Capture();
                }

                var promise = Func();
                var asyncTrace = promise._ref.Trace;
                promise.Forget();

                CausalityTrace.IsEnabled = false;
                deferred1.Resolve();
                TestHelper.ExecuteForegroundCallbacks();
                CausalityTrace.IsEnabled = true;
                deferred2.Resolve();

                Assert.IsNotNull(asyncTrace);
                Assert.AreSame(asyncTrace, traceAfterSecondAwait.Previous);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.s_asyncFlowExecutionContextEnabled = wasFlowExecutionContextEnabled;
            }
        }
#endif

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
                Assert.IsNull(traceAfterAwait.Previous);
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
            Volatile.Write(ref s_frameKeeper, 3);
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
                    AssertPreviousTrace(channelTrace, trace);
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
            Assert.IsNotNull(callbackTrace.Previous);
            StringAssert.Contains(methodName, callbackTrace.Previous.StackTrace.ToString());
        }

        // Like AssertRanInsideTheScopeStartedBy, for a callback that the method invoked synchronously, where scopeTrace is the current trace when the callback ran.
        // The operation's trace is skipped as the callback trace's previous trace, because the method is on the callback's stack,
        // except in developer mode, which doesn't hide internal frames.
        private static void AssertRanSynchronouslyInsideTheScopeStartedBy(CausalityTrace callbackTrace, CausalityTrace scopeTrace, string methodName)
        {
            Assert.IsNotNull(callbackTrace);
            Assert.IsNotNull(scopeTrace);
            StringAssert.Contains(methodName, scopeTrace.StackTrace.ToString());
#if PROTO_PROMISE_DEVELOPER_MODE
            Assert.AreSame(scopeTrace, callbackTrace.Previous);
#else
            // The scope's trace is skipped, because it's printed like the outer frames of the callback's trace,
            // unless one of its hidden frames is printed with another function's name (see MayPrintHiddenFramesWithOtherNames).
            if (!MayPrintHiddenFramesWithOtherNames || callbackTrace.Previous != scopeTrace)
            {
                Assert.AreSame(scopeTrace.Previous, callbackTrace.Previous);
            }
            StringAssert.Contains(methodName, callbackTrace.StackTrace.ToString());
#endif
        }

        private static CausalityTrace GetCurrentTraceForTraceTest()
        {
            var traces = CausalityTrace.ts_traces;
            return traces == null || traces.Count == 0 ? null : traces.Peek();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void RegisterForTraceTest(CancelationToken cancelationToken, CausalityTrace[] callbackTrace)
        {
            cancelationToken.Register(() => callbackTrace[0] = CausalityTrace.Capture());
            Volatile.Write(ref s_frameKeeper, 4);
        }

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
            Volatile.Write(ref s_frameKeeper, 5);
        }

        [Test]
        public void AwaiterContinuation_RunsInsideTheScopeOfWhereItWasAwaited(
            [Values] bool configured)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                var deferredTrace = deferred.Promise._ref.Trace;
                var continuationTrace = new CausalityTrace[1];
                OnCompletedForTraceTest(deferred.Promise, configured, continuationTrace);

                ResolveForTraceTest(deferred);
                TestHelper.ExecuteForegroundCallbacks();

                AssertRanInsideTheScopeStartedBy(continuationTrace[0], nameof(OnCompletedForTraceTest));
                // Only the continuation that was scheduled on the context has where it was scheduled from.
                // The continuation that the deferred invoked synchronously runs in the deferred's scope, so its outer trace is the deferred's trace instead.
                var text = continuationTrace[0].ToString();
                if (configured)
                {
                    Assert.IsNotNull(OuterTrace(continuationTrace[0]), text);
                    Assert.IsTrue(ContainsMethod(OuterTrace(continuationTrace[0]), nameof(ResolveForTraceTest)), text);
                }
                else
                {
                    CollectionAssert.AreEqual(OuterTracesFromTheSameMethod(deferredTrace), continuationTrace[0].OuterTraces, text);
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ReleaseForTraceTest(Proto.Promises.Threading.AsyncLock.Key key)
        {
            key.Dispose();
            Volatile.Write(ref s_frameKeeper, 6);
        }

        private static async Promise LockOnTheCapturedContextForTraceTest(Proto.Promises.Threading.AsyncLock asyncLock, CausalityTrace[] trace)
        {
            using (await asyncLock.LockAsync(true))
            {
                trace[0] = CaptureAtLeaf();
            }
        }

        [Test]
        public void AsyncLockContinuationOnTheCapturedContext_IncludesWhereTheLockWasReleasedFrom()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var asyncLock = new Proto.Promises.Threading.AsyncLock();
                var trace = new CausalityTrace[1];
                var key = asyncLock.Lock();
                // The captured context is the foreground context of the test thread (see Promise.Manager.ThreadStaticSynchronizationContext).
                LockOnTheCapturedContextForTraceTest(asyncLock, trace).Forget();
                ReleaseForTraceTest(key);
                Assert.IsNull(trace[0]);
                TestHelper.ExecuteForegroundCallbacks();

                // The waiter continued on the context, so where the lock was released from is not on its stack.
                Assert.IsNotNull(trace[0]);
                var text = trace[0].ToString();
                Assert.IsNotNull(OuterTrace(trace[0]), text);
                Assert.IsTrue(ContainsMethod(OuterTrace(trace[0]), nameof(ReleaseForTraceTest)), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Progress NewProgressForTraceTest(CausalityTrace[] handlerTrace)
        {
            var progress = Progress.New(_ => handlerTrace[0] = CausalityTrace.Capture(), SynchronizationOption.Synchronous);
            Volatile.Write(ref s_frameKeeper, 7);
            return progress;
        }

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

#if !UNITY_WEBGL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Proto.Timers.Timer CreateTimerForTraceTest(Proto.Timers.TimerFactory timerFactory, CausalityTrace[] callbackTrace, System.Threading.ManualResetEventSlim invoked)
        {
            var timer = timerFactory.CreateTimer(_ =>
            {
                callbackTrace[0] = CausalityTrace.Capture();
                invoked.Set();
            }, null, System.TimeSpan.FromMilliseconds(1), System.Threading.Timeout.InfiniteTimeSpan);
            Volatile.Write(ref s_frameKeeper, 8);
            return timer;
        }

        [Test]
        public void TimerCallback_RunsInsideTheScopeOfWhereTheTimerWasCreated(
            [Values] bool customTimeProvider)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var timerFactory = customTimeProvider
                    ? Proto.Timers.TimerFactory.FromTimeProvider(new CustomTimeProviderForTraceTest())
                    : Proto.Timers.TimerFactory.System;
                var callbackTrace = new CausalityTrace[1];
                // Not disposed, because a late callback may set it after the wait timed out.
                var invoked = new System.Threading.ManualResetEventSlim();
                var timer = CreateTimerForTraceTest(timerFactory, callbackTrace, invoked);
                try
                {
                    Assert.IsTrue(invoked.Wait(System.TimeSpan.FromSeconds(5)));
                }
                finally
                {
                    // Waits for the callback to return.
                    timer.DisposeAsync().WaitWithTimeout(System.TimeSpan.FromSeconds(5));
                }

                AssertRanInsideTheScopeStartedBy(callbackTrace[0], nameof(CreateTimerForTraceTest));
                Assert.IsNull(OuterTrace(callbackTrace[0]), callbackTrace[0].ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private sealed class CustomTimeProviderForTraceTest : System.TimeProvider { }
#endif // !UNITY_WEBGL

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise<int> GetLazyPromiseForTraceTest(AsyncLazy<int> lazy)
        {
            var promise = lazy.Promise;
            Volatile.Write(ref s_frameKeeper, 9);
            return promise;
        }

        [Test]
        public void AsyncLazyFactory_RunsInsideTheScopeOfWhereTheResultWasFirstRequested(
            [Values] bool withProgress)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                CausalityTrace factoryTrace = null, scopeTrace = null;
                var lazy = withProgress
                    ? new AsyncLazy<int>(_ =>
                    {
                        scopeTrace = GetCurrentTraceForTraceTest();
                        factoryTrace = CausalityTrace.Capture();
                        return Promise.Resolved(1);
                    })
                    : new AsyncLazy<int>(() =>
                    {
                        scopeTrace = GetCurrentTraceForTraceTest();
                        factoryTrace = CausalityTrace.Capture();
                        return Promise.Resolved(1);
                    });

                Assert.AreEqual(1, GetLazyPromiseForTraceTest(lazy)
                    .WaitWithTimeoutWhileExecutingForegroundContext(System.TimeSpan.FromSeconds(1)));

                AssertRanSynchronouslyInsideTheScopeStartedBy(factoryTrace, scopeTrace, nameof(GetLazyPromiseForTraceTest));
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
            var promise = asyncEnumerable
                ? Proto.Promises.Threading.ParallelAsync.ForEach(Proto.Promises.Linq.AsyncEnumerable.Range(0, 3), options, body)
                : Proto.Promises.Threading.ParallelAsync.ForEach(Enumerable.Range(0, 3), options, body);
            Volatile.Write(ref s_frameKeeper, 10);
            return promise;
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
        public void CausalityTraceMaxOuterTraces_MustNotBeNegative()
        {
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => CausalityTrace.MaxOuterTraces = -1);
        }

        [Test]
        public void CausalityTraceCapture_MoreOuterScopesThanMaxOuterTraces_KeepsTheInnermostOnes(
            [Values(0, 2, 3)] int maxOuterTraces)
        {
            CausalityTrace.MaxOuterTraces = maxOuterTraces;
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                // From the innermost.
                var outers = new[] { CaptureAtStep(), CaptureAtLoop(), CaptureAtRoot() };
                var trace = CaptureInScopesForTraceTest(other, outers);

                var text = trace.ToString();
                Assert.AreSame(other, trace.Previous, text);
                CollectionAssert.AreEqual(outers.Take(maxOuterTraces), trace.OuterTraces, text);
                Assert.AreEqual(maxOuterTraces < outers.Length, trace.AreOuterTracesTruncated, text);
                if (trace.AreOuterTracesTruncated)
                {
                    // Printed after the outer traces, indented one more level.
                    StringAssert.Contains(System.Environment.NewLine + new string(' ', 3 * (maxOuterTraces + 2))
                        + "--- Outer traces truncated, increase CausalityTrace.MaxOuterTraces to see more ---" + System.Environment.NewLine, text);
                }
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                CausalityTrace.MaxOuterTraces = 8;
            }
        }

        [Test]
        public void CausalityTraceCapture_RepeatOfThePreviousTrace_TheOuterTracesAreTruncatedIfItsOuterTracesDontFitOrWereTruncated(
            [Values] bool sameOuterTrace)
        {
            CausalityTrace.MaxOuterTraces = 1;
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                // The repeat's outer traces are truncated.
                var previousOuters = new[] { CaptureAtLoop(), CaptureAtRoot() };
                // If the trace's outer trace is the repeat's, the repeat's outer traces fit, but the ones that it dropped are still missing.
                // A null scope doesn't change the current trace, so that the second trace is captured in as many nested calls as the first.
                var outers = new[] { sameOuterTrace ? previousOuters[0] : CaptureAtStep(), null };
                // Captured from the same line, so that the second trace is a repeat of the first.
                var trace = other;
                for (int i = 0; i < 2; ++i)
                {
                    trace = CaptureInScopesForTraceTest(trace, i == 0 ? previousOuters : outers);
                }

                var text = trace.ToString();
                Assert.AreEqual(1, trace.Repeats.Count, text);
                Assert.AreSame(other, trace.Previous, text);
                CollectionAssert.AreEqual(new[] { outers[0] }, trace.OuterTraces, text);
                Assert.IsTrue(trace.AreOuterTracesTruncated, text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                CausalityTrace.MaxOuterTraces = 8;
            }
        }

        [Test]
        public void CausalityTraceCapture_TruncatedCopyOfATraceWhoseOuterTracesWereTruncated_TheyAreStillTruncated()
        {
            CausalityTrace.MaxOuterTraces = 0;
            CausalityTrace.MaxDepth = 4;
            CausalityTrace.IsEnabled = true;
            try
            {
                var trace = CaptureAtRoot();
                for (int i = 0; i < 2; ++i)
                {
                    var scope = CausalityTrace.EnterScope(trace);
                    trace = i == 0 ? CaptureAtLoop() : CaptureAtLeaf();
                    scope.Dispose();
                }
                var withTruncatedOuterTraces = CaptureInScopesForTraceTest(trace, new[] { CaptureAtStep() });
                Assert.IsTrue(withTruncatedOuterTraces.AreOuterTracesTruncated, withTruncatedOuterTraces.ToString());
                // The trace is at the max depth, so the next trace has a truncated copy of it.
                var deepScope = CausalityTrace.EnterScope(withTruncatedOuterTraces);
                var last = CaptureAtOther();
                deepScope.Dispose();

                var text = last.ToString();
                Assert.AreEqual(2, GetCausalityTraceDepth(last), text);
                Assert.AreNotSame(withTruncatedOuterTraces, last.Previous, text);
                Assert.IsTrue(last.Previous.AreOuterTracesTruncated, text);
                AssertCausalityTraceIsPrintedCorrectly(last, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                CausalityTrace.MaxDepth = 128;
                CausalityTrace.MaxOuterTraces = 8;
            }
        }

        // Captures inside the scopes of the outer traces (from the innermost) and the previous trace.
        private static CausalityTrace CaptureInScopesForTraceTest(CausalityTrace previous, CausalityTrace[] outers)
            => CaptureInScopesForTraceTest(previous, outers, outers.Length - 1);

        private static CausalityTrace CaptureInScopesForTraceTest(CausalityTrace previous, CausalityTrace[] outers, int index)
        {
            if (index < 0)
            {
                return CaptureInScopeForTraceTest(previous);
            }
            var scope = CausalityTrace.EnterScope(outers[index]);
            try
            {
                return CaptureInScopesForTraceTest(previous, outers, index - 1);
            }
            finally
            {
                scope.Dispose();
            }
        }

        [Test]
        public void CausalityTraceCapture_DeepChainOfAsyncFunctionsThatCompleteSynchronously_KeepsAtMostMaxOuterTraces()
        {
            CausalityTrace.MaxOuterTraces = 4;
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each async function continues the one that awaited it while it's still running, so their scopes are nested.
                var deferred = Promise.NewDeferred();
                var traces = new List<CausalityTrace>();
                var promise = AwaitRecursivelyForTraceTest(deferred.Promise, 16, traces);
                deferred.Resolve();
                promise.WaitWithTimeout(System.TimeSpan.FromSeconds(5));

                Assert.AreEqual(17, traces.Count);
                var last = traces[traces.Count - 1];
                var text = last.ToString();
                foreach (var trace in traces)
                {
                    Assert.LessOrEqual(trace.OuterTraces.Count, 4, trace.ToString());
                }
                // Without line numbers, the outer traces are printed like the outer frames of the trace, so they're dropped.
                if (KeepsOuterTracesFromTheSameMethod)
                {
                    Assert.IsTrue(last.AreOuterTracesTruncated, text);
                }
                AssertCausalityTraceIsPrintedCorrectly(last, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                CausalityTrace.MaxOuterTraces = 8;
            }
        }

        private static async Promise AwaitRecursivelyForTraceTest(Promise promise, int depth, List<CausalityTrace> traces)
        {
            if (depth == 0)
            {
                await promise;
            }
            else
            {
                await AwaitRecursivelyForTraceTest(promise, depth - 1, traces);
            }
            traces.Add(CausalityTrace.Capture());
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
                    // Captured at two locations in turn, so that each trace is not a repeat of its previous trace (see CausalityTrace.Repeats).
                    trace = i % 2 == 0 ? CaptureAtStep() : CaptureAtLoop();
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
                // Captured in another method, so that the previous trace is not skipped without line numbers.
                var nextTrace = CaptureAtLeaf();
                int depth = GetCausalityTraceDepth(nextTrace);
                Assert.LessOrEqual(depth, maxDepth);
                if (remaining > 0)
                {
                    CaptureNestedCausalityTraces(nextTrace, remaining - 1, maxDepth);
                }
                else
                {
                    Assert.Greater(depth, 1);
                }
            }
            finally
            {
                scope.Dispose();
            }
        }

        [Test]
        public void CausalityTrace_RecursivelyScheduledContinuations_AreFoldedIntoOneTrace()
        {
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
                            // Not the last call, so that it's not a tail call, which would remove this callback's frame from the next traces.
                            schedule();
                            callbackTrace = CausalityTrace.Capture();
                        })
                        .Forget();
                };
                schedule();

                for (int i = 0; i < 100; ++i)
                {
                    deferred.Resolve();
                    // The callback, the continuation that was scheduled from the previous callback, and the first continuation.
                    Assert.LessOrEqual(GetCausalityTraceDepth(callbackTrace), 3);
                }
                // Each continuation was scheduled from the same location as the previous one, so it replaced it, and the others are its repeats.
                var continuationTrace = callbackTrace.Previous;
                Assert.AreEqual(1, continuationTrace.Repeats.Count);
                Assert.AreEqual(98, continuationTrace.Repeats[0].Count);
                Assert.AreEqual(continuationTrace.ThreadId, continuationTrace.Repeats[0].ThreadId);
                Assert.That(continuationTrace.Repeats[0].FirstTimestamp, Is.LessThanOrEqualTo(continuationTrace.Repeats[0].LastTimestamp));
                Assert.That(continuationTrace.Repeats[0].LastTimestamp, Is.LessThanOrEqualTo(continuationTrace.Timestamp));
                Assert.That(continuationTrace.Repeats[0].FirstTimestamp, Is.GreaterThanOrEqualTo(continuationTrace.Previous.Timestamp));
                StringAssert.Contains("Repeated 98 times: ", callbackTrace.ToString());

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
        public void CausalityTraceToString_RecursivelyScheduledContinuations_DoNotGrowWithTheIterations()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The traces are captured from the same line, so that their stack traces are the same.
                var traces = new[] { 200, 400 }.Select(iterations => GetTraceFromRecursivelyScheduledContinuations(iterations)).ToArray();
                var text200 = traces[0].ToString();
                var trace400 = traces[1];
                var text400 = trace400.ToString();
                StringAssert.IsMatch(@"--- Repeated \d+ times: Thread \d+ ", text400);
                // Only the number of repeats is different.
                AssertPrintedTheSame(text200.Replace("Repeated 199 times", "Repeated 399 times"), text400);
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
                        // Not the last call, so that it's not a tail call, which would remove this callback's frame from the next traces.
                        schedule();
                        callbackTrace = CausalityTrace.Capture();
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
        public void CausalityTraceToString_AsyncLoop_DoesNotGrowWithTheIterations()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each awaited operation is started inside the async function's scope, so its cause is the async function, not the previous iteration.
                // The traces are captured from the same line, so that their stack traces are the same.
                var traces = new[] { 16, 64 }.Select(iterations => GetTraceFromAsyncLoop(iterations)).ToArray();
                var text16 = traces[0].ToString();
                var trace64 = traces[1];
                var text64 = trace64.ToString();
                AssertPrintedTheSame(text16, text64);
                AssertCausalityTraceIsPrintedCorrectly(trace64, text64);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_NegativeSkipFrames_Throws()
        {
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => CausalityTrace.Capture(-1));
        }

        [Test]
        public void CausalityTrace_CapturesInTheSameTooDeepScopeAfterTheMaxDepthChanged_AreTruncatedToTheNewDepth()
        {
            int originalMaxDepth = CausalityTrace.MaxDepth;
            CausalityTrace.MaxDepth = 6;
            CausalityTrace.IsEnabled = true;
            try
            {
                // Build a scope whose trace is at the max depth, so that every capture inside it is truncated (see the test below).
                var trace = CaptureAtRoot();
                for (int i = 1; i < 6; ++i)
                {
                    var scope = CausalityTrace.EnterScope(trace);
                    trace = i % 2 == 0 ? CaptureAtStep() : CaptureAtLoop();
                    scope.Dispose();
                }
                var deepScope = CausalityTrace.EnterScope(trace);
                var trace1 = CaptureAtLeaf();
                CausalityTrace.MaxDepth = 4;
                var trace2 = CaptureAtLeaf();
                deepScope.Dispose();

                // Half of the max depth is kept.
                Assert.AreEqual(3, GetCausalityTraceDepth(trace1));
                Assert.AreEqual(2, GetCausalityTraceDepth(trace2));
            }
            finally
            {
                CausalityTrace.MaxDepth = originalMaxDepth;
                CausalityTrace.IsEnabled = false;
            }
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
                // The traces are captured at two locations in turn, so that each trace is not a repeat of its previous trace (see CausalityTrace.Repeats),
                // and each location is a call, so that each trace doesn't contain its previous trace (see CausalityTrace.Previous).
                var trace = CaptureAtRoot();
                for (int i = 1; i < maxDepth; ++i)
                {
                    var scope = CausalityTrace.EnterScope(trace);
                    trace = i % 2 == 0 ? CaptureAtStep() : CaptureAtLoop();
                    scope.Dispose();
                }
                var deepScope = CausalityTrace.EnterScope(trace);
                var trace1 = CausalityTrace.Capture();
                var trace2 = CausalityTrace.Capture();
                deepScope.Dispose();

                Assert.AreNotSame(trace, trace1.Previous, "The previous trace was expected to be truncated.");
                Assert.AreSame(trace1.Previous, trace2.Previous);
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
                    var outerTrace = CaptureAtOther();
                    var innerTrace = CaptureAtStep();
                    var outerScope = CausalityTrace.EnterScope(outerTrace);
                    var innerScope = CausalityTrace.EnterScope(innerTrace);
                    innerScope.Dispose();
                    AssertDisposeThrows(innerScope, true);

                    // A scope that was entered in the same place on the stack is not exited either.
                    var laterTrace = CaptureAtLoop();
                    var laterScope = CausalityTrace.EnterScope(laterTrace);
                    AssertDisposeThrows(innerScope, true);
                    Assert.AreSame(laterTrace, CausalityTrace.Capture().Previous);
                    laterScope.Dispose();

                    // The outer scope is still the current scope.
                    var trace = CausalityTrace.Capture();
                    outerScope.Dispose();
                    Assert.AreSame(outerTrace, trace.Previous);
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
                    var outerTrace = CaptureAtOther();
                    var innerTrace = CaptureAtStep();
                    var outerScope = CausalityTrace.EnterScope(outerTrace);
                    var innerScope = CausalityTrace.EnterScope(innerTrace);
                    outerScope.Dispose();
                    reports.AssertScopeMisuses("before its nested");
                    Assert.IsNull(CausalityTrace.Capture().Previous);

                    // Disposing the inner scope later throws, and doesn't exit a scope that was entered after the outer scope was disposed.
                    var laterTrace = CaptureAtLoop();
                    var laterScope = CausalityTrace.EnterScope(laterTrace);
                    AssertDisposeThrows(innerScope, false);
                    var trace = CausalityTrace.Capture();
                    laterScope.Dispose();
                    Assert.AreSame(laterTrace, trace.Previous);
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
                    Assert.IsNull(CausalityTrace.Capture().Previous);
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
        {
            var thenPromise = promise.Then(() => { CausalityTrace.EnterScope(CausalityTrace.Capture()); });
            Volatile.Write(ref s_frameKeeper, 11);
            return thenPromise;
        }

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
                    Assert.IsNull(CausalityTrace.Capture().Previous);
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
        {
            CausalityTrace.EnterScope(isTraceNull ? null : CausalityTrace.Capture());
            Volatile.Write(ref s_frameKeeper, 12);
        }
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
                    Assert.IsNull(CausalityTrace.Capture().Previous);
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
                    Assert.IsNull(CausalityTrace.Capture().Previous);
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
                    Assert.IsNull(CausalityTrace.Capture().Previous);
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
                    Assert.IsNull(handlerTrace.Previous);
                    Assert.IsNull(CausalityTrace.Capture().Previous);
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
                // Captured in different methods, so that the trace is not a repeat of the outer trace (see CausalityTraceToString_PrintsTheChainOfPreviousTraces).
                var outerTrace = CaptureAtOther();
                var outerScope = CausalityTrace.EnterScope(outerTrace);
                var nullScope = CausalityTrace.EnterScope(null);
                var trace = CaptureAtStep();
                nullScope.Dispose();
                outerScope.Dispose();
                Assert.AreSame(outerTrace, trace.Previous);
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
                // Traces without frames in the chain and as the root. They're not repeats of each other, because they have no frames to compare.
                var root = CopyTrace(CaptureWithoutFrames(), false, CopyTrace(CaptureWithoutFrames(), false, CaptureAtLeaf()));

                var text = root.ToString();
                Assert.AreEqual(2, CountOccurrences(text, "--- All frames are hidden ---"), text);
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
                StringAssert.StartsWith($"   --- Thread {root.ThreadId} ", firstLine);
                StringAssert.DoesNotContain("earlier", firstLine);
                const string time = @", \d+\.\d{3} ms earlier ---";
                StringAssert.IsMatch($@"--- Thread {foreground.ThreadId} \(foreground\){time}", text);
                StringAssert.IsMatch($@"--- {ThreadPattern(named)}{time}", text);
                StringAssert.IsMatch($@"--- Thread {background.ThreadId} \(background\){time}", text);
                // Newer runtimes name thread pool threads.
                var poolThread = pooled.ThreadName == null ? $@"Thread {pooled.ThreadId} \(thread pool\)" : ThreadPattern(pooled);
                StringAssert.IsMatch($@"--- {poolThread}{time}", text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_ThreadNameWithLineBreaks_IsPrintedOnOneLine()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                CausalityTrace trace = null;
                // A new thread, because the name of a thread can only be set once.
                var thread = new System.Threading.Thread(() => trace = CaptureAtLeaf()) { Name = "First\r\nSecond\nThird" };
                thread.Start();
                thread.Join();

                var text = trace.ToString();
                StringAssert.StartsWith($"   --- Thread {trace.ThreadId} \"First  Second Third\" ---", text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
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
                // like a loop that moved between threads. Each second time on thread A replaced the first one, which is its repeat.
                var last = TurnCapturer.Capture(CaptureAtOther(), 2, 0, 0, 1, 0, 0);
                var root = CaptureInScopeForTraceTest(last);
                Assert.AreEqual(4, GetCausalityTraceDepth(last));
                Assert.AreEqual(1, last.Repeats.Count);
                var text = root.ToString();

                // The last time is printed, and the others are folded into it, grouped by thread from the latest to the earliest.
                var threadA = ThreadPattern(last);
                var threadB = ThreadPattern(last.Previous);
                StringAssert.IsMatch($@"--- {threadA}, \d+\.\d{{3}} ms earlier ---", text);
                const string time = @"(\d+\.\d{3})";
                var match = System.Text.RegularExpressions.Regex.Match(text, $@"--- Repeated 4 times: {threadA} 3 times at {time} to {time} ms earlier; "
                    + $@"{threadB} 1 time at {time} ms earlier ---");
                Assert.IsTrue(match.Success, text);
                var times = Enumerable.Range(1, 3).Select(i => double.Parse(match.Groups[i].Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                Assert.That(times[0], Is.LessThan(times[1]), text);
                Assert.That(times[2], Is.InRange(times[0], times[1]), text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_RepeatsOnThreadPoolThreads_AreFoldedAsOneThread()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like a loop that continues on the thread pool, which may be a different thread each time.
                var other = CaptureAtOther();
                var traces = CaptureOnDifferentThreadPoolThreads(other, 3);
                Assert.AreEqual(3, traces.Select(t => t.ThreadId).Distinct().Count());

                var trace = traces[2];
                Assert.AreSame(other, trace.Previous);
                Assert.AreEqual(1, trace.Repeats.Count);
                Assert.IsTrue(trace.Repeats[0].IsThreadPoolThread);
                Assert.AreEqual(2, trace.Repeats[0].Count);
                Assert.AreEqual(traces[1].ThreadId, trace.Repeats[0].ThreadId);
                var text = trace.ToString();
                StringAssert.IsMatch(@"--- Repeated 2 times: thread pool 2 times at \d+\.\d{3} to \d+\.\d{3} ms earlier ---", text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static string ThreadPattern(CausalityTrace namedThreadTrace)
            => $@"Thread {namedThreadTrace.ThreadId} ""{System.Text.RegularExpressions.Regex.Escape(namedThreadTrace.ThreadName)}""";
#endif // !UNITY_WEBGL

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

        [Test]
        public void CausalityTraceCapture_RepeatOfThePreviousTrace_IsFoldedIntoIt()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each trace is captured at the same location inside the scope of the previous trace, like a callback that registers itself again.
                var other = CaptureAtOther();
                var trace = other;
                var traces = new CausalityTrace[5];
                for (int i = 0; i < 5; ++i)
                {
                    trace = CaptureInScopeForTraceTest(trace);
                    traces[i] = trace;
                }

                // The first trace in the loop was not a repeat, because it was captured inside the scope of a trace from another location.
                Assert.AreSame(other, trace.Previous);
                Assert.AreEqual(1, trace.Repeats.Count);
                var repeat = trace.Repeats[0];
                Assert.AreEqual(4, repeat.Count);
                Assert.AreEqual(trace.ThreadId, repeat.ThreadId);
                Assert.AreEqual(trace.ThreadName, repeat.ThreadName);
                Assert.AreEqual(trace.IsThreadPoolThread, repeat.IsThreadPoolThread);
                Assert.AreEqual(trace.IsBackgroundThread, repeat.IsBackgroundThread);
                Assert.AreEqual(traces[0].Timestamp, repeat.FirstTimestamp);
                Assert.AreEqual(traces[3].Timestamp, repeat.LastTimestamp);

                var text = trace.ToString();
                StringAssert.IsMatch(@"--- Repeated 4 times: Thread \d+ .* 4 times at \d+\.\d{3} to \d+\.\d{3} ms earlier ---", text);
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureInScopeForTraceTest)), text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't hide internal frames, so the traces of the library's calls are not contained by other traces.
        [Test]
        public void CausalityTraceCapture_InsideTheCallThatCapturedThePreviousTrace_SkipsIt()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                CausalityTrace newTrace, callbackTrace = null;
                Promise.Deferred deferred = default;
                var scope = CausalityTrace.EnterScope(other);
                try
                {
                    // Promise.New captures the promise's trace, and invokes the callback inside its scope.
                    var promise = Promise.New(d =>
                    {
                        deferred = d;
                        callbackTrace = CaptureAtLeaf();
                    });
                    newTrace = promise._ref.Trace;
                    promise.Forget();
                }
                finally
                {
                    scope.Dispose();
                }
                deferred.Resolve();

                // The promise's trace is skipped, because its frames are the outer frames of the callback's trace. Its cause is kept.
                // It's kept instead if one of its hidden frames is printed with another function's name (see MayPrintHiddenFramesWithOtherNames).
                Assert.IsNotNull(newTrace);
                Assert.AreSame(other, newTrace.Previous);
                if (!MayPrintHiddenFramesWithOtherNames || callbackTrace.Previous != newTrace)
                {
                    Assert.AreSame(other, callbackTrace.Previous);
                }
                AssertCausalityTraceIsPrintedCorrectly(callbackTrace, callbackTrace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_AfterSwitchingToAContext_SkipsTheTraceOfTheAsyncFunctionThatSwitched()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var trace = new CausalityTrace[1];
                CaptureAfterSwitchForTraceTest(context, trace).Forget();
                context.Execute();

                var text = trace[0].ToString();
                // The async function's trace was captured where it was called, which is in the trace of where it switched,
                // so the trace and where it was scheduled from are the whole chain.
                bool isSkipped = OuterTrace(trace[0]) != null;
#if !PROMISE_DEBUG
                // In release builds, the async function's trace is captured where it first awaits, which is where it switched.
                // Depending on the runtime's frames, it's skipped the same way, or where the trace was scheduled from is dropped as its repeat instead.
                if (!isSkipped)
                {
                    Assert.AreEqual(2, GetCausalityTraceDepth(trace[0]), text);
                    StringAssert.Contains(nameof(CausalityTraceCapture_AfterSwitchingToAContext_SkipsTheTraceOfTheAsyncFunctionThatSwitched), trace[0].Previous.ToString());
                }
#endif
                if (isSkipped && MayPrintHiddenFramesWithOtherNames)
                {
                    // The async function's trace is kept if one of its hidden frames is printed with another function's name.
                    Assert.LessOrEqual(GetCausalityTraceDepth(trace[0]), 2, text);
                    StringAssert.Contains(nameof(CausalityTraceCapture_AfterSwitchingToAContext_SkipsTheTraceOfTheAsyncFunctionThatSwitched), OuterTrace(trace[0]).ToString());
                }
                else if (isSkipped)
                {
                    Assert.AreEqual(1, GetCausalityTraceDepth(trace[0]), text);
                    StringAssert.Contains(nameof(CausalityTraceCapture_AfterSwitchingToAContext_SkipsTheTraceOfTheAsyncFunctionThatSwitched), OuterTrace(trace[0]).ToString());
                }
#if PROMISE_DEBUG
                Assert.IsTrue(isSkipped, text);
#endif
                AssertCausalityTraceIsPrintedCorrectly(trace[0], text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async Promise CaptureAfterSwitchForTraceTest(QueuedContext context, CausalityTrace[] trace)
        {
            await Promise.SwitchToContextAwait(context, forceAsync: true);
            trace[0] = CausalityTrace.Capture();
        }

        // The tests below capture with CaptureFromTheSameLineForTraceTest in a loop, so that a trace that is captured deeper contains the trace before it,
        // like a trace captured in Promise.New's callback contains the trace of Promise.New. They enter the scope of an outer trace around it directly,
        // like a callback that was scheduled from that trace.

        // Captures a trace inside the scope of the previous trace, here or one call deeper, from the same line.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureFromTheSameLineForTraceTest(CausalityTrace previous, bool deeper)
        {
            var scope = CausalityTrace.EnterScope(previous);
            try
            {
                return deeper ? CaptureAtLeaf() : CausalityTrace.Capture();
            }
            finally
            {
                scope.Dispose();
            }
        }

        // Captures a trace and then a deeper trace inside its scope, each inside the scope of an outer trace (if it's not null), as if their callbacks were scheduled from it.
        private static CausalityTrace CaptureContainedForTraceTest(CausalityTrace previous, CausalityTrace outer, CausalityTrace deeperOuter, out CausalityTrace contained)
        {
            contained = null;
            var trace = previous;
            for (int i = 0; i < 2; ++i)
            {
                var scope = CausalityTrace.EnterScope(i == 0 ? outer : deeperOuter);
                try
                {
                    trace = CaptureFromTheSameLineForTraceTest(trace, i == 1);
                }
                finally
                {
                    scope.Dispose();
                }
                if (i == 0)
                {
                    contained = trace;
                }
            }
            return trace;
        }

        [Test]
        public void CausalityTraceCapture_SkippedTrace_ItsOuterTracesAreKept(
            [Values] bool deeperHasAnOuterTrace)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var outer = CaptureAtStep();
                var deeperOuter = deeperHasAnOuterTrace ? CaptureAtLoop() : null;
                var trace = CaptureContainedForTraceTest(other, outer, deeperOuter, out var contained);

                CollectionAssert.AreEqual(new[] { outer }, contained.OuterTraces);
                // The contained trace is skipped, and its outer traces are kept after the trace's own.
                var text = trace.ToString();
                Assert.AreSame(other, trace.Previous, text);
                CollectionAssert.AreEqual(deeperHasAnOuterTrace ? new[] { deeperOuter, outer } : new[] { outer }, trace.OuterTraces, text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_TraceContainedByWhereTheCallbackWasScheduledFrom_IsSkippedAndItsOuterTracesAreKept(
            [Values] bool containedHasAnOuterTrace)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The trace of where the callback was scheduled from contains the previous trace, like an async function that switched to another context.
                var other = CaptureAtOther();
                var containedOuter = containedHasAnOuterTrace ? CaptureAtStep() : null;
                var scheduledFrom = CaptureContainedForTraceTest(other, containedOuter, null, out var contained);
                CausalityTrace trace;
                var scope = CausalityTrace.EnterScope(scheduledFrom);
                try
                {
                    trace = CaptureInScopeForTraceTest(contained);
                }
                finally
                {
                    scope.Dispose();
                }

                // The contained trace is skipped, and its outer traces are kept after the trace's own.
                var text = trace.ToString();
                Assert.AreSame(other, trace.Previous, text);
                Assert.AreEqual(containedHasAnOuterTrace ? 2 : 1, trace.OuterTraces.Count, text);
                Assert.AreSame(scheduledFrom.StackTrace, trace.OuterTraces[0].StackTrace, text);
                if (containedHasAnOuterTrace)
                {
                    Assert.AreSame(containedOuter, trace.OuterTraces[1], text);
                }
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_SkippedTraceWithRepeats_IsKept()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The second capture is a repeat of the first, and the third contains it.
                var other = CaptureAtOther();
                var trace = other;
                CausalityTrace repeated = null;
                for (int i = 0; i < 3; ++i)
                {
                    repeated = trace;
                    trace = CaptureFromTheSameLineForTraceTest(trace, i == 2);
                }

                // The repeats would be lost, so it's kept.
                Assert.AreEqual(1, repeated.Repeats.Count);
                Assert.AreSame(repeated, trace.Previous, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_SkippedTruncatedTrace_TheTraceIsTruncated()
        {
            int maxDepth = CausalityTrace.MaxDepth;
            CausalityTrace.MaxDepth = 2;
            CausalityTrace.IsEnabled = true;
            try
            {
                var step = CopyTrace(CaptureAtStep(), false, CaptureAtOther());
                var trace = CaptureContainedForTraceTest(step, null, null, out var contained);

                // The contained trace was the oldest trace that was kept, so the trace is now the oldest.
                Assert.IsTrue(contained.IsTruncated);
                Assert.IsNull(trace.Previous);
                Assert.IsTrue(trace.IsTruncated);
                var text = trace.ToString();
                StringAssert.Contains("Older traces truncated", text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.MaxDepth = maxDepth;
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_LoopThroughACallThatInvokesItsCallback_DoesNotGrowTheChain()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like a loop whose iterations call Promise.New, and capture in its callback.
                var other = CaptureAtOther();
                var trace = other;
                for (int i = 0; i < 10; ++i)
                {
                    trace = CaptureFromTheSameLineForTraceTest(trace, i % 2 == 1);
                }

                // Each contained trace is skipped, which exposes the previous iteration's trace, which is a repeat.
                var text = trace.ToString();
                Assert.AreSame(other, trace.Previous, text);
                Assert.AreEqual(1, trace.Repeats.Count, text);
                Assert.AreEqual(4, trace.Repeats[0].Count, text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_SkippedTraceInsideTheScopeOfWhereTheCallbackWasScheduledFrom_DoesNotIncludeItTwice()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like a scheduled callback without its own trace (whose scope is where it was scheduled from) that calls Promise.New.
                var scheduledFrom = CaptureAtOther();
                CausalityTrace trace, contained;
                var scope = CausalityTrace.EnterScope(scheduledFrom);
                try
                {
                    trace = CaptureContainedForTraceTest(scheduledFrom, scheduledFrom, scheduledFrom, out contained);
                }
                finally
                {
                    scope.Dispose();
                }

                var text = trace.ToString();
                Assert.AreSame(scheduledFrom, trace.Previous, text);
                Assert.IsNull(OuterTrace(trace), text);
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtOther)), text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_TraceContainedByATraceScheduledFromTheSameTrace_IsSkipped(
            [Values] bool isCopy)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var scheduledFrom = CaptureAtStep();
                // A copy of the same trace, like copies made on different threads.
                var deeperScheduledFrom = isCopy ? CopyTrace(scheduledFrom, false) : scheduledFrom;
                var trace = CaptureContainedForTraceTest(other, scheduledFrom, deeperScheduledFrom, out var contained);

                // Both traces were scheduled from the same trace, so nothing is lost.
                var text = trace.ToString();
                Assert.AreSame(scheduledFrom, OuterTrace(contained), text);
                Assert.AreSame(other, trace.Previous, text);
                Assert.AreSame(deeperScheduledFrom, OuterTrace(trace), text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_PromiseNewInAContinuationOnAContext_SkipsTheTraceOfPromiseNew()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                var traces = new CausalityTrace[2];
                PromiseNewAfterSwitchForTraceTest(deferred.Promise, context, traces).Forget();
                ResolveForTraceTest(deferred);
                context.Execute();

                // Both traces were captured in the continuation, so they were scheduled from the same trace.
                var promiseTrace = traces[0];
                var callbackTrace = traces[1];
                var text = callbackTrace.ToString();
                Assert.IsNotNull(OuterTrace(promiseTrace), promiseTrace.ToString());
                Assert.AreSame(promiseTrace.Previous, callbackTrace.Previous, text);
                Assert.AreSame(OuterTrace(promiseTrace), OuterTrace(callbackTrace), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async Promise PromiseNewAfterSwitchForTraceTest(Promise promise, QueuedContext context, CausalityTrace[] traces)
        {
            await promise.ConfigureAwait(new ContinuationOptions(context));
            var newPromise = Promise.New(deferred =>
            {
                traces[1] = CausalityTrace.Capture();
                deferred.Resolve();
            });
            traces[0] = newPromise._ref.Trace;
            await newPromise;
        }

        [Test]
        public void CausalityTraceCapture_SkippedTraceWithoutOuterTraces_OuterTraceAtTheSameLocationAsTheNewPreviousTraceIsDropped()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var steps = new CausalityTrace[2];
                for (int i = 0; i < 2; ++i)
                {
                    steps[i] = CaptureAtStep();
                }
                // The deeper trace is scheduled from a repeat of the contained trace's previous trace, which is its previous trace after the skip.
                var trace = CaptureContainedForTraceTest(steps[0], null, steps[1], out _);

                var text = trace.ToString();
                Assert.AreSame(steps[0], trace.Previous, text);
                Assert.IsNull(OuterTrace(trace), text);
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtStep)), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_SkippedTraceScheduledFromTheSameLocationAsTheTrace_ItsOuterTraceIsDropped()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                // Like a callback that schedules itself again from Promise.New's callback: the second time, both traces are scheduled from the first deeper trace,
                // which was captured at the same location as the second deeper trace.
                var traces = new CausalityTrace[2];
                CausalityTrace contained = null;
                for (int i = 0; i < 2; ++i)
                {
                    var scheduledFrom = i == 0 ? null : traces[0];
                    traces[i] = CaptureContainedForTraceTest(other, scheduledFrom, scheduledFrom, out contained);
                }

                var trace = traces[1];
                var text = trace.ToString();
                Assert.AreSame(traces[0].StackTrace, OuterTrace(contained).StackTrace, text);
                Assert.AreSame(other, trace.Previous, text);
                Assert.IsNull(OuterTrace(trace), text);
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtLeaf)), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_CallbackThatSchedulesItselfAgainInPromiseNew_IsNotScheduledFromItself()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var scheduledFrom = new CausalityTrace[1];
                ScheduleInPromiseNewForTraceTest(context, scheduledFrom, 3);
                context.Execute();

                // The last continuation was scheduled from the same location as the one before it, so that isn't one of its outer traces.
                var text = scheduledFrom[0].ToString();
                Assert.IsFalse(scheduledFrom[0].OuterTraces.Any(trace => HasSameFrames(trace, scheduledFrom[0])), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static void ScheduleInPromiseNewForTraceTest(QueuedContext context, CausalityTrace[] scheduledFrom, int count)
        {
            // The promise is awaited from the callback.
            _ = Promise.New(deferred =>
            {
                ContinueOnContextForTraceTest(deferred.Promise, context, scheduledFrom, count).Forget();
                deferred.Resolve();
            });
        }

        private static async Promise ContinueOnContextForTraceTest(Promise promise, QueuedContext context, CausalityTrace[] scheduledFrom, int count)
        {
            await promise.ConfigureAwait(new ContinuationOptions(context, CompletedContinuationBehavior.Asynchronous));
            // The continuation runs in the scope of where it was scheduled from, which is the outermost scope, because the test runs the context outside of any scope.
            var traces = CausalityTrace.ts_traces.ToArray();
            scheduledFrom[0] = traces[traces.Length - 1];
            if (count > 1)
            {
                ScheduleInPromiseNewForTraceTest(context, scheduledFrom, count - 1);
            }
        }

        [Test]
        public void CausalityTraceCapture_OuterTraceThatIsThePreviousTraceWithoutVisibleFrames_IsDropped()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like a callback without its own trace, whose scope is where it was scheduled from, which entered the scope again.
                var hidden = CaptureWithoutFrames();
                CausalityTrace trace;
                var scope = CausalityTrace.EnterScope(hidden);
                try
                {
                    trace = CaptureInScopeForTraceTest(hidden);
                }
                finally
                {
                    scope.Dispose();
                }

                var text = trace.ToString();
                Assert.AreSame(hidden, trace.Previous, text);
                Assert.IsNull(OuterTrace(trace), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_SkippedTraceInsideTheScopeOfWhereTheCallbackWasScheduledFromWithoutVisibleFrames_DoesNotIncludeItTwice()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like a scheduled callback without its own trace (whose scope is where it was scheduled from) that calls Promise.New.
                var hidden = CaptureWithoutFrames();
                var trace = CaptureContainedForTraceTest(hidden, hidden, hidden, out _);

                var text = trace.ToString();
                Assert.AreSame(hidden, trace.Previous, text);
                Assert.IsNull(OuterTrace(trace), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_InsideTheScopeOfATraceWithoutVisibleFrames_KeepsIt()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // A trace without visible frames has nothing to compare, so it's not contained by the trace.
                var hidden = CaptureWithoutFrames();
                var trace = CaptureInScopeForTraceTest(hidden);

                Assert.AreSame(hidden, trace.Previous, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_WithoutVisibleFramesInsideTheScopeOfATraceWithoutVisibleFrames_IsNotARepeat()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Traces without visible frames have nothing to compare, so they're not repeats of each other.
                var hidden = CaptureWithoutFrames();
                CausalityTrace trace;
                var scope = CausalityTrace.EnterScope(hidden);
                try
                {
                    trace = CaptureWithoutFrames();
                }
                finally
                {
                    scope.Dispose();
                }

                Assert.AreSame(hidden, trace.Previous, trace.ToString());
                Assert.AreEqual(0, trace.Repeats.Count, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_InACallbackThatAContextRanSynchronously_DoesNotIncludeWhereItWasScheduled()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new InlineContext();
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                CaptureAfterAwaitOnContextForTraceTest(deferred.Promise, context, t => trace = t).Forget();
                ResolveForTraceTest(deferred);

                // What resolved the deferred is already in the trace, so where it was scheduled is not one of its outer traces.
                var text = trace.ToString();
                Assert.IsTrue(ContainsMethod(trace, nameof(ResolveForTraceTest)), text);
                Assert.IsFalse(trace.OuterTraces.Any(outer => ContainsMethod(outer, nameof(ResolveForTraceTest))), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }
#endif // !PROTO_PROMISE_DEVELOPER_MODE

        [Test]
        public void CausalityTraceCapture_RepeatOfThePreviousTrace_ItsOuterTracesAreKept()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var scheduledFrom = CaptureAtStep();
                // Captured from the same line, so that the second trace is a repeat of the first.
                var trace = other;
                for (int i = 0; i < 2; ++i)
                {
                    var scope = CausalityTrace.EnterScope(i == 0 ? scheduledFrom : null);
                    try
                    {
                        trace = CaptureInScopeForTraceTest(trace);
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                }

                var text = trace.ToString();
                Assert.AreEqual(1, trace.Repeats.Count, text);
                Assert.AreSame(other, trace.Previous, text);
                Assert.AreSame(scheduledFrom, OuterTrace(trace), text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_RepeatOfThePreviousTrace_ItsOwnOuterTracesAreFirst(
            [Values] bool previousHasAnOuterTrace)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var scheduledFrom = CaptureAtStep();
                var previousScheduledFrom = previousHasAnOuterTrace ? CaptureAtLoop() : null;
                // Captured from the same line, so that the second trace is a repeat of the first.
                var trace = other;
                for (int i = 0; i < 2; ++i)
                {
                    var scope = CausalityTrace.EnterScope(i == 0 ? previousScheduledFrom : scheduledFrom);
                    try
                    {
                        trace = CaptureInScopeForTraceTest(trace);
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                }

                // The repeat's outer traces are kept after the trace's own.
                var text = trace.ToString();
                Assert.AreEqual(1, trace.Repeats.Count, text);
                CollectionAssert.AreEqual(previousHasAnOuterTrace ? new[] { scheduledFrom, previousScheduledFrom } : new[] { scheduledFrom }, trace.OuterTraces, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_RepeatOfATruncatedTrace_TheTraceIsTruncated()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The second trace is captured inside the scope of a truncated copy of the first, from the same line.
                var trace = CaptureAtOther();
                for (int i = 0; i < 2; ++i)
                {
                    trace = CaptureInScopeForTraceTest(i == 0 ? trace : CopyTrace(trace, true));
                }

                var text = trace.ToString();
                Assert.AreEqual(1, trace.Repeats.Count, text);
                Assert.IsNull(trace.Previous, text);
                Assert.IsTrue(trace.IsTruncated, text);
                StringAssert.Contains("Older traces truncated", text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_OuterTraceAtTheSameLocationAsTheTrace_IsDropped()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                // Like an operation whose trace was captured where it was scheduled: both traces are captured from the same line.
                CausalityTrace trace = null;
                for (int i = 0; i < 2; ++i)
                {
                    var scope = CausalityTrace.EnterScope(trace);
                    try
                    {
                        trace = CaptureInScopeForTraceTest(other);
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                }

                var text = trace.ToString();
                Assert.AreSame(other, trace.Previous, text);
                Assert.IsNull(OuterTrace(trace), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_OuterTraceAtTheSameLocationAsThePreviousTrace_IsDropped()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var steps = new CausalityTrace[2];
                for (int i = 0; i < 2; ++i)
                {
                    steps[i] = CaptureAtStep();
                }
                CausalityTrace trace;
                var scope = CausalityTrace.EnterScope(steps[1]);
                try
                {
                    trace = CaptureInScopeForTraceTest(steps[0]);
                }
                finally
                {
                    scope.Dispose();
                }

                var text = trace.ToString();
                Assert.AreSame(steps[0], trace.Previous, text);
                Assert.IsNull(OuterTrace(trace), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_InAListenerOfAQueuedEvent_IncludesWhereTheEventWasRaised()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like an event that is raised, then dispatched later: the dispatcher enters the scope of where the event was raised,
                // then each listener is invoked in the scope of where it was added.
                var listenerTrace = CaptureAtStep();
                var raiseTrace = CaptureAtLoop();
                CausalityTrace trace;
                var scope = CausalityTrace.EnterScope(raiseTrace);
                try
                {
                    trace = CaptureInScopeForTraceTest(listenerTrace);
                }
                finally
                {
                    scope.Dispose();
                }

                // The listener's trace is the cause, and where the event was raised is an outer trace.
                var text = trace.ToString();
                Assert.AreSame(listenerTrace, trace.Previous, text);
                CollectionAssert.AreEqual(new[] { raiseTrace }, trace.OuterTraces, text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_OuterTraces_EachIsIndentedOneMoreLevel()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var outers = new[] { CaptureAtStep(), CaptureAtLoop() };
                CausalityTrace trace;
                // The outermost scope is entered first.
                var outerScope = CausalityTrace.EnterScope(outers[1]);
                try
                {
                    var innerScope = CausalityTrace.EnterScope(outers[0]);
                    try
                    {
                        trace = CaptureInScopeForTraceTest(other);
                    }
                    finally
                    {
                        innerScope.Dispose();
                    }
                }
                finally
                {
                    outerScope.Dispose();
                }

                var text = trace.ToString();
                CollectionAssert.AreEqual(outers, trace.OuterTraces, text);
                // The outer traces are printed after the trace, from the innermost, each indented one more level than the one before it.
                StringAssert.IsMatch(@"(?m)^      --- Thread \d+ .*, \d+\.\d{3} ms earlier ---\r?$", text);
                StringAssert.IsMatch(@"(?m)^         --- Thread \d+ .*, \d+\.\d{3} ms earlier ---\r?$", text);
                Assert.Less(text.IndexOf(nameof(CaptureAtStep)), text.IndexOf(nameof(CaptureAtLoop)), text);
                Assert.Less(text.IndexOf(nameof(CaptureAtLoop)), text.IndexOf(nameof(CaptureAtOther)), text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_InsideAScopeWithANullTrace_KeepsTheTracesOfTheScopesOutsideOfIt(
            [Values] bool hasInnerScope)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // A scope with a null trace doesn't change the current trace, so the traces outside of it are kept.
                var outer = CaptureAtStep();
                var inner = CaptureAtLoop();
                CausalityTrace trace;
                var outerScope = CausalityTrace.EnterScope(outer);
                try
                {
                    var nullScope = CausalityTrace.EnterScope(null);
                    try
                    {
                        trace = hasInnerScope ? CaptureInScopeForTraceTest(inner) : CaptureAtLeaf();
                    }
                    finally
                    {
                        nullScope.Dispose();
                    }
                }
                finally
                {
                    outerScope.Dispose();
                }

                var text = trace.ToString();
                Assert.AreSame(hasInnerScope ? inner : outer, trace.Previous, text);
                CollectionAssert.AreEqual(hasInnerScope ? new[] { outer } : new CausalityTrace[0], trace.OuterTraces, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceOuterTraces_IsCachedAndReadOnly()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var outer = CaptureAtStep();
                var cause = CaptureAtLoop();
                CausalityTrace trace;
                var scope = CausalityTrace.EnterScope(outer);
                try
                {
                    trace = CaptureInScopeForTraceTest(cause);
                }
                finally
                {
                    scope.Dispose();
                }

                var outerTraces = trace.OuterTraces;
                Assert.AreSame(cause, trace.Previous, trace.ToString());
                CollectionAssert.AreEqual(new[] { outer }, outerTraces, trace.ToString());
                Assert.AreSame(outerTraces, trace.OuterTraces);
                Assert.Throws<System.NotSupportedException>(() => ((IList<CausalityTrace>) outerTraces)[0] = null);
                // A trace that was captured outside of any scope has no previous or outer traces.
                Assert.IsNull(outer.Previous);
                CollectionAssert.IsEmpty(outer.OuterTraces);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_AsyncLoop_DoesNotKeepTheEarlierIterations()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each iteration's async function is called while the previous one is completing, so the previous one's scope is an outer scope of its trace.
                // The outer traces are copies without their previous traces, so the iterations don't keep each other.
                var counts = new[] { 4, 16 }.Select(iterations => CountReachableTraces(GetTraceFromAsyncLoop(iterations))).ToArray();
                Assert.AreEqual(counts[0], counts[1]);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        // The number of distinct traces that the trace keeps, through its previous traces.
        private static int CountReachableTraces(CausalityTrace trace)
        {
            var reached = new HashSet<CausalityTrace>();
            var pending = new Stack<CausalityTrace>();
            pending.Push(trace);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (reached.Add(current))
                {
                    if (current.Previous != null)
                    {
                        pending.Push(current.Previous);
                    }
                    foreach (var outer in current.OuterTraces)
                    {
                        pending.Push(outer);
                    }
                }
            }
            return reached.Count;
        }

        [Test]
        public void CausalityTraceToString_RepeatedPartOfTheChainScheduledFromAnotherLocation_IsNotFolded()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var scheduledFroms = new[] { CaptureAtStep(), CaptureAtLoop() };
                var traces = new CausalityTrace[2];
                for (int i = 0; i < 2; ++i)
                {
                    var scope = CausalityTrace.EnterScope(scheduledFroms[i]);
                    try
                    {
                        traces[i] = CaptureInScopeForTraceTest(other);
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                }
                CollectionAssert.AreEqual(new[] { scheduledFroms[0] }, traces[0].OuterTraces);
                CollectionAssert.AreEqual(new[] { scheduledFroms[1] }, traces[1].OuterTraces);

                // The traces have the same frames, but folding them would lose where one of them was scheduled from.
                var trace = CopyTrace(traces[1], false, CopyTrace(traces[0], false, other));
                var text = trace.ToString();
                StringAssert.DoesNotContain("--- Repeated", text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if PROMISE_DEBUG // In optimized code, two calls in one method may report the same line.
        [Test]
        public void CausalityTraceCapture_InsideTheScopeOfATraceCapturedAtAnotherLineOfTheSameCall_KeepsIt()
        {
            if (!s_hasLineNumbers)
            {
                Assert.Ignore("Without line numbers, the traces are printed the same, so the previous trace is skipped.");
            }

            CausalityTrace.IsEnabled = true;
            try
            {
                var traces = CaptureTwiceForTraceTest();

                // The method is on the stack, but the previous trace was captured at another line, which is not in the trace.
                Assert.AreSame(traces[0], traces[1].Previous);
                AssertCausalityTraceIsPrintedCorrectly(traces[1], traces[1].ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace[] CaptureTwiceForTraceTest()
        {
            var trace = CausalityTrace.Capture();
            return new[] { trace, CaptureInScopeForTraceTest(trace) };
        }

#if !ENABLE_IL2CPP // IL2CPP doesn't provide accurate IL offsets.
        [Test]
        public void CausalityTraceCapture_InsideTheScopeOfATraceWithTheSameFramesFromAnotherCallOnTheSameLine_KeepsIt()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var traces = CaptureTwiceFromOneLineForTraceTest();

                // The traces are printed the same, but they were captured by different calls, so the previous trace is not a repeat,
                // and the trace has no more frames than the previous trace, so it doesn't contain it.
                Assert.AreSame(traces[0], traces[1].Previous);
                Assert.AreEqual(0, traces[1].Repeats.Count);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace[] CaptureTwiceFromOneLineForTraceTest()
        {
            // Both traces are captured on the same line, so that they have the same method and line, at different IL offsets.
            var traces = new CausalityTrace[2]; traces[0] = CausalityTrace.Capture(); var scope = CausalityTrace.EnterScope(traces[0]); try { traces[1] = CausalityTrace.Capture(); } finally { scope.Dispose(); }
            return traces;
        }

        [Test]
        public void CausalityTraceToString_TracesFromDifferentCallsOnTheSameLine_AreNotFolded()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Copies that are next to each other in the chain, so that the printer compares them.
                var traces = CaptureTwiceFromOneLineForTraceTest();
                var root = CopyTrace(CaptureAtRoot(), false, CopyTrace(traces[1], false, CopyTrace(traces[0], false, CaptureAtOther())));

                // They're printed the same, but they were captured by different calls, so they're not repeats.
                var text = root.ToString();
                Assert.AreEqual(2, CountOccurrences(text, nameof(CaptureTwiceFromOneLineForTraceTest)), text);
                StringAssert.DoesNotContain("Repeated", text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_ScheduledFromAnotherCallOnTheSameLineOfAMethodThatIsStillRunning_KeepsIt()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var trace = new CausalityTrace[1];
                SwitchAndCaptureOnOneLineForTraceTest(context, trace).Forget();
                context.Execute();

                // The context ran the continuation from the method that scheduled it, so where it was scheduled from is printed like the outer frames of the trace,
                // but the async function had moved on to another call on the same line, so the context didn't run it synchronously.
                Assert.IsNotNull(OuterTrace(trace[0]), trace[0].ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async Promise SwitchAndCaptureOnOneLineForTraceTest(QueuedContext context, CausalityTrace[] trace)
        {
            await Promise.SwitchToContextAwait(context, forceAsync: true);
            // Scheduled from inside the context on the same line as the capture, so that they have the same method and line, at different IL offsets.
            await Promise.SwitchToContextAwait(context, forceAsync: true); trace[0] = CaptureAtLeaf();
        }
#endif // !ENABLE_IL2CPP
#endif // PROMISE_DEBUG

        [Test]
        public void CausalityTraceToString_RepeatedPartOfTheChain_IsPrintedOnce()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // A part of the chain with two traces repeats, like two callbacks that schedule each other.
                var step = CaptureAtStep();
                var loop = CaptureAtLoop();
                var trace = CaptureAtOther();
                for (int i = 0; i < 3; ++i)
                {
                    trace = CopyTrace(step, false, CopyTrace(loop, false, trace));
                }
                var root = CopyTrace(CaptureAtRoot(), false, trace);

                var text = root.ToString();
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtStep)), text);
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtLoop)), text);
                StringAssert.IsMatch(@"--- #1 repeated 2 times: Thread \d+ .* 2 times at \d+\.\d{3} to \d+\.\d{3} ms earlier ---", text);
                StringAssert.Contains(nameof(CaptureAtOther), text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_RepeatedPartOfTheChainWithThreeTraces_IsPrintedOnce()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like three callbacks that schedule each other.
                var step = CaptureAtStep();
                var loop = CaptureAtLoop();
                var leaf = CaptureAtLeaf();
                var trace = CaptureAtOther();
                for (int i = 0; i < 3; ++i)
                {
                    trace = CopyTrace(step, false, CopyTrace(loop, false, CopyTrace(leaf, false, trace)));
                }
                var root = CopyTrace(CaptureAtRoot(), false, trace);

                var text = root.ToString();
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtStep)), text);
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtLoop)), text);
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtLeaf)), text);
                StringAssert.Contains("--- #1 repeated 2 times: ", text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !UNITY_WEBGL
        [Test]
        public void CausalityTraceToString_RepeatedPartOfTheChainOnOtherThreads_IncludesTheThreadOfEachCopy()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each copy of the part was captured on another thread, from the same frames.
                var steps = new CausalityTrace[3];
                var loops = new CausalityTrace[3];
                RunOnDifferentNewThreads(3, i =>
                {
                    steps[i] = CaptureAtStep();
                    loops[i] = CaptureAtLoop();
                });
                var trace = CaptureAtOther();
                for (int i = 0; i < 3; ++i)
                {
                    trace = CopyTrace(steps[i], false, CopyTrace(loops[i], false, trace));
                }
                var root = CopyTrace(CaptureAtRoot(), false, trace);

                // The latest copy is printed, and the threads and times of the earlier copies are listed from the latest to the earliest.
                var text = root.ToString();
                StringAssert.IsMatch($@"--- Thread {steps[2].ThreadId} \(background\), \d+\.\d{{3}} ms earlier ---", text);
                StringAssert.IsMatch($@"--- #1 repeated 2 times: Thread {steps[1].ThreadId} \(background\) 1 time at \d+\.\d{{3}} ms earlier; "
                    + $@"Thread {steps[0].ThreadId} \(background\) 1 time at \d+\.\d{{3}} ms earlier ---", text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        // Runs the action on new threads, one after another. Each thread is blocked until every action has run, so that the threads have different ids.
        private static void RunOnDifferentNewThreads(int count, System.Action<int> action)
        {
            var threads = new System.Threading.Thread[count];
            System.Runtime.ExceptionServices.ExceptionDispatchInfo exception = null;
            // The events are not disposed, because a thread may still use them after a wait timed out.
            var release = new System.Threading.ManualResetEventSlim();
            try
            {
                for (int i = 0; i < count; ++i)
                {
                    int index = i;
                    var ran = new System.Threading.ManualResetEventSlim();
                    threads[i] = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            action(index);
                        }
                        catch (System.Exception e)
                        {
                            exception = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e);
                        }
                        finally
                        {
                            ran.Set();
                        }
                        release.Wait();
                    }) { IsBackground = true };
                    threads[i].Start();
                    Assert.IsTrue(ran.Wait(System.TimeSpan.FromSeconds(10)), "The thread did not run the action in time.");
                }
            }
            finally
            {
                release.Set();
                foreach (var thread in threads)
                {
                    if (thread != null)
                    {
                        Assert.IsTrue(thread.Join(System.TimeSpan.FromSeconds(10)), "The thread did not exit in time.");
                    }
                }
            }
            exception?.Throw();
        }
#endif // !UNITY_WEBGL

        [Test]
        public void CausalityTraceToString_RepeatedPartOfTheChain_IncludesTheRepeatsOfEachCopy()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The traces with repeats are captured from the same line, so that they have the same stack trace.
                var withRepeats = new[] { 3, 5 }.Select(count => GetTraceWithRepeatsForTraceTest(count)).ToArray();
                var step = CaptureAtStep();
                var trace = CaptureAtOther();
                foreach (var repeated in withRepeats)
                {
                    trace = CopyTrace(step, false, CopyTrace(repeated, false, trace));
                }

                var text = trace.ToString();
                StringAssert.Contains("--- #1 repeated 1 time: ", text);
                StringAssert.Contains("--- Repeated 8 times: ", text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        // Gets a trace with the count of repeats.
        private static CausalityTrace GetTraceWithRepeatsForTraceTest(int count)
        {
            var trace = CaptureAtRoot();
            for (int i = 0; i <= count; ++i)
            {
                trace = CaptureInScopeForTraceTest(trace);
            }
            return CopyTrace(trace, false);
        }

        [Test]
        public void CausalityTraceCapture_AfterSequentialAwaits_DoesNotIncludeTheEarlierAwaits()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var pending = new Queue<Promise.Deferred>();
                var trace = new CausalityTrace[1];
                InitializeForTraceTest(pending, trace).Forget();
                while (pending.Count > 0)
                {
                    pending.Dequeue().Resolve();
                }

                // The operations that the async function awaited were started inside its scope, so their causes are the async function, not each other.
                // The trace's cause is where the async function was called, and the code that completed the last operation is on the stack.
                if (s_hasLineNumbers)
                {
                    Assert.AreEqual(2, GetCausalityTraceDepth(trace[0]), trace[0].ToString());
                }
                else
                {
                    // Without line numbers, where the async function was called may be printed like the outer frames of the trace, so it's skipped.
                    Assert.LessOrEqual(GetCausalityTraceDepth(trace[0]), 2, trace[0].ToString());
                }
                Assert.IsFalse(HistoryContainsMethod(trace[0], nameof(LoadConfigForTraceTest)), trace[0].ToString());
                Assert.IsFalse(HistoryContainsMethod(trace[0], nameof(ConnectForTraceTest)), trace[0].ToString());
                AssertPreviousTraceIsFrom(trace[0], nameof(CausalityTraceCapture_AfterSequentialAwaits_DoesNotIncludeTheEarlierAwaits));
                AssertCausalityTraceIsPrintedCorrectly(trace[0], trace[0].ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async Promise InitializeForTraceTest(Queue<Promise.Deferred> pending, CausalityTrace[] trace)
        {
            await LoadConfigForTraceTest(pending);
            await ConnectForTraceTest(pending);
            await GetPendingPromise(pending);
            trace[0] = CausalityTrace.Capture();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise LoadConfigForTraceTest(Queue<Promise.Deferred> pending)
        {
            var promise = GetPendingPromise(pending);
            Volatile.Write(ref s_frameKeeper, 13);
            return promise;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise ConnectForTraceTest(Queue<Promise.Deferred> pending)
        {
            var promise = GetPendingPromise(pending);
            Volatile.Write(ref s_frameKeeper, 14);
            return promise;
        }

        [Test]
        public void CausalityTraceToString_LoopOnAContext_DoesNotGrowWithTheIterations()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like a loop that runs once per frame on a game's main thread.
                // The traces are captured from the same line, so that their stack traces are the same.
                var traces = new[] { 4, 16 }.Select(iterations => GetTraceFromLoopOnContext(iterations)).ToArray();
                var text4 = traces[0].ToString();
                var trace16 = traces[1];
                var text16 = trace16.ToString();
                AssertPrintedTheSame(text4, text16);
                // The continuation was scheduled by the previous iteration.
                if (s_hasLineNumbers)
                {
                    Assert.AreEqual(2, GetCausalityTraceDepth(trace16), text16);
                }
                else
                {
                    // Without line numbers, the previous trace may be printed like the outer frames of the trace, so it's skipped.
                    Assert.LessOrEqual(GetCausalityTraceDepth(trace16), 2, text16);
                }
                // The async function's frame is its state machine's MoveNext, which is printed with the function's name.
                // The context runs it from this method (see KeepsOuterTracesFromTheSameMethod).
                if (KeepsOuterTracesFromTheSameMethod)
                {
                    StringAssert.Contains(nameof(SwitchInLoopForTraceTest), OuterTrace(trace16).ToString());
                    // Outer traces are indented one more level.
                    StringAssert.Contains("      --- Thread ", text16);
                }
                else
                {
                    Assert.IsNull(OuterTrace(trace16), text16);
                }
                AssertCausalityTraceIsPrintedCorrectly(trace16, text16);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static CausalityTrace GetTraceFromLoopOnContext(int iterations)
        {
            var context = new QueuedContext();
            var trace = new CausalityTrace[1];
            SwitchInLoopForTraceTest(context, iterations, trace).Forget();
            context.Execute();
            return trace[0];
        }

        private static async Promise SwitchInLoopForTraceTest(QueuedContext context, int iterations, CausalityTrace[] trace)
        {
            for (int i = 0; i < iterations; ++i)
            {
                await Promise.SwitchToContextAwait(context, forceAsync: true);
            }
            trace[0] = CaptureAtLeaf();
        }

#if !UNITY_WEBGL
        [Test]
        public void CausalityTraceCapture_AfterContinuingOnTheThreadPool_IncludesWhereItWasScheduledFrom()
        {
            var backgroundContext = Promise.Config.BackgroundContext;
            // Without a background context, background continuations run on the thread pool.
            Promise.Config.BackgroundContext = null;
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                var promise = CaptureAfterAwaitOnBackgroundForTraceTest(deferred.Promise, t => trace = t);
                ResolveForTraceTest(deferred);
                Assert.IsTrue(promise.TryWait(System.TimeSpan.FromSeconds(10)));

                var text = trace.ToString();
                Assert.IsTrue(trace.IsThreadPoolThread, text);
                Assert.IsNotNull(OuterTrace(trace), text);
                Assert.IsTrue(ContainsMethod(OuterTrace(trace), nameof(ResolveForTraceTest)), text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.BackgroundContext = backgroundContext;
            }
        }

        private static async Promise CaptureAfterAwaitOnBackgroundForTraceTest(Promise promise, System.Action<CausalityTrace> onCaptured)
        {
            await promise.ConfigureAwait(ContinuationOptions.Background);
            onCaptured(CaptureAtLeaf());
        }

        // Captures a trace on a new unnamed thread (the threads of ThreadHelper have names).
        private static CausalityTrace CaptureOnNewThread(bool isBackground, CausalityTrace previous)
            => CaptureOnNewThreadWith(isBackground, () => CaptureInScopeForTraceTest(previous), out _);

        private static CausalityTrace CaptureOnNewThreadWith(bool isBackground, System.Func<CausalityTrace> capture, out System.Threading.Thread thread)
        {
            CausalityTrace trace = null;
            System.Runtime.ExceptionServices.ExceptionDispatchInfo exception = null;
            thread = new System.Threading.Thread(() =>
            {
                try
                {
                    trace = capture();
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

#if !PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't skip frames, so the innermost frame of every trace is the same, and no trace contains another.
        [Test]
        public void CausalityTraceCapture_InsideTheScopeOfATraceWithTheOuterFramesOnAnotherThread_KeepsIt()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var previous = CaptureFromTheSameLineOnNewThread(null, false, out var previousThread);
                var trace = CaptureFromTheSameLineOnNewThread(previous, true, out _);
                // A thread's id may be reused after its Thread is collected.
                System.GC.KeepAlive(previousThread);

                // The previous trace's frames are the outer frames of the trace, but they were on another thread's stack, so they're not the calls that are running.
                var frames = trace.GetFrames();
                var previousFrames = previous.GetFrames();
                Assert.Greater(frames.Length, previousFrames.Length);
                for (int i = 1; i <= previousFrames.Length; ++i)
                {
                    Assert.AreEqual(previousFrames[previousFrames.Length - i].GetMethod(), frames[frames.Length - i].GetMethod());
                    Assert.AreEqual(previousFrames[previousFrames.Length - i].GetFileLineNumber(), frames[frames.Length - i].GetFileLineNumber());
                }
                Assert.AreNotEqual(previous.ThreadId, trace.ThreadId);
                Assert.AreSame(previous, trace.Previous);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static CausalityTrace CaptureFromTheSameLineOnNewThread(CausalityTrace previous, bool deeper, out System.Threading.Thread thread)
            => CaptureOnNewThreadWith(false, () => CaptureFromTheSameLineForTraceTest(previous, deeper), out thread);
#endif // !PROTO_PROMISE_DEVELOPER_MODE

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
            // Not disposed, because the thread pool may still run the callback after the wait timed out.
            var captured = new System.Threading.ManualResetEventSlim();
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
            Assert.IsTrue(captured.Wait(System.TimeSpan.FromSeconds(10)), "The thread pool did not run the callback in time.");
            exception?.Throw();
            return trace;
        }

        // Captures traces on thread pool threads, each inside the scope of the previous one. Each thread is blocked until every trace is captured,
        // so that each trace is captured on a different thread.
        private static CausalityTrace[] CaptureOnDifferentThreadPoolThreads(CausalityTrace previous, int count)
        {
            var traces = new CausalityTrace[count];
            // The events are not disposed, because the threads may still be waiting on them after they're set, or use them after a wait timed out.
            var release = new System.Threading.ManualResetEventSlim();
            try
            {
                for (int i = 0; i < count; ++i)
                {
                    int index = i;
                    var scopeTrace = i == 0 ? previous : traces[i - 1];
                    var captured = new System.Threading.ManualResetEventSlim();
                    System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            traces[index] = CaptureInScopeForTraceTest(scopeTrace);
                        }
                        finally
                        {
                            captured.Set();
                        }
                        release.Wait();
                    });
                    Assert.IsTrue(captured.Wait(System.TimeSpan.FromSeconds(10)), "The thread pool did not run the callback in time.");
                    Assert.IsNotNull(traces[i]);
                }
            }
            finally
            {
                release.Set();
            }
            return traces;
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
                Assert.IsFalse(ContainsMethod(trace, nameof(ResolveForTraceTest)), trace.ToString());
                Assert.IsNotNull(OuterTrace(trace), trace.ToString());
                Assert.IsTrue(ContainsMethod(OuterTrace(trace), nameof(ResolveForTraceTest)), trace.ToString());
                // It only has its own stack trace.
                Assert.IsNull(OuterTrace(trace).Previous);
                Assert.IsNull(OuterTrace(OuterTrace(trace)));
                CollectionAssert.IsEmpty(OuterTrace(trace).Repeats);
                // The async function is still the previous trace.
                AssertPreviousTraceIsFrom(trace, nameof(CausalityTraceCapture_InContinuationOnAnotherContext_IncludesWhereTheContinuationWasScheduled));
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

                Assert.IsFalse(ContainsMethod(trace, nameof(ResolveForTraceTest)), trace.ToString());
                Assert.IsNotNull(OuterTrace(trace), trace.ToString());
                Assert.IsTrue(ContainsMethod(OuterTrace(trace), nameof(ResolveForTraceTest)), trace.ToString());
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
                    // The callback's operation is still the previous trace.
                    AssertPreviousTraceIsFrom(trace, nameof(UnhandledRejection_ExceptionThrownInCallback_TraceIncludesWhatInvokedTheCallback));
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
                    // Where the async function was called is still the previous trace.
                    AssertPreviousTraceIsFrom(trace, nameof(UnhandledRejection_ExceptionThrownInAsyncFunction_TraceIncludesWhatContinuedTheFunction));
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
                    AssertPreviousTraceIsFrom(trace, nameof(UnhandledRejection_ExceptionThrownInCatchContinueWithOrFinally_TraceIncludesWhatInvokedTheCallback));
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
                    AssertPreviousTraceIsFrom(trace, nameof(UnhandledRejection_ExceptionThrownInPromiseNewOnAContext_TraceIncludesWhatRanTheCallback));
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
                    Assert.IsNull(trace.Previous, trace.ToString());
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
                    Assert.IsNull(trace.Previous, trace.ToString());
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

#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
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
                // The trace of where it was scheduled is the previous trace, so it's not also the trace it was scheduled from.
                Assert.IsNull(OuterTrace(trace), trace.ToString());
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

#if !UNITY_WEBGL
        [Test]
        public void CausalityTraceCapture_AfterAwaitingACompletedPromiseConfiguredToTheThreadPoolAsValueTask_IncludesWhatRanTheAwait()
        {
            var backgroundContext = Promise.Config.BackgroundContext;
            // Without a background context, background continuations run on the thread pool.
            Promise.Config.BackgroundContext = null;
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                System.Threading.Tasks.Task task = null;
                // The value task's source has no trace of its own, so the await relies on the trace captured where it was scheduled.
                deferred.Promise
                    .Then(() => { task = CaptureAfterAwaitingValueTaskForTraceTest(Promise.Resolved().ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous)), captured => trace = captured); })
                    .Forget();
                ResolveForTraceTest(deferred);
                Assert.IsTrue(task.Wait(System.TimeSpan.FromSeconds(10)));

                Assert.IsTrue(trace.IsThreadPoolThread, trace.ToString());
                Assert.IsTrue(HistoryContainsMethod(trace, nameof(ResolveForTraceTest)), trace.ToString());
                // The trace of where it was scheduled is the previous trace, so it's not also the trace it was scheduled from.
                Assert.IsNull(OuterTrace(trace), trace.ToString());
                AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.BackgroundContext = backgroundContext;
            }
        }
#endif

        // Awaits the promise as a value task, whose source has no trace of its own, so the trace captured after the await only has the traces of the scopes that the continuation ran in.
        private static async System.Threading.Tasks.Task CaptureAfterAwaitingAsValueTaskForTraceTest<T>(Promise<T> promise, System.Action<CausalityTrace> onCaptured)
        {
            try
            {
                // ConfigureAwait(false), so that the continuation runs synchronously where the promise is completed.
                await promise.AsValueTask().ConfigureAwait(false);
            }
            catch (System.Exception)
            {
                // The promise may be canceled or rejected.
            }
            onCaptured(CausalityTrace.Capture());
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise<int>.Deferred NewDeferredForTraceTest()
        {
            var deferred = Promise<int>.NewDeferred();
            Volatile.Write(ref s_frameKeeper, 23);
            return deferred;
        }

        [Test]
        public void DeferredCompletion_ContinuationsInvokedSynchronously_RunInsideTheDeferredScope(
            [Values] bool cancel)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = NewDeferredForTraceTest();
                CausalityTrace trace = null;
                _ = CaptureAfterAwaitingAsValueTaskForTraceTest(deferred.Promise, captured => trace = captured);
                if (cancel)
                {
                    deferred.Cancel();
                }
                else
                {
                    deferred.Resolve(1);
                }

                Assert.IsNotNull(trace);
                Assert.IsTrue(HistoryContainsMethod(trace, nameof(NewDeferredForTraceTest)), trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        public enum ChannelCompletionForTraceTest
        {
            Cancel,
            Close,
            Dispose
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Proto.Promises.Channels.Channel<int> NewChannelForTraceTest()
        {
            var channel = Proto.Promises.Channels.Channel<int>.NewUnbounded();
            Volatile.Write(ref s_frameKeeper, 24);
            return channel;
        }

        [Test]
        public void ChannelCompletion_ContinuationsInvokedSynchronously_RunInsideTheChannelScope(
            [Values] ChannelCompletionForTraceTest completion)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var channel = NewChannelForTraceTest();
                try
                {
                    CausalityTrace trace = null;
                    _ = CaptureAfterAwaitingAsValueTaskForTraceTest(channel.Reader.ReadAsync(false), captured => trace = captured);
                    switch (completion)
                    {
                        case ChannelCompletionForTraceTest.Cancel:
                            channel.Writer.TryCancel();
                            break;
                        case ChannelCompletionForTraceTest.Close:
                            channel.Writer.TryClose();
                            break;
                        default:
                            channel.Dispose();
                            break;
                    }

                    Assert.IsNotNull(trace);
                    Assert.IsTrue(HistoryContainsMethod(trace, nameof(NewChannelForTraceTest)), trace.ToString());
                }
                finally
                {
                    if (completion != ChannelCompletionForTraceTest.Dispose)
                    {
                        channel.Dispose();
                    }
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }
#endif // UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ExecuteForTraceTest(QueuedContext context)
        {
            context.Execute();
            Volatile.Write(ref s_frameKeeper, 15);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ReportProgressForTraceTest(ProgressToken token)
        {
            token.Report(0.5);
            Volatile.Write(ref s_frameKeeper, 16);
        }

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
                var runPromise = Promise.Run(() => { trace = CausalityTrace.Capture(); }, context);
                var runTrace = runPromise._ref.Trace;
                runPromise.Forget();
                context.Execute();

                // The run promise's trace is the only cause; scheduling the callback when the promise was created doesn't add another.
                Assert.IsNull(runTrace.Previous, runTrace.ToString());
                AssertPreviousTrace(runTrace, trace);
                Assert.IsNull(OuterTrace(trace), trace.ToString());
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
                var thenPromise = configured.Then(() => { trace = CausalityTrace.Capture(); });
                var thenTrace = thenPromise._ref.Trace;
                thenPromise.Forget();
                context.Execute();

                // The callback promise's trace is the only cause; scheduling the callback when it was hooked up doesn't add another.
                Assert.IsNull(thenTrace.Previous, thenTrace.ToString());
                AssertPreviousTrace(thenTrace, trace);
                Assert.IsNull(OuterTrace(trace), trace.ToString());
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
        {
            deferred.Resolve();
            Volatile.Write(ref s_frameKeeper, 17);
        }

        // Whether the method is in the frames of the trace or its previous traces, or their outer traces.
        private static bool HistoryContainsMethod(CausalityTrace trace, string methodName)
        {
            for (var current = trace; current != null; current = current.Previous)
            {
                if (ContainsMethod(current, methodName) || current.OuterTraces.Any(outer => ContainsMethod(outer, methodName)))
                {
                    return true;
                }
            }
            return false;
        }

        // The trace's only outer trace (in these tests, where the callback that captured it was scheduled from), or null if it has none.
        private static CausalityTrace OuterTrace(CausalityTrace trace)
        {
            Assert.LessOrEqual(trace.OuterTraces.Count, 1, trace.ToString());
            return trace.OuterTraces.Count == 0 ? null : trace.OuterTraces[0];
        }

        // Whether the traces have the same visible frames, by their methods and lines.
        private static bool HasSameFrames(CausalityTrace trace, CausalityTrace other)
            => trace.GetFrames().Select(frame => (frame.GetMethod(), frame.GetFileLineNumber()))
                .SequenceEqual(other.GetFrames().Select(frame => (frame.GetMethod(), frame.GetFileLineNumber())));

        private static bool ContainsMethod(CausalityTrace trace, string methodName)
            => trace.GetFrames().Any(frame => frame.GetMethod().Name == methodName);

        // The NoInlining helpers whose frames the tests look for write a different value to this after their calls, so that IL2CPP's C++ compiler
        // doesn't turn the calls into tail calls or fold helpers with the same code together, which would remove their frames from the traces.
        // It's a volatile write, so that it can't be moved before the call.
        private static int s_frameKeeper;

        // Without line numbers (e.g. IL2CPP only prints method names by default), a trace that was captured earlier in a method that is still running
        // is printed like the outer frames of a trace captured in that method, so it's skipped (see CausalityTrace.Previous).
        private static readonly bool s_hasLineNumbers = new System.Diagnostics.StackFrame(0, true).GetFileLineNumber() != 0;

        // Whether an outer trace that was captured earlier in a method that is still running is kept (e.g. where a callback was scheduled from,
        // when a context runs the callback from that method, or where a deferred was created, when that method resolves it).
        // Without line numbers and accurate IL offsets (IL2CPP's default), it's printed like the outer frames of the trace, so it's dropped (see CausalityTrace.OuterTraces).
        // Developer mode doesn't hide internal frames, so it's never printed like the outer frames of the trace.
#if ENABLE_IL2CPP && !PROTO_PROMISE_DEVELOPER_MODE
        private static bool KeepsOuterTracesFromTheSameMethod => s_hasLineNumbers;
#else
        private static bool KeepsOuterTracesFromTheSameMethod => true;
#endif

        // The expected outer traces, which were captured earlier in the test method that is still running (e.g. where a deferred was created),
        // so they're dropped if they're printed like the outer frames of the trace (see KeepsOuterTracesFromTheSameMethod).
        private static CausalityTrace[] OuterTracesFromTheSameMethod(params CausalityTrace[] traces)
            => KeepsOuterTracesFromTheSameMethod ? traces : new CausalityTrace[0];

        // Whether a hidden frame may be printed with the name of another function, so that a trace that's printed like the outer frames of another trace is not skipped.
        // IL2CPP's C++ linker in Unity versions before 2021.2 folds some library functions with identical functions, whose names are printed in their place
        // (e.g. UnityEngine.ImageConversion.LoadImage in Unity 2019.4 on Windows).
#if ENABLE_IL2CPP && !UNITY_2021_2_OR_NEWER && !PROTO_PROMISE_DEVELOPER_MODE
        private static bool MayPrintHiddenFramesWithOtherNames => true;
#else
        private static bool MayPrintHiddenFramesWithOtherNames => false;
#endif

        // Asserts that the previous trace of the trace is the expected trace, or, without line numbers, that it was skipped.
        private static void AssertPreviousTrace(CausalityTrace expected, CausalityTrace trace)
        {
            if (s_hasLineNumbers || trace.Previous == expected)
            {
                Assert.AreSame(expected, trace.Previous, trace.ToString());
            }
            else
            {
                Assert.AreSame(expected.Previous, trace.Previous, trace.ToString());
            }
        }

        // Asserts that the previous trace of the trace was captured in the method, or, without line numbers, that it was skipped.
        // The callers capture the previous trace without an outer scope, so the trace has no previous trace if it was skipped.
        private static void AssertPreviousTraceIsFrom(CausalityTrace trace, string methodName)
        {
            if (s_hasLineNumbers || (trace.Previous != null && ContainsMethod(trace.Previous, methodName)))
            {
                Assert.IsTrue(ContainsMethod(trace.Previous, methodName), trace.ToString());
            }
            else
            {
                Assert.IsTrue(ContainsMethod(trace, methodName), trace.ToString());
                Assert.IsNull(trace.Previous, trace.ToString());
            }
        }

        [Test]
        public void CausalityTraceCapture_InACallbackOfAPromiseCreatedInTheSameScheduledCallback_DoesNotIncludeWhereItWasScheduledTwice()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                var trace = new CausalityTrace[1];
                CaptureInCallbackAfterAwaitOnContextForTraceTest(deferred.Promise, context, trace).Forget();
                ResolveForTraceTest(deferred);
                context.Execute();

                // The callback's trace and its promise's trace were both captured after the async function continued on the context,
                // so they were scheduled from the same trace, which is printed once.
                var text = trace[0].ToString();
                Assert.AreEqual(1, CountOccurrences(text, nameof(ResolveForTraceTest)), text);
                AssertCausalityTraceIsPrintedCorrectly(trace[0], text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async Promise CaptureInCallbackAfterAwaitOnContextForTraceTest(Promise promise, System.Threading.SynchronizationContext context, CausalityTrace[] trace)
        {
            await promise.ConfigureAwait(new ContinuationOptions(context));
            var deferred = Promise.NewDeferred();
            var thenPromise = deferred.Promise.Then(() => { trace[0] = CausalityTrace.Capture(); });
            deferred.Resolve();
            await thenPromise;
        }

        [Test]
        public void CausalityTraceCapture_PreviousTraceScheduledFromTheSameTrace_OuterTraceIsOnlyKeptIfThePreviousTraceIsTruncated(
            [Values] bool isTruncated)
        {
            int originalMaxDepth = CausalityTrace.MaxDepth;
            // A max depth of 2 keeps only the trace when the chain is truncated.
            CausalityTrace.MaxDepth = isTruncated ? 2 : originalMaxDepth;
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var scheduledFrom = CaptureAtStep();
                CausalityTrace previous, trace;
                // Both traces are captured in the same callback.
                var outerScope = CausalityTrace.EnterScope(scheduledFrom);
                try
                {
                    previous = CaptureInScopeForTraceTest(other);
                    var scope = CausalityTrace.EnterScope(previous);
                    try
                    {
                        trace = CaptureAtLeaf();
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                }
                finally
                {
                    outerScope.Dispose();
                }

                var text = trace.ToString();
                Assert.AreSame(scheduledFrom, OuterTrace(previous), text);
                if (isTruncated)
                {
                    Assert.IsNull(trace.Previous, text);
                    Assert.AreSame(scheduledFrom, OuterTrace(trace), text);
                }
                else
                {
                    Assert.AreSame(previous, trace.Previous, text);
                    Assert.IsNull(OuterTrace(trace), text);
                }
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtStep)), text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.MaxDepth = originalMaxDepth;
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_AfterAContextRanACallbackSynchronously_IncludesWhereTheOuterCallbackWasScheduledFrom()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                var traces = new CausalityTrace[2];
                CaptureAroundAContinuationOnAnInlineContextForTraceTest(deferred.Promise, context, traces).Forget();
                ResolveForTraceTest(deferred);
                context.Execute();

                // Where the outer callback was scheduled from is restored after the callback that the other context ran synchronously.
                Assert.IsNotNull(OuterTrace(traces[0]), traces[0].ToString());
                Assert.AreSame(OuterTrace(traces[0]), OuterTrace(traces[1]), traces[1].ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async Promise CaptureAroundAContinuationOnAnInlineContextForTraceTest(Promise promise, QueuedContext context, CausalityTrace[] traces)
        {
            await promise.ConfigureAwait(new ContinuationOptions(context));
            traces[0] = CaptureAtLeaf();
            var deferred = Promise.NewDeferred();
            AwaitOnAnInlineContextForTraceTest(deferred.Promise).Forget();
            deferred.Resolve();
            traces[1] = CaptureAtStep();
        }

        private static async Promise AwaitOnAnInlineContextForTraceTest(Promise promise)
        {
            await promise.ConfigureAwait(new ContinuationOptions(new InlineContext(), forceAsync: true));
        }

        // Runs the posted callbacks synchronously.
        private sealed class InlineContext : System.Threading.SynchronizationContext
        {
            public override void Post(System.Threading.SendOrPostCallback d, object state)
                => d.Invoke(state);
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

        // Copies the trace with another previous trace, so that the copy has the same stack trace.
        private static CausalityTrace CopyTrace(CausalityTrace trace, bool isTruncated, CausalityTrace previous = null)
            => new CausalityTrace(trace, previous, isTruncated);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtLeaf()
        {
            var trace = CausalityTrace.Capture();
            Volatile.Write(ref s_frameKeeper, 18);
            return trace;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtLoop()
        {
            var trace = CausalityTrace.Capture();
            Volatile.Write(ref s_frameKeeper, 19);
            return trace;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtStep()
        {
            var trace = CausalityTrace.Capture();
            Volatile.Write(ref s_frameKeeper, 20);
            return trace;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtRoot()
        {
            var trace = CausalityTrace.Capture();
            Volatile.Write(ref s_frameKeeper, 21);
            return trace;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtOther()
        {
            var trace = CausalityTrace.Capture();
            Volatile.Write(ref s_frameKeeper, 22);
            return trace;
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

        // Repeated blocks must be labeled once, and printed before they are repeated.
        // Every stack trace in the chain, and every one of their outer traces (indented one more level than the one before it), must be printed (as a whole),
        // and the truncation must be printed if the chain is truncated.
        private static void AssertCausalityTraceIsPrintedCorrectly(CausalityTrace trace, string text)
        {
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text, @"--- #(\d+) repeated (\d+) times?(?:: .*?)? ---"))
            {
                var number = match.Groups[1].Value;
                int beginIndex = text.IndexOf("--- #" + number + " Begin ---");
                int endIndex = text.IndexOf("--- #" + number + " End ---");
                Assert.GreaterOrEqual(beginIndex, 0, "Missing begin label for " + match.Value + ": " + text);
                Assert.Greater(endIndex, beginIndex, "Missing end label for " + match.Value + ": " + text);
                Assert.Less(endIndex, match.Index, match.Value + " must be after its block: " + text);
                Assert.AreEqual(1, CountOccurrences(text, "--- #" + number + " Begin ---"), "Duplicate begin label for " + match.Value + ": " + text);
                Assert.AreNotEqual("0", match.Groups[2].Value, text);
            }
            var openLabels = new Stack<string>();
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text, @"--- #(\d+) (Begin|End) ---"))
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
            var chain = new List<CausalityTrace>();
            for (var current = trace; current != null; current = current.Previous)
            {
                chain.Add(current);
                AssertStackTraceIsPrinted(current, textLines, text, "");
                string indent = "";
                foreach (var outer in current.OuterTraces)
                {
                    indent += "   ";
                    AssertStackTraceIsPrinted(outer, textLines, text, indent);
                    // Outer traces don't have their own histories, so that a chain of causes is only printed once.
                    Assert.IsNull(outer.Previous, text);
                    CollectionAssert.IsEmpty(outer.OuterTraces, text);
                    CollectionAssert.IsEmpty(outer.Repeats, text);
                }
            }
            Assert.AreEqual(chain[chain.Count - 1].IsTruncated, text.Contains("Older traces truncated"), text);
            Assert.AreEqual(chain.Any(current => current.AreOuterTracesTruncated), text.Contains("Outer traces truncated"), text);

            // Each trace's thread and time is printed at most once with its stack trace and once with each of its outer traces,
            // and each group of repeats has at most two times (including the traces that are folded as repeats),
            // so the output can't grow faster than the number of traces, their outer traces and their repeats.
            int timeCount = System.Text.RegularExpressions.Regex.Matches(text, @"\d+\.\d{3}(?= ms earlier| to )").Count;
            Assert.LessOrEqual(timeCount, chain.Sum(current => 4 + current.OuterTraces.Count + current.Repeats.Count * 2), text);
        }

        private static void AssertStackTraceIsPrinted(CausalityTrace trace, string[] textLines, string text, string indent)
        {
            var stackTraceText = string.Join(System.Environment.NewLine,
                trace.GetFrames().Select(frame => indent + new System.Diagnostics.StackTrace(frame).ToString().TrimEnd('\r', '\n')));
            var stackTraceLines = stackTraceText.Length == 0
                ? new[] { indent + "   --- All frames are hidden ---" }
                : stackTraceText.Split(new[] { System.Environment.NewLine }, System.StringSplitOptions.None);
            Assert.IsTrue(ContainsBlock(textLines, stackTraceLines), "Missing stack trace:" + System.Environment.NewLine + stackTraceText + System.Environment.NewLine + "in:" + System.Environment.NewLine + text);
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

        // Asserts that the traces are printed the same, other than the threads and times.
        private static void AssertPrintedTheSame(string expected, string actual)
            => Assert.AreEqual(WithoutThreadsAndTimes(expected), WithoutThreadsAndTimes(actual), expected + System.Environment.NewLine + "----" + System.Environment.NewLine + actual);

        // Removes the threads and times of repeats, and replaces the times of the stack traces.
        private static string WithoutThreadsAndTimes(string text)
        {
            text = System.Text.RegularExpressions.Regex.Replace(text, @"[:;] (?:Thread|thread pool) .*?(?= ---\r?$)", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            return System.Text.RegularExpressions.Regex.Replace(text, @"\d+\.\d{3} ms earlier", "0.000 ms earlier");
        }
    }
}