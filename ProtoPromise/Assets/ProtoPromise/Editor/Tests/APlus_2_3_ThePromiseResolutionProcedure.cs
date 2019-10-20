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
                Promise promise = null;
                Promise<int> promiseInt = null;
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

                resolveDeferredInt.Resolve(0);
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


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

                rejectDeferred.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                promiseInt = rejectDeferredInt.Promise.Catch(() => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Catch((object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Catch<object>(() => rejectWaitDeferredInt.Promise);
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


                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Then<int, object>(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);

                rejectDeferredInt.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);

                yield return null;

                Assert.AreEqual(expectedCompleteCount, completeCounter);
                resolveWaitDeferred.Resolve();
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.resolveVoidCallbacks + TestHelper.completeCallbacks) * 8;
                Assert.AreEqual(expectedCompleteCount, completeCounter);

                yield return null;

                Assert.AreEqual(expectedCompleteCount, completeCounter);
                resolveWaitDeferredInt.Resolve(0);
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.resolveTCallbacks + TestHelper.completeCallbacks) * 8;
                Assert.AreEqual(expectedCompleteCount, completeCounter);

                yield return null;

                Assert.AreEqual(expectedCompleteCount, completeCounter);
                rejectWaitDeferred.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.rejectVoidCallbacks + TestHelper.completeCallbacks) * 6;
                Assert.AreEqual(expectedCompleteCount, completeCounter);

                yield return null;

                Assert.AreEqual(expectedCompleteCount, completeCounter);
                rejectWaitDeferredInt.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.rejectTCallbacks + TestHelper.completeCallbacks) * 9;
                Assert.AreEqual(expectedCompleteCount, completeCounter);

                // Clean up.
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }
    }
}