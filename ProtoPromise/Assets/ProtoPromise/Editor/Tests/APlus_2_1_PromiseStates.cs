using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class APlus_2_1_PromiseStates
    {
        public class _2_1_1_WhenPendingAPromise
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
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void _2_1_1_1_MayTransitionToEitherTheFulfilledOrRejectedState()
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
        }

        public class _2_1_2_WhenFulfilledAPromise
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
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void _2_1_2_1_MustNotTransitionToAnyOtherState()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                deferred.Resolve();

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");

                deferred.Reject();
                deferredInt.Resolve(0);

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");

                deferredInt.Reject();

                Assert.AreEqual(Promise.State.Resolved, deferred.State);
                Assert.AreEqual(Promise.State.Resolved, deferredInt.State);
            }


            [Test]
            public void _2_1_2_2_MustHaveAValueWhichMustNotChange()
            {
                var deferred = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Retain();
                int result = -1;
                int expected = 0;

                TestHelper.AddCallbacks(deferred.Promise, num => { Assert.AreEqual(expected, num); result = num; }, null);
                deferred.Resolve(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, result);

                TestHelper.AddCallbacks(deferred.Promise, num => { Assert.AreEqual(expected, num); result = num; }, null);

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");

                deferred.Resolve(1);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, result);

                deferred.Release();
            }
        }

        public class _2_1_3_WhenRejectedAPromise
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
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void _2_1_3_1_MustNotTransitionToAnyOtherState()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                deferred.Reject("Fail Value");

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");

                deferred.Resolve();
                deferredInt.Reject("Fail Value");

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");

                deferredInt.Resolve(0);

                Assert.AreEqual(Promise.State.Rejected, deferred.State);
                Assert.AreEqual(Promise.State.Rejected, deferredInt.State);
            }

            [Test]
            public void _2_1_3_2_MustHaveAReasonWhichMustNotChange_0()
            {
                var deferred = Promise.NewDeferred();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Retain();
                string rejection = null;
                string expected = "Fail Value";
                TestHelper.AddCallbacks(deferred.Promise, null, failValue => {
                    rejection = failValue;

                    Assert.AreEqual(expected, failValue);
                }, expected);
                deferred.Reject(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, rejection);

                TestHelper.AddCallbacks(deferred.Promise, null, failValue => {
                    rejection = failValue;

                    Assert.AreEqual(expected, failValue);
                }, expected);

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");

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

            [Test]
            public void _2_1_3_2_MustHaveAReasonWhichMustNotChange_1()
            {
                var deferred = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Retain();
                string rejection = null;
                string expected = "Fail Value";
                TestHelper.AddCallbacks(deferred.Promise, null, failValue => {
                    rejection = failValue;

                    Assert.AreEqual(expected, failValue);
                }, expected);
                deferred.Reject(expected);
                Promise.Manager.HandleCompletes();

                TestHelper.AddCallbacks(deferred.Promise, null, failValue => {
                    rejection = failValue;

                    Assert.AreEqual(expected, failValue);
                }, expected);

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");

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
}