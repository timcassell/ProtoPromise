#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE1006 // Naming Styles

using NUnit.Framework;
using Proto.Promises;
using System;

namespace ProtoPromiseTests.APIs
{
    public class APlus_2_2_TheThenMethod
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

#if PROMISE_DEBUG
        // These will only pass in DEBUG mode.
        public class _2_2_1_BothOnResolveAndOnRejectedAreOptionalArgument
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
            public void _2_2_1_1_IfOnFulfilledIsNull_Throw_void()
            {
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise;

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Action)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<Promise>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<Promise<int>>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Action), () => { }));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Action), (string failValue) => { }));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int>), () => default(int)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int>), (string failValue) => default(int)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<Promise>), () => default(Promise)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<Promise>), (string failValue) => default(Promise)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<Promise<int>>), () => default(Promise<int>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<Promise<int>>), (string failValue) => default(Promise<int>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Action), () => default(Promise)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Action), (string failValue) => default(Promise)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int>), () => default(Promise<int>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int>), (string failValue) => default(Promise<int>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<Promise>), () => { }));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<Promise>), (string failValue) => { }));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<Promise<int>>), () => default(int)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<Promise<int>>), (string failValue) => default(int)));

                deferred.Resolve();
                promise.Forget();
            }

            [Test]
            public void _2_2_1_1_IfOnFulfilledIsNull_Throw_T()
            {
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise;

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Action<int>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, int>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, Promise>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, Promise<int>>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Action<int>), () => { }));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Action<int>), (string failValue) => { }));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, int>), () => default(int)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, int>), (string failValue) => default(int)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, Promise>), () => default(Promise)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, Promise>), (string failValue) => default(Promise)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, Promise<int>>), () => default(Promise<int>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, Promise<int>>), (string failValue) => default(Promise<int>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Action<int>), () => default(Promise)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Action<int>), (string failValue) => default(Promise)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, int>), () => default(Promise<int>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, int>), (string failValue) => default(Promise<int>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, Promise>), () => { }));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, Promise>), (string failValue) => { }));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, Promise<int>>), () => default(int)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(default(Func<int, Promise<int>>), (string failValue) => default(int)));

                deferred.Resolve(0);
                promise.Forget();
            }

            [Test]
            public void _2_2_1_2_IfOnRejectedIsNull_Throw_void()
            {
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise;

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Catch(default(Action)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Catch(default(Action<string>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Catch(default(Func<Promise>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Catch(default(Func<string, Promise>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => { }, default(Action)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => { }, default(Action<string>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => default(Promise), default(Func<Promise>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => default(Promise), default(Func<string, Promise>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => "string", default(Func<string>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => "string", default(Func<Exception, string>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => default(Promise<string>), default(Func<Promise<string>>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => default(Promise<string>), default(Func<Exception, Promise<string>>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => default(Promise), default(Action)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => default(Promise), default(Action<string>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => { }, default(Func<Promise>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => { }, default(Func<string, Promise>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => default(Promise<string>), default(Func<string>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => default(Promise<string>), default(Func<Exception, string>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => "string", default(Func<Promise<string>>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then(() => "string", default(Func<Exception, Promise<string>>)));

                deferred.Resolve();
                promise.Forget();
            }

            [Test]
            public void _2_2_1_2_IfOnRejectedIsNull_Throw_T()
            {
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise;

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Catch(default(Func<int>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Catch(default(Func<string, int>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Catch(default(Func<Promise<int>>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Catch(default(Func<string, Promise<int>>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => { }, default(Action)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => { }, default(Action<string>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => default(Promise), default(Func<Promise>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => default(Promise), default(Func<string, Promise>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => "string", default(Func<string>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => "string", default(Func<Exception, string>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => default(Promise<string>), default(Func<Promise<string>>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => default(Promise<string>), default(Func<Exception, Promise<string>>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => default(Promise), default(Action)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => default(Promise), default(Action<string>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => { }, default(Func<Promise>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => { }, default(Func<string, Promise>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => default(Promise<string>), default(Func<string>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => default(Promise<string>), default(Func<Exception, string>)));

                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => "string", default(Func<Promise<string>>)));
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Then((int x) => "string", default(Func<Exception, Promise<string>>)));

                deferred.Resolve(0);
                promise.Forget();
            }
        }
#endif

        public class IfOnFulfilledIsAFunction_2_2_2
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
            public void _2_2_2_1_ItMustBeCalledAfterPromiseIsFulfilledWithPromisesValueAsItsFirstArgument()
            {
                var promisedValue = 100;
                var resolved = false;
                var deferred = Promise.NewDeferred<int>();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    TestHelper.AddResolveCallbacks<int, bool, string>(promiseRetainer.WaitAsync(),
                        onResolve: v =>
                        {
                            Assert.AreEqual(promisedValue, v);
                            resolved = true;
                        }
                    );
                    TestHelper.AddCallbacks<int, bool, object, string>(promiseRetainer.WaitAsync(),
                        onResolve: v =>
                        {
                            Assert.AreEqual(promisedValue, v);
                            resolved = true;
                        }
                    );
                    deferred.Resolve(promisedValue);

                    Assert.True(resolved);
                }
            }

            [Test]
            public void _2_2_2_2_ItMustNotBeCalledBeforePromiseIsFulfilled_void()
            {
                var resolved = false;
                var deferred = Promise.NewDeferred();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    TestHelper.AddResolveCallbacks<bool, string>(promiseRetainer.WaitAsync(),
                    () => resolved = true
                );
                    TestHelper.AddCallbacks<bool, object, string>(promiseRetainer.WaitAsync(),
                        () => resolved = true,
                        s => Assert.Fail("Promise was rejected when it should have been resolved."),
                        () => Assert.Fail("Promise was rejected when it should have been resolved.")
                    );

                    Assert.False(resolved);

                    deferred.Resolve();

                    Assert.True(resolved);
                }
            }

            [Test]
            public void _2_2_2_2_ItMustNotBeCalledBeforePromiseIsFulfilled_T()
            {
                var resolved = false;
                var deferred = Promise.NewDeferred<int>();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    TestHelper.AddResolveCallbacks<int, bool, string>(promiseRetainer.WaitAsync(),
                        v => resolved = true
                    );
                    TestHelper.AddCallbacks<int, bool, object, string>(promiseRetainer.WaitAsync(),
                        v => resolved = true,
                        s => Assert.Fail("Promise was rejected when it should have been resolved."),
                        () => Assert.Fail("Promise was rejected when it should have been resolved.")
                    );

                    Assert.False(resolved);

                    deferred.Resolve(100);

                    Assert.True(resolved);
                }
            }

            [Test]
            public void _2_2_2_3_ItMustNotBeCalledMoreThanOnce_void()
            {
                var resolveCount = 0;
                var deferred = Promise.NewDeferred();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    TestHelper.AddResolveCallbacks<bool, string>(promiseRetainer.WaitAsync(),
                        () => ++resolveCount
                    );
                    TestHelper.AddCallbacks<bool, object, string>(promiseRetainer.WaitAsync(),
                        () => ++resolveCount,
                        s => Assert.Fail("Promise was rejected when it should have been resolved."),
                        () => Assert.Fail("Promise was rejected when it should have been resolved.")
                    );
                    deferred.Resolve();

                    Assert.IsFalse(deferred.TryResolve());
                    Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Resolve());

                    Assert.AreEqual(
                        (TestHelper.resolveVoidVoidCallbacks + TestHelper.resolveVoidConvertCallbacks +
                        TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks) * 2,
                        resolveCount
                    );
                }
            }

            [Test]
            public void _2_2_2_3_ItMustNotBeCalledMoreThanOnce_T()
            {
                var resolveCount = 0;
                var deferred = Promise.NewDeferred<int>();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    TestHelper.AddResolveCallbacks<int, bool, string>(promiseRetainer.WaitAsync(),
                        x => ++resolveCount
                    );
                    TestHelper.AddCallbacks<int, bool, object, string>(promiseRetainer.WaitAsync(),
                        x => ++resolveCount,
                        s => Assert.Fail("Promise was rejected when it should have been resolved."),
                        () => Assert.Fail("Promise was rejected when it should have been resolved.")
                    );
                    deferred.Resolve(1);

                    Assert.IsFalse(deferred.TryResolve(1));
                    Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Resolve(100));

                    Assert.AreEqual(
                        (TestHelper.resolveTVoidCallbacks + TestHelper.resolveTConvertCallbacks +
                        TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks) * 2,
                        resolveCount
                    );
                }
            }
        }

        public class _2_2_3_IfOnRejectedIsAFunction
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
            public void _2_2_3_1_ItMustBeCalledAfterPromiseIsRejected_WithPromisesReasonAsItsFirstArgument_void()
            {
                var rejectReason = "Fail value";
                var errored = false;
                var deferred = Promise.NewDeferred<int>();

                TestHelper.AddCallbacks<int, bool, string, string>(deferred.Promise,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    reason =>
                    {
                        Assert.AreEqual(rejectReason, reason);
                        errored = true;
                    }
                );
                deferred.Reject(rejectReason);

                Assert.True(errored);
            }

            [Test]
            public void _2_2_3_1_ItMustBeCalledAfterPromiseIsRejected_WithPromisesReasonAsItsFirstArgument_T()
            {
                var rejectReason = "Fail value";
                var errored = false;
                var deferred = Promise.NewDeferred();

                TestHelper.AddCallbacks<bool, string, string>(deferred.Promise,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    reason =>
                    {
                        Assert.AreEqual(rejectReason, reason);
                        errored = true;
                    }
                );
                deferred.Reject(rejectReason);

                Assert.True(errored);
            }

            [Test]
            public void _2_2_3_2_ItMustNotBeCalledBeforePromiseIsRejected_void()
            {
                var errored = false;
                var deferred = Promise.NewDeferred<int>();

                TestHelper.AddCallbacks<int, bool, string, string>(deferred.Promise,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    reason => errored = true
                );

                Assert.False(errored);

                deferred.Reject("Fail value");

                Assert.True(errored);
            }

            [Test]
            public void _2_2_3_2_ItMustNotBeCalledBeforePromiseIsRejected_T()
            {
                var errored = false;
                var deferred = Promise.NewDeferred();

                TestHelper.AddCallbacks<bool, string, string>(deferred.Promise,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    reason => errored = true
                );

                Assert.False(errored);

                deferred.Reject("Fail value");

                Assert.True(errored);
            }

            [Test]
            public void _2_2_3_3_ItMustNotBeCalledMoreThanOnce_void()
            {
                var errorCount = 0;
                var deferred = Promise.NewDeferred();

                TestHelper.AddCallbacks<bool, object, string>(deferred.Promise,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    x => ++errorCount,
                    () => ++errorCount
                );
                deferred.Reject("Fail value");

                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Reject("Fail value"));

                Assert.AreEqual(
                    (TestHelper.rejectVoidVoidCallbacks + TestHelper.rejectVoidConvertCallbacks +
                    TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks) * 2,
                    errorCount
                );
            }

            [Test]
            public void _2_2_3_3_ItMustNotBeCalledMoreThanOnce_T()
            {
                var errorCount = 0;
                var deferred = Promise.NewDeferred<int>();

                TestHelper.AddCallbacks<int, bool, object, string>(deferred.Promise,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    x => ++errorCount,
                    () => ++errorCount
                );
                deferred.Reject("Fail value");

                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Reject("Fail value"));

                Assert.AreEqual(
                    (TestHelper.rejectTVoidCallbacks + TestHelper.rejectTConvertCallbacks + TestHelper.rejectTTCallbacks +
                    TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks) * 2,
                    errorCount
                );
            }
        }

        // This is implemented in C# via a SynchronizationContext set in the Config.
        // Unit tests here directly invoke the callbacks sent to the SynchronizationContext, but different applications may handle it differently.
        // (In Unity, it executes all callbacks on the main thread every frame).
        [Test]
        public void _2_2_4_OnFulfilledMustNotBeCalledUntilTheExecutionContextStackContainsOnlyPlatformCode_void()
        {
            bool resolved = false;
            var deferred = Promise.NewDeferred();
            using (var promiseRetainer = deferred.Promise.GetRetainer())
            {
                TestHelper.AddResolveCallbacks<bool, string>(promiseRetainer.WaitAsync(),
                    () => resolved = true,
                    continuationOptions: new ContinuationOptions(SynchronizationOption.Foreground, forceAsync: true)
                );
                TestHelper.AddCallbacks<bool, object, string>(promiseRetainer.WaitAsync(),
                    () => resolved = true,
                    s => Assert.Fail("Promise was rejected when it should have been resolved."),
                    continuationOptions: new ContinuationOptions(SynchronizationOption.Foreground, forceAsync: true)
                );
                deferred.Resolve();
                Assert.False(resolved);

                TestHelper.ExecuteForegroundCallbacks();
                Assert.True(resolved);
            }
        }

        [Test]
        public void _2_2_4_OnFulfilledMustNotBeCalledUntilTheExecutionContextStackContainsOnlyPlatformCode_T()
        {
            bool resolved = false;
            var deferred = Promise.NewDeferred<int>();
            using (var promiseRetainer = deferred.Promise.GetRetainer())
            {
                TestHelper.AddResolveCallbacks<int, bool, string>(promiseRetainer.WaitAsync(),
                    v => resolved = true,
                    continuationOptions: new ContinuationOptions(SynchronizationOption.Foreground, forceAsync: true)
                );
                TestHelper.AddCallbacks<int, bool, object, string>(promiseRetainer.WaitAsync(),
                    v => resolved = true,
                    s => Assert.Fail("Promise was rejected when it should have been resolved."),
                    continuationOptions: new ContinuationOptions(SynchronizationOption.Foreground, forceAsync: true)
                );
                deferred.Resolve(1);
                Assert.False(resolved);

                TestHelper.ExecuteForegroundCallbacks();
                Assert.True(resolved);
            }
        }

        [Test]
        public void _2_2_4_OnRejectedMustNotBeCalledUntilTheExecutionContextStackContainsOnlyPlatformCode_void()
        {
            bool errored = false;
            var deferred = Promise.NewDeferred();

            TestHelper.AddCallbacks<bool, object, string>(deferred.Promise,
                () => Assert.Fail("Promise was resolved when it should have been rejected."),
                s => errored = true,
                continuationOptions: new ContinuationOptions(SynchronizationOption.Foreground, forceAsync: true)
            );
            deferred.Reject("Fail value");
            Assert.False(errored);

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(errored);
        }

        [Test]
        public void _2_2_4_OnRejectedMustNotBeCalledUntilTheExecutionContextStackContainsOnlyPlatformCode_T()
        {
            bool errored = false;
            var deferred = Promise.NewDeferred<int>();

            TestHelper.AddCallbacks<int, bool, object, string>(deferred.Promise,
                v => Assert.Fail("Promise was resolved when it should have been rejected."),
                s => errored = true,
                continuationOptions: new ContinuationOptions(SynchronizationOption.Foreground, forceAsync: true)
            );
            deferred.Reject("Fail value");
            Assert.False(errored);

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(errored);
        }

        // Not relevant for C#
        // 2.2.5 onFulfilled and onRejected must be called as functions (i.e. with no this value)

        public class _2_2_6_ThenMayBeCalledMultipleTimesOnTheSamePromise
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
            public void _2_2_6_1_IfWhenPromiseIsFulfilled_AllRespectiveOnFulfilledCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToThen_void()
            {
                var deferred = Promise.NewDeferred();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    int counter = 0;
                    for (int i = 0; i < 10; ++i)
                    {
                        int expected = i;
                        promiseRetainer.WaitAsync()
                            .Then(() => Assert.AreEqual(expected, counter++))
                            .Forget();
                    }

                    deferred.Resolve();
                    Assert.AreEqual(10, counter);
                }
            }

            [Test]
            public void _2_2_6_1_IfWhenPromiseIsFulfilled_AllRespectiveOnFulfilledCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToThen_T()
            {
                var deferred = Promise.NewDeferred<int>();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    int counter = 0;
                    for (int i = 0; i < 10; ++i)
                    {
                        int expected = i;
                        promiseRetainer.WaitAsync()
                            .Then(v => Assert.AreEqual(expected, counter++))
                            .Forget();
                    }

                    deferred.Resolve(100);
                    Assert.AreEqual(10, counter);
                }
            }

            [Test]
            public void _2_2_6_2_IfWhenPromiseIsRejected_AllRespectiveOnRejectedCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToThenOrCatch_void()
            {
                var deferred = Promise.NewDeferred();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    int counter = 0;
                    for (int i = 0; i < 10; ++i)
                    {
                        int expected = i;
                        promiseRetainer.WaitAsync()
                        .Catch(() => Assert.AreEqual(expected, counter++))
                        .Forget();
                    }

                    deferred.Reject("Fail value");
                    Assert.AreEqual(10, counter);
                }
            }

            [Test]
            public void _2_2_6_2_IfWhenPromiseIsRejected_AllRespectiveOnRejectedCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToThenOrCatch_T()
            {
                var deferred = Promise.NewDeferred<int>();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    int counter = 0;
                    for (int i = 0; i < 10; ++i)
                    {
                        int expected = i;
                        promiseRetainer.WaitAsync()
                        .Catch(() => Assert.AreEqual(expected, counter++))
                        .Forget();
                    }

                    deferred.Reject("Fail value");
                    Assert.AreEqual(10, counter);
                }
            }
        }

        public class ThenMustReturnAPromise_2_2_7
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

            // 2.2.7.1 Promise Resolution Procedure in 2.3

            [Test]
            public void _2_2_7_2_IfOnFulfilledThrowsAnExceptionE_Promise2MustBeRejectedWithEAsTheReason_void()
            {
                var deferred = Promise.NewDeferred();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    int exceptionCount = 0;
                    Exception expected = new Exception("Fail value");

                    Action<Promise> catchCallback = p =>
                        p.Catch((Exception e) =>
                        {
                            Assert.AreEqual(expected, e);
                            ++exceptionCount;
                        }).Forget();

                    TestHelper.AddResolveCallbacks<bool, string>(promiseRetainer.WaitAsync(),
                        onResolve: () => { throw expected; },
                        onResolveCapture: _ => { throw expected; },
                        onCallbackAdded: (ref Promise p) => catchCallback(p),
                        onCallbackAddedConvert: (ref Promise<bool> p) => catchCallback(p)
                    );
                    TestHelper.AddCallbacks<bool, object, string>(promiseRetainer.WaitAsync(),
                        onReject: s => Assert.Fail("Promise was rejected when it should have been resolved."),
                        onUnknownRejection: () => Assert.Fail("Promise was rejected when it should have been resolved."),
                        onResolve: () => { throw expected; },
                        onResolveCapture: _ => { throw expected; },
                        onCallbackAdded: (ref Promise p) => catchCallback(p),
                        onCallbackAddedConvert: (ref Promise<bool> p) => catchCallback(p)
                    );

                    deferred.Resolve();

                    Assert.AreEqual(
                        (TestHelper.resolveVoidVoidCallbacks + TestHelper.resolveVoidConvertCallbacks +
                        TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks) * 2,
                        exceptionCount
                    );
                }
            }

            [Test]
            public void _2_2_7_2_IfOnFulfilledThrowsAnExceptionE_Promise2MustBeRejectedWithEAsTheReason_T()
            {
                var deferred = Promise.NewDeferred<int>();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    int exceptionCount = 0;
                    Exception expected = new Exception("Fail value");

                    Action<Promise> catchCallback = p =>
                        p.Catch((Exception e) =>
                        {
                            Assert.AreEqual(expected, e);
                            ++exceptionCount;
                        }).Forget();

                    TestHelper.AddResolveCallbacks<int, bool, string>(promiseRetainer.WaitAsync(),
                        onResolve: v => { throw expected; },
                        onResolveCapture: _ => { throw expected; },
                        onCallbackAdded: (ref Promise p) => catchCallback(p),
                        onCallbackAddedConvert: (ref Promise<bool> p) => catchCallback(p)
                    );
                    TestHelper.AddCallbacks<int, bool, object, string>(promiseRetainer.WaitAsync(),
                        onReject: s => Assert.Fail("Promise was rejected when it should have been resolved."),
                        onUnknownRejection: () => Assert.Fail("Promise was rejected when it should have been resolved."),
                        onResolve: v => { throw expected; },
                        onResolveCapture: _ => { throw expected; },
                        onCallbackAdded: (ref Promise p) => catchCallback(p),
                        onCallbackAddedConvert: (ref Promise<bool> p) => catchCallback(p),
                        onCallbackAddedT: (ref Promise<int> p) => catchCallback(p)
                    );

                    deferred.Resolve(100);

                    Assert.AreEqual(
                        (TestHelper.resolveTVoidCallbacks + TestHelper.resolveTConvertCallbacks +
                        TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks) * 2,
                        exceptionCount
                    );
                }
            }

            [Test]
            public void _2_2_7_2_IfOnRejectedThrowsAnExceptionE_Promise2MustBeRejectedWithEAsTheReason_void()
            {
                var deferred = Promise.NewDeferred();

                int exceptionCount = 0;
                Exception expected = new Exception("Fail value");

                Action<Promise> catchCallback = p =>
                    p.Catch((Exception e) =>
                    {
                        Assert.AreEqual(expected, e);
                        ++exceptionCount;
                    }).Forget();

                TestHelper.AddCallbacks<bool, object, string>(deferred.Promise,
                    onResolve: () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    onReject: _ => { throw expected; },
                    onRejectCapture: _ => { throw expected; },
                    onUnknownRejection: () => { throw expected; },
                    onUnknownRejectionCapture: _ => { throw expected; },
                    onCallbackAdded: (ref Promise p) => catchCallback(p),
                    onCallbackAddedConvert: (ref Promise<bool> p) => catchCallback(p)
                );

                deferred.Reject("Fail value");

                Assert.AreEqual(
                    (TestHelper.rejectVoidVoidCallbacks + TestHelper.rejectVoidConvertCallbacks +
                    TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks) * 2,
                    exceptionCount
                );
            }

            [Test]
            public void _2_2_7_2_IfOnRejectedThrowsAnExceptionE_Promise2MustBeRejectedWithEAsTheReason_T()
            {
                var deferred = Promise.NewDeferred<int>();

                int exceptionCount = 0;
                Exception expected = new Exception("Fail value");

                Action<Promise> catchCallback = p =>
                    p.Catch((Exception e) =>
                    {
                        Assert.AreEqual(expected, e);
                        ++exceptionCount;
                    }).Forget();

                TestHelper.AddCallbacks<int, bool, object, string>(deferred.Promise,
                    onResolve: _ => Assert.Fail("Promise was resolved when it should have been rejected."),
                    onReject: _ => { throw expected; },
                    onRejectCapture: _ => { throw expected; },
                    onUnknownRejection: () => { throw expected; },
                    onUnknownRejectionCapture: _ => { throw expected; },
                    onCallbackAdded: (ref Promise p) => catchCallback(p),
                    onCallbackAddedConvert: (ref Promise<bool> p) => catchCallback(p),
                    onCallbackAddedT: (ref Promise<int> p) => catchCallback(p)
                );

                deferred.Reject("Fail value");

                Assert.AreEqual(
                    (TestHelper.rejectTVoidCallbacks + TestHelper.rejectTConvertCallbacks + TestHelper.rejectTTCallbacks +
                    TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks) * 2,
                    exceptionCount
                );
            }

            [Test]
            public void _2_2_7_3_IfOnFulfilledIsNotAFunctionAndPromise1IsFulfilled_Promise2MustBeFulfilledWithTheSameValueAsPromise1_void_0()
            {
                int counter = 0;

                var deferred = Promise.NewDeferred();
                var promise1 = deferred.Promise;
                using (var promiseRetainer2 = promise1
                    .Catch(() => { Assert.Fail("Promise was rejected when it should have been resolved."); return; })
                    .GetRetainer())
                {
                    TestHelper.AddResolveCallbacks<bool, string>(promiseRetainer2.WaitAsync(),
                        () => ++counter
                    );
                    TestHelper.AddCallbacks<bool, object, string>(promiseRetainer2.WaitAsync(),
                        () => ++counter,
                        s => Assert.Fail("Promise was rejected when it should have been resolved.")
                    );

                    deferred.Resolve();

                    Assert.AreEqual(
                        TestHelper.resolveVoidCallbacks * 2,
                        counter
                    );
                }
            }

            [Test]
            public void _2_2_7_3_IfOnFulfilledIsNotAFunctionAndPromise1IsFulfilled_Promise2MustBeFulfilledWithTheSameValueAsPromise1_void_1()
            {
                int counter = 0;

                var deferred = Promise.NewDeferred();
                var promise1 = deferred.Promise;
                using (var promiseRetainer2 = promise1
                    .Catch(() => { Assert.Fail("Promise was rejected when it should have been resolved."); return Promise.Resolved(); })
                    .GetRetainer())
                {
                    TestHelper.AddResolveCallbacks<bool, string>(promiseRetainer2.WaitAsync(),
                        () => ++counter
                    );
                    TestHelper.AddCallbacks<bool, object, string>(promiseRetainer2.WaitAsync(),
                        () => ++counter,
                        s => Assert.Fail("Promise was rejected when it should have been resolved.")
                    );

                    deferred.Resolve();

                    Assert.AreEqual(
                        TestHelper.resolveVoidCallbacks * 2,
                        counter
                    );
                }
            }

            [Test]
            public void _2_2_7_3_IfOnFulfilledIsNotAFunctionAndPromise1IsFulfilled_Promise2MustBeFulfilledWithTheSameValueAsPromise1_T_0()
            {
                int expected = 100;
                int counter = 0;

                var deferred = Promise.NewDeferred<int>();
                var promise1 = deferred.Promise;
                using (var promiseRetainer2 = promise1
                    .Catch(() => { Assert.Fail("Promise was rejected when it should have been resolved."); return 50; })
                    .GetRetainer())
                {
                    TestHelper.AddResolveCallbacks<int, bool, string>(promiseRetainer2.WaitAsync(),
                        v => { Assert.AreEqual(expected, v); ++counter; }
                    );
                    TestHelper.AddCallbacks<int, bool, object, string>(promiseRetainer2.WaitAsync(),
                        v => { Assert.AreEqual(expected, v); ++counter; },
                        s => Assert.Fail("Promise was rejected when it should have been resolved.")
                    );

                    deferred.Resolve(expected);

                    Assert.AreEqual(
                        TestHelper.resolveTCallbacks * 2,
                        counter
                    );
                }
            }

            [Test]
            public void _2_2_7_3_IfOnFulfilledIsNotAFunctionAndPromise1IsFulfilled_Promise2MustBeFulfilledWithTheSameValueAsPromise1_T_1()
            {
                int expected = 100;
                int counter = 0;

                var deferred = Promise.NewDeferred<int>();
                var promise1 = deferred.Promise;
                using (var promiseRetainer2 = promise1
                    .Catch(() => { Assert.Fail("Promise was rejected when it should have been resolved."); return Promise.Resolved(50); })
                    .GetRetainer())
                {
                    TestHelper.AddResolveCallbacks<int, bool, string>(promiseRetainer2.WaitAsync(),
                        v => { Assert.AreEqual(expected, v); ++counter; }
                    );
                    TestHelper.AddCallbacks<int, bool, object, string>(promiseRetainer2.WaitAsync(),
                        v => { Assert.AreEqual(expected, v); ++counter; },
                        s => Assert.Fail("Promise was rejected when it should have been resolved.")
                    );

                    deferred.Resolve(expected);

                    Assert.AreEqual(
                        TestHelper.resolveTCallbacks * 2,
                        counter
                    );
                }
            }

            [Test]
            public void _2_2_7_4_IfOnRejectedIsNotAFunctionAndPromise1IsRejected_Promise2MustBeRejectedWithTheSameReasonAsPromise1_void_0()
            {
                string expected = "Fail value";
                int counter = 0;

                var deferred = Promise.NewDeferred();
                var promise1 = deferred.Promise;
                var promise2 = promise1
                    .Then(() => { Assert.Fail("Promise was resolved when it should have been rejected."); return; });

                TestHelper.AddCallbacks<bool, object, string>(promise2,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    e => { Assert.AreEqual(expected, e); ++counter; },
                    () => ++counter
                );

                deferred.Reject(expected);

                Assert.AreEqual(
                    TestHelper.rejectVoidCallbacks * 2,
                    counter
                );
            }

            [Test]
            public void _2_2_7_4_IfOnRejectedIsNotAFunctionAndPromise1IsRejected_Promise2MustBeRejectedWithTheSameReasonAsPromise1_void_1()
            {
                string expected = "Fail value";
                int counter = 0;

                var deferred = Promise.NewDeferred();
                var promise1 = deferred.Promise;
                var promise2 = promise1
                    .Then(() => { Assert.Fail("Promise was resolved when it should have been rejected."); return Promise.Resolved(); });

                TestHelper.AddCallbacks<bool, object, string>(promise2,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    e => { Assert.AreEqual(expected, e); ++counter; },
                    () => ++counter
                );

                deferred.Reject(expected);

                Assert.AreEqual(
                    TestHelper.rejectVoidCallbacks * 2,
                    counter
                );
            }

            [Test]
            public void _2_2_7_4_IfOnRejectedIsNotAFunctionAndPromise1IsRejected_Promise2MustBeRejectedWithTheSameReasonAsPromise1_void_2()
            {
                string expected = "Fail value";
                int counter = 0;

                var deferred = Promise.NewDeferred();
                var promise1 = deferred.Promise;
                var promise2 = promise1
                    .Then(() => { Assert.Fail("Promise was resolved when it should have been rejected."); return 50; });

                TestHelper.AddCallbacks<int, bool, object, string>(promise2,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    e => { Assert.AreEqual(expected, e); ++counter; },
                    () => ++counter
                );

                deferred.Reject(expected);

                Assert.AreEqual(
                    TestHelper.rejectVoidCallbacks * 2,
                    counter
                );
            }

            [Test]
            public void _2_2_7_4_IfOnRejectedIsNotAFunctionAndPromise1IsRejected_Promise2MustBeRejectedWithTheSameReasonAsPromise1_void_3()
            {
                string expected = "Fail value";
                int counter = 0;

                var deferred = Promise.NewDeferred();
                var promise1 = deferred.Promise;
                var promise2 = promise1
                    .Then(() => { Assert.Fail("Promise was resolved when it should have been rejected."); return Promise.Resolved(50); });

                TestHelper.AddCallbacks<int, bool, object, string>(promise2,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    e => { Assert.AreEqual(expected, e); ++counter; },
                    () => ++counter
                );

                deferred.Reject(expected);

                Assert.AreEqual(
                    TestHelper.rejectVoidCallbacks * 2,
                    counter
                );
            }

            [Test]
            public void _2_2_7_4_IfOnRejectedIsNotAFunctionAndPromise1IsRejected_Promise2MustBeRejectedWithTheSameReasonAsPromise1_T_0()
            {
                string expected = "Fail value";
                int counter = 0;

                var deferred = Promise.NewDeferred<int>();
                var promise1 = deferred.Promise;
                var promise2 = promise1
                    .Then(v => { Assert.Fail("Promise was resolved when it should have been rejected."); return 50; });

                TestHelper.AddCallbacks<int, bool, object, string>(promise2,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    e => { Assert.AreEqual(expected, e); ++counter; },
                    () => ++counter
                );

                deferred.Reject(expected);

                Assert.AreEqual(
                    TestHelper.rejectTCallbacks * 2,
                    counter
                );
            }

            [Test]
            public void _2_2_7_4_IfOnRejectedIsNotAFunctionAndPromise1IsRejected_Promise2MustBeRejectedWithTheSameReasonAsPromise1_T_1()
            {
                string expected = "Fail value";
                int counter = 0;

                var deferred = Promise.NewDeferred<int>();
                var promise1 = deferred.Promise;
                var promise2 = promise1
                    .Then(v => { Assert.Fail("Promise was resolved when it should have been rejected."); return Promise.Resolved(50); });

                TestHelper.AddCallbacks<int, bool, object, string>(promise2,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    e => { Assert.AreEqual(expected, e); ++counter; },
                    () => ++counter
                );

                deferred.Reject(expected);

                Assert.AreEqual(
                    TestHelper.rejectTCallbacks * 2,
                    counter
                );
            }

            [Test]
            public void _2_2_7_4_IfOnRejectedIsNotAFunctionAndPromise1IsRejected_Promise2MustBeRejectedWithTheSameReasonAsPromise1_T_2()
            {
                string expected = "Fail value";
                int counter = 0;

                var deferred = Promise.NewDeferred<int>();
                var promise1 = deferred.Promise;
                var promise2 = promise1
                    .Then(v => { Assert.Fail("Promise was resolved when it should have been rejected."); return; });

                TestHelper.AddCallbacks<bool, object, string>(promise2,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    e => { Assert.AreEqual(expected, e); ++counter; },
                    () => ++counter
                );

                deferred.Reject(expected);

                Assert.AreEqual(
                    TestHelper.rejectTCallbacks * 2,
                    counter
                );
            }

            [Test]
            public void _2_2_7_4_IfOnRejectedIsNotAFunctionAndPromise1IsRejected_Promise2MustBeRejectedWithTheSameReasonAsPromise1_T_3()
            {
                string expected = "Fail value";
                int counter = 0;

                var deferred = Promise.NewDeferred<int>();
                var promise1 = deferred.Promise;
                var promise2 = promise1
                    .Then(v => { Assert.Fail("Promise was resolved when it should have been rejected."); return Promise.Resolved(); });

                TestHelper.AddCallbacks<bool, object, string>(promise2,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    e => { Assert.AreEqual(expected, e); ++counter; },
                    () => ++counter
                );

                deferred.Reject(expected);

                Assert.AreEqual(
                    TestHelper.rejectTCallbacks * 2,
                    counter
                );
            }

            [Test]
            public void IfPromise1IsRejectedAndItsReasonIsNotCompatibleWithOnRejected_ItMustNotBeInvoked_void()
            {
                int counter = 0;
                var deferred = Promise.NewDeferred();
                var promise1 = deferred.Promise;

                TestHelper.AddCallbacks<bool, string, string>(promise1,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    e => { ++counter; Assert.Fail("OnRejected was invoked with a string when the promise was rejected with an integer."); },
                    onCallbackAdded: (ref Promise p) => p.Catch((int _) => { }).Forget(),
                    onCallbackAddedConvert: (ref Promise<bool> p) => p.Catch((int _) => { }).Forget()
                );

                deferred.Reject(100);

                Assert.AreEqual(0, counter);
            }

            [Test]
            public void IfPromise1IsRejectedAndItsReasonIsNotCompatibleWithOnRejected_ItMustNotBeInvoked_T()
            {
                int counter = 0;
                var deferred = Promise.NewDeferred<int>();
                var promise1 = deferred.Promise;

                TestHelper.AddCallbacks<int, bool, string, string>(promise1,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    e => { ++counter; Assert.Fail("OnRejected was invoked with a string when the promise was rejected with an integer."); },
                    onCallbackAdded: (ref Promise p) => p.Catch((int _) => { }).Forget(),
                    onCallbackAddedConvert: (ref Promise<bool> p) => p.Catch((int _) => { }).Forget(),
                    onCallbackAddedT: (ref Promise<int> p) => p.Catch((int _) => { }).Forget()
                );

                deferred.Reject(100);

                Assert.AreEqual(0, counter);
            }

            [Test]
            public void IfPromise1IsRejectedAndItsReasonIsNotCompatibleWithOnRejected_Promise2MustBeRejectedWithTheSameReasonAsPromise1_void()
            {
                int expected = 100;
                int counter = 0;

                var deferred = Promise.NewDeferred();
                var promise1 = deferred.Promise;

                Action<Promise> catchCallback = p =>
                    p.Catch((int i) =>
                    {
                        Assert.AreEqual(expected, i);
                        ++counter;
                    }).Forget();

                TestHelper.AddCallbacks<int, string, string>(promise1,
                    onResolve: () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    onReject: (string cancelString) => Assert.Fail("OnRejected was invoked with a string when the promise was rejected with an integer."),
                    onCallbackAdded: (ref Promise p) => catchCallback(p),
                    onCallbackAddedConvert: (ref Promise<int> p) => catchCallback(p)
                );

                deferred.Reject(expected);

                Assert.AreEqual(
                    (TestHelper.rejectVoidVoidCallbacks + TestHelper.rejectVoidConvertCallbacks +
                    TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks
                    - TestHelper.rejectVoidKnownCallbacks) * 2,
                    counter
                );
            }

            [Test]
            public void IfPromise1IsRejectedAndItsReasonIsNotCompatibleWithOnRejected_Promise2MustBeRejectedWithTheSameReasonAsPromise1_T()
            {
                int expected = 100;
                int counter = 0;

                var deferred = Promise.NewDeferred<int>();
                var promise1 = deferred.Promise;

                Action<Promise> catchCallback = p =>
                    p.Catch((int i) =>
                    {
                        Assert.AreEqual(expected, i);
                        ++counter;
                    }).Forget();

                TestHelper.AddCallbacks<int, int, string, string>(promise1,
                    onResolve: _ => Assert.Fail("Promise was resolved when it should have been rejected."),
                    onReject: (string cancelString) => Assert.Fail("OnRejected was invoked with a string when the promise was rejected with an integer."),
                    onCallbackAdded: (ref Promise p) => catchCallback(p),
                    onCallbackAddedConvert: (ref Promise<int> p) => catchCallback(p),
                    onCallbackAddedT: (ref Promise<int> p) => catchCallback(p)
                );

                deferred.Reject(expected);

                Assert.AreEqual(
                    (TestHelper.rejectTVoidCallbacks + TestHelper.rejectTConvertCallbacks + TestHelper.rejectTTCallbacks +
                    TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks
                    - TestHelper.rejectTKnownCallbacks) * 2,
                    counter
                );
            }
        }
    }
}