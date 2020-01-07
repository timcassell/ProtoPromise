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

            promiseInt = deferred.Promise.Then(() => promiseInt);
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt, () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt, (object failValue) => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);


            promise = deferredInt.Promise.Then(v => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise, () => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise, (object failValue) => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferredInt.Promise.Then(v => promiseInt);
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt, () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt, (object failValue) => { rejectAssert(); return promiseInt; });
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

            promiseInt = deferredInt.Promise.Catch(() => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Catch((object failValue) => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return promiseInt; }, () => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return promiseInt; }, (object failValue) => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);


            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return promiseInt; }, () => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return promiseInt; }, (object failValue) => promiseInt);
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
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise, () => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = resolveDeferred.Promise.Then(() => resolveWaitDeferred.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferred.Promise; });
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

                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise, () => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = resolveDeferred.Promise.Then(() => rejectWaitDeferred.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferred.Promise; });
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

                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
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

                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
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

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferred.Promise; }, () => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferred.Promise; }, (object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());

                promise = rejectDeferred.Promise.Catch(() => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = rejectDeferred.Promise.Catch((object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferred.Promise; }, () => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferred.Promise; }, (object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++completeCounter);
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
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

                promise = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferred.Promise; }, () => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());
                promise = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferred.Promise; }, (object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => ++completeCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => ++completeCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Catch(() => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Catch((object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);

                promise = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferred.Promise; }, () => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => resolveAssert(), s => ++completeCounter);
                promise = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferred.Promise; }, (object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCompleteCallbacks(promise, () => ++completeCounter);
                TestHelper.AddCallbacks(promise, () => resolveAssert(), s => ++completeCounter);

                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);

                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCompleteCallbacks(promiseInt, () => ++completeCounter);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++completeCounter);



                rejectDeferredInt.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferred.Resolve();
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.resolveVoidCallbacks + TestHelper.completeCallbacks) * 12;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferredInt.Resolve(0);
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.resolveTCallbacks + TestHelper.completeCallbacks) * 14;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferred.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.rejectVoidCallbacks + TestHelper.completeCallbacks) * 12;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferredInt.Reject("Fail value");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += (TestHelper.rejectTCallbacks + TestHelper.completeCallbacks) * 14;
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

                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferred.Promise.Then(() => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());


                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise, () => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                promise = resolveDeferredInt.Promise.Then(v => resolveWaitDeferred.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());

                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, () => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = resolveDeferredInt.Promise.Then(v => resolveWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return resolveWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());


                promise = rejectDeferred.Promise.Catch(() => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Catch((object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferred.Promise; }, () => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return resolveWaitDeferred.Promise; }, (object failValue) => resolveWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, () => ++resolveCounter, s => rejectAssert());


                promiseInt = rejectDeferredInt.Promise.Catch(() => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Catch((object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());

                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, () => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return resolveWaitDeferredInt.Promise; }, (object failValue) => resolveWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => ++resolveCounter, s => rejectAssert());

                Assert.AreEqual(0, resolveCounter);


                if (firstRun)
                {
                    resolveWaitDeferred.Resolve();
                    resolveWaitDeferredInt.Resolve(0);
                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(TestHelper.resolveVoidCallbacks * 10 + TestHelper.resolveTCallbacks * 12, resolveCounter);
                    firstRun = false;
                    goto RunAgain;
                }

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(TestHelper.resolveVoidCallbacks * 10 + TestHelper.resolveTCallbacks * 12, resolveCounter);

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

                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = resolveDeferred.Promise.Then(() => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);


                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);
                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise, () => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);
                promise = resolveDeferredInt.Promise.Then(v => rejectWaitDeferred.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferred.Promise; });
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);

                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, () => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = resolveDeferredInt.Promise.Then(v => rejectWaitDeferredInt.Promise, (object failValue) => { rejectAssert(); return rejectWaitDeferredInt.Promise; });
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);


                promise = rejectDeferred.Promise.Catch(() => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);
                promise = rejectDeferred.Promise.Catch((object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);

                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferred.Promise; }, () => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);
                promise = rejectDeferred.Promise.Then(() => { resolveAssert(); return rejectWaitDeferred.Promise; }, (object failValue) => rejectWaitDeferred.Promise);
                TestHelper.AddCallbacks(promise, resolveAssert, s => ++rejectCounter);


                promiseInt = rejectDeferredInt.Promise.Catch(() => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Catch((object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);

                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Then(v => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);


                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, () => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);
                promiseInt = rejectDeferredInt.Promise.Then(() => { resolveAssert(); return rejectWaitDeferredInt.Promise; }, (object failValue) => rejectWaitDeferredInt.Promise);
                TestHelper.AddCallbacks(promiseInt, v => resolveAssert(), s => ++rejectCounter);

                Assert.AreEqual(0, rejectCounter);


                if (firstRun)
                {
                    rejectWaitDeferred.Reject("Fail value");
                    rejectWaitDeferredInt.Reject("Fail value");
                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(TestHelper.rejectVoidCallbacks * 10 + TestHelper.rejectTCallbacks * 12, rejectCounter);
                    firstRun = false;
                    goto RunAgain;
                }

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(TestHelper.rejectVoidCallbacks * 10 + TestHelper.rejectTCallbacks * 12, rejectCounter);

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

            Action<Promise> assertResolved = promise =>
            {
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

            assertResolved.Invoke(rejectDeferred.Promise.Then(() => Assert.Fail("Promise was resolved when it should have been rejected"), () => { }));
            assertResolved.Invoke(rejectDeferred.Promise.Then<object>(() => Assert.Fail("Promise was resolved when it should have been rejected"), failValue => { }));
            assertResolved.Invoke(rejectDeferred.Promise.Catch(() => { }));
            assertResolved.Invoke(rejectDeferred.Promise.Catch<object>(failValue => { }));

            assertResolved.Invoke(resolveDeferredInt.Promise.Then(v => { }));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then(v => { }, () => Assert.Fail("Promise was rejected when it should have been resolved")));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then<object>(v => { }, failValue => Assert.Fail("Promise was rejected when it should have been resolved")));

            assertResolved.Invoke(rejectDeferredInt.Promise.Then(v => Assert.Fail("Promise was resolved when it should have been rejected"), () => { }));
            assertResolved.Invoke(rejectDeferredInt.Promise.Then<object>(v => Assert.Fail("Promise was resolved when it should have been rejected"), failValue => { }));
            assertResolved.Invoke(rejectDeferredInt.Promise.Catch(() => { }));
            assertResolved.Invoke(rejectDeferredInt.Promise.Catch<object>(failValue => { }));


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

            Action<Promise<int>> assertResolved = promise =>
            {
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

            assertResolved.Invoke(rejectDeferred.Promise.Then<int>(() => { Assert.Fail("Promise was resolved when it should have been rejected"); return 0; }, () => expected));
            assertResolved.Invoke(rejectDeferred.Promise.Then<int, object>(() => { Assert.Fail("Promise was resolved when it should have been rejected"); return 0; }, failValue => expected));

            assertResolved.Invoke(resolveDeferredInt.Promise.Then<int>(v => expected));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then<int>(v => expected, () => { Assert.Fail("Promise was rejected when it should have been resolved"); return 0; }));
            assertResolved.Invoke(resolveDeferredInt.Promise.Then<int, object>(v => expected, failValue => { Assert.Fail("Promise was rejected when it should have been resolved"); return 0; }));

            assertResolved.Invoke(rejectDeferredInt.Promise.Then<int>(v => { Assert.Fail("Promise was resolved when it should have been rejected"); return 0; }, () => expected));
            assertResolved.Invoke(rejectDeferredInt.Promise.Then<int, object>(v => { Assert.Fail("Promise was resolved when it should have been rejected"); return 0; }, failValue => expected));
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

            promiseInt = deferred.Promise.Then(() => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0), () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0), (object failValue) => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);


            promise = deferredInt.Promise.Then(v => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }));
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }), () => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise.Then(() => { }).Then(() => { }).Catch((InvalidReturnException _) => { }), (object failValue) => { rejectAssert(); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferredInt.Promise.Then(v => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0), () => { rejectAssert(); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0), (object failValue) => { rejectAssert(); return promiseInt; });
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

            promiseInt = deferredInt.Promise.Catch(() => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Catch((object failValue) => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return promiseInt; }, () => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { resolveAssert(); return promiseInt; }, (object failValue) => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);


            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return promiseInt; }, () => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => { resolveAssert(); return promiseInt; }, (object failValue) => promiseInt.Then(() => { }).Then(() => 0).Catch((InvalidReturnException _) => 0));
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