using System;
using System.Collections;
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

        [Test]
        public void IfOnCanceledIsNullThrow()
        {
            var deferred = Promise.NewDeferred();

            Assert.AreEqual(Promise.State.Pending, deferred.State);

            Assert.Throws<ArgumentNullException>(() => {
                deferred.Promise.CatchCancelation(default(Action));
            });
            Assert.Throws<ArgumentNullException>(() => {
                deferred.Promise.CatchCancelation(default(Action<int>));
            });

            deferred.Cancel();

            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            Assert.Throws<ArgumentNullException>(() => {
                deferredInt.Promise.CatchCancelation(default(Action));
            });
            Assert.Throws<ArgumentNullException>(() => {
                deferredInt.Promise.CatchCancelation(default(Action<Exception>));
            });

            deferredInt.Cancel(0);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        public class IfOnCanceledIsAFunction
        {
            [Test]
            public void ItMustBeCalledAfterPromiseIsCanceledWithPromisesReasonAsItsFirstArgument()
            {
                string cancelReason = "Cancel value";
                var canceled = false;
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                deferred.Promise.CatchCancelation<string>(r => {
                    Assert.AreEqual(cancelReason, r);
                    canceled = true;
                });
                deferred.Cancel(cancelReason);
                Promise.Manager.HandleCompletes();

                Assert.True(canceled);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void ItMustNotBeCalledBeforePromiseIsCanceled()
            {
                string cancelReason = "Cancel value";
                var canceled = false;
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                deferred.Promise.CatchCancelation(() => {
                    canceled = true;
                });
                deferred.Promise.CatchCancelation<string>(r => {
                    Assert.AreEqual(cancelReason, r);
                    canceled = true;
                });
                Promise.Manager.HandleCompletes();

                Assert.False(canceled);

                deferred.Cancel(cancelReason);
                Promise.Manager.HandleCompletes();

                Assert.True(canceled);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void ItMustNotBeCalledMoreThanOnce()
            {
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                deferred.Retain();
                var cancelCount = 0;
                deferred.Promise.CatchCancelation(() => ++cancelCount);
                deferred.Promise.CatchCancelation<string>(r => ++cancelCount);
                deferred.Cancel("Cancel value");
                Promise.Manager.HandleCompletes();
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Cancel - Deferred is not in the pending state.");
                deferred.Cancel("Cancel value");
                deferred.Release();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(2, cancelCount);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        [UnityTest]
        public IEnumerator OnCanceledMustNotBeCalledUntilTheExecutionContextStackContainsOnlyPlatformCode()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            bool canceled = false;
            deferred.Promise.CatchCancelation(() => canceled = true);
            deferred.Cancel("Cancel value");
            Assert.False(canceled);
            yield return null;
            Promise.Manager.HandleCompletes();
            Assert.True(canceled);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        public class CatchCancelationMayBeCalledMultipleTimesOnTheSameIPotentialCancelation
        {
            [Test]
            public void IfWhenIPotentialCancelationIsCanceledAllRespectiveOnCanceledCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToCatchCancelation()
            {
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);

                int counter = 0;

                deferred.Promise.CatchCancelation(() => Assert.AreEqual(0, counter++));
                deferred.Promise.CatchCancelation(() => Assert.AreEqual(1, counter++));
                deferred.Promise.CatchCancelation(() => Assert.AreEqual(2, counter++));

                deferred.Cancel();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(3, counter);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class CatchCancelationMayReturnAnIPotentialCancelation
        {
            [Test]
            public void IfIPotentialCancelationIsCanceledAndItsReasonIsNotCompatibleWithOnCanceledItMustNotBeInvoked()
            {
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Promise.CatchCancelation((string v) => Assert.Fail("onCanceled was invoked with a string when the promise was canceled with an integer"));

                deferred.Cancel(100);
                Promise.Manager.HandleCompletes();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void IfIPotentialCancelationIsCanceledAndItsReasonIsNotCompatibleWithOnCanceledTheReturnedIPotentialCancelationMustBeCanceledWithTheSameReason()
            {
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);

                int expected = 100;

                deferred.Promise
                    .CatchCancelation((string v) => Assert.Fail("onCanceled was invoked with a string when the promise was canceled with an integer"))
                    .CatchCancelation((int v) => Assert.AreEqual(expected, v));

                deferred.Cancel(expected);
                Promise.Manager.HandleCompletes();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        [Test]
        public void IfPromiseIsCanceledOnResolveAndOnRejectedMustNotBeInvoked()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been canceled.");
            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been canceled.");

            TestHelper.AddCallbacks(deferred.Promise, resolveAssert, (object o) => rejectAssert(), rejectAssert);
            TestHelper.AddCallbacks(deferredInt.Promise, v => resolveAssert(), (object o) => rejectAssert(), rejectAssert);

            deferred.Cancel();
            deferredInt.Cancel();
            Promise.Manager.HandleCompletes();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CancelationsDoNotPropagateToRoot()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            bool resolved = false;

            ICancelableAny cancelable = deferred.Promise
                .Then(() => resolved = true)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."));

            cancelable.Cancel();
            deferred.Resolve();
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, resolved);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CancelationsPropagateToBranches()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            bool invoked = false;

            deferred.Promise
                .Then(() => { })
                .CatchCancelation(() => invoked = true);

            deferred.Cancel();
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }
    }
}