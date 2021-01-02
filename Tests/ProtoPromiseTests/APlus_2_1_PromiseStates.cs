#pragma warning disable IDE1006 // Naming Styles

using NUnit.Framework;

namespace Proto.Promises.Tests
{
    public class APlus_2_1_PromiseStates
    {
        [SetUp]
        public void Setup()
        {
            TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = null;
        }

        [TearDown]
        public void Teardown()
        {
            Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
        }

        public class _2_1_1_WhenPendingAPromise
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
                Promise.Config.UncaughtRejectionHandler = null;
            }

            [TearDown]
            public void Teardown()
            {
                Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
            }

            [Test]
            public void _2_1_1_1_MayTransitionToEitherTheFulfilledOrRejectedState_void()
            {
                string Resolved = "Resolved", Rejected = "Rejected";
                string state = null;

                var deferred = Promise.NewDeferred();
                deferred.Promise
                    .Then(() => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.AreEqual(null, state);

                deferred.Resolve();
                Promise.Manager.HandleCompletesAndProgress();

                Assert.AreEqual(Resolved, state);

                state = null;
                deferred = Promise.NewDeferred();
                deferred.Promise
                    .Then(() => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.AreEqual(null, state);

                deferred.Reject("Fail Value");
                Promise.Manager.HandleCompletesAndProgress();

                Assert.AreEqual(Rejected, state);

                TestHelper.Cleanup();
            }

            [Test]
            public void _2_1_1_1_MayTransitionToEitherTheFulfilledOrRejectedState_T()
            {
                string Resolved = "Resolved", Rejected = "Rejected";
                string state = null;

                var deferred = Promise.NewDeferred<int>();
                deferred.Promise
                    .Then(v => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.AreEqual(null, state);

                deferred.Resolve(1);
                Promise.Manager.HandleCompletesAndProgress();

                Assert.AreEqual(Resolved, state);

                state = null;
                deferred = Promise.NewDeferred<int>();
                deferred.Promise
                    .Then(v => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.AreEqual(null, state);

                deferred.Reject("Fail Value");
                Promise.Manager.HandleCompletesAndProgress();

                Assert.AreEqual(Rejected, state);

                TestHelper.Cleanup();
            }
        }

        public class _2_1_2_WhenFulfilledAPromise
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
                Promise.Config.UncaughtRejectionHandler = null;
            }

            [TearDown]
            public void Teardown()
            {
                Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
            }

            [Test]
            public void _2_1_2_1_MustNotTransitionToAnyOtherState_void()
            {
                string RejectValue = "Rejected";

                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                bool voidResolved = false, voidRejected = false;
                promise
                    .Then(() => voidResolved = true, () => voidRejected = true)
                    .Forget();

                deferred.Resolve();
                Promise.Manager.HandleCompletes();
                Assert.IsTrue(voidResolved);
                Assert.IsFalse(voidRejected);

                Assert.AreEqual(false, deferred.TryResolve());
                Assert.Throws<InvalidOperationException>(() => deferred.Resolve());
                Assert.AreEqual(false, deferred.TryReject(RejectValue));
                Assert.Throws<InvalidOperationException>(() => deferred.Reject(RejectValue));

                promise
                    .Then(() => voidResolved = true, () => voidRejected = true)
                    .Forget();
                Promise.Manager.HandleCompletes();
                Assert.IsTrue(voidResolved);
                Assert.IsFalse(voidRejected);

                promise.Forget();

                TestHelper.Cleanup();
            }

            [Test]
            public void _2_1_2_1_MustNotTransitionToAnyOtherState_T()
            {
                string RejectValue = "Rejected";

                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();
                bool intResolved = false, intRejected = false;
                promise
                    .Then(_ => intResolved = true, () => intRejected = true)
                    .Forget();

                deferred.Resolve(1);
                Promise.Manager.HandleCompletes();
                Assert.IsTrue(intResolved);
                Assert.IsFalse(intRejected);

                Assert.AreEqual(false, deferred.TryResolve(1));
                Assert.Throws<InvalidOperationException>(() => deferred.Resolve(1));
                Assert.AreEqual(false, deferred.TryReject(RejectValue));
                Assert.Throws<InvalidOperationException>(() => deferred.Reject(RejectValue));

                promise
                    .Then(_ => intResolved = true, () => intRejected = true)
                    .Forget();
                Promise.Manager.HandleCompletes();
                Assert.IsTrue(intResolved);
                Assert.IsFalse(intRejected);

                promise.Forget();

                TestHelper.Cleanup();
            }

            [Test]
            public void _2_1_2_2_MustHaveAValueWhichMustNotChange()
            {
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                int result = -1;
                int expected = 1;

                TestHelper.AddCallbacks<int, bool, object, string>(promise,
                    onResolve: num => { Assert.AreEqual(expected, num); result = num; },
                    onReject: s => Assert.Fail("Promise was rejected when it should have been resolved."),
                    onUnknownRejection: () => Assert.Fail("Promise was rejected when it should have been resolved.")
                );
                deferred.Resolve(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, result);

                TestHelper.AddCallbacks<int, bool, object, string>(promise,
                    onResolve: num => { Assert.AreEqual(expected, num); result = num; },
                    onReject: s => Assert.Fail("Promise was rejected when it should have been resolved."),
                    onUnknownRejection: () => Assert.Fail("Promise was rejected when it should have been resolved.")
                );
                Assert.AreEqual(false, deferred.TryResolve(100));
                Assert.Throws<InvalidOperationException>(() => deferred.Resolve(100));

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expected, result);

                promise.Forget();

                TestHelper.Cleanup();
            }
        }

        public class _2_1_3_WhenRejectedAPromise
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
                Promise.Config.UncaughtRejectionHandler = null;
            }

            [TearDown]
            public void Teardown()
            {
                Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
            }

            [Test]
            public void _2_1_3_1_MustNotTransitionToAnyOtherState_void()
            {
                string RejectValue = "Rejected";

                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                bool voidResolved = false, voidRejected = false;
                promise
                    .Then(() => voidResolved = true, () => voidRejected = true)
                    .Forget();

                deferred.Reject(RejectValue);
                Promise.Manager.HandleCompletes();
                Assert.IsFalse(voidResolved);
                Assert.IsTrue(voidRejected);

                Assert.AreEqual(false, deferred.TryResolve());
                Assert.Throws<InvalidOperationException>(() => deferred.Resolve());
                Assert.AreEqual(false, deferred.TryReject(RejectValue));
                Assert.Throws<InvalidOperationException>(() => deferred.Reject(RejectValue));

                promise
                    .Then(() => voidResolved = true, () => voidRejected = true)
                    .Forget();
                Promise.Manager.HandleCompletes();
                Assert.IsFalse(voidResolved);
                Assert.IsTrue(voidRejected);

                promise.Forget();

                TestHelper.Cleanup();
            }

            [Test]
            public void _2_1_3_1_MustNotTransitionToAnyOtherState_T()
            {
                string RejectValue = "Rejected";

                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();
                bool intResolved = false, intRejected = false;
                promise
                    .Then(_ => intResolved = true, () => intRejected = true)
                    .Forget();

                deferred.Reject(RejectValue);
                Promise.Manager.HandleCompletes();
                Assert.IsFalse(intResolved);
                Assert.IsTrue(intRejected);

                Assert.AreEqual(false, deferred.TryResolve(1));
                Assert.Throws<InvalidOperationException>(() => deferred.Resolve(1));
                Assert.AreEqual(false, deferred.TryReject(RejectValue));
                Assert.Throws<InvalidOperationException>(() => deferred.Reject(RejectValue));

                promise
                    .Then(_ => intResolved = true, () => intRejected = true)
                    .Forget();
                Promise.Manager.HandleCompletes();
                Assert.IsFalse(intResolved);
                Assert.IsTrue(intRejected);

                promise.Forget();

                TestHelper.Cleanup();
            }

            [Test]
            public void _2_1_3_2_MustHaveAReasonWhichMustNotChange_void()
            {
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                string rejection = null;
                string expected = "Fail Value";
                TestHelper.AddCallbacks<int, string, string>(promise,
                    onResolve: () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    onReject: failValue =>
                    {
                        Assert.AreEqual(expected, failValue);
                        rejection = failValue;
                    });
                deferred.Reject(expected);

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expected, rejection);

                Assert.AreEqual(false, deferred.TryReject("Different Fail Value"));
                Assert.Throws<InvalidOperationException>(() => deferred.Reject("Different Fail Value"));
                TestHelper.AddCallbacks<int, string, string>(promise,
                    onResolve: () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    onReject: failValue =>
                    {
                        Assert.AreEqual(expected, failValue);
                        rejection = failValue;
                    });

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expected, rejection);

                promise.Forget();
                TestHelper.Cleanup();
            }

            [Test]
            public void _2_1_3_2_MustHaveAReasonWhichMustNotChange_T()
            {
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                string rejection = null;
                string expected = "Fail Value";
                TestHelper.AddCallbacks<int, bool, string, string>(promise,
                    onResolve: v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    onReject: failValue =>
                    {
                        Assert.AreEqual(expected, failValue);
                        rejection = failValue;
                    });
                deferred.Reject(expected);

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expected, rejection);

                Assert.AreEqual(false, deferred.TryReject("Different Fail Value"));
                Assert.Throws<InvalidOperationException>(() => deferred.Reject("Different Fail Value"));
                TestHelper.AddCallbacks<int, bool, string, string>(promise,
                    onResolve: v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    onReject: failValue =>
                    {
                        Assert.AreEqual(expected, failValue);
                        rejection = failValue;
                    });

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expected, rejection);

                promise.Forget();
                TestHelper.Cleanup();
            }
        }
    }
}