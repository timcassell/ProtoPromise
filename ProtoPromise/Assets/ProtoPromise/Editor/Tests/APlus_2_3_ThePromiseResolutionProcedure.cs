using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class APlus_2_3_ThePromiseResolutionProcedure
    {
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
            promise = deferred.Promise.Then<object>(() => promise, () => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferred.Promise.Then(() => promiseInt);
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt, () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt, (object failValue) => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then<int, object>(() => promiseInt, () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);


            promise = deferredInt.Promise.Then(v => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise, () => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise, (object failValue) => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then<object>(v => promise, () => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferredInt.Promise.Then(v => promiseInt);
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt, () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt, (object failValue) => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then<int, object>(v => promiseInt, () => { rejectAssert(); return promiseInt; });
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
            promise = deferred.Promise.Catch<object>(() => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => { resolveAssert(); return promise; }, () => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => { resolveAssert(); return promise; }, (object failValue) => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then<object>(() => { resolveAssert(); return promise; }, () => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferredInt.Promise.Catch(() => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Catch((object failValue) => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Catch<object>(() => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return promiseInt; }, () => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return promiseInt; }, (object failValue) => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then<int, object>(() => { resolveAssert(); return promiseInt; }, () => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);


            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return promiseInt; }, () => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return promiseInt; }, (object failValue) => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then<int, object>(v => { resolveAssert(); return promiseInt; }, () => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);

            deferred.Release();
            deferredInt.Release();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

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
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise, () => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then<object>(() => resolveWaitDeferred.Promise, () => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());

                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then<int, object>(() => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());

                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise, () => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = resolveDeferred.Promise.Then<object>(() => rejectWaitDeferred.Promise, () => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);

                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = resolveDeferred.Promise.Then<int, object>(() => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);


                resolveDeferred.Resolve();
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise, () => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then<object>(v => resolveWaitDeferred.Promise, () => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());

                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then<int, object>(v => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());

                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise, () => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = resolveDeferredInt.Promise.Then<object>(v => rejectWaitDeferred.Promise, () => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);

                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = resolveDeferredInt.Promise.Then<int, object>(v => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);


                resolveDeferredInt.Resolve(0);
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                promise = rejectDeferred.Promise.Catch(() => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Catch((object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Catch<object>(() => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferred.Promise; }, () => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferred.Promise; }, (object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then<object>(() => { resolveAssert(); return resolveWaitDeferred.Promise; }, () => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then<object>(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());

                promise = rejectDeferred.Promise.Catch(() => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = rejectDeferred.Promise.Catch((object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = rejectDeferred.Promise.Catch<object>(() => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferred.Promise; }, () => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferred.Promise; }, (object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = rejectDeferred.Promise.Then<object>(() => { resolveAssert(); return rejectWaitDeferred.Promise; }, () => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = rejectDeferred.Promise.Then<object>(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);


                rejectDeferred.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                promiseInt = rejectDeferredInt.Promise.Catch(() => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Catch((object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Catch<object>(() => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());

                promise = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferred.Promise; }, () => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferred.Promise; }, (object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferredInt.Promise.Then<object>(v => { resolveAssert(); return resolveWaitDeferred.Promise; }, () => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then<int, object>(v => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then<int, object>(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Catch(() => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Catch((object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Catch<object>(() => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);

                promise = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferred.Promise; }, () => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => resolveAssert(), s => ++completeCounter);
                promise = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferred.Promise; }, (object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => resolveAssert(), s => ++completeCounter);
                promise = rejectDeferredInt.Promise.Then<object>(v => { resolveAssert(); return rejectWaitDeferred.Promise; }, () => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => resolveAssert(), s => ++completeCounter);

                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Then<int, object>(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);

                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Then<int, object>(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);



                rejectDeferredInt.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferred.Resolve();
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.resolveVoidCallbacks + TestHelper.completeCallbacks) * 17;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferredInt.Resolve(0);
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.resolveTCallbacks + TestHelper.completeCallbacks) * 20;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferred.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.rejectVoidCallbacks + TestHelper.completeCallbacks) * 17;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferredInt.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.rejectTCallbacks + TestHelper.completeCallbacks) * 20;
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
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise, () => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then<object>(() => resolveWaitDeferred.Promise, () => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());

                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then<int, object>(() => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());


                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise, () => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then<object>(v => resolveWaitDeferred.Promise, () => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());

                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then<int, object>(v => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());


                promise = rejectDeferred.Promise.Catch(() => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Catch((object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Catch<object>(() => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferred.Promise; }, () => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferred.Promise; }, (object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then<object>(() => { resolveAssert(); return resolveWaitDeferred.Promise; }, () => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());


                promiseInt = rejectDeferredInt.Promise.Catch(() => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Catch((object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Catch<object>(() => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then<int, object>(v => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then<int, object>(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());

                Assert.AreEqual(0, resolveCounter);


                if (firstRun)
                {
                    resolveWaitDeferred.Resolve();
                    resolveWaitDeferredInt.Resolve(0);
                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(TestHelper.resolveVoidCallbacks * 14 + TestHelper.resolveTCallbacks * 17, resolveCounter);
                    firstRun = false;
                    goto RunAgain;
                }

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(TestHelper.resolveVoidCallbacks * 14 + TestHelper.resolveTCallbacks * 17, resolveCounter);

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
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);
                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise, () => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);
                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);
                promise = resolveDeferred.Promise.Then<object>(() => rejectWaitDeferred.Promise, () => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);

                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = resolveDeferred.Promise.Then<int, object>(() => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);


                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);
                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise, () => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);
                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);
                promise = resolveDeferredInt.Promise.Then<object>(v => rejectWaitDeferred.Promise, () => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);

                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = resolveDeferredInt.Promise.Then<int, object>(v => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);


                promise = rejectDeferred.Promise.Catch(() => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);
                promise = rejectDeferred.Promise.Catch((object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);
                promise = rejectDeferred.Promise.Catch<object>(() => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferred.Promise; }, () => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferred.Promise; }, (object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);
                promise = rejectDeferred.Promise.Then<object>(() => { resolveAssert(); return rejectWaitDeferred.Promise; }, () => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);


                promiseInt = rejectDeferredInt.Promise.Catch(() => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Catch((object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Catch<object>(() => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);

                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Then<int, object>(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);


                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Then<int, object>(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);

                Assert.AreEqual(0, rejectCounter);


                if (firstRun)
                {
                    rejectWaitDeferred.Reject("Fail value");
                    rejectWaitDeferredInt.Reject("Fail value");
                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(TestHelper.rejectVoidCallbacks * 14 + TestHelper.rejectTCallbacks * 17, rejectCounter);
                    firstRun = false;
                    goto RunAgain;
                }

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(TestHelper.rejectVoidCallbacks * 14 + TestHelper.rejectTCallbacks * 17, rejectCounter);

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

        public class _2_3_3_IfXIsAFunction
        {
            public class _2_3_3_3_IfXIsAFunctionCallItWithADeferred
            {
                [Test]
                public void _2_3_3_3_1_IfWhenDeferredIsResolvedResolvePromise()
                {
                    Promise promise;

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

                    Action<Promise.Deferred> resolveAction = deferred => deferred.Resolve();

                    // Test pending -> resolved and already resolved.
                    bool firstRun = true;
                RunAgain:
                    int resolveCounter = 0;

                    promise = resolveDeferred.Promise.ThenDefer(() => resolveAction);
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                    promise = resolveDeferred.Promise.ThenDefer(() => resolveAction, () => { rejectAssert(); return resolveAction; });
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                    promise = resolveDeferred.Promise.ThenDefer<object>(() => resolveAction, failValue => { rejectAssert(); return resolveAction; });
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                    promise = resolveDeferred.Promise.ThenDefer<object>(() => resolveAction, () => { rejectAssert(); return resolveAction; });
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());


                    promise = resolveDeferredInt.Promise.ThenDefer(v => resolveAction);
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                    promise = resolveDeferredInt.Promise.ThenDefer(v => resolveAction, () => { rejectAssert(); return resolveAction; });
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                    promise = resolveDeferredInt.Promise.ThenDefer<object>(v => resolveAction, failValue => { rejectAssert(); return resolveAction; });
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                    promise = resolveDeferredInt.Promise.ThenDefer<object>(v => resolveAction, () => { rejectAssert(); return resolveAction; });
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());


                    promise = rejectDeferred.Promise.CatchDefer(() => resolveAction);
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                    promise = rejectDeferred.Promise.CatchDefer<object>(failValue => resolveAction);
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                    promise = rejectDeferred.Promise.CatchDefer<object>(() => resolveAction);
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());

                    promise = rejectDeferred.Promise.ThenDefer(() => { resolveAssert(); return resolveAction; }, () => resolveAction);
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                    promise = rejectDeferred.Promise.ThenDefer<object>(() => { resolveAssert(); return resolveAction; }, failValue => resolveAction);
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                    promise = rejectDeferred.Promise.ThenDefer<object>(() => { resolveAssert(); return resolveAction; }, () => resolveAction);
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());


                    promise = rejectDeferredInt.Promise.ThenDefer(v => { resolveAssert(); return resolveAction; }, () => resolveAction);
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                    promise = rejectDeferredInt.Promise.ThenDefer<object>(v => { resolveAssert(); return resolveAction; }, failValue => resolveAction);
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                    promise = rejectDeferredInt.Promise.ThenDefer<object>(v => { resolveAssert(); return resolveAction; }, () => resolveAction);
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());

                    Assert.AreEqual(0, resolveCounter);


                    int expectedCount = TestHelper.resolveVoidCallbacks * 17;

                    if (firstRun)
                    {
                        Promise.Manager.HandleCompletes();

                        Assert.AreEqual(expectedCount, resolveCounter);
                        firstRun = false;
                        goto RunAgain;
                    }

                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(expectedCount, resolveCounter);

                    resolveDeferred.Release();
                    resolveDeferredInt.Release();
                    rejectDeferred.Release();
                    rejectDeferredInt.Release();

                    // Clean up.
                    GC.Collect();
                    Promise.Manager.HandleCompletes();
                    LogAssert.NoUnexpectedReceived();
                }

                [Test]
                public void _2_3_3_3_1_IfWhenDeferredIsResolvedWithAValueResolvePromiseWithThatValue()
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

                    int expected = 100;

                    Action<Promise.Deferred> resolveAction = deferred => deferred.Resolve();
                    Action<Promise<int>.Deferred> resolveActionInt = deferred => deferred.Resolve(expected);

                    // Test pending -> resolved and already resolved.
                    bool firstRun = true;
                RunAgain:
                    int resolveCounter = 0;

                    promiseInt = resolveDeferred.Promise.ThenDefer<int>(() => resolveActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());
                    promiseInt = resolveDeferred.Promise.ThenDefer<int>(() => resolveActionInt, () => { rejectAssert(); return resolveActionInt; });
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());
                    promiseInt = resolveDeferred.Promise.ThenDefer<int, object>(() => resolveActionInt, failValue => { rejectAssert(); return resolveActionInt; });
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());
                    promiseInt = resolveDeferred.Promise.ThenDefer<int, object>(() => resolveActionInt, () => { rejectAssert(); return resolveActionInt; });
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());


                    promiseInt = resolveDeferredInt.Promise.ThenDefer<int>(v => resolveActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());
                    promiseInt = resolveDeferredInt.Promise.ThenDefer<int>(v => resolveActionInt, () => { rejectAssert(); return resolveActionInt; });
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());
                    promiseInt = resolveDeferredInt.Promise.ThenDefer<int, object>(v => resolveActionInt, failValue => { rejectAssert(); return resolveActionInt; });
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());
                    promiseInt = resolveDeferredInt.Promise.ThenDefer<int, object>(v => resolveActionInt, () => { rejectAssert(); return resolveActionInt; });
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());


                    promiseInt = rejectDeferred.Promise.ThenDefer<int>(() => { resolveAssert(); return resolveActionInt; }, () => resolveActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());
                    promiseInt = rejectDeferred.Promise.ThenDefer<int, object>(() => { resolveAssert(); return resolveActionInt; }, failValue => resolveActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());
                    promiseInt = rejectDeferred.Promise.ThenDefer<int, object>(() => { resolveAssert(); return resolveActionInt; }, () => resolveActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());


                    promise = rejectDeferredInt.Promise.CatchDefer(() => resolveAction);
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                    promise = rejectDeferredInt.Promise.CatchDefer<object>(failValue => resolveAction);
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                    promise = rejectDeferredInt.Promise.CatchDefer<object>(() => resolveAction);
                    TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());

                    promiseInt = rejectDeferredInt.Promise.CatchDefer(() => resolveActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());
                    promiseInt = rejectDeferredInt.Promise.CatchDefer<object>(failValue => resolveActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());
                    promiseInt = rejectDeferredInt.Promise.CatchDefer<object>(() => resolveActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());

                    promiseInt = rejectDeferredInt.Promise.ThenDefer<int>(v => { resolveAssert(); return resolveActionInt; }, () => resolveActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());
                    promiseInt = rejectDeferredInt.Promise.ThenDefer<int, object>(v => { resolveAssert(); return resolveActionInt; }, failValue => resolveActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());
                    promiseInt = rejectDeferredInt.Promise.ThenDefer<int, object>(v => { resolveAssert(); return resolveActionInt; }, () => resolveActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => { Assert.AreEqual(expected, v); ++resolveCounter; }, s => rejectAssert());

                    Assert.AreEqual(0, resolveCounter);


                    int expectedCount = TestHelper.resolveVoidCallbacks * 3 + TestHelper.resolveTCallbacks * 17;

                    if (firstRun)
                    {
                        Promise.Manager.HandleCompletes();

                        Assert.AreEqual(expectedCount, resolveCounter);
                        firstRun = false;
                        goto RunAgain;
                    }

                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(expectedCount, resolveCounter);

                    resolveDeferred.Release();
                    resolveDeferredInt.Release();
                    rejectDeferred.Release();
                    rejectDeferredInt.Release();

                    // Clean up.
                    GC.Collect();
                    Promise.Manager.HandleCompletes();
                    LogAssert.NoUnexpectedReceived();
                }

                [Test]
                public void _2_3_3_3_2_IfWhenDeferredIsRejectedWithAReasonRejectPromiseWithThatReason()
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

                    string expected = "Reject value";

                    Action<Promise.Deferred> rejectAction = deferred => deferred.Reject(expected);
                    Action<Promise<int>.Deferred> rejectActionInt = deferred => deferred.Reject(expected);

                    // Test pending -> rejected and already rejected.
                    bool firstRun = true;
                RunAgain:
                    int rejectCounter = 0;

                    promise = resolveDeferred.Promise.ThenDefer(() => rejectAction);
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promise = resolveDeferred.Promise.ThenDefer(() => rejectAction, () => { rejectAssert(); return rejectAction; });
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promise = resolveDeferred.Promise.ThenDefer<object>(() => rejectAction, failValue => { rejectAssert(); return rejectAction; });
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promise = resolveDeferred.Promise.ThenDefer<object>(() => rejectAction, () => { rejectAssert(); return rejectAction; });
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);


                    promise = resolveDeferredInt.Promise.ThenDefer(v => rejectAction);
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promise = resolveDeferredInt.Promise.ThenDefer(v => rejectAction, () => { rejectAssert(); return rejectAction; });
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promise = resolveDeferredInt.Promise.ThenDefer<object>(v => rejectAction, failValue => { rejectAssert(); return rejectAction; });
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promise = resolveDeferredInt.Promise.ThenDefer<object>(v => rejectAction, () => { rejectAssert(); return rejectAction; });
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);


                    promise = rejectDeferred.Promise.CatchDefer(() => rejectAction);
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promise = rejectDeferred.Promise.CatchDefer<object>(failValue => rejectAction);
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promise = rejectDeferred.Promise.CatchDefer<object>(() => rejectAction);
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);

                    promise = rejectDeferred.Promise.ThenDefer(() => { resolveAssert(); return rejectAction; }, () => rejectAction);
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promise = rejectDeferred.Promise.ThenDefer<object>(() => { resolveAssert(); return rejectAction; }, failValue => rejectAction);
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promise = rejectDeferred.Promise.ThenDefer<object>(() => { resolveAssert(); return rejectAction; }, () => rejectAction);
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);


                    promise = rejectDeferredInt.Promise.ThenDefer(v => { resolveAssert(); return rejectAction; }, () => rejectAction);
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promise = rejectDeferredInt.Promise.ThenDefer<object>(v => { resolveAssert(); return rejectAction; }, failValue => rejectAction);
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promise = rejectDeferredInt.Promise.ThenDefer<object>(v => { resolveAssert(); return rejectAction; }, () => rejectAction);
                    TestHelper.AddCallbacks(promise, resolveAssert, s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);

                    promiseInt = resolveDeferred.Promise.ThenDefer<int>(() => rejectActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promiseInt = resolveDeferred.Promise.ThenDefer<int>(() => rejectActionInt, () => { rejectAssert(); return rejectActionInt; });
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promiseInt = resolveDeferred.Promise.ThenDefer<int, object>(() => rejectActionInt, failValue => { rejectAssert(); return rejectActionInt; });
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promiseInt = resolveDeferred.Promise.ThenDefer<int, object>(() => rejectActionInt, () => { rejectAssert(); return rejectActionInt; });
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);


                    promiseInt = resolveDeferredInt.Promise.ThenDefer<int>(v => rejectActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promiseInt = resolveDeferredInt.Promise.ThenDefer<int>(v => rejectActionInt, () => { rejectAssert(); return rejectActionInt; });
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promiseInt = resolveDeferredInt.Promise.ThenDefer<int, object>(v => rejectActionInt, failValue => { rejectAssert(); return rejectActionInt; });
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promiseInt = resolveDeferredInt.Promise.ThenDefer<int, object>(v => rejectActionInt, () => { rejectAssert(); return rejectActionInt; });
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);


                    promiseInt = rejectDeferred.Promise.ThenDefer<int>(() => { resolveAssert(); return rejectActionInt; }, () => rejectActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promiseInt = rejectDeferred.Promise.ThenDefer<int, object>(() => { resolveAssert(); return rejectActionInt; }, failValue => rejectActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promiseInt = rejectDeferred.Promise.ThenDefer<int, object>(() => { resolveAssert(); return rejectActionInt; }, () => rejectActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);


                    promise = rejectDeferredInt.Promise.CatchDefer(() => rejectAction);
                    TestHelper.AddCallbacks(promise, () => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promise = rejectDeferredInt.Promise.CatchDefer<object>(failValue => rejectAction);
                    TestHelper.AddCallbacks(promise, () => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promise = rejectDeferredInt.Promise.CatchDefer<object>(() => rejectAction);
                    TestHelper.AddCallbacks(promise, () => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);

                    promiseInt = rejectDeferredInt.Promise.CatchDefer(() => rejectActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promiseInt = rejectDeferredInt.Promise.CatchDefer<object>(failValue => rejectActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promiseInt = rejectDeferredInt.Promise.CatchDefer<object>(() => rejectActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);

                    promiseInt = rejectDeferredInt.Promise.ThenDefer<int>(v => { resolveAssert(); return rejectActionInt; }, () => rejectActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promiseInt = rejectDeferredInt.Promise.ThenDefer<int, object>(v => { resolveAssert(); return rejectActionInt; }, failValue => rejectActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);
                    promiseInt = rejectDeferredInt.Promise.ThenDefer<int, object>(v => { resolveAssert(); return rejectActionInt; }, () => rejectActionInt);
                    TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => { Assert.AreEqual(expected, s); ++rejectCounter; }, expected);

                    Assert.AreEqual(0, rejectCounter);


                    int expectedCount = TestHelper.rejectVoidCallbacks * 17 + TestHelper.rejectTCallbacks * 20;

                    if (firstRun)
                    {
                        Promise.Manager.HandleCompletes();

                        Assert.AreEqual(expectedCount, rejectCounter);
                        firstRun = false;
                        goto RunAgain;
                    }

                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(expectedCount, rejectCounter);

                    resolveDeferred.Release();
                    resolveDeferredInt.Release();
                    rejectDeferred.Release();
                    rejectDeferredInt.Release();

                    // Clean up.
                    GC.Collect();
                    Promise.Manager.HandleCompletes();
                    LogAssert.NoUnexpectedReceived();
                }

                [Test]
                public void _2_3_3_3_3_IfWhenDeferredIsResolvedAnyFurtherCallsToResolveOrRejectAreIgnored()
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

                    Promise.Deferred waitDeferred = null;
                    Action<Promise.Deferred> deferredAction = deferred => {
                        waitDeferred = deferred;
                        Assert.AreEqual(Promise.State.Pending, waitDeferred.State);
                    };
                    Promise<int>.Deferred waitDeferredInt = null;
                    Action<Promise<int>.Deferred> deferredIntAction = deferred => {
                        waitDeferredInt = deferred;
                        Assert.AreEqual(Promise.State.Pending, waitDeferredInt.State);
                    };

                    Promise.State expectedState = Promise.State.Pending;

                    Action ignoreResolveAction = () => {
                        LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");
                        waitDeferred.Resolve();
                        Assert.AreEqual(expectedState, waitDeferred.State);
                        Promise.Manager.HandleCompletes();
                    };
                    Action ignoreRejectAction = () => {
                        LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");
                        waitDeferred.Reject("Fail value");
                        Assert.AreEqual(expectedState, waitDeferred.State);
                        Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);
                    };
                    Action ignoreResolveIntAction = () => {
                        LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");
                        waitDeferredInt.Resolve(0);
                        Assert.AreEqual(expectedState, waitDeferred.State);
                        Promise.Manager.HandleCompletes();
                    };
                    Action ignoreRejectIntAction = () => {
                        LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");
                        waitDeferredInt.Reject("Fail value");
                        Assert.AreEqual(expectedState, waitDeferred.State);
                        Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);
                    };

                    Action<Promise> assertResolved = promise => {
                        promise.Retain();
                        Promise.Manager.HandleCompletes();
                        expectedState = Promise.State.Resolved;
                        waitDeferred.Retain();
                        waitDeferred.Resolve();
                        TestHelper.AssertIgnore(promise, TestHelper.resolveVoidCallbacks, 0, ignoreRejectAction, ignoreResolveAction, ignoreRejectAction, ignoreResolveAction);
                        waitDeferred.Release();
                        promise.Release();
                    };
                    Action<Promise<int>> assertResolvedInt = promiseInt => {
                        promiseInt.Retain();
                        Promise.Manager.HandleCompletes();
                        expectedState = Promise.State.Resolved;
                        waitDeferredInt.Retain();
                        waitDeferredInt.Resolve(0);
                        TestHelper.AssertIgnore(promiseInt, TestHelper.resolveTCallbacks, 0, ignoreRejectIntAction, ignoreResolveIntAction, ignoreRejectIntAction, ignoreResolveIntAction);
                        waitDeferredInt.Release();
                        promiseInt.Release();
                    };

                    assertResolved.Invoke(resolveDeferred.Promise.ThenDefer(() => deferredAction));
                    assertResolved.Invoke(resolveDeferred.Promise.ThenDefer(() => deferredAction, () => deferredAction));
                    assertResolved.Invoke(resolveDeferred.Promise.ThenDefer<object>(() => deferredAction, failValue => deferredAction));
                    assertResolved.Invoke(resolveDeferred.Promise.ThenDefer<object>(() => deferredAction, () => deferredAction));
                    assertResolvedInt.Invoke(resolveDeferred.Promise.ThenDefer<int>(() => deferredIntAction));
                    assertResolvedInt.Invoke(resolveDeferred.Promise.ThenDefer<int>(() => deferredIntAction, () => deferredIntAction));
                    assertResolvedInt.Invoke(resolveDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, failValue => deferredIntAction));
                    assertResolvedInt.Invoke(resolveDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, () => deferredIntAction));

                    assertResolved.Invoke(rejectDeferred.Promise.ThenDefer(() => deferredAction, () => deferredAction));
                    assertResolved.Invoke(rejectDeferred.Promise.ThenDefer<object>(() => deferredAction, failValue => deferredAction));
                    assertResolved.Invoke(rejectDeferred.Promise.ThenDefer<object>(() => deferredAction, () => deferredAction));
                    assertResolved.Invoke(rejectDeferred.Promise.CatchDefer(() => deferredAction));
                    assertResolved.Invoke(rejectDeferred.Promise.CatchDefer<object>(failValue => deferredAction));
                    assertResolved.Invoke(rejectDeferred.Promise.CatchDefer<object>(() => deferredAction));
                    assertResolvedInt.Invoke(rejectDeferred.Promise.ThenDefer<int>(() => deferredIntAction, () => deferredIntAction));
                    assertResolvedInt.Invoke(rejectDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, failValue => deferredIntAction));
                    assertResolvedInt.Invoke(rejectDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, () => deferredIntAction));

                    assertResolved.Invoke(resolveDeferredInt.Promise.ThenDefer(v => deferredAction));
                    assertResolved.Invoke(resolveDeferredInt.Promise.ThenDefer(v => deferredAction, () => deferredAction));
                    assertResolved.Invoke(resolveDeferredInt.Promise.ThenDefer<object>(v => deferredAction, failValue => deferredAction));
                    assertResolved.Invoke(resolveDeferredInt.Promise.ThenDefer<object>(v => deferredAction, () => deferredAction));
                    assertResolvedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction));
                    assertResolvedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction, () => deferredIntAction));
                    assertResolvedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, failValue => deferredIntAction));
                    assertResolvedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, () => deferredIntAction));

                    assertResolved.Invoke(rejectDeferredInt.Promise.ThenDefer(v => deferredAction, () => deferredAction));
                    assertResolved.Invoke(rejectDeferredInt.Promise.ThenDefer<object>(v => deferredAction, failValue => deferredAction));
                    assertResolved.Invoke(rejectDeferredInt.Promise.ThenDefer<object>(v => deferredAction, () => deferredAction));
                    assertResolved.Invoke(rejectDeferredInt.Promise.CatchDefer(() => deferredAction));
                    assertResolved.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(failValue => deferredAction));
                    assertResolved.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(() => deferredAction));
                    assertResolvedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction, () => deferredIntAction));
                    assertResolvedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, failValue => deferredIntAction));
                    assertResolvedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, () => deferredIntAction));
                    assertResolvedInt.Invoke(rejectDeferredInt.Promise.CatchDefer(() => deferredIntAction));
                    assertResolvedInt.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(failValue => deferredIntAction));
                    assertResolvedInt.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(() => deferredIntAction));


                    resolveDeferred.Release();
                    resolveDeferredInt.Release();
                    rejectDeferred.Release();
                    rejectDeferredInt.Release();

                    // Clean up.
                    GC.Collect();
                    Promise.Manager.HandleCompletes();
                    LogAssert.NoUnexpectedReceived();
                }

                [Test]
                public void _2_3_3_3_3_IfWhenDeferredIsRejectedAnyFurtherCallsToResolveOrRejectAreIgnored()
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

                    Promise.Deferred waitDeferred = null;
                    Action<Promise.Deferred> deferredAction = deferred => {
                        waitDeferred = deferred;
                        Assert.AreEqual(Promise.State.Pending, waitDeferred.State);
                    };
                    Promise<int>.Deferred waitDeferredInt = null;
                    Action<Promise<int>.Deferred> deferredIntAction = deferred => {
                        waitDeferredInt = deferred;
                        Assert.AreEqual(Promise.State.Pending, waitDeferredInt.State);
                    };

                    Promise.State expectedState = Promise.State.Pending;

                    Action ignoreResolveAction = () => {
                        LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");
                        waitDeferred.Resolve();
                        Assert.AreEqual(expectedState, waitDeferred.State);
                        Promise.Manager.HandleCompletes();
                    };
                    Action ignoreRejectAction = () => {
                        LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");
                        waitDeferred.Reject("Fail value");
                        Assert.AreEqual(expectedState, waitDeferred.State);
                        Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);
                    };
                    Action ignoreResolveIntAction = () => {
                        LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");
                        waitDeferredInt.Resolve(0);
                        Assert.AreEqual(expectedState, waitDeferred.State);
                        Promise.Manager.HandleCompletes();
                    };
                    Action ignoreRejectIntAction = () => {
                        LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");
                        waitDeferredInt.Reject("Fail value");
                        Assert.AreEqual(expectedState, waitDeferred.State);
                        Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);
                    };

                    Action<Promise> assertRejected = promise => {
                        promise.Retain();
                        Promise.Manager.HandleCompletes();
                        expectedState = Promise.State.Rejected;
                        waitDeferred.Retain();
                        waitDeferred.Reject("Fail value");
                        TestHelper.AssertIgnore(promise, 0, TestHelper.rejectVoidCallbacks, ignoreRejectAction, ignoreResolveAction, ignoreRejectAction, ignoreResolveAction);
                        waitDeferred.Release();
                        promise.Release();
                    };
                    Action<Promise<int>> assertRejectedInt = promiseInt => {
                        promiseInt.Retain();
                        Promise.Manager.HandleCompletes();
                        expectedState = Promise.State.Rejected;
                        waitDeferredInt.Retain();
                        waitDeferredInt.Reject("Fail value");
                        TestHelper.AssertIgnore(promiseInt, 0, TestHelper.rejectTCallbacks, ignoreRejectIntAction, ignoreResolveIntAction, ignoreRejectIntAction, ignoreResolveIntAction);
                        waitDeferredInt.Release();
                        promiseInt.Release();
                    };

                    assertRejected.Invoke(resolveDeferred.Promise.ThenDefer(() => deferredAction));
                    assertRejected.Invoke(resolveDeferred.Promise.ThenDefer(() => deferredAction, () => deferredAction));
                    assertRejected.Invoke(resolveDeferred.Promise.ThenDefer<object>(() => deferredAction, failValue => deferredAction));
                    assertRejected.Invoke(resolveDeferred.Promise.ThenDefer<object>(() => deferredAction, () => deferredAction));
                    assertRejectedInt.Invoke(resolveDeferred.Promise.ThenDefer<int>(() => deferredIntAction));
                    assertRejectedInt.Invoke(resolveDeferred.Promise.ThenDefer<int>(() => deferredIntAction, () => deferredIntAction));
                    assertRejectedInt.Invoke(resolveDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, failValue => deferredIntAction));
                    assertRejectedInt.Invoke(resolveDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, () => deferredIntAction));

                    assertRejected.Invoke(rejectDeferred.Promise.ThenDefer(() => deferredAction, () => deferredAction));
                    assertRejected.Invoke(rejectDeferred.Promise.ThenDefer<object>(() => deferredAction, failValue => deferredAction));
                    assertRejected.Invoke(rejectDeferred.Promise.ThenDefer<object>(() => deferredAction, () => deferredAction));
                    assertRejected.Invoke(rejectDeferred.Promise.CatchDefer(() => deferredAction));
                    assertRejected.Invoke(rejectDeferred.Promise.CatchDefer<object>(failValue => deferredAction));
                    assertRejected.Invoke(rejectDeferred.Promise.CatchDefer<object>(() => deferredAction));
                    assertRejectedInt.Invoke(rejectDeferred.Promise.ThenDefer<int>(() => deferredIntAction, () => deferredIntAction));
                    assertRejectedInt.Invoke(rejectDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, failValue => deferredIntAction));
                    assertRejectedInt.Invoke(rejectDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, () => deferredIntAction));

                    assertRejected.Invoke(resolveDeferredInt.Promise.ThenDefer(v => deferredAction));
                    assertRejected.Invoke(resolveDeferredInt.Promise.ThenDefer(v => deferredAction, () => deferredAction));
                    assertRejected.Invoke(resolveDeferredInt.Promise.ThenDefer<object>(v => deferredAction, failValue => deferredAction));
                    assertRejected.Invoke(resolveDeferredInt.Promise.ThenDefer<object>(v => deferredAction, () => deferredAction));
                    assertRejectedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction));
                    assertRejectedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction, () => deferredIntAction));
                    assertRejectedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, failValue => deferredIntAction));
                    assertRejectedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, () => deferredIntAction));

                    assertRejected.Invoke(rejectDeferredInt.Promise.ThenDefer(v => deferredAction, () => deferredAction));
                    assertRejected.Invoke(rejectDeferredInt.Promise.ThenDefer<object>(v => deferredAction, failValue => deferredAction));
                    assertRejected.Invoke(rejectDeferredInt.Promise.ThenDefer<object>(v => deferredAction, () => deferredAction));
                    assertRejected.Invoke(rejectDeferredInt.Promise.CatchDefer(() => deferredAction));
                    assertRejected.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(failValue => deferredAction));
                    assertRejected.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(() => deferredAction));
                    assertRejectedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction, () => deferredIntAction));
                    assertRejectedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, failValue => deferredIntAction));
                    assertRejectedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, () => deferredIntAction));
                    assertRejectedInt.Invoke(rejectDeferredInt.Promise.CatchDefer(() => deferredIntAction));
                    assertRejectedInt.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(failValue => deferredIntAction));
                    assertRejectedInt.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(() => deferredIntAction));


                    resolveDeferred.Release();
                    resolveDeferredInt.Release();
                    rejectDeferred.Release();
                    rejectDeferredInt.Release();

                    // Clean up.
                    GC.Collect();
                    Promise.Manager.HandleCompletes();
                    LogAssert.NoUnexpectedReceived();
                }

                public class _2_3_3_3_4_IfCallingXThrowsAnExceptionE
                {
                    [Test]
                    public void _2_3_3_3_4_1_IfDeferredIsResolvedIgnoreIt()
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

                        Action<Promise.Deferred> deferredAction = deferred => {
                            deferred.Resolve();
                            throw new Exception("Fail value");
                        };
                        Action<Promise<int>.Deferred> deferredIntAction = deferred => {
                            deferred.Resolve(0);
                            throw new Exception("Fail value");
                        };

                        Action<Promise> assertResolved = promise => {
                            int resolveCount = 0;
                            TestHelper.AddCallbacks<object>(promise,
                                () => ++resolveCount,
                                o => Assert.Fail("Promise was rejected when it should have been resolved: " + o),
                                () => Assert.Fail("Promise was rejected when it should have been resolved"));
                            // Ignored exceptions get added to the unhandled exception queue instead of rejecting the promise.
                            Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);
                            Assert.AreEqual(TestHelper.resolveVoidCallbacks, resolveCount);
                        };
                        Action<Promise<int>> assertResolvedInt = promise => {
                            int resolveCount = 0;
                            TestHelper.AddCallbacks<int, object>(promise,
                                v => ++resolveCount,
                                o => Assert.Fail("Promise was rejected when it should have been resolved: " + o),
                                () => Assert.Fail("Promise was rejected when it should have been resolved"));
                            // Ignored exceptions get added to the unhandled exception queue instead of rejecting the promise.
                            Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);
                            Assert.AreEqual(TestHelper.resolveTCallbacks, resolveCount);
                        };

                        assertResolved.Invoke(resolveDeferred.Promise.ThenDefer(() => deferredAction));
                        assertResolved.Invoke(resolveDeferred.Promise.ThenDefer(() => deferredAction, () => deferredAction));
                        assertResolved.Invoke(resolveDeferred.Promise.ThenDefer<object>(() => deferredAction, failValue => deferredAction));
                        assertResolved.Invoke(resolveDeferred.Promise.ThenDefer<object>(() => deferredAction, () => deferredAction));
                        assertResolvedInt.Invoke(resolveDeferred.Promise.ThenDefer<int>(() => deferredIntAction));
                        assertResolvedInt.Invoke(resolveDeferred.Promise.ThenDefer<int>(() => deferredIntAction, () => deferredIntAction));
                        assertResolvedInt.Invoke(resolveDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, failValue => deferredIntAction));
                        assertResolvedInt.Invoke(resolveDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, () => deferredIntAction));

                        assertResolved.Invoke(rejectDeferred.Promise.ThenDefer(() => deferredAction, () => deferredAction));
                        assertResolved.Invoke(rejectDeferred.Promise.ThenDefer<object>(() => deferredAction, failValue => deferredAction));
                        assertResolved.Invoke(rejectDeferred.Promise.ThenDefer<object>(() => deferredAction, () => deferredAction));
                        assertResolved.Invoke(rejectDeferred.Promise.CatchDefer(() => deferredAction));
                        assertResolved.Invoke(rejectDeferred.Promise.CatchDefer<object>(failValue => deferredAction));
                        assertResolved.Invoke(rejectDeferred.Promise.CatchDefer<object>(() => deferredAction));
                        assertResolvedInt.Invoke(rejectDeferred.Promise.ThenDefer<int>(() => deferredIntAction, () => deferredIntAction));
                        assertResolvedInt.Invoke(rejectDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, failValue => deferredIntAction));
                        assertResolvedInt.Invoke(rejectDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, () => deferredIntAction));

                        assertResolved.Invoke(resolveDeferredInt.Promise.ThenDefer(v => deferredAction));
                        assertResolved.Invoke(resolveDeferredInt.Promise.ThenDefer(v => deferredAction, () => deferredAction));
                        assertResolved.Invoke(resolveDeferredInt.Promise.ThenDefer<object>(v => deferredAction, failValue => deferredAction));
                        assertResolved.Invoke(resolveDeferredInt.Promise.ThenDefer<object>(v => deferredAction, () => deferredAction));
                        assertResolvedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction));
                        assertResolvedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction, () => deferredIntAction));
                        assertResolvedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, failValue => deferredIntAction));
                        assertResolvedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, () => deferredIntAction));

                        assertResolved.Invoke(rejectDeferredInt.Promise.ThenDefer(v => deferredAction, () => deferredAction));
                        assertResolved.Invoke(rejectDeferredInt.Promise.ThenDefer<object>(v => deferredAction, failValue => deferredAction));
                        assertResolved.Invoke(rejectDeferredInt.Promise.ThenDefer<object>(v => deferredAction, () => deferredAction));
                        assertResolved.Invoke(rejectDeferredInt.Promise.CatchDefer(() => deferredAction));
                        assertResolved.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(failValue => deferredAction));
                        assertResolved.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(() => deferredAction));
                        assertResolvedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction, () => deferredIntAction));
                        assertResolvedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, failValue => deferredIntAction));
                        assertResolvedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, () => deferredIntAction));
                        assertResolvedInt.Invoke(rejectDeferredInt.Promise.CatchDefer(() => deferredIntAction));
                        assertResolvedInt.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(failValue => deferredIntAction));
                        assertResolvedInt.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(() => deferredIntAction));


                        resolveDeferred.Release();
                        resolveDeferredInt.Release();
                        rejectDeferred.Release();
                        rejectDeferredInt.Release();

                        // Clean up.
                        GC.Collect();
                        Promise.Manager.HandleCompletes();
                        LogAssert.NoUnexpectedReceived();
                    }

                    [Test]
                    public void _2_3_3_3_4_1_IfDeferredIsRejectedIgnoreIt()
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

                        Action<Promise.Deferred> deferredAction = deferred => {
                            deferred.Reject("Fail value");
                            throw new Exception("Fail value");
                        };
                        Action<Promise<int>.Deferred> deferredIntAction = deferred => {
                            deferred.Reject("Fail value");
                            throw new Exception("Fail value");
                        };

                        Action<Promise> assertRejected = promise => {
                            int rejectCount = 0;
                            TestHelper.AddCallbacks<string>(promise,
                                () => Assert.Fail("Promise was resolved when it should have been rejected"),
                                s => ++rejectCount,
                                () => ++rejectCount);
                            // Ignored exceptions get added to the unhandled exception queue instead of rejecting the promise.
                            Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);
                            Assert.AreEqual(TestHelper.rejectVoidCallbacks, rejectCount);
                        };
                        Action<Promise<int>> assertRejectedInt = promise => {
                            int rejectCount = 0;
                            TestHelper.AddCallbacks<int, string>(promise,
                                v => Assert.Fail("Promise was resolved when it should have been rejected: " + v),
                                s => ++rejectCount,
                                () => ++rejectCount);
                            // Ignored exceptions get added to the unhandled exception queue instead of rejecting the promise.
                            Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);
                            Assert.AreEqual(TestHelper.rejectVoidCallbacks, rejectCount);
                        };

                        assertRejected.Invoke(resolveDeferred.Promise.ThenDefer(() => deferredAction));
                        assertRejected.Invoke(resolveDeferred.Promise.ThenDefer(() => deferredAction, () => deferredAction));
                        assertRejected.Invoke(resolveDeferred.Promise.ThenDefer<object>(() => deferredAction, failValue => deferredAction));
                        assertRejected.Invoke(resolveDeferred.Promise.ThenDefer<object>(() => deferredAction, () => deferredAction));
                        assertRejectedInt.Invoke(resolveDeferred.Promise.ThenDefer<int>(() => deferredIntAction));
                        assertRejectedInt.Invoke(resolveDeferred.Promise.ThenDefer<int>(() => deferredIntAction, () => deferredIntAction));
                        assertRejectedInt.Invoke(resolveDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, failValue => deferredIntAction));
                        assertRejectedInt.Invoke(resolveDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, () => deferredIntAction));

                        assertRejected.Invoke(rejectDeferred.Promise.ThenDefer(() => deferredAction, () => deferredAction));
                        assertRejected.Invoke(rejectDeferred.Promise.ThenDefer<object>(() => deferredAction, failValue => deferredAction));
                        assertRejected.Invoke(rejectDeferred.Promise.ThenDefer<object>(() => deferredAction, () => deferredAction));
                        assertRejected.Invoke(rejectDeferred.Promise.CatchDefer(() => deferredAction));
                        assertRejected.Invoke(rejectDeferred.Promise.CatchDefer<object>(failValue => deferredAction));
                        assertRejected.Invoke(rejectDeferred.Promise.CatchDefer<object>(() => deferredAction));
                        assertRejectedInt.Invoke(rejectDeferred.Promise.ThenDefer<int>(() => deferredIntAction, () => deferredIntAction));
                        assertRejectedInt.Invoke(rejectDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, failValue => deferredIntAction));
                        assertRejectedInt.Invoke(rejectDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, () => deferredIntAction));

                        assertRejected.Invoke(resolveDeferredInt.Promise.ThenDefer(v => deferredAction));
                        assertRejected.Invoke(resolveDeferredInt.Promise.ThenDefer(v => deferredAction, () => deferredAction));
                        assertRejected.Invoke(resolveDeferredInt.Promise.ThenDefer<object>(v => deferredAction, failValue => deferredAction));
                        assertRejected.Invoke(resolveDeferredInt.Promise.ThenDefer<object>(v => deferredAction, () => deferredAction));
                        assertRejectedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction));
                        assertRejectedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction, () => deferredIntAction));
                        assertRejectedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, failValue => deferredIntAction));
                        assertRejectedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, () => deferredIntAction));

                        assertRejected.Invoke(rejectDeferredInt.Promise.ThenDefer(v => deferredAction, () => deferredAction));
                        assertRejected.Invoke(rejectDeferredInt.Promise.ThenDefer<object>(v => deferredAction, failValue => deferredAction));
                        assertRejected.Invoke(rejectDeferredInt.Promise.ThenDefer<object>(v => deferredAction, () => deferredAction));
                        assertRejected.Invoke(rejectDeferredInt.Promise.CatchDefer(() => deferredAction));
                        assertRejected.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(failValue => deferredAction));
                        assertRejected.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(() => deferredAction));
                        assertRejectedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction, () => deferredIntAction));
                        assertRejectedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, failValue => deferredIntAction));
                        assertRejectedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, () => deferredIntAction));
                        assertRejectedInt.Invoke(rejectDeferredInt.Promise.CatchDefer(() => deferredIntAction));
                        assertRejectedInt.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(failValue => deferredIntAction));
                        assertRejectedInt.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(() => deferredIntAction));


                        resolveDeferred.Release();
                        resolveDeferredInt.Release();
                        rejectDeferred.Release();
                        rejectDeferredInt.Release();

                        // Clean up.
                        GC.Collect();
                        Promise.Manager.HandleCompletes();
                        LogAssert.NoUnexpectedReceived();
                    }

                    [Test]
                    public void _2_3_3_3_4_2_IfDeferredIsPendingRejectPromiseWithEAsTheReason()
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

                        Exception expectedException = new Exception("Fail value");

                        Action<Promise.Deferred> deferredAction = deferred => {
                            throw expectedException;
                        };
                        Action<Promise<int>.Deferred> deferredIntAction = deferred => {
                            throw expectedException;
                        };

                        Action<Promise> assertExcepted = promise => {
                            int exceptionCount = 0;
                            TestHelper.AddCallbacks<string>(promise,
                                () => Assert.Fail("Promise was resolved when it should have been rejected"),
                                s => Assert.Fail("Promise was rejected with a string when it should have been rejected with an Exception: " + s),
                                () => ++exceptionCount,
                                e => { Assert.AreEqual(expectedException, e); ++exceptionCount; });
                            Promise.Manager.HandleCompletes();
                            Assert.AreEqual(TestHelper.totalVoidCallbacks, exceptionCount);
                        };
                        Action<Promise<int>> assertExceptedInt = promise => {
                            int exceptionCount = 0;
                            TestHelper.AddCallbacks<int, string>(promise,
                                v => Assert.Fail("Promise was resolved when it should have been rejected: " + v),
                                s => Assert.Fail("Promise was rejected when it should have been resolved: " + s),
                                () => ++exceptionCount,
                                e => { Assert.AreEqual(expectedException, e); ++exceptionCount; });
                            Promise.Manager.HandleCompletes();
                            Assert.AreEqual(TestHelper.totalTCallbacks, exceptionCount);
                        };

                        assertExcepted.Invoke(resolveDeferred.Promise.ThenDefer(() => deferredAction));
                        assertExcepted.Invoke(resolveDeferred.Promise.ThenDefer(() => deferredAction, () => deferredAction));
                        assertExcepted.Invoke(resolveDeferred.Promise.ThenDefer<object>(() => deferredAction, failValue => deferredAction));
                        assertExcepted.Invoke(resolveDeferred.Promise.ThenDefer<object>(() => deferredAction, () => deferredAction));
                        assertExceptedInt.Invoke(resolveDeferred.Promise.ThenDefer<int>(() => deferredIntAction));
                        assertExceptedInt.Invoke(resolveDeferred.Promise.ThenDefer<int>(() => deferredIntAction, () => deferredIntAction));
                        assertExceptedInt.Invoke(resolveDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, failValue => deferredIntAction));
                        assertExceptedInt.Invoke(resolveDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, () => deferredIntAction));

                        assertExcepted.Invoke(rejectDeferred.Promise.ThenDefer(() => deferredAction, () => deferredAction));
                        assertExcepted.Invoke(rejectDeferred.Promise.ThenDefer<object>(() => deferredAction, failValue => deferredAction));
                        assertExcepted.Invoke(rejectDeferred.Promise.ThenDefer<object>(() => deferredAction, () => deferredAction));
                        assertExcepted.Invoke(rejectDeferred.Promise.CatchDefer(() => deferredAction));
                        assertExcepted.Invoke(rejectDeferred.Promise.CatchDefer<object>(failValue => deferredAction));
                        assertExcepted.Invoke(rejectDeferred.Promise.CatchDefer<object>(() => deferredAction));
                        assertExceptedInt.Invoke(rejectDeferred.Promise.ThenDefer<int>(() => deferredIntAction, () => deferredIntAction));
                        assertExceptedInt.Invoke(rejectDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, failValue => deferredIntAction));
                        assertExceptedInt.Invoke(rejectDeferred.Promise.ThenDefer<int, object>(() => deferredIntAction, () => deferredIntAction));

                        assertExcepted.Invoke(resolveDeferredInt.Promise.ThenDefer(v => deferredAction));
                        assertExcepted.Invoke(resolveDeferredInt.Promise.ThenDefer(v => deferredAction, () => deferredAction));
                        assertExcepted.Invoke(resolveDeferredInt.Promise.ThenDefer<object>(v => deferredAction, failValue => deferredAction));
                        assertExcepted.Invoke(resolveDeferredInt.Promise.ThenDefer<object>(v => deferredAction, () => deferredAction));
                        assertExceptedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction));
                        assertExceptedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction, () => deferredIntAction));
                        assertExceptedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, failValue => deferredIntAction));
                        assertExceptedInt.Invoke(resolveDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, () => deferredIntAction));

                        assertExcepted.Invoke(rejectDeferredInt.Promise.ThenDefer(v => deferredAction, () => deferredAction));
                        assertExcepted.Invoke(rejectDeferredInt.Promise.ThenDefer<object>(v => deferredAction, failValue => deferredAction));
                        assertExcepted.Invoke(rejectDeferredInt.Promise.ThenDefer<object>(v => deferredAction, () => deferredAction));
                        assertExcepted.Invoke(rejectDeferredInt.Promise.CatchDefer(() => deferredAction));
                        assertExcepted.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(failValue => deferredAction));
                        assertExcepted.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(() => deferredAction));
                        assertExceptedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int>(v => deferredIntAction, () => deferredIntAction));
                        assertExceptedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, failValue => deferredIntAction));
                        assertExceptedInt.Invoke(rejectDeferredInt.Promise.ThenDefer<int, object>(v => deferredIntAction, () => deferredIntAction));
                        assertExceptedInt.Invoke(rejectDeferredInt.Promise.CatchDefer(() => deferredIntAction));
                        assertExceptedInt.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(failValue => deferredIntAction));
                        assertExceptedInt.Invoke(rejectDeferredInt.Promise.CatchDefer<object>(() => deferredIntAction));


                        resolveDeferred.Release();
                        resolveDeferredInt.Release();
                        rejectDeferred.Release();
                        rejectDeferredInt.Release();

                        // Clean up.
                        GC.Collect();
                        Promise.Manager.HandleCompletes();
                        LogAssert.NoUnexpectedReceived();
                    }
                }
            }
        }

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

            Action<Promise> assertResolved = promise => {
                int resolveCount = 0;
                TestHelper.AddCallbacks<object>(promise,
                    () => ++resolveCount,
                    o => Assert.Fail("Promise was rejected when it should have been resolved: " + o),
                    () => Assert.Fail("Promise was rejected when it should have been resolved"));
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(TestHelper.resolveVoidCallbacks, resolveCount);
            };

            assertResolved.Invoke(resolveDeferred.Promise.Then(() => { }));
            assertResolved.Invoke(resolveDeferred.Promise.Then(() => { }, () => Assert.Fail("Promise was rejected when it should have been resolved")));
            assertResolved.Invoke(resolveDeferred.Promise.Then<object>(() => { }, failValue => Assert.Fail("Promise was rejected when it should have been resolved")));
            assertResolved.Invoke(resolveDeferred.Promise.Then<object>(() => { }, () => Assert.Fail("Promise was rejected when it should have been resolved")));

            assertResolved.Invoke(rejectDeferred.Promise.Then(() => Assert.Fail("Promise was resolved when it should have been rejected"), () => { }));
            assertResolved.Invoke(rejectDeferred.Promise.Then<object>(() => Assert.Fail("Promise was resolved when it should have been rejected"), failValue => { }));
            assertResolved.Invoke(rejectDeferred.Promise.Then<object>(() => Assert.Fail("Promise was resolved when it should have been rejected"), () => { }));
            assertResolved.Invoke(rejectDeferred.Promise.Catch(() => { }));
            assertResolved.Invoke(rejectDeferred.Promise.Catch<object>(failValue => { }));
            assertResolved.Invoke(rejectDeferred.Promise.Catch<object>(() => { }));

            assertResolved.Invoke(resolveDeferredInt.Promise.Then(v => { }));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then(v => { }, () => Assert.Fail("Promise was rejected when it should have been resolved")));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then<object>(v => { }, failValue => Assert.Fail("Promise was rejected when it should have been resolved")));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then<object>(v => { }, () => Assert.Fail("Promise was rejected when it should have been resolved")));

            assertResolved.Invoke(rejectDeferredInt.Promise.Then(v => Assert.Fail("Promise was resolved when it should have been rejected"), () => { }));
            assertResolved.Invoke(rejectDeferredInt.Promise.Then<object>(v => Assert.Fail("Promise was resolved when it should have been rejected"), failValue => { }));
            assertResolved.Invoke(rejectDeferredInt.Promise.Then<object>(v => Assert.Fail("Promise was resolved when it should have been rejected"), () => { }));
            assertResolved.Invoke(rejectDeferredInt.Promise.Catch(() => { }));
            assertResolved.Invoke(rejectDeferredInt.Promise.Catch<object>(failValue => { }));
            assertResolved.Invoke(rejectDeferredInt.Promise.Catch<object>(() => { }));


            resolveDeferred.Release();
            resolveDeferredInt.Release();
            rejectDeferred.Release();
            rejectDeferredInt.Release();

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

            Action<Promise<int>> assertResolved = promise => {
                int resolveCount = 0;
                TestHelper.AddCallbacks<int, object>(promise,
                    v => { Assert.AreEqual(expected, v); ++resolveCount; },
                    o => Assert.Fail("Promise was rejected when it should have been resolved: " + o),
                    () => Assert.Fail("Promise was rejected when it should have been resolved"));
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(TestHelper.resolveTCallbacks, resolveCount);
            };

            assertResolved.Invoke(resolveDeferred.Promise.Then<int>(() => expected));
            assertResolved.Invoke(resolveDeferred.Promise.Then<int>(() => expected, () => { Assert.Fail("Promise was rejected when it should have been resolved"); return 0; }));
            assertResolved.Invoke(resolveDeferred.Promise.Then<int, object>(() => expected, failValue => { Assert.Fail("Promise was rejected when it should have been resolved"); return 0; }));
            assertResolved.Invoke(resolveDeferred.Promise.Then<int, object>(() => expected, () => { Assert.Fail("Promise was rejected when it should have been resolved"); return 0; }));

            assertResolved.Invoke(rejectDeferred.Promise.Then<int>(() => { Assert.Fail("Promise was resolved when it should have been rejected"); return 0; }, () => expected));
            assertResolved.Invoke(rejectDeferred.Promise.Then<int, object>(() => { Assert.Fail("Promise was resolved when it should have been rejected"); return 0; }, failValue => expected));
            assertResolved.Invoke(rejectDeferred.Promise.Then<int, object>(() => { Assert.Fail("Promise was resolved when it should have been rejected"); return 0; }, () => expected));

            assertResolved.Invoke(resolveDeferredInt.Promise.Then<int>(v => expected));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then<int>(v => expected, () => { Assert.Fail("Promise was rejected when it should have been resolved"); return 0; }));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then<int, object>(v => expected, failValue => { Assert.Fail("Promise was rejected when it should have been resolved"); return 0; }));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then<int, object>(v => expected, () => { Assert.Fail("Promise was rejected when it should have been resolved"); return 0; }));

            assertResolved.Invoke(rejectDeferredInt.Promise.Then<int>(v => { Assert.Fail("Promise was resolved when it should have been rejected"); return 0; }, () => expected));
            assertResolved.Invoke(rejectDeferredInt.Promise.Then<int, object>(v => { Assert.Fail("Promise was resolved when it should have been rejected"); return 0; }, failValue => expected));
            assertResolved.Invoke(rejectDeferredInt.Promise.Then<int, object>(v => { Assert.Fail("Promise was resolved when it should have been rejected"); return 0; }, () => expected));
            assertResolved.Invoke(rejectDeferredInt.Promise.Catch(() => expected));
            assertResolved.Invoke(rejectDeferredInt.Promise.Catch<object>(failValue => expected));
            assertResolved.Invoke(rejectDeferredInt.Promise.Catch<object>(() => expected));


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


            promise = deferred.Promise.Then(() => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }), () => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }), (object failValue) => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then<object>(() => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }), () => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferred.Promise.Then(() => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0));
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0), () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0), (object failValue) => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then<int, object>(() => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0), () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);


            promise = deferredInt.Promise.Then(v => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }), () => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }), (object failValue) => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then<object>(v => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }), () => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferredInt.Promise.Then(v => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0));
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0), () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0), (object failValue) => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then<int, object>(v => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0), () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);


            promise = deferred.Promise.Complete(() => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promiseInt = deferred.Promise.Complete(() => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0));
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


            promise = deferred.Promise.Catch(() => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Catch((object failValue) => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Catch<object>(() => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => { resolveAssert(); return promise; }, () => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => { resolveAssert(); return promise; }, (object failValue) => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then<object>(() => { resolveAssert(); return promise; }, () => promise.Then(() => { }).Then(() => { }).Catch<InvalidReturnException>(() => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferredInt.Promise.Catch(() => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Catch((object failValue) => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Catch<object>(() => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return promiseInt; }, () => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return promiseInt; }, (object failValue) => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then<int, object>(() => { resolveAssert(); return promiseInt; }, () => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);


            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return promiseInt; }, () => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return promiseInt; }, (object failValue) => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then<int, object>(v => { resolveAssert(); return promiseInt; }, () => promiseInt.Then(() => { }).Then(() => 0).Catch<InvalidReturnException>(() => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);

            deferred.Release();
            deferredInt.Release();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }
    }
}