#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class APlus_2_3_ThePromiseResolutionProcedure
    {
#if PROMISE_DEBUG
        [Test]
        public void _2_3_1_IfPromiseAndXReferToTheSameObjectRejectPromiseWithInvalidReturnExceptionAsTheReason()
        {
            Promise promise = null;
            Promise<int> promiseInt = null;
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            deferred.Retain();
            deferred.Resolve();
            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);
            deferredInt.Retain();
            deferredInt.Resolve(0);

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");


            promise = deferred.Promise.Then(() => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => promise, () => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => promise, (object failValue) => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => promise, () => rejectAssert());
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => promise, (object failValue) => rejectAssert());
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferred.Promise.Then(() => promiseInt);
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt, () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt, (object failValue) => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt, () => { rejectAssert(); return 0; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt, (object failValue) => { rejectAssert(); return 0; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);


            promise = deferredInt.Promise.Then(v => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise, () => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise, (object failValue) => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise, () => rejectAssert());
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise, (object failValue) => rejectAssert());
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferredInt.Promise.Then(v => promiseInt);
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt, () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt, (object failValue) => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt, () => { rejectAssert(); return 0; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt, (object failValue) => { rejectAssert(); return 0; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);


            promise = deferred.Promise.Complete(() => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promiseInt = deferred.Promise.Complete(() => promiseInt);
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);


            deferred.Release();
            deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            deferred.Retain();
            deferred.Reject("Fail value");
            deferredInt.Release();
            deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);
            deferredInt.Retain();
            deferredInt.Reject("Fail value");


            promise = deferred.Promise.Catch(() => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Catch((object failValue) => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => { resolveAssert(); return promise; }, () => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => { resolveAssert(); return promise; }, (object failValue) => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => resolveAssert(), () => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => resolveAssert(), (object failValue) => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferredInt.Promise.Catch(() => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Catch((object failValue) => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return promiseInt; }, () => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return promiseInt; }, (object failValue) => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return 0; }, () => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return 0; }, (object failValue) => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);


            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return promiseInt; }, () => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return promiseInt; }, (object failValue) => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return 0; }, () => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return 0; }, (object failValue) => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);

            deferred.Release();
            deferredInt.Release();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }
#endif

        public class _2_3_2_IfXIsAPromiseAdoptItsState
        {
            [Test]
            public void _2_3_2_1_IfXIsPendingPromiseMustRemainPendingUntilXIsFulfilledOrRejected()
            {
                Promise promise;
                Promise<int> promiseInt;
                int expectedCompleteCount = 0;
                int completeCounter = 0;

                var resolveDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, resolveDeferred.State);
                var rejectDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, rejectDeferred.State);
                var resolveDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, resolveDeferredInt.State);
                var rejectDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, rejectDeferredInt.State);

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                var resolveWaitDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, resolveWaitDeferred.State);
                var resolveWaitDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, resolveWaitDeferredInt.State);
                var rejectWaitDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, rejectWaitDeferred.State);
                var rejectWaitDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, rejectWaitDeferredInt.State);


                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise, () => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise, () => rejectAssert());
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise, (object failValue) => rejectAssert());
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());

                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return 0; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return 0; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());

                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise, () => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise, () => rejectAssert());
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise, (object failValue) => rejectAssert());
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);

                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return 0; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return 0; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);


                resolveDeferred.Resolve();
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise, () => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise, () => rejectAssert());
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise, (object failValue) => rejectAssert());
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());

                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return 0; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return 0; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());

                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise, () => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise, () => rejectAssert());
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise, (object failValue) => rejectAssert());
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);

                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return 0; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return 0; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);


                resolveDeferredInt.Resolve(0);
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                promise = rejectDeferred.Promise.Catch(() => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Catch((object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferred.Promise; }, () => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferred.Promise; }, (object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => resolveAssert(), () => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => resolveAssert(), (object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return 0; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return 0; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());

                promise = rejectDeferred.Promise.Catch(() => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = rejectDeferred.Promise.Catch((object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferred.Promise; }, () => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferred.Promise; }, (object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = rejectDeferred.Promise.Then(() => resolveAssert(), () => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = rejectDeferred.Promise.Then(() => resolveAssert(), (object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return 0; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return 0; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++completeCounter, () => ++completeCounter);


                rejectDeferred.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                promiseInt = rejectDeferredInt.Promise.Catch(() => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Catch((object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());

                promise = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferred.Promise; }, () => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferred.Promise; }, (object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferredInt.Promise.Then(v => resolveAssert(), () => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferredInt.Promise.Then(v => resolveAssert(), (object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => ++completeCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return 0; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return 0; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return 0; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return 0; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++completeCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Catch(() => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Catch((object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);

                promise = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferred.Promise; }, () => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promise = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferred.Promise; }, (object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promise = rejectDeferredInt.Promise.Then(v => resolveAssert(), () => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promise = rejectDeferredInt.Promise.Then(v => resolveAssert(), (object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks<object>(promise, () => resolveAssert(), s => ++completeCounter, () => ++completeCounter);

                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return 0; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return 0; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);

                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return 0; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return 0; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++completeCounter, () => ++completeCounter);



                rejectDeferredInt.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferred.Resolve();
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.resolveVoidCallbacks + TestHelper.completeCallbacks) * 20;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferredInt.Resolve(0);
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.resolveTCallbacks + TestHelper.completeCallbacks) * 24;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferred.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.rejectVoidCallbacks + TestHelper.completeCallbacks) * 20;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferredInt.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.rejectTCallbacks + TestHelper.completeCallbacks) * 22;
                Assert.AreEqual(expectedCompleteCount, completeCounter);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void _2_3_2_2_IfWhenXIsFulfilledFulfillPromiseWithTheSameValue()
            {
                Promise promise;
                Promise<int> promiseInt;

                var resolveDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, resolveDeferred.State);
                resolveDeferred.Retain();
                resolveDeferred.Resolve();
                Assert.AreEqual(Promise.State.Resolved, resolveDeferred.State);

                var rejectDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, rejectDeferred.State);
                rejectDeferred.Retain();
                rejectDeferred.Reject("Fail value");
                Assert.AreEqual(Promise.State.Rejected, rejectDeferred.State);

                var resolveDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, resolveDeferredInt.State);
                resolveDeferredInt.Retain();
                resolveDeferredInt.Resolve(0);
                Assert.AreEqual(Promise.State.Resolved, resolveDeferredInt.State);

                var rejectDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, rejectDeferredInt.State);
                rejectDeferredInt.Retain();
                rejectDeferredInt.Reject("Fail value");
                Assert.AreEqual(Promise.State.Rejected, rejectDeferredInt.State);

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                var resolveWaitDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, resolveWaitDeferred.State);
                resolveWaitDeferred.Retain();
                var resolveWaitDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, resolveWaitDeferredInt.State);
                resolveWaitDeferredInt.Retain();

                // Test pending -> resolved and already resolved.
                bool firstRun = true;
            RunAgain:
                int resolveCounter = 0;

                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise, () => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise, () => rejectAssert());
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise, (object failValue) => rejectAssert());
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());

                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return 0; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return 0; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());


                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise, () => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise, () => rejectAssert());
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise, (object failValue) => rejectAssert());
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());

                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return 0; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return 0; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());


                promise = rejectDeferred.Promise.Catch(() => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Catch((object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferred.Promise; }, () => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferred.Promise; }, (object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => resolveAssert(), () => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => resolveAssert(), (object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, () => ++resolveCounter, s => rejectAssert());


                promiseInt = rejectDeferredInt.Promise.Catch(() => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Catch((object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return 0; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return 0; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return 0; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return 0; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => ++resolveCounter, s => rejectAssert());

                Assert.AreEqual(0, resolveCounter);


                if (firstRun)
                {
                    resolveWaitDeferred.Resolve();
                    resolveWaitDeferredInt.Resolve(0);
                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(TestHelper.resolveVoidCallbacks * 16 + TestHelper.resolveTCallbacks * 20, resolveCounter);
                    firstRun = false;
                    goto RunAgain;
                }

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(TestHelper.resolveVoidCallbacks * 16 + TestHelper.resolveTCallbacks * 20, resolveCounter);

                resolveDeferred.Release();
                resolveDeferredInt.Release();
                rejectDeferred.Release();
                rejectDeferredInt.Release();

                resolveWaitDeferred.Release();
                resolveWaitDeferredInt.Release();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void _2_3_2_3_IfWhenXIsRejectedRejectPromiseWithTheSameReason()
            {
                Promise promise;
                Promise<int> promiseInt;

                var resolveDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, resolveDeferred.State);
                resolveDeferred.Retain();
                resolveDeferred.Resolve();
                Assert.AreEqual(Promise.State.Resolved, resolveDeferred.State);

                var rejectDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, rejectDeferred.State);
                rejectDeferred.Retain();
                rejectDeferred.Reject("Fail value");
                Assert.AreEqual(Promise.State.Rejected, rejectDeferred.State);

                var resolveDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, resolveDeferredInt.State);
                resolveDeferredInt.Retain();
                resolveDeferredInt.Resolve(0);
                Assert.AreEqual(Promise.State.Resolved, resolveDeferredInt.State);

                var rejectDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, rejectDeferredInt.State);
                rejectDeferredInt.Retain();
                rejectDeferredInt.Reject("Fail value");
                Assert.AreEqual(Promise.State.Rejected, rejectDeferredInt.State);

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                var rejectWaitDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, rejectWaitDeferred.State);
                rejectWaitDeferred.Retain();
                var rejectWaitDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, rejectWaitDeferredInt.State);
                rejectWaitDeferredInt.Retain();

                // Test pending -> resolved and already resolved.
                bool firstRun = true;
            RunAgain:
                int rejectCounter = 0;

                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);
                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise, () => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);
                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);
                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise, () => rejectAssert());
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);
                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise, (object failValue) => rejectAssert());
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);

                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return 0; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return 0; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);


                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);
                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise, () => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);
                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);
                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise, () => rejectAssert());
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);
                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise, (object failValue) => rejectAssert());
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);

                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return 0; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return 0; });
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);


                promise = rejectDeferred.Promise.Catch(() => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);
                promise = rejectDeferred.Promise.Catch((object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferred.Promise; }, () => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferred.Promise; }, (object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);
                promise = rejectDeferred.Promise.Then(() => resolveAssert(), () => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);
                promise = rejectDeferred.Promise.Then(() => resolveAssert(), (object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks<object>(promise, resolveAssert, s => ++rejectCounter, () => ++rejectCounter);


                promiseInt = rejectDeferredInt.Promise.Catch(() => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Catch((object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);

                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return 0; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return 0; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);


                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return 0; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return 0; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks<int, object>(promiseInt, v => resolveAssert(), s => ++rejectCounter, () => ++rejectCounter);

                Assert.AreEqual(0, rejectCounter);


                if (firstRun)
                {
                    rejectWaitDeferred.Reject("Fail value");
                    rejectWaitDeferredInt.Reject("Fail value");
                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(TestHelper.rejectVoidCallbacks * 16 + TestHelper.rejectTCallbacks * 20, rejectCounter);
                    firstRun = false;
                    goto RunAgain;
                }

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(TestHelper.rejectVoidCallbacks * 16 + TestHelper.rejectTCallbacks * 20, rejectCounter);

                resolveDeferred.Release();
                resolveDeferredInt.Release();
                rejectDeferred.Release();
                rejectDeferredInt.Release();

                rejectWaitDeferred.Release();
                rejectWaitDeferredInt.Release();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        // Not supported. You may alternatively "return Promise.New(deferred => {...});".
        // 2.3.3 if X is a function...

        [Test]
        public void _2_3_4_IfOnResolvedOrOnRejectedReturnsSuccessfullyResolvePromise()
        {
            var resolveDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, resolveDeferred.State);
            resolveDeferred.Retain();
            resolveDeferred.Resolve();
            Assert.AreEqual(Promise.State.Resolved, resolveDeferred.State);

            var rejectDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, rejectDeferred.State);
            rejectDeferred.Retain();
            rejectDeferred.Reject("Fail value");
            Assert.AreEqual(Promise.State.Rejected, rejectDeferred.State);

            var resolveDeferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, resolveDeferredInt.State);
            resolveDeferredInt.Retain();
            resolveDeferredInt.Resolve(0);
            Assert.AreEqual(Promise.State.Resolved, resolveDeferredInt.State);

            var rejectDeferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, rejectDeferredInt.State);
            rejectDeferredInt.Retain();
            rejectDeferredInt.Reject("Fail value");
            Assert.AreEqual(Promise.State.Rejected, rejectDeferredInt.State);

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

            Action<Promise> assertResolved = promise =>
            {
                int resolveCount = 0;
                TestHelper.AddCallbacks<object>(promise,
                    () => ++resolveCount,
                    o => rejectAssert(),
                    () => rejectAssert());
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(TestHelper.resolveVoidCallbacks, resolveCount);
            };

            assertResolved.Invoke(resolveDeferred.Promise.Then(() => { }));
            assertResolved.Invoke(resolveDeferred.Promise.Then(() => { }, () => rejectAssert()));
            assertResolved.Invoke(resolveDeferred.Promise.Then<object>(() => { }, failValue => rejectAssert()));
            assertResolved.Invoke(resolveDeferred.Promise.Then(() => { }, () => { rejectAssert(); return default(Promise); }));
            assertResolved.Invoke(resolveDeferred.Promise.Then<object>(() => { }, failValue => { rejectAssert(); return default(Promise); }));

            assertResolved.Invoke(rejectDeferred.Promise.Then(() => { resolveAssert(); return default(Promise); }, () => { }));
            assertResolved.Invoke(rejectDeferred.Promise.Then<object>(() => { resolveAssert(); return default(Promise); }, failValue => { }));
            assertResolved.Invoke(rejectDeferred.Promise.Then(() => resolveAssert(), () => { }));
            assertResolved.Invoke(rejectDeferred.Promise.Then<object>(() => resolveAssert(), failValue => { }));
            assertResolved.Invoke(rejectDeferred.Promise.Catch(() => { }));
            assertResolved.Invoke(rejectDeferred.Promise.Catch<object>(failValue => { }));

            assertResolved.Invoke(resolveDeferredInt.Promise.Then(v => { }));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then(v => { }, () => rejectAssert()));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then<object>(v => { }, failValue => rejectAssert()));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then(v => { }, () => { rejectAssert(); return default(Promise); }));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then<object>(v => { }, failValue => { rejectAssert(); return default(Promise); }));

            assertResolved.Invoke(rejectDeferredInt.Promise.Then(v => resolveAssert(), () => { }));
            assertResolved.Invoke(rejectDeferredInt.Promise.Then<object>(v => resolveAssert(), failValue => { }));
            assertResolved.Invoke(rejectDeferredInt.Promise.Then(v => { resolveAssert(); return default(Promise); }, () => { }));
            assertResolved.Invoke(rejectDeferredInt.Promise.Then<object>(v => { resolveAssert(); return default(Promise); }, failValue => { }));
            assertResolved.Invoke(rejectDeferredInt.Promise.Catch(() => { }));
            assertResolved.Invoke(rejectDeferredInt.Promise.Catch<object>(failValue => { }));


            resolveDeferred.Release();
            resolveDeferredInt.Release();
            rejectDeferred.Release();
            rejectDeferredInt.Release();

            Promise.Manager.HandleCompletes();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void _2_3_4_IfXIsNotAPromiseOrAFunctionFulfillPromiseWithX()
        {
            var resolveDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, resolveDeferred.State);
            resolveDeferred.Retain();
            resolveDeferred.Resolve();
            Assert.AreEqual(Promise.State.Resolved, resolveDeferred.State);

            var rejectDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, rejectDeferred.State);
            rejectDeferred.Retain();
            rejectDeferred.Reject("Fail value");
            Assert.AreEqual(Promise.State.Rejected, rejectDeferred.State);

            var resolveDeferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, resolveDeferredInt.State);
            resolveDeferredInt.Retain();
            resolveDeferredInt.Resolve(0);
            Assert.AreEqual(Promise.State.Resolved, resolveDeferredInt.State);

            var rejectDeferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, rejectDeferredInt.State);
            rejectDeferredInt.Retain();
            rejectDeferredInt.Reject("Fail value");
            Assert.AreEqual(Promise.State.Rejected, rejectDeferredInt.State);

            int expected = 100;

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

            Action<Promise<int>> assertResolved = promise =>
            {
                int resolveCount = 0;
                TestHelper.AddCallbacks<int, object>(promise,
                    v => { Assert.AreEqual(expected, v); ++resolveCount; },
                    o => rejectAssert(),
                    () => rejectAssert());
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(TestHelper.resolveTCallbacks, resolveCount);
            };

            assertResolved.Invoke(resolveDeferred.Promise.Then<int>(() => expected));
            assertResolved.Invoke(resolveDeferred.Promise.Then<int>(() => expected, () => { rejectAssert(); return 0; }));
            assertResolved.Invoke(resolveDeferred.Promise.Then<int, object>(() => expected, failValue => { rejectAssert(); return 0; }));
            assertResolved.Invoke(resolveDeferred.Promise.Then<int>(() => expected, () => { rejectAssert(); return default(Promise<int>); }));
            assertResolved.Invoke(resolveDeferred.Promise.Then<int, object>(() => expected, failValue => { rejectAssert(); return default(Promise<int>); }));

            assertResolved.Invoke(rejectDeferred.Promise.Then<int>(() => { resolveAssert(); return 0; }, () => expected));
            assertResolved.Invoke(rejectDeferred.Promise.Then<int, object>(() => { resolveAssert(); return 0; }, failValue => expected));
            assertResolved.Invoke(rejectDeferred.Promise.Then<int>(() => { resolveAssert(); return default(Promise<int>); }, () => expected));
            assertResolved.Invoke(rejectDeferred.Promise.Then<int, object>(() => { resolveAssert(); return default(Promise<int>); }, failValue => expected));

            assertResolved.Invoke(resolveDeferredInt.Promise.Then<int>(v => expected));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then<int>(v => expected, () => { rejectAssert(); return 0; }));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then<int, object>(v => expected, failValue => { rejectAssert(); return 0; }));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then<int>(v => expected, () => { rejectAssert(); return default(Promise<int>); }));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then<int, object>(v => expected, failValue => { rejectAssert(); return default(Promise<int>); }));

            assertResolved.Invoke(rejectDeferredInt.Promise.Then<int>(v => { resolveAssert(); return 0; }, () => expected));
            assertResolved.Invoke(rejectDeferredInt.Promise.Then<int, object>(v => { resolveAssert(); return 0; }, failValue => expected));
            assertResolved.Invoke(rejectDeferredInt.Promise.Then<int>(v => { resolveAssert(); return default(Promise<int>); }, () => expected));
            assertResolved.Invoke(rejectDeferredInt.Promise.Then<int, object>(v => { resolveAssert(); return default(Promise<int>); }, failValue => expected));
            assertResolved.Invoke(rejectDeferredInt.Promise.Catch(() => expected));
            assertResolved.Invoke(rejectDeferredInt.Promise.Catch<object>(failValue => expected));


            resolveDeferred.Release();
            resolveDeferredInt.Release();
            rejectDeferred.Release();
            rejectDeferredInt.Release();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        // If a promise is resolved with a thenable that participates in a circular thenable chain, such that the recursive
        // nature of[[Resolve]](promise, thenable) eventually causes[[Resolve]](promise, thenable) to be
        // called again, following the above algorithm will lead to infinite recursion.Implementations are encouraged, but
        // not required, to detect such recursion and reject promise with an informative Exception as the reason.

#if PROMISE_DEBUG
        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChainRejectPromiseWithInvalidReturnExceptionAsTheReason()
        {
            Promise promise = null;
            Promise<int> promiseInt = null;
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            deferred.Retain();
            deferred.Resolve();
            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);
            deferredInt.Retain();
            deferredInt.Resolve(0);

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");


            promise = deferred.Promise.Then(() => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }), () => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }), (object failValue) => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }), () => rejectAssert());
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }), (object failValue) => rejectAssert());
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferred.Promise.Then(() => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0), () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0), (object failValue) => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0), () => { rejectAssert(); return 0; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0), (object failValue) => { rejectAssert(); return 0; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);


            promise = deferredInt.Promise.Then(v => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }), () => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }), (object failValue) => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }), () => rejectAssert());
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }), (object failValue) => rejectAssert());
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferredInt.Promise.Then(v => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0), () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0), (object failValue) => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0), () => { rejectAssert(); return 0; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0), (object failValue) => { rejectAssert(); return 0; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);


            promise = deferred.Promise.Complete(() => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promiseInt = deferred.Promise.Complete(() => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);


            deferred.Release();
            deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            deferred.Retain();
            deferred.Reject("Fail value");
            deferredInt.Release();
            deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);
            deferredInt.Retain();
            deferredInt.Reject("Fail value");


            promise = deferred.Promise.Catch(() => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Catch((object failValue) => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => { resolveAssert(); return promise; }, () => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => { resolveAssert(); return promise; }, (object failValue) => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => resolveAssert(), () => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => resolveAssert(), (object failValue) => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferredInt.Promise.Catch(() => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Catch((object failValue) => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return promiseInt; }, () => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return promiseInt; }, (object failValue) => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return 0; }, () => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return 0; }, (object failValue) => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);


            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return promiseInt; }, () => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return promiseInt; }, (object failValue) => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return 0; }, () => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return 0; }, (object failValue) => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);

            deferred.Release();
            deferredInt.Release();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }
#endif
    }
}