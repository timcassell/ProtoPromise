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
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        public class _2_3_2_IfXIsAPromiseAdoptItsState
        {
            [UnityTest]
            public IEnumerator _2_3_2_1_IfXIsPendingPromiseMustRemainPendingUntilXIsFulfilledOrRejected()
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

                yield return null;

                Assert.AreEqual(expectedCompleteCount, completeCounter);
                resolveWaitDeferred.Resolve();
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.resolveVoidCallbacks + TestHelper.completeCallbacks) * 17;
                Assert.AreEqual(expectedCompleteCount, completeCounter);

                yield return null;

                Assert.AreEqual(expectedCompleteCount, completeCounter);
                resolveWaitDeferredInt.Resolve(0);
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.resolveTCallbacks + TestHelper.completeCallbacks) * 20;
                Assert.AreEqual(expectedCompleteCount, completeCounter);

                yield return null;

                Assert.AreEqual(expectedCompleteCount, completeCounter);
                rejectWaitDeferred.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.rejectVoidCallbacks + TestHelper.completeCallbacks) * 17;
                Assert.AreEqual(expectedCompleteCount, completeCounter);

                yield return null;

                Assert.AreEqual(expectedCompleteCount, completeCounter);
                rejectWaitDeferredInt.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.rejectTCallbacks + TestHelper.completeCallbacks) * 20;
                Assert.AreEqual(expectedCompleteCount, completeCounter);

                // Clean up.
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [UnityTest]
            public IEnumerator _2_3_2_2_IfWhenXIsFulfilledFulfillPromiseWithTheSameValue()
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

                yield return null;

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
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [UnityTest]
            public IEnumerator _2_3_2_3_IfWhenXIsRejectedRejectPromiseWithTheSameReason()
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

                yield return null;

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
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class _2_3_3_IfXIsAFunction
        {
            public class _2_3_3_3_IfXIsAFunctionCallItWithADeferred
            {
                [UnityTest]
                public IEnumerator _2_3_3_3_1_IfWhenDeferredIsResolvedResolvePromise()
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

                    yield return null;

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
                    Promise.Manager.HandleCompletes();
                    LogAssert.NoUnexpectedReceived();
                }

                [UnityTest]
                public IEnumerator _2_3_3_3_1_IfWhenDeferredIsResolvedWithAValueResolvePromiseWithThatValue()
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

                    yield return null;

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
                    Promise.Manager.HandleCompletes();
                    LogAssert.NoUnexpectedReceived();
                }

                [UnityTest]
                public IEnumerator _2_3_3_3_2_IfWhenDeferredIsRejectedWithAReasonRejectPromiseWithThatReason()
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

                    yield return null;

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
                    Promise.Manager.HandleCompletes();
                    LogAssert.NoUnexpectedReceived();
                }
            }
        }
    }
}