using NUnit.Framework;
using Proto.Promises;

namespace Tests
{
    public class APlus2_1
    {
        [TearDown]
        public void Teardown()
        {
            // Clean up.
            try
            {
                Promise.Manager.HandleCompletes();
            }
            catch (System.AggregateException) { }
        }

        [Test]
        public void WhenPendingAPromiseMayTransitionToEitherTheFulfilledOrRejectedState_2_1_1_1()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            deferred.Resolve();

            Assert.AreEqual(Promise.State.Resolved, deferred.State);

            deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            deferred.Reject();

            Assert.AreEqual(Promise.State.Rejected, deferred.State);
        }

        [Test]
        public void WhenFulfilledAPromiseMustNotTransitionToAnyOtherState_2_1_2_1()
        {
            var deferred = Promise.NewDeferred();

            Assert.AreEqual(Promise.State.Pending, deferred.State);

            deferred.Resolve();
            deferred.Reject();

            Assert.AreEqual(Promise.State.Resolved, deferred.State);
        }

        [Test]
        public void WhenFulfilledAPromiseMustHaveAValueWhichMustNotChange_2_1_2_2()
        {
            var deferred = Promise.NewDeferred<int>();

            UnityEngine.Debug.LogWarning(deferred.State);
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            deferred.Retain();
            int result = -1;
            deferred.Promise.Then(value => result = value);
            deferred.Resolve(0);
            Promise.Manager.HandleCompletes();
            deferred.Promise.Then(value => result = value);
            deferred.Resolve(1);
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(0, result);

            deferred.Release();
        }

        [Test]
        public void WhenRejectedAPromiseMustNotTransitionToAnyOtherState_2_1_3_1()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            deferred.Reject("Fail Value");
            deferred.Resolve();

            Assert.AreEqual(Promise.State.Rejected, deferred.State);
        }

        [Test]
        public void WhenRejectedAPromiseMustHaveAReasonWhichMustNotChange_2_1_3_2()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            deferred.Retain();
            object rejection = null;
            deferred.Promise.Catch((object fail) => rejection = fail);
            deferred.Promise.Catch((object fail) => UnityEngine.Debug.LogWarning("fail: " + fail));
            object expected = "Fail Value";
            deferred.Reject(expected);
            Promise.Manager.HandleCompletes();
            deferred.Promise.Catch((object fail) => rejection = fail);
            deferred.Reject("Different Fail Value");
            // The second rejection will be added to the unhandled rejection queue instead of set as the promise's reason.
            try
            {
                Promise.Manager.HandleCompletes();
            }
            catch (System.AggregateException) { }

            Assert.AreEqual(expected, rejection);

            deferred.Release();
        }
    }
}
