using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class APlus_2_1_PromiseStates
    {
        public class _2_1_1_WhenPendingAPromise
        {
            [Test]
            public void _2_1_1_1_MayTransitionToEitherTheFulfilledOrRejectedState()
            {
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Resolve();

                Assert.AreEqual(Promise.State.Resolved, deferred.State);

                deferred = Promise.NewDeferred();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Reject("Fail Value");

                Assert.AreEqual(Promise.State.Rejected, deferred.State);

                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class _2_1_2_WhenFulfilledAPromise
        {
            [Test]
            public void _2_1_2_1_MustNotTransitionToAnyOtherState()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                deferred.Resolve();

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");

                deferred.Reject("Fail Value");
                deferredInt.Resolve(0);

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");

                deferredInt.Reject("Fail Value");

                Assert.AreEqual(Promise.State.Resolved, deferred.State);
                Assert.AreEqual(Promise.State.Resolved, deferredInt.State);

                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void _2_1_2_2_MustHaveAValueWhichMustNotChange()
            {
                var deferred = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Retain();
                int result = -1;
                int expected = 0;

                TestHelper.AddCallbacks<int, object>(deferred.Promise,
                    num => { Assert.AreEqual(expected, num); result = num; },
                    s => Assert.Fail("Promise was rejected when it should have been resolved."),
                    () => Assert.Fail("Promise was rejected when it should have been resolved."));
                deferred.Resolve(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, result);

                TestHelper.AddCallbacks<int, object>(deferred.Promise,
                    num => { Assert.AreEqual(expected, num); result = num; },
                    s => Assert.Fail("Promise was rejected when it should have been resolved."),
                    () => Assert.Fail("Promise was rejected when it should have been resolved."));

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");

                deferred.Resolve(1);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, result);

                deferred.Release();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class _2_1_3_WhenRejectedAPromise
        {
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

                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void _2_1_3_2_MustHaveAReasonWhichMustNotChange_0()
            {
                var deferred = Promise.NewDeferred();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Retain();
                string rejection = null;
                string expected = "Fail Value";
                TestHelper.AddCallbacks<string>(deferred.Promise,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    failValue =>
                    {
                        rejection = failValue;

                        Assert.AreEqual(expected, failValue);
                    });
                deferred.Reject(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, rejection);

                TestHelper.AddCallbacks<string>(deferred.Promise,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    failValue =>
                    {
                        rejection = failValue;

                        Assert.AreEqual(expected, failValue);
                    });

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");

                deferred.Reject("Different Fail Value");
                deferred.Release();
                // The second rejection will be added to the unhandled rejection queue instead of set as the promise's reason.
                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                Assert.AreEqual(expected, rejection);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void _2_1_3_2_MustHaveAReasonWhichMustNotChange_1()
            {
                var deferred = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Retain();
                string rejection = null;
                string expected = "Fail Value";
                TestHelper.AddCallbacks<int, string>(deferred.Promise,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    failValue =>
                    {
                        rejection = failValue;

                        Assert.AreEqual(expected, failValue);
                    });
                deferred.Reject(expected);
                Promise.Manager.HandleCompletes();

                TestHelper.AddCallbacks<int, string>(deferred.Promise,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    failValue =>
                    {
                        rejection = failValue;

                        Assert.AreEqual(expected, failValue);
                    });

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");

                deferred.Reject("Different Fail Value");
                deferred.Release();
                // The second rejection will be added to the unhandled rejection queue instead of set as the promise's reason.
                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                Assert.AreEqual(expected, rejection);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }
    }
}