#pragma warning disable IDE1006 // Naming Styles

using NUnit.Framework;
using Proto.Promises;

namespace ProtoPromise.Tests.APIs
{
    public class APlus_2_1_PromiseStates
    {
        public class _2_1_1_WhenPendingAPromise
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.Setup();
            }

            [TearDown]
            public void Teardown()
            {
                TestHelper.Cleanup();
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
                Assert.IsNull(state);

                deferred.Resolve();
                Assert.AreEqual(Resolved, state);

                state = null;
                deferred = Promise.NewDeferred();

                deferred.Promise
                    .Then(() => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                deferred.Reject("Fail Value");
                Assert.AreEqual(Rejected, state);
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
                Assert.IsNull(state);

                deferred.Resolve(1);
                Assert.AreEqual(Resolved, state);

                state = null;
                deferred = Promise.NewDeferred<int>();

                deferred.Promise
                    .Then(v => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                deferred.Reject("Fail Value");
                Assert.AreEqual(Rejected, state);
            }
        }

        public class _2_1_2_WhenFulfilledAPromise
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.Setup();
            }

            [TearDown]
            public void Teardown()
            {
                TestHelper.Cleanup();
            }

            [Test]
            public void _2_1_2_1_MustNotTransitionToAnyOtherState_void()
            {
                string RejectValue = "Rejected";

                var deferred = Promise.NewDeferred();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    bool voidResolved = false, voidRejected = false;
                    promiseRetainer.WaitAsync()
                        .Then(() => voidResolved = true, () => voidRejected = true)
                        .Forget();

                    deferred.Resolve();
                    Assert.IsTrue(voidResolved);
                    Assert.IsFalse(voidRejected);

                    Assert.Catch<InvalidOperationException>(() => deferred.Resolve());
                    Assert.Catch<InvalidOperationException>(() => deferred.Reject(RejectValue));

                    promiseRetainer.WaitAsync()
                        .Then(() => voidResolved = true, () => voidRejected = true)
                        .Forget();
                    Assert.IsTrue(voidResolved);
                    Assert.IsFalse(voidRejected);
                }
            }

            [Test]
            public void _2_1_2_1_MustNotTransitionToAnyOtherState_T()
            {
                string RejectValue = "Rejected";

                var deferred = Promise.NewDeferred<int>();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    bool intResolved = false, intRejected = false;
                    promiseRetainer.WaitAsync()
                        .Then(_ => intResolved = true, () => intRejected = true)
                        .Forget();

                    deferred.Resolve(1);
                    Assert.IsTrue(intResolved);
                    Assert.IsFalse(intRejected);

                    Assert.Catch<InvalidOperationException>(() => deferred.Resolve(1));
                    Assert.Catch<InvalidOperationException>(() => deferred.Reject(RejectValue));

                    promiseRetainer.WaitAsync()
                        .Then(_ => intResolved = true, () => intRejected = true)
                        .Forget();
                    Assert.IsTrue(intResolved);
                    Assert.IsFalse(intRejected);
                }
            }

            [Test]
            public void _2_1_2_2_MustHaveAValueWhichMustNotChange()
            {
                var deferred = Promise.NewDeferred<int>();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    int result = -1;
                    int expected = 1;

                    TestHelper.AddCallbacks<int, bool, object, string>(promiseRetainer.WaitAsync(),
                        onResolve: num => { Assert.AreEqual(expected, num); result = num; },
                        onReject: s => Assert.Fail("Promise was rejected when it should have been resolved."),
                        onUnknownRejection: () => Assert.Fail("Promise was rejected when it should have been resolved.")
                    );
                    deferred.Resolve(expected);

                    Assert.AreEqual(expected, result);

                    TestHelper.AddCallbacks<int, bool, object, string>(promiseRetainer.WaitAsync(),
                        onResolve: num => { Assert.AreEqual(expected, num); result = num; },
                        onReject: s => Assert.Fail("Promise was rejected when it should have been resolved."),
                        onUnknownRejection: () => Assert.Fail("Promise was rejected when it should have been resolved.")
                    );
                    Assert.Catch<InvalidOperationException>(() => deferred.Resolve(100));

                    Assert.AreEqual(expected, result);
                }
            }
        }

        public class _2_1_3_WhenRejectedAPromise
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.Setup();
            }

            [TearDown]
            public void Teardown()
            {
                TestHelper.Cleanup();
            }

            [Test]
            public void _2_1_3_1_MustNotTransitionToAnyOtherState_void()
            {
                string RejectValue = "Rejected";

                var deferred = Promise.NewDeferred();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    bool voidResolved = false, voidRejected = false;
                    promiseRetainer.WaitAsync()
                        .Then(() => voidResolved = true, () => voidRejected = true)
                        .Forget();

                    deferred.Reject(RejectValue);
                    Assert.IsFalse(voidResolved);
                    Assert.IsTrue(voidRejected);

                    Assert.Catch<InvalidOperationException>(() => deferred.Resolve());
                    Assert.Catch<InvalidOperationException>(() => deferred.Reject(RejectValue));

                    promiseRetainer.WaitAsync()
                        .Then(() => voidResolved = true, () => voidRejected = true)
                        .Forget();
                    Assert.IsFalse(voidResolved);
                    Assert.IsTrue(voidRejected);
                }
            }

            [Test]
            public void _2_1_3_1_MustNotTransitionToAnyOtherState_T()
            {
                string RejectValue = "Rejected";

                var deferred = Promise.NewDeferred<int>();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    bool intResolved = false, intRejected = false;
                    promiseRetainer.WaitAsync()
                        .Then(_ => intResolved = true, () => intRejected = true)
                        .Forget();

                    deferred.Reject(RejectValue);
                    Assert.IsFalse(intResolved);
                    Assert.IsTrue(intRejected);

                    Assert.Catch<InvalidOperationException>(() => deferred.Resolve(1));
                    Assert.Catch<InvalidOperationException>(() => deferred.Reject(RejectValue));

                    promiseRetainer.WaitAsync()
                        .Then(_ => intResolved = true, () => intRejected = true)
                        .Forget();
                    Assert.IsFalse(intResolved);
                    Assert.IsTrue(intRejected);
                }
            }

            [Test]
            public void _2_1_3_2_MustHaveAReasonWhichMustNotChange_void()
            {
                var deferred = Promise.NewDeferred();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    string rejection = null;
                    string expected = "Fail Value";
                    TestHelper.AddCallbacks<int, string, string>(promiseRetainer.WaitAsync(),
                        onResolve: () => Assert.Fail("Promise was resolved when it should have been rejected."),
                        onReject: failValue =>
                        {
                            Assert.AreEqual(expected, failValue);
                            rejection = failValue;
                        });
                    deferred.Reject(expected);

                    Assert.AreEqual(expected, rejection);

                    Assert.Catch<InvalidOperationException>(() => deferred.Reject("Different Fail Value"));
                    TestHelper.AddCallbacks<int, string, string>(promiseRetainer.WaitAsync(),
                        onResolve: () => Assert.Fail("Promise was resolved when it should have been rejected."),
                        onReject: failValue =>
                        {
                            Assert.AreEqual(expected, failValue);
                            rejection = failValue;
                        });

                    Assert.AreEqual(expected, rejection);
                }
            }

            [Test]
            public void _2_1_3_2_MustHaveAReasonWhichMustNotChange_T()
            {
                var deferred = Promise.NewDeferred<int>();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    string rejection = null;
                    string expected = "Fail Value";
                    TestHelper.AddCallbacks<int, bool, string, string>(promiseRetainer.WaitAsync(),
                        onResolve: v => Assert.Fail("Promise was resolved when it should have been rejected."),
                        onReject: failValue =>
                        {
                            Assert.AreEqual(expected, failValue);
                            rejection = failValue;
                        });
                    deferred.Reject(expected);

                    Assert.AreEqual(expected, rejection);

                    Assert.Catch<InvalidOperationException>(() => deferred.Reject("Different Fail Value"));
                    TestHelper.AddCallbacks<int, bool, string, string>(promiseRetainer.WaitAsync(),
                        onResolve: v => Assert.Fail("Promise was resolved when it should have been rejected."),
                        onReject: failValue =>
                        {
                            Assert.AreEqual(expected, failValue);
                            rejection = failValue;
                        });

                    Assert.AreEqual(expected, rejection);
                }
            }
        }
    }
}