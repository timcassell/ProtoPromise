using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class APlus_2_3_ThePromiseResolutionProcedure
    {
        [TearDown]
        public void Teardown()
        {
            // Clean up.
            try
            {
                Promise.Manager.HandleCompletes();
            }
            catch (AggregateException) { }
            LogAssert.NoUnexpectedReceived();
        }

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


            promise = deferred.Promise.Then(() => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => promise, () => { Assert.Fail("Promise was rejected when it should have been resolved."); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => promise, (object failValue) => { Assert.Fail("Promise was rejected with when it should have been resolved. Fail value: " + failValue); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then<object>(() => promise, () => { Assert.Fail("Promise was rejected when it should have been resolved."); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferred.Promise.Then(() => promiseInt);
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt, () => { Assert.Fail("Promise was rejected when it should have been resolved."); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then(() => promiseInt, (object failValue) => { Assert.Fail("Promise was rejected with when it should have been resolved. Fail value: " + failValue); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferred.Promise.Then<int, object>(() => promiseInt, () => { Assert.Fail("Promise was rejected when it should have been resolved."); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);


            promise = deferredInt.Promise.Then(v => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise, () => { Assert.Fail("Promise was rejected when it should have been resolved."); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then(v => promise, (object failValue) => { Assert.Fail("Promise was rejected with when it should have been resolved. Fail value: " + failValue); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferredInt.Promise.Then<object>(v => promise, () => { Assert.Fail("Promise was rejected when it should have been resolved."); return promise; });
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferredInt.Promise.Then(v => promiseInt);
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt, () => { Assert.Fail("Promise was rejected when it should have been resolved."); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => promiseInt, (object failValue) => { Assert.Fail("Promise was rejected with when it should have been resolved. Fail value: " + failValue); return promiseInt; });
            TestHelper.AssertRejectType<int, InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then<int, object>(v => promiseInt, () => { Assert.Fail("Promise was rejected when it should have been resolved."); return promiseInt; });
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
            promise = deferred.Promise.Then(() => { Assert.Fail("Promise was resolve when it should have been rejected."); return promise; }, () => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then(() => { Assert.Fail("Promise was resolve when it should have been rejected."); return promise; }, (object failValue) => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);
            promise = deferred.Promise.Then<object>(() => { Assert.Fail("Promise was resolve when it should have been rejected."); return promise; }, () => promise);
            TestHelper.AssertRejectType<InvalidReturnException>(promise);

            promiseInt = deferredInt.Promise.Catch(() => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Catch((object failValue) => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Catch<object>(() => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { Assert.Fail("Promise was resolve when it should have been rejected."); return promiseInt; }, () => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(() => { Assert.Fail("Promise was resolve when it should have been rejected."); return promiseInt; }, (object failValue) => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then<int, object>(() => { Assert.Fail("Promise was resolve when it should have been rejected."); return promiseInt; }, () => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);


            promiseInt = deferredInt.Promise.Then(v => { Assert.Fail("Promise was resolve when it should have been rejected."); return promiseInt; }, () => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then(v => { Assert.Fail("Promise was resolve when it should have been rejected."); return promiseInt; }, (object failValue) => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);
            promiseInt = deferredInt.Promise.Then<int, object>(v => { Assert.Fail("Promise was resolve when it should have been rejected."); return promiseInt; }, () => promiseInt);
            TestHelper.AssertRejectType<InvalidReturnException>(promiseInt);

            deferred.Release();
            deferredInt.Release();
        }
    }
}