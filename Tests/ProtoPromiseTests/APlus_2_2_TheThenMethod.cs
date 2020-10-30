#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE1006 // Naming Styles

using NUnit.Framework;
using System;

namespace Proto.Promises.Tests
{
    public class APlus_2_2_TheThenMethod
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

#if PROMISE_DEBUG
        // These will only pass in DEBUG mode.
        public class _2_2_1_BothOnResolveAndOnRejectedAreOptionalArgument
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
            public void _2_2_1_1_IfOnFulfilledIsNullThrow()
            {
                var deferred = Promise.NewDeferred();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                var promise = deferred.Promise;

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Action));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<int>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<Promise>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<Promise<int>>));
                });


                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Action), () => { });
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Action), (string failValue) => { });
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<int>), () => default(int));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<int>), (string failValue) => default(int));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<Promise>), () => default(Promise));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<Promise>), (string failValue) => default(Promise));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<Promise<int>>), () => default(Promise<int>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<Promise<int>>), (string failValue) => default(Promise<int>));
                });


                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Action), () => default(Promise));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Action), (string failValue) => default(Promise));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<int>), () => default(Promise<int>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<int>), (string failValue) => default(Promise<int>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<Promise>), () => { });
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<Promise>), (string failValue) => { });
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<Promise<int>>), () => default(int));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(default(Func<Promise<int>>), (string failValue) => default(int));
                });

                deferred.Resolve();

                var deferredInt = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                var promiseInt = deferredInt.Promise;

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Action<int>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, int>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, Promise>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, Promise<int>>));
                });


                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Action<int>), () => { });
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Action<int>), (string failValue) => { });
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, int>), () => default(int));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, int>), (string failValue) => default(int));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, Promise>), () => default(Promise));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, Promise>), (string failValue) => default(Promise));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, Promise<int>>), () => default(Promise<int>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, Promise<int>>), (string failValue) => default(Promise<int>));
                });


                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Action<int>), () => default(Promise));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Action<int>), (string failValue) => default(Promise));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, int>), () => default(Promise<int>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, int>), (string failValue) => default(Promise<int>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, Promise>), () => { });
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, Promise>), (string failValue) => { });
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, Promise<int>>), () => default(int));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then(default(Func<int, Promise<int>>), (string failValue) => default(int));
                });

                deferredInt.Resolve(0);

                TestHelper.Cleanup();
            }

            [Test]
            public void _2_2_1_2_IfOnRejectedIsNullThrow()
            {
                var deferred = Promise.NewDeferred();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                var promise = deferred.Promise;

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Catch(default(Action));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Catch(default(Action<string>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Catch(default(Func<Promise>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Catch(default(Func<string, Promise>));
                });


                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => { }, default(Action));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => { }, default(Action<string>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => default(Promise), default(Func<Promise>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => default(Promise), default(Func<string, Promise>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => "string", default(Func<string>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => "string", default(Func<Exception, string>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => default(Promise<string>), default(Func<Promise<string>>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => default(Promise<string>), default(Func<Exception, Promise<string>>));
                });


                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => default(Promise), default(Action));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => default(Promise), default(Action<string>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => { }, default(Func<Promise>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => { }, default(Func<string, Promise>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => default(Promise<string>), default(Func<string>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => default(Promise<string>), default(Func<Exception, string>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => "string", default(Func<Promise<string>>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.Then(() => "string", default(Func<Exception, Promise<string>>));
                });

                deferred.Resolve();

                var deferredInt = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                var promiseInt = deferredInt.Promise;

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Catch(default(Func<int>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Catch(default(Func<string, int>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Catch(default(Func<Promise<int>>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Catch(default(Func<string, Promise<int>>));
                });


                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => { }, default(Action));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => { }, default(Action<string>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => default(Promise), default(Func<Promise>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => default(Promise), default(Func<string, Promise>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => "string", default(Func<string>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => "string", default(Func<Exception, string>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => default(Promise<string>), default(Func<Promise<string>>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => default(Promise<string>), default(Func<Exception, Promise<string>>));
                });


                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => default(Promise), default(Action));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => default(Promise), default(Action<string>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => { }, default(Func<Promise>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => { }, default(Func<string, Promise>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => default(Promise<string>), default(Func<string>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => default(Promise<string>), default(Func<Exception, string>));
                });

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => "string", default(Func<Promise<string>>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promiseInt.Then((int x) => "string", default(Func<Exception, Promise<string>>));
                });

                deferredInt.Resolve(0);

                TestHelper.Cleanup();
            }
        }
#endif

        public class IfOnFulfilledIsAFunction_2_2_2
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
            public void _2_2_2_1_ItMustBeCalledAfterPromiseIsFulfilledWithPromisesValueAsItsFirstArgument()
            {
                var promisedValue = 100;
                var resolved = false;
                var deferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);
                TestHelper.AddResolveCallbacks<int, bool, string>(deferredInt.Promise,
                    onResolve: v =>
                    {
                        Assert.AreEqual(promisedValue, v);
                        resolved = true;
                    }
                );
                TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise,
                    onResolve: v =>
                    {
                        Assert.AreEqual(promisedValue, v);
                        resolved = true;
                    }
                );
                deferredInt.Resolve(promisedValue);
                Promise.Manager.HandleCompletes();

                Assert.True(resolved);

                TestHelper.Cleanup();
            }

            [Test]
            public void _2_2_2_2_ItMustNotBeCalledBeforePromiseIsFulfilled()
            {
                var resolved = false;
                var deferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);
                TestHelper.AddResolveCallbacks<int, bool, string>(deferredInt.Promise,
                    v => resolved = true
                );
                TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise,
                    v => resolved = true,
                    s => Assert.Fail("Promise was rejected when it should have been resolved."),
                    () => Assert.Fail("Promise was rejected when it should have been resolved.")
                );
                var deferred = Promise.NewDeferred();
                TestHelper.AddResolveCallbacks<bool, string>(deferred.Promise,
                    () => resolved = true
                );
                TestHelper.AddCallbacks<bool, object, string>(deferred.Promise,
                    () => resolved = true,
                    s => Assert.Fail("Promise was rejected when it should have been resolved."),
                    () => Assert.Fail("Promise was rejected when it should have been resolved.")
                );
                Promise.Manager.HandleCompletes();

                Assert.False(resolved);

                deferredInt.Resolve(100);
                deferred.Resolve();
                Promise.Manager.HandleCompletes();

                Assert.True(resolved);

                TestHelper.Cleanup();
            }

            [Test]
            public void _2_2_2_3_ItMustNotBeCalledMoreThanOnce()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);
                deferred.Retain();
                deferredInt.Retain();
                var resolveCount = 0;
                TestHelper.AddResolveCallbacks<bool, string>(deferred.Promise,
                    () => ++resolveCount
                );
                TestHelper.AddCallbacks<bool, object, string>(deferred.Promise,
                    () => ++resolveCount,
                    s => Assert.Fail("Promise was rejected when it should have been resolved."),
                    () => Assert.Fail("Promise was rejected when it should have been resolved.")
                );
                TestHelper.AddResolveCallbacks<int, bool, string>(deferredInt.Promise,
                    x => ++resolveCount
                );
                TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise,
                    x => ++resolveCount,
                    s => Assert.Fail("Promise was rejected when it should have been resolved."),
                    () => Assert.Fail("Promise was rejected when it should have been resolved.")
                );
                deferred.Resolve();
                deferredInt.Resolve(0);
                Promise.Manager.HandleCompletes();
                TestHelper.ExpectWarning("Deferred.Resolve - Deferred is not in the pending state.");
                deferred.Resolve();
                TestHelper.ExpectWarning("Deferred.Resolve - Deferred is not in the pending state.");
                deferredInt.Resolve(100);
                deferred.Release();
                deferredInt.Release();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.resolveVoidVoidCallbacks + TestHelper.resolveVoidConvertCallbacks +
                    TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                    TestHelper.resolveTVoidCallbacks + TestHelper.resolveTConvertCallbacks +
                    TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks) * 2,
                    resolveCount
                );

                TestHelper.Cleanup();
            }
        }

        public class _2_2_3_IfOnRejectedIsAFunction
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
            public void _2_2_3_1_ItMustBeCalledAfterPromiseIsRejectedWithPromisesReasonAsItsFirstArgument()
            {
                var rejectReason = "Fail value";
                var errored = false;
                Action<string> callback = (string reason) =>
                {
                    Assert.AreEqual(rejectReason, reason);
                    errored = true;
                };
                var deferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);
                TestHelper.AddCallbacks<int, bool, string, string>(deferredInt.Promise,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    callback
                );
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                TestHelper.AddCallbacks<bool, string, string>(deferred.Promise,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    callback
                );
                deferredInt.Reject(rejectReason);
                deferred.Reject(rejectReason);
                Promise.Manager.HandleCompletes();

                Assert.True(errored);

                TestHelper.Cleanup();
            }

            [Test]
            public void _2_2_3_2_ItMustNotBeCalledBeforePromiseIsRejected()
            {
                var errored = false;
                Action<string> callback = (string reason) => errored = true;
                var deferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);
                TestHelper.AddCallbacks<int, bool, string, string>(deferredInt.Promise,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    callback
                );
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                TestHelper.AddCallbacks<bool, string, string>(deferred.Promise,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    callback
                );
                Promise.Manager.HandleCompletes();

                Assert.False(errored);

                deferredInt.Reject("Fail value");
                deferred.Reject("Fail value");
                Promise.Manager.HandleCompletes();

                Assert.True(errored);

                TestHelper.Cleanup();
            }

            [Test]
            public void _2_2_3_3_ItMustNotBeCalledMoreThanOnce()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);
                deferred.Retain();
                deferredInt.Retain();
                var errorCount = 0;
                TestHelper.AddCallbacks<bool, object, string>(deferred.Promise,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    x => ++errorCount,
                    () => ++errorCount
                );
                TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    x => ++errorCount,
                    () => ++errorCount
                );
                deferred.Reject("Fail value");
                deferredInt.Reject("Fail value");
                Promise.Manager.HandleCompletes();

                TestHelper.ExpectWarning("Deferred.Reject - Deferred is not in the pending state.");
                deferred.Reject("Fail value");
                TestHelper.ExpectWarning("Deferred.Reject - Deferred is not in the pending state.");
                deferredInt.Reject("Fail value");
                deferred.Release();
                deferredInt.Release();
                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                Assert.AreEqual(
                    (TestHelper.rejectVoidVoidCallbacks + TestHelper.rejectVoidConvertCallbacks +
                    TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                    TestHelper.rejectTVoidCallbacks + TestHelper.rejectTConvertCallbacks + TestHelper.rejectTTCallbacks +
                    TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks) * 2,
                    errorCount
                );

                TestHelper.Cleanup();
            }
        }

        // I interpret this to mean it's the last thing that is ran in the frame.
        // That is implemented during runtime by using Unity's execution order on a Monobehaviour to call Promise.Manager.HandleCompletes().
        // Since this is a test, I call it directly.
        [Test]
        public void _2_2_4_OnFulfilledMustNotBeCalledUntilTheExecutionContextStackContainsOnlyPlatformCode()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            bool resolved = false;
            TestHelper.AddResolveCallbacks<bool, string>(deferred.Promise,
                () => resolved = true
            );
            TestHelper.AddCallbacks<bool, object, string>(deferred.Promise,
                () => resolved = true,
                s => Assert.Fail("Promise was rejected when it should have been resolved.")
            );
            TestHelper.AddResolveCallbacks<int, bool, string>(deferredInt.Promise,
                v => resolved = true
            );
            TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise,
                v => resolved = true,
                s => Assert.Fail("Promise was rejected when it should have been resolved.")
            );
            deferred.Resolve();
            deferredInt.Resolve(0);
            Assert.False(resolved);

            Promise.Manager.HandleCompletes();
            Assert.True(resolved);

            TestHelper.Cleanup();
        }

        [Test]
        public void _2_2_4_OnRejectedMustNotBeCalledUntilTheExecutionContextStackContainsOnlyPlatformCode()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            bool errored = false;
            TestHelper.AddCallbacks<bool, object, string>(deferred.Promise,
                () => Assert.Fail("Promise was resolved when it should have been rejected."),
                s => errored = true
            );
            TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise,
                v => Assert.Fail("Promise was resolved when it should have been rejected."),
                s => errored = true
            );
            deferred.Reject("Fail value");
            deferredInt.Reject("Fail value");
            Assert.False(errored);

            Promise.Manager.HandleCompletes();
            Assert.True(errored);

            TestHelper.Cleanup();
        }

        // Not relevant for C#
        // 2.2.5 onFulfilled and onRejected must be called as functions (i.e. with no this value)

        public class _2_2_6_ThenMayBeCalledMultipleTimesOnTheSamePromise
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
            public void _2_2_6_1_IfWhenPromiseIsFulfilledAllRespectiveOnFulfilledCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToThen()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                int order = 0;
                int counter = 0;

                Action<int> callback = expected =>
                {
                    Assert.AreEqual(expected, order);
                    if (++counter == TestHelper.resolveVoidCallbacks * 2)
                    {
                        counter = 0;
                        ++order;
                    }
                };

                TestHelper.AddResolveCallbacks<bool, string>(deferred.Promise, () => callback(0));
                TestHelper.AddCallbacks<bool, object, string>(deferred.Promise, () => callback(0), s => Assert.Fail("Promise was rejected when it should have been resolved."));

                TestHelper.AddResolveCallbacks<bool, string>(deferred.Promise, () => callback(1));
                TestHelper.AddCallbacks<bool, object, string>(deferred.Promise, () => callback(1), s => Assert.Fail("Promise was rejected when it should have been resolved."));

                TestHelper.AddResolveCallbacks<bool, string>(deferred.Promise, () => callback(2));
                TestHelper.AddCallbacks<bool, object, string>(deferred.Promise, () => callback(2), s => Assert.Fail("Promise was rejected when it should have been resolved."));

                int orderT = 0;
                int counterT = 0;

                Action<int> callbackT = expected =>
                {
                    Assert.AreEqual(expected, orderT);
                    if (++counterT == TestHelper.resolveTCallbacks * 2)
                    {
                        counterT = 0;
                        ++orderT;
                    }
                };

                TestHelper.AddResolveCallbacks<int, bool, string>(deferredInt.Promise, v => callbackT(0));
                TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise, v => callbackT(0), s => Assert.Fail("Promise was rejected when it should have been resolved."));

                TestHelper.AddResolveCallbacks<int, bool, string>(deferredInt.Promise, v => callbackT(1));
                TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise, v => callbackT(1), s => Assert.Fail("Promise was rejected when it should have been resolved."));

                TestHelper.AddResolveCallbacks<int, bool, string>(deferredInt.Promise, v => callbackT(2));
                TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise, v => callbackT(2), s => Assert.Fail("Promise was rejected when it should have been resolved."));
                deferred.Resolve();
                deferredInt.Resolve(100);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(3, order);
                Assert.AreEqual(3, orderT);

                TestHelper.Cleanup();
            }

            [Test]
            public void _2_2_6_2_IfWhenPromiseIsRejectedAllRespectiveOnRejectedCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToThenOrCatch()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                int order = 0;
                int counter = 0;

                Action<int> callback = expected =>
                {
                    Assert.AreEqual(expected, order);
                    if (++counter == TestHelper.rejectVoidCallbacks * 2)
                    {
                        counter = 0;
                        ++order;
                    }
                };

                TestHelper.AddCallbacks<bool, object, string>(deferred.Promise,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    _ => callback(0),
                    () => callback(0)
                );

                TestHelper.AddCallbacks<bool, object, string>(deferred.Promise,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    _ => callback(1),
                    () => callback(1)
                );

                TestHelper.AddCallbacks<bool, object, string>(deferred.Promise,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    _ => callback(2),
                    () => callback(2)
                );

                int orderT = 0;
                int counterT = 0;

                Action<int> callbackT = expected =>
                {
                    Assert.AreEqual(expected, orderT);
                    if (++counterT == TestHelper.rejectTCallbacks * 2)
                    {
                        counterT = 0;
                        ++orderT;
                    }
                };

                TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    _ => callbackT(0),
                    () => callbackT(0)
                );

                TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    _ => callbackT(1),
                    () => callbackT(1)
                );

                TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    _ => callbackT(2),
                    () => callbackT(2)
                );
                deferred.Reject("Fail value");
                deferredInt.Reject("Fail value");
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(3, order);
                Assert.AreEqual(3, orderT);

                TestHelper.Cleanup();
            }
        }

        public class ThenMustReturnAPromise_2_2_7
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

            // 2.2.7.1 Promise Resolution Procedure in 2.3

            [Test]
            public void _2_2_7_2_IfOnFulfilledThrowsAnExceptionEPromise2MustBeRejectedWithEAsTheReason()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                int exceptionCount = 0;
                Exception expected = new Exception("Fail value");

                Action<Promise> callback = p =>
                {
                    p.Catch((Exception e) =>
                    {
                        Assert.AreEqual(expected, e);
                        ++exceptionCount;
                    });
                    throw expected;
                };

                TestHelper.AddResolveCallbacks<bool, string>(deferred.Promise,
                    promiseToVoid: p => callback(p),
                    promiseToConvert: p => { callback(p); return false; },
                    promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                    promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(false); }
                );
                TestHelper.AddCallbacks<bool, object, string>(deferred.Promise,
                    onReject: s => Assert.Fail("Promise was rejected when it should have been resolved."),
                    onUnknownRejection: () => Assert.Fail("Promise was rejected when it should have been resolved."),
                    promiseToVoid: p => callback(p),
                    promiseToConvert: p => { callback(p); return false; },
                    promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                    promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(false); }
                );

                TestHelper.AddResolveCallbacks<int, bool, string>(deferredInt.Promise,
                    promiseToVoid: p => callback(p),
                    promiseToConvert: p => { callback(p); return false; },
                    promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                    promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(false); }
                );
                TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise,
                    onReject: s => Assert.Fail("Promise was rejected when it should have been resolved."),
                    onUnknownRejection: () => Assert.Fail("Promise was rejected when it should have been resolved."),
                    promiseToVoid: p => callback(p),
                    promiseToConvert: p => { callback(p); return false; },
                    promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                    promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(false); }
                );

                deferred.Resolve();
                deferredInt.Resolve(100);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.resolveVoidVoidCallbacks + TestHelper.resolveVoidConvertCallbacks +
                    TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                    TestHelper.resolveTVoidCallbacks + TestHelper.resolveTConvertCallbacks +
                    TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks) * 2,
                    exceptionCount
                );

                TestHelper.Cleanup();
            }

            [Test]
            public void _2_2_7_2_IfOnRejectedThrowsAnExceptionEPromise2MustBeRejectedWithEAsTheReason()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                int exceptionCount = 0;
                Exception expected = new Exception("Fail value");

                Action<Promise> callback = p =>
                {
                    p.Catch((Exception e) =>
                    {
                        Assert.AreEqual(expected, e);
                        ++exceptionCount;
                    });
                    throw expected;
                };

                TestHelper.AddCallbacks<bool, object, string>(deferred.Promise,
                    onResolve: () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    promiseToVoid: p => callback(p),
                    promiseToConvert: p => { callback(p); return false; },
                    promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                    promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(false); }
                );

                TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise,
                    onResolve: _ => Assert.Fail("Promise was resolved when it should have been rejected."),
                    promiseToVoid: p => callback(p),
                    promiseToConvert: p => { callback(p); return false; },
                    promiseToT: p => { callback(p); return 0; },
                    promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                    promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(false); },
                    promiseToPromiseT: p => { callback(p); return Promise.Resolved(0); }
                );

                deferred.Reject("Fail value");
                deferredInt.Reject("Fail value");
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.rejectVoidVoidCallbacks + TestHelper.rejectVoidConvertCallbacks +
                    TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                    TestHelper.rejectTVoidCallbacks + TestHelper.rejectTConvertCallbacks + TestHelper.rejectTTCallbacks +
                    TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks) * 2,
                    exceptionCount
                );

                TestHelper.Cleanup();
            }

            [Test]
            public void _2_2_7_3_IfOnFulfilledIsNotAFunctionAndPromise1IsFulfilledPromise2MustBeFulfilledWithTheSameValueAsPromise1()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                int expected = 100;
                int counter = 0;

                var promise1Void = deferred.Promise.Catch(() => { Assert.Fail("Promise was rejected when it should have been resolved."); return; });
                var promise1Int = deferredInt.Promise.Catch(() => { Assert.Fail("Promise was rejected when it should have been resolved."); return 50; });

                TestHelper.AddResolveCallbacks<bool, string>(promise1Void,
                    () => ++counter
                );
                TestHelper.AddCallbacks<bool, object, string>(promise1Void,
                    () => ++counter,
                    s => Assert.Fail("Promise was rejected when it should have been resolved.")
                );

                TestHelper.AddResolveCallbacks<int, bool, string>(promise1Int,
                    v => { Assert.AreEqual(expected, v); ++counter; }
                );
                TestHelper.AddCallbacks<int, bool, object, string>(promise1Int,
                    v => { Assert.AreEqual(expected, v); ++counter; },
                    s => Assert.Fail("Promise was rejected when it should have been resolved.")
                );

                deferred.Resolve();
                deferredInt.Resolve(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.resolveVoidCallbacks + TestHelper.resolveTCallbacks) * 2,
                    counter
                );

                TestHelper.Cleanup();
            }

            [Test]
            public void _2_2_7_4_IfOnRejectedIsNotAFunctionAndPromise1IsRejectedPromise2MustBeRejectedWithTheSameReasonAsPromise1()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                int counter = 0;
                string expected = "Fail value";

                var promise1Void = deferred.Promise.Then(() => { Assert.Fail("Promise was resolved when it should have been rejected."); return; });
                var promise1Int = deferredInt.Promise.Then(() => { Assert.Fail("Promise was resolved when it should have been rejected."); return 50; });

                TestHelper.AddCallbacks<bool, object, string>(promise1Void,
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    e => { Assert.AreEqual(expected, e); ++counter; },
                    () => ++counter
                );
                TestHelper.AddCallbacks<int, bool, object, string>(promise1Int,
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    e => { Assert.AreEqual(expected, e); ++counter; },
                    () => ++counter
                );

                deferred.Reject(expected);
                deferredInt.Reject(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.rejectVoidCallbacks + TestHelper.rejectTCallbacks) * 2,
                    counter
                );

                TestHelper.Cleanup();
            }

            [Test]
            public void IfPromise1IsRejectedAndItsReasonIsNotCompatibleWithOnRejectedItMustNotBeInvoked()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                var promise1Void = deferred.Promise.Then(() => { Assert.Fail("Promise was rejected when it should have been resolved."); return; });
                var promise1Int = deferredInt.Promise.Then(() => { Assert.Fail("Promise was resolved when it should have been rejected."); return 50; });

                TestHelper.AddCallbacks<bool, string, string>(promise1Void,
                    () => Assert.Fail("Promise was rejected when it should have been resolved."),
                    e => Assert.Fail("OnRejected was invoked with a string when the promise was rejected with an integer."),
                    onCallbackAdded: p => p.Catch((int _) => { }),
                    onCallbackAddedConvert: p => p.Catch((int _) => { })
                );
                TestHelper.AddCallbacks<int, bool, string, string>(promise1Int,
                    v => Assert.Fail("Promise was rejected when it should have been resolved."),
                    e => Assert.Fail("OnRejected was invoked with a string when the promise was rejected with an integer."),
                    onCallbackAdded: p => p.Catch((int _) => { }),
                    onCallbackAddedConvert: p => p.Catch((int _) => { }),
                    onCallbackAddedT: p => p.Catch((int _) => { })
                );

                deferred.Reject(100);
                deferredInt.Reject(100);
                Promise.Manager.HandleCompletes();

                TestHelper.Cleanup();
            }

            [Test]
            public void IfPromise1IsRejectedAndItsReasonIsNotCompatibleWithOnRejectedPromise2MustBeRejectedWithTheSameReasonAsPromise1()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                int expected = 100;
                int counterVoid = 0;
                int counterT = 0;

                TestHelper.AddCallbacks<int, string, string>(deferred.Promise,
                    onResolve: () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    onReject: (string cancelString) => Assert.Fail("OnRejected was invoked with a string when the promise was rejected with an integer."),
                    onCallbackAdded: p => p.Catch((int cancelInt) => { Assert.AreEqual(expected, cancelInt); ++counterVoid; }),
                    onCallbackAddedConvert: p => p.Catch((int cancelInt) => { Assert.AreEqual(expected, cancelInt); ++counterT; })
                );

                TestHelper.AddCallbacks<int, int, string, string>(deferredInt.Promise,
                    onResolve: _ => Assert.Fail("Promise was resolved when it should have been rejected."),
                    onReject: (string cancelString) => Assert.Fail("OnRejected was invoked with a string when the promise was rejected with an integer."),
                    onCallbackAdded: p => p.Catch((int cancelInt) => { Assert.AreEqual(expected, cancelInt); ++counterVoid; }),
                    onCallbackAddedConvert: p => p.Catch((int cancelInt) => { Assert.AreEqual(expected, cancelInt); ++counterT; }),
                    onCallbackAddedT: p => p.Catch((int cancelInt) => { Assert.AreEqual(expected, cancelInt); ++counterT; })
                );

                deferred.Reject(expected);
                deferredInt.Reject(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.rejectVoidVoidCallbacks + TestHelper.rejectTVoidCallbacks +
                    TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectTPromiseVoidCallbacks - TestHelper.rejectVoidKnownCallbacks) * 2,
                    counterVoid
                );
                Assert.AreEqual(
                    (TestHelper.rejectVoidConvertCallbacks + TestHelper.rejectTConvertCallbacks +
                    TestHelper.rejectTConvertCallbacks + TestHelper.rejectTTCallbacks +
                    TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks - TestHelper.rejectTKnownCallbacks) * 2,
                    counterT
                );

                TestHelper.Cleanup();
            }
        }
    }
}