using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class Cancelations
    {
        public class WhenPendingAPromise
        {
            [Test]
            public void MayTransitionToEitherTheFulfilledOrRejectedOrCanceledState()
            {
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Resolve();

                Assert.AreEqual(Promise.State.Resolved, deferred.State);

                deferred = Promise.NewDeferred();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Reject();

                Assert.AreEqual(Promise.State.Rejected, deferred.State);

                deferred = Promise.NewDeferred();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Cancel();

                Assert.AreEqual(Promise.State.Canceled, deferred.State);

                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class WhenFulfilledAPromise
        {
            [Test]
            public void MustNotTransitionToAnyOtherState()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                deferred.Resolve();
                deferredInt.Resolve(0);

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");
                deferred.Reject();
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");
                deferredInt.Reject();

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Cancel - Deferred is not in the pending state.");
                deferred.Cancel();
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Cancel - Deferred is not in the pending state.");
                deferredInt.Cancel();

                Assert.AreEqual(Promise.State.Resolved, deferred.State);
                Assert.AreEqual(Promise.State.Resolved, deferredInt.State);

                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class WhenRejectedAPromise
        {
            [Test]
            public void MustNotTransitionToAnyOtherState()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                deferred.Reject("Fail Value");
                deferredInt.Reject("Fail Value");

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");
                deferred.Resolve();
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");
                deferredInt.Resolve(0);

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Cancel - Deferred is not in the pending state.");
                deferred.Cancel();
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Cancel - Deferred is not in the pending state.");
                deferredInt.Cancel();

                Assert.AreEqual(Promise.State.Rejected, deferred.State);
                Assert.AreEqual(Promise.State.Rejected, deferredInt.State);

                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class WhenCanceledAPromise
        {
            [Test]
            public void MustNotTransitionToAnyOtherState()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                deferred.Cancel("Cancel Value");
                deferredInt.Cancel("Cancel Value");

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");
                deferred.Resolve();
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");
                deferredInt.Resolve(0);

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");
                deferred.Reject();
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");
                deferredInt.Reject();

                Assert.AreEqual(Promise.State.Canceled, deferred.State);
                Assert.AreEqual(Promise.State.Canceled, deferredInt.State);

                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void MustHaveAReasonWhichMustNotChange_0()
            {
                var deferred = Promise.NewDeferred();

                Assert.AreEqual(Promise.State.Pending, deferred.State);
                 
                deferred.Retain();
                string cancelation = null;
                string expected = "Cancel Value";

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                TestHelper.AddCallbacks(deferred.Promise, resolveAssert, failValue => rejectAssert());
                deferred.Promise.CatchCancelation<string>(cancelValue => { cancelation = cancelValue; Assert.AreEqual(expected, cancelValue); });
                deferred.Cancel(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                TestHelper.AddCallbacks(deferred.Promise, resolveAssert, failValue => rejectAssert());
                deferred.Promise.CatchCancelation<string>(cancelValue => { cancelation = cancelValue; Assert.AreEqual(expected, cancelValue); });

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Cancel - Deferred is not in the pending state.");

                deferred.Cancel("Different Cancel Value");
                deferred.Release();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void MustHaveAReasonWhichMustNotChange_1()
            {
                var deferred = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Retain();
                string cancelation = null;
                string expected = "Cancel Value";

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                TestHelper.AddCallbacks(deferred.Promise, resolveAssert, failValue => rejectAssert());
                deferred.Promise.CatchCancelation<string>(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue));
                deferred.Cancel(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                TestHelper.AddCallbacks(deferred.Promise, resolveAssert, failValue => rejectAssert());
                deferred.Promise.CatchCancelation<string>(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue));

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Cancel - Deferred is not in the pending state.");

                deferred.Cancel("Different Cancel Value");
                deferred.Release();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }
    }
}