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
    public class APlus_2_3_ThePromiseResolutionProcedure
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
        [Test]
        public void _2_3_1_IfPromiseAndXReferToTheSameObject_RejectPromiseWithInvalidReturnExceptionAsTheReason_void()
        {
            int exceptionCounter = 0;

            var resolveDeferred = Promise.NewDeferred();
            var resolvePromise = resolveDeferred.Promise.Preserve();
            var rejectDeferred = Promise.NewDeferred();
            var rejectPromise = rejectDeferred.Promise.Preserve();

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

            TestAction<Promise> catchCallback = (ref Promise p) =>
            {
                var preserved = p.Preserve();
                p = preserved;
                p.Finally(() => preserved.Forget())
                .Catch((object e) =>
                {
                    Assert.IsInstanceOf<InvalidReturnException>(e);
                    ++exceptionCounter;
                }).Forget();
            };
            TestAction<Promise<int>> catchCallbackConvert = (ref Promise<int> p) =>
            {
                var preserved = p.Preserve();
                p = preserved;
                p.Finally(() => preserved.Forget())
                .Catch((object e) =>
                {
                    Assert.IsInstanceOf<InvalidReturnException>(e);
                    ++exceptionCounter;
                }).Forget();
            };

            TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                promiseToPromise: p => p,
                promiseToPromiseConvert: p => p,
                onCallbackAdded: catchCallback,
                onCallbackAddedConvert: catchCallbackConvert
            );
            TestHelper.AddCallbacks<int, bool, string>(resolvePromise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: p => p,
                promiseToPromiseConvert: p => p,
                onCallbackAdded: catchCallback,
                onCallbackAddedConvert: catchCallbackConvert
            );
            TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                promiseToPromise: p => p,
                promiseToPromiseConvert: p => p,
                onCallbackAdded: catchCallback,
                onCallbackAddedConvert: catchCallbackConvert
            );

            TestHelper.AddCallbacks<int, string, string>(rejectPromise,
                onResolve: resolveAssert,
                promiseToPromise: p => p,
                promiseToPromiseConvert: p => p,
                onCallbackAdded: catchCallback,
                onCallbackAddedConvert: catchCallbackConvert
            );
            TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                promiseToPromise: p => p,
                promiseToPromiseConvert: p => p,
                onCallbackAdded: catchCallback,
                onCallbackAddedConvert: catchCallbackConvert
            );

            resolveDeferred.Resolve();
            rejectDeferred.Reject("Fail value");

            Assert.AreEqual(
                (TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                (TestHelper.continueVoidPromiseVoidCallbacks + TestHelper.continueVoidPromiseConvertCallbacks) * 2) * 2,
                exceptionCounter
            );

            resolvePromise.Forget();
            rejectPromise.Forget();
        }

        [Test]
        public void _2_3_1_IfPromiseAndXReferToTheSameObject_RejectPromiseWithInvalidReturnExceptionAsTheReason_T()
        {
            int exceptionCounter = 0;

            var resolveDeferred = Promise.NewDeferred<int>();
            var resolvePromise = resolveDeferred.Promise.Preserve();
            var rejectDeferred = Promise.NewDeferred<int>();
            var rejectPromise = rejectDeferred.Promise.Preserve();

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

            TestAction<Promise> catchCallback = (ref Promise p) =>
            {
                var preserved = p.Preserve();
                p = preserved;
                p.Finally(() => preserved.Forget())
                .Catch((object e) =>
                {
                    Assert.IsInstanceOf<InvalidReturnException>(e);
                    ++exceptionCounter;
                }).Forget();
            };
            TestAction<Promise<int>> catchCallbackConvert = (ref Promise<int> p) =>
            {
                var preserved = p.Preserve();
                p = preserved;
                p.Finally(() => preserved.Forget())
                .Catch((object e) =>
                {
                    Assert.IsInstanceOf<InvalidReturnException>(e);
                    ++exceptionCounter;
                }).Forget();
            };

            TestHelper.AddResolveCallbacks<int, int, string>(resolvePromise,
                promiseToPromise: p => p,
                promiseToPromiseConvert: p => p,
                onCallbackAdded: catchCallback,
                onCallbackAddedConvert: catchCallbackConvert
            );
            TestHelper.AddCallbacks<int, int, string, string>(resolvePromise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: p => p,
                promiseToPromiseConvert: p => p,
                promiseToPromiseT: p => p,
                onCallbackAdded: catchCallback,
                onCallbackAddedConvert: catchCallbackConvert,
                onCallbackAddedT: catchCallbackConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(resolvePromise,
                promiseToPromise: p => p,
                promiseToPromiseConvert: p => p,
                onCallbackAdded: catchCallback,
                onCallbackAddedConvert: catchCallbackConvert
            );

            TestHelper.AddCallbacks<int, int, string, string>(rejectPromise,
                onResolve: _ => resolveAssert(),
                promiseToPromise: p => p,
                promiseToPromiseConvert: p => p,
                promiseToPromiseT: p => p,
                onCallbackAdded: catchCallback,
                onCallbackAddedConvert: catchCallbackConvert,
                onCallbackAddedT: catchCallbackConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(rejectPromise,
                promiseToPromise: p => p,
                promiseToPromiseConvert: p => p,
                onCallbackAdded: catchCallback,
                onCallbackAddedConvert: catchCallbackConvert
            );

            resolveDeferred.Resolve(1);
            rejectDeferred.Reject("Fail value");

            Assert.AreEqual(
                (TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                (TestHelper.continueTPromiseVoidCallbacks + TestHelper.continueTPromiseConvertCallbacks) * 2) * 2,
                exceptionCounter
            );

            resolvePromise.Forget();
            rejectPromise.Forget();
        }
#endif

        public class _2_3_2_IfXIsAPromiseAdoptItsState
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
            public void _2_3_2_1_IfXIsPending_PromiseMustRemainPendingUntilXIsFulfilledOrRejected_void()
            {
                int expectedCompleteCount = 0;
                int completeCounter = 0;

                var resolveDeferred = Promise.NewDeferred();
                var rejectDeferred = Promise.NewDeferred();

                var resolvePromise = resolveDeferred.Promise.Preserve();
                var rejectPromise = rejectDeferred.Promise.Preserve();

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                var resolveWaitDeferred = Promise.NewDeferred();
                var resolveWaitDeferredInt = Promise.NewDeferred<int>();
                var rejectWaitDeferred = Promise.NewDeferred();
                var rejectWaitDeferredInt = Promise.NewDeferred<int>();

                var resolveWaitPromise = resolveWaitDeferred.Promise.Preserve();
                var rejectWaitPromise = rejectWaitDeferred.Promise.Preserve();
                var resolveWaitPromiseInt = resolveWaitDeferredInt.Promise.Preserve();
                var rejectWaitPromiseInt = rejectWaitDeferredInt.Promise.Preserve();

                TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                {
                    var preserved = p = p.Preserve();
                    preserved
                        .Catch(() => { })
                        .Finally(() => preserved.Forget())
                        .Forget();
                };
                TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                {
                    var preserved = p = p.Preserve();
                    preserved
                        .Catch(() => { })
                        .Finally(() => preserved.Forget())
                        .Forget();
                };

                TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddCallbacks<int, object, string>(resolvePromise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddCallbacks<int, bool, string>(resolvePromise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                resolveDeferred.Resolve();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                TestHelper.AddCallbacks<int, object, string>(rejectPromise,
                    onResolve: resolveAssert,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                TestHelper.AddCallbacks<int, object, string>(rejectPromise,
                    onResolve: resolveAssert,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                rejectDeferred.Reject("Fail outer");
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferred.Resolve();
                expectedCompleteCount +=
                    (TestHelper.resolveVoidPromiseVoidCallbacks +
                    TestHelper.rejectVoidPromiseVoidCallbacks +
                    (TestHelper.continueVoidPromiseVoidCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferredInt.Resolve(1);
                expectedCompleteCount +=
                    (TestHelper.resolveVoidPromiseConvertCallbacks +
                    TestHelper.rejectVoidPromiseConvertCallbacks +
                    (TestHelper.continueVoidPromiseConvertCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferred.Reject("Fail inner");
                expectedCompleteCount +=
                    (TestHelper.resolveVoidPromiseVoidCallbacks +
                    TestHelper.rejectVoidPromiseVoidCallbacks +
                    (TestHelper.continueVoidPromiseVoidCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferredInt.Reject("Fail inner");
                expectedCompleteCount +=
                    (TestHelper.resolveVoidPromiseConvertCallbacks +
                    TestHelper.rejectVoidPromiseConvertCallbacks +
                    (TestHelper.continueVoidPromiseConvertCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);

                resolvePromise.Forget();
                rejectPromise.Forget();
                resolveWaitPromise.Forget();
                rejectWaitPromise.Forget();
                resolveWaitPromiseInt.Forget();
                rejectWaitPromiseInt.Forget();
            }

            [Test]
            public void _2_3_2_1_IfXIsPending_PromiseMustRemainPendingUntilXIsFulfilledOrRejected_T()
            {
                int expectedCompleteCount = 0;
                int completeCounter = 0;

                var resolveDeferredInt = Promise.NewDeferred<int>();
                var rejectDeferredInt = Promise.NewDeferred<int>();

                var resolvePromiseInt = resolveDeferredInt.Promise.Preserve();
                var rejectPromiseInt = rejectDeferredInt.Promise.Preserve();

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                var resolveWaitDeferred = Promise.NewDeferred();
                var resolveWaitDeferredInt = Promise.NewDeferred<int>();
                var rejectWaitDeferred = Promise.NewDeferred();
                var rejectWaitDeferredInt = Promise.NewDeferred<int>();

                var resolveWaitPromise = resolveWaitDeferred.Promise.Preserve();
                var resolveWaitPromiseInt = resolveWaitDeferredInt.Promise.Preserve();
                var rejectWaitPromise = rejectWaitDeferred.Promise.Preserve();
                var rejectWaitPromiseInt = rejectWaitDeferredInt.Promise.Preserve();

                TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                {
                    var preserved = p = p.Preserve();
                    preserved
                        .Catch(() => { })
                        .Finally(() => preserved.Forget())
                        .Forget();
                };
                TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                {
                    var preserved = p = p.Preserve();
                    preserved
                        .Catch(() => { })
                        .Finally(() => preserved.Forget())
                        .Forget();
                };

                TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseInt,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(resolvePromiseInt,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseInt,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromiseInt;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(resolvePromiseInt,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                resolveDeferredInt.Resolve(1);
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseInt,
                    onResolve: _ => resolveAssert(),
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert,
                    onCallbackAddedT: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseInt,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseInt,
                    onResolve: _ => resolveAssert(),
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromiseInt;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert,
                    onCallbackAddedT: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseInt,
                    promiseToPromise: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                        return rejectWaitPromiseInt;
                    },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                rejectDeferredInt.Reject("Fail outer");
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferred.Resolve();
                expectedCompleteCount +=
                    (TestHelper.resolveTPromiseVoidCallbacks +
                    TestHelper.rejectTPromiseVoidCallbacks +
                    (TestHelper.continueTPromiseVoidCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferredInt.Resolve(1);
                expectedCompleteCount +=
                    (TestHelper.resolveTPromiseConvertCallbacks +
                    TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                    (TestHelper.continueTPromiseConvertCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferred.Reject("Fail inner");
                expectedCompleteCount +=
                    (TestHelper.resolveTPromiseVoidCallbacks +
                    TestHelper.rejectTPromiseVoidCallbacks +
                    (TestHelper.continueTPromiseVoidCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferredInt.Reject("Fail inner");
                expectedCompleteCount +=
                    (TestHelper.resolveTPromiseConvertCallbacks +
                    TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                    (TestHelper.continueTPromiseConvertCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);

                resolvePromiseInt.Forget();
                rejectPromiseInt.Forget();
                resolveWaitPromise.Forget();
                resolveWaitPromiseInt.Forget();
                rejectWaitPromise.Forget();
                rejectWaitPromiseInt.Forget();
            }

            [Test]
            public void _2_3_2_2_IfWhenXIsFulfilled_FulfillPromiseWithTheSameValue()
            {
                var resolveDeferred = Promise.NewDeferred();
                var rejectDeferred = Promise.NewDeferred();
                var resolveDeferredInt = Promise.NewDeferred<int>();
                var rejectDeferredInt = Promise.NewDeferred<int>();

                resolveDeferred.Resolve();
                rejectDeferred.Reject("Fail value");
                resolveDeferredInt.Resolve(1);
                rejectDeferredInt.Reject("Fail value");

                var resolvePromise = resolveDeferred.Promise.Preserve();
                var rejectPromise = rejectDeferred.Promise.Preserve();
                var resolvePromiseInt = resolveDeferredInt.Promise.Preserve();
                var rejectPromiseInt = rejectDeferredInt.Promise.Preserve();

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                int resolveValue = 100;
                int resolveCounter = 0;

                TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                {
                    p.Then(v =>
                    {
                        if (resolveValue == v)
                        {
                            ++resolveCounter;
                        }
                    }).Forget();
                };

                var resolveWaitDeferredInt = Promise.NewDeferred<int>();
                var resolveWaitPromiseInt = resolveWaitDeferredInt.Promise.Preserve();

                Func<Promise<int>, Promise<int>> promiseToPromiseConvert = p => resolveWaitPromiseInt;

                // Test pending -> resolved and already resolved.
                bool firstRun = true;
            RunAgain:
                resolveCounter = 0;

                TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddCallbacks<int, object, string>(resolvePromise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseInt,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(resolvePromiseInt,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );


                TestHelper.AddCallbacks<int, object, string>(rejectPromise,
                    onResolve: resolveAssert,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseInt,
                    onResolve: _ => resolveAssert(),
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    promiseToPromiseT: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert,
                    onCallbackAddedT: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(rejectPromiseInt,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );


                if (firstRun)
                {
                    Assert.AreEqual(0, resolveCounter);
                    resolveWaitDeferredInt.Resolve(resolveValue);
                }

                Assert.AreEqual(
                    (TestHelper.resolveVoidPromiseConvertCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                    TestHelper.rejectVoidPromiseConvertCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                    ((TestHelper.continueVoidPromiseConvertCallbacks + TestHelper.continueTPromiseConvertCallbacks) * 2)) * 2,
                    resolveCounter
                );

                if (firstRun)
                {
                    firstRun = false;
                    goto RunAgain;
                }

                resolvePromise.Forget();
                rejectPromise.Forget();
                resolvePromiseInt.Forget();
                rejectPromiseInt.Forget();

                resolveWaitPromiseInt.Forget();
            }

            [Test]
            public void _2_3_2_3_IfWhenXIsRejected_RejectPromiseWithTheSameReason_void()
            {
                var resolveDeferred = Promise.NewDeferred();
                var rejectDeferred = Promise.NewDeferred();

                resolveDeferred.Resolve();
                rejectDeferred.Reject("Fail value");

                var resolvePromise = resolveDeferred.Promise.Preserve();
                var rejectPromise = rejectDeferred.Promise.Preserve();

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                string rejectValue = "Waited Rejection";
                int rejectCounter = 0;

                TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                {
                    p.Catch((string rej) =>
                    {
                        if (rejectValue == rej)
                        {
                            ++rejectCounter;
                        }
                    }).Forget();
                };
                TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                {
                    p.Catch((string rej) =>
                    {
                        if (rejectValue == rej)
                        {
                            ++rejectCounter;
                        }
                    }).Forget();
                };

                var rejectWaitDeferred = Promise.NewDeferred();
                var rejectWaitDeferredInt = Promise.NewDeferred<int>();

                var rejectWaitPromise = rejectWaitDeferred.Promise.Preserve();
                var rejectWaitPromiseInt = rejectWaitDeferredInt.Promise.Preserve();

                Func<Promise, Promise> promiseToPromise = p => rejectWaitPromise;
                Func<Promise<int>, Promise<int>> promiseToPromiseConvert = p => rejectWaitPromiseInt;

                // Test pending -> rejected and already rejected.
                bool firstRun = true;
            RunAgain:
                rejectCounter = 0;

                TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                    promiseToPromise: promiseToPromise,
                    onCallbackAdded: onCallbackAdded,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddCallbacks<int, object, string>(resolvePromise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: promiseToPromise,
                    onCallbackAdded: onCallbackAdded,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                    promiseToPromise: promiseToPromise,
                    onCallbackAdded: onCallbackAdded,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                TestHelper.AddCallbacks<int, object, string>(rejectPromise,
                    onResolve: resolveAssert,
                    promiseToPromise: promiseToPromise,
                    onCallbackAdded: onCallbackAdded,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                    promiseToPromise: promiseToPromise,
                    onCallbackAdded: onCallbackAdded,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );


                if (firstRun)
                {
                    Assert.AreEqual(0, rejectCounter);
                    rejectWaitDeferred.Reject(rejectValue);
                    rejectWaitDeferredInt.Reject(rejectValue);
                }

                Assert.AreEqual(
                    (TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                    TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                    ((TestHelper.continueVoidPromiseVoidCallbacks + TestHelper.continueVoidPromiseConvertCallbacks) * 2)) * 2,
                    rejectCounter
                );

                if (firstRun)
                {
                    firstRun = false;
                    goto RunAgain;
                }

                resolvePromise.Forget();
                rejectPromise.Forget();

                rejectWaitPromise.Forget();
                rejectWaitPromiseInt.Forget();
            }

            [Test]
            public void _2_3_2_3_IfWhenXIsRejected_RejectPromiseWithTheSameReason_T()
            {
                var resolveDeferredInt = Promise.NewDeferred<int>();
                var rejectDeferredInt = Promise.NewDeferred<int>();

                resolveDeferredInt.Resolve(1);
                rejectDeferredInt.Reject("Fail value");

                var resolvePromiseInt = resolveDeferredInt.Promise.Preserve();
                var rejectPromiseInt = rejectDeferredInt.Promise.Preserve();

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                string rejectValue = "Waited Rejection";
                int rejectCounter = 0;

                TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                {
                    p.Catch((string rej) =>
                    {
                        if (rejectValue == rej)
                        {
                            ++rejectCounter;
                        }
                    }).Forget();
                };
                TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                {
                    p.Catch((string rej) =>
                    {
                        if (rejectValue == rej)
                        {
                            ++rejectCounter;
                        }
                    }).Forget();
                };

                var rejectWaitDeferred = Promise.NewDeferred();
                var rejectWaitDeferredInt = Promise.NewDeferred<int>();

                var rejectWaitPromise = rejectWaitDeferred.Promise.Preserve();
                var rejectWaitPromiseInt = rejectWaitDeferredInt.Promise.Preserve();


                Func<Promise, Promise> promiseToPromise = p => rejectWaitPromise;
                Func<Promise<int>, Promise<int>> promiseToPromiseConvert = p => rejectWaitPromiseInt;

                // Test pending -> rejected and already rejected.
                bool firstRun = true;
            RunAgain:
                rejectCounter = 0;

                TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                    promiseToPromise: promiseToPromise,
                    onCallbackAdded: onCallbackAdded,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseInt,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: promiseToPromise,
                    onCallbackAdded: onCallbackAdded,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert,
                    promiseToPromiseT: promiseToPromiseConvert,
                    onCallbackAddedT: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, int, string>(resolvePromiseInt,
                    promiseToPromise: promiseToPromise,
                    onCallbackAdded: onCallbackAdded,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseInt,
                    onResolve: _ => resolveAssert(),
                    promiseToPromise: promiseToPromise,
                    onCallbackAdded: onCallbackAdded,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert,
                    promiseToPromiseT: promiseToPromiseConvert,
                    onCallbackAddedT: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseInt,
                    promiseToPromise: promiseToPromise,
                    onCallbackAdded: onCallbackAdded,
                    promiseToPromiseConvert: promiseToPromiseConvert,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );


                if (firstRun)
                {
                    Assert.AreEqual(0, rejectCounter);
                    rejectWaitDeferred.Reject(rejectValue);
                    rejectWaitDeferredInt.Reject(rejectValue);
                }

                Assert.AreEqual(
                    (TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                    TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                    ((TestHelper.continueTPromiseVoidCallbacks + TestHelper.continueTPromiseConvertCallbacks) * 2)) * 2,
                    rejectCounter
                );

                if (firstRun)
                {
                    firstRun = false;
                    goto RunAgain;
                }

                resolvePromiseInt.Forget();
                rejectPromiseInt.Forget();

                rejectWaitPromise.Forget();
                rejectWaitPromiseInt.Forget();
            }
        }

        // Not supported. You may alternatively "return Promise.New(deferred => {...});".
        // 2.3.3 if X is a function...

        [Test]
        public void _2_3_4_IfOnResolvedOrOnRejectedReturnsSuccessfully_ResolvePromise_void()
        {
            var resolveDeferred = Promise.NewDeferred();
            var rejectDeferred = Promise.NewDeferred();

            var resolvePromise = resolveDeferred.Promise.Preserve();
            var rejectPromise = rejectDeferred.Promise.Preserve();

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

            int resolveCounter = 0;

            TestAction<Promise> onCallbackAdded = (ref Promise p) => p.Then(() => ++resolveCounter).Forget();
            TestAction<Promise<string>> onCallbackAddedConvert = (ref Promise<string> p) => p.Then(() => ++resolveCounter).Forget();

            TestHelper.AddResolveCallbacks<string, string>(resolvePromise,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddCallbacks<string, object, string>(resolvePromise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<string, string>(resolvePromise,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            TestHelper.AddCallbacks<string, object, string>(rejectPromise,
                onResolve: resolveAssert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<string, string>(rejectPromise,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            resolveDeferred.Resolve();
            rejectDeferred.Reject("Fail value");

            Assert.AreEqual(
                (TestHelper.resolveOnlyVoidCallbacks + TestHelper.resolveVoidCallbacks +
                TestHelper.rejectVoidCallbacks +
                (TestHelper.continueVoidCallbacks * 2)) * 2,
                resolveCounter
            );

            resolvePromise.Forget();
            rejectPromise.Forget();
        }

        [Test]
        public void _2_3_4_IfOnResolvedOrOnRejectedReturnsSuccessfully_ResolvePromise_T()
        {
            var resolveDeferredInt = Promise.NewDeferred<int>();
            var rejectDeferredInt = Promise.NewDeferred<int>();

            var resolvePromiseInt = resolveDeferredInt.Promise.Preserve();
            var rejectPromiseInt = rejectDeferredInt.Promise.Preserve();

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

            int resolveCounter = 0;

            TestAction<Promise> onCallbackAdded = (ref Promise p) => p.Then(() => ++resolveCounter).Forget();
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) => p.Then(() => ++resolveCounter).Forget();

            TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseInt,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert,
                onCallbackAddedT: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(resolvePromiseInt,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseInt,
                onResolve: _ => resolveAssert(),
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert,
                onCallbackAddedT: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseInt,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            resolveDeferredInt.Resolve(1);
            rejectDeferredInt.Reject("Fail value");

            Assert.AreEqual(
                (TestHelper.resolveOnlyTCallbacks + TestHelper.resolveTCallbacks +
                TestHelper.rejectTCallbacks +
                (TestHelper.continueTCallbacks * 2)) * 2,
                resolveCounter
            );

            resolvePromiseInt.Forget();
            rejectPromiseInt.Forget();
        }

        [Test]
        public void _2_3_4_IfXIsNotAPromiseOrAFunction_FulfillPromiseWithX_void()
        {
            var resolveDeferred = Promise.NewDeferred();
            var rejectDeferred = Promise.NewDeferred();

            var resolvePromise = resolveDeferred.Promise.Preserve();
            var rejectPromise = rejectDeferred.Promise.Preserve();

            int expected = 100;

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

            int resolveCounter = 0;

            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                p.Then(v =>
                {
                    Assert.AreEqual(expected, v);
                    ++resolveCounter;
                }).Forget();

            TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                onCallbackAddedConvert: onCallbackAddedConvert,
                convertValue: expected
            );
            TestHelper.AddCallbacks<int, object, string>(resolvePromise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                onCallbackAddedConvert: onCallbackAddedConvert,
                convertValue: expected
            );
            TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                onCallbackAddedConvert: onCallbackAddedConvert,
                convertValue: expected
            );

            TestHelper.AddCallbacks<int, object, string>(rejectPromise,
                onResolve: resolveAssert,
                onCallbackAddedConvert: onCallbackAddedConvert,
                convertValue: expected
            );
            TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                onCallbackAddedConvert: onCallbackAddedConvert,
                convertValue: expected
            );

            resolveDeferred.Resolve();
            rejectDeferred.Reject("Fail value");

            Assert.AreEqual(
                (TestHelper.resolveVoidConvertCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                TestHelper.rejectVoidConvertCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                ((TestHelper.continueVoidConvertCallbacks + TestHelper.continueVoidPromiseConvertCallbacks) * 2)) * 2,
                resolveCounter
            );

            resolvePromise.Forget();
            rejectPromise.Forget();
        }

        [Test]
        public void _2_3_4_IfXIsNotAPromiseOrAFunction_FulfillPromiseWithX_T()
        {
            var resolveDeferredInt = Promise.NewDeferred<int>();
            var rejectDeferredInt = Promise.NewDeferred<int>();

            var resolvePromiseInt = resolveDeferredInt.Promise.Preserve();
            var rejectPromiseInt = rejectDeferredInt.Promise.Preserve();

            int expected = 100;

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

            int resolveCounter = 0;

            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                p.Then(v =>
                {
                    Assert.AreEqual(expected, v);
                    ++resolveCounter;
                }).Forget();

            TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                onCallbackAddedConvert: onCallbackAddedConvert,
                convertValue: expected
            );
            TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseInt,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                onCallbackAddedConvert: onCallbackAddedConvert,
                convertValue: expected
            );
            TestHelper.AddContinueCallbacks<int, string>(resolvePromiseInt,
                onCallbackAddedConvert: onCallbackAddedConvert,
                convertValue: expected
            );

            TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseInt,
                onResolve: _ => resolveAssert(),
                onCallbackAddedConvert: onCallbackAddedConvert,
                onCallbackAddedT: onCallbackAddedConvert,
                convertValue: expected,
                TValue: expected
            );
            TestHelper.AddContinueCallbacks<int, string>(rejectPromiseInt,
                onCallbackAddedConvert: onCallbackAddedConvert,
                convertValue: expected
            );

            resolveDeferredInt.Resolve(1);
            rejectDeferredInt.Reject("Fail value");

            Assert.AreEqual(
                (TestHelper.resolveTConvertCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                TestHelper.rejectTConvertCallbacks + TestHelper.rejectTTCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                ((TestHelper.continueTConvertCallbacks + TestHelper.continueTPromiseConvertCallbacks) * 2)) * 2,
                resolveCounter
            );

            resolvePromiseInt.Forget();
            rejectPromiseInt.Forget();
        }

        // If a promise is resolved with a thenable that participates in a circular thenable chain, such that the recursive
        // nature of[[Resolve]](promise, thenable) eventually causes[[Resolve]](promise, thenable) to be
        // called again, following the above algorithm will lead to infinite recursion.Implementations are encouraged, but
        // not required, to detect such recursion and reject promise with an informative Exception as the reason.

#if PROMISE_DEBUG
        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_void()
        {
            var resolveDeferred = Promise.NewDeferred();
            var rejectDeferred = Promise.NewDeferred();

            var resolvePromise = resolveDeferred.Promise.Preserve();
            var rejectPromise = rejectDeferred.Promise.Preserve();

            int exceptionCounter = 0;

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");
            Action<object> catcher = (object o) =>
            {
                Assert.IsInstanceOf<InvalidReturnException>(o);
                ++exceptionCounter;
            };

            Func<Promise, Promise> promiseToPromise = promise =>
            {
                promise.Catch(catcher).Forget();
                return promise.ThenDuplicate().ThenDuplicate().Catch(() => { });
            };

            Func<Promise<int>, Promise<int>> promiseToPromiseConvert = promise =>
            {
                promise.Catch(catcher).Forget();
                return promise.ThenDuplicate().ThenDuplicate().Catch(() => 1);
            };

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };

            TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddCallbacks<int, bool, string>(resolvePromise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            TestHelper.AddCallbacks<int, string, string>(rejectPromise,
                onResolve: resolveAssert,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            resolveDeferred.Resolve();
            rejectDeferred.Reject("Fail value");

            Assert.AreEqual(
                (TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                (TestHelper.continueVoidPromiseVoidCallbacks + TestHelper.continueVoidPromiseConvertCallbacks) * 2) * 2,
                exceptionCounter
            );

            resolvePromise.Forget();
            rejectPromise.Forget();
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_T()
        {
            var resolveDeferredInt = Promise.NewDeferred<int>();
            var rejectDeferredInt = Promise.NewDeferred<int>();

            var resolvePromiseInt = resolveDeferredInt.Promise.Preserve();
            var rejectPromiseInt = rejectDeferredInt.Promise.Preserve();

            int exceptionCounter = 0;

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");
            Action<object> catcher = (object o) =>
            {
                Assert.IsInstanceOf<InvalidReturnException>(o);
                ++exceptionCounter;
            };

            Func<Promise, Promise> promiseToPromise = promise =>
            {
                promise.Catch(catcher).Forget();
                return promise.ThenDuplicate().ThenDuplicate().Catch(() => { });
            };

            Func<Promise<int>, Promise<int>> promiseToPromiseConvert = promise =>
            {
                promise.Catch(catcher).Forget();
                return promise.ThenDuplicate().ThenDuplicate().Catch(() => 1);
            };

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };

            TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddCallbacks<int, int, string, string>(resolvePromiseInt,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(resolvePromiseInt,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            TestHelper.AddCallbacks<int, int, string, string>(rejectPromiseInt,
                onResolve: _ => resolveAssert(),
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                promiseToPromiseT: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert,
                onCallbackAddedT: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseInt,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            resolveDeferredInt.Resolve(1);
            rejectDeferredInt.Reject("Fail value");

            Assert.AreEqual(
                (TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                (TestHelper.continueTPromiseVoidCallbacks + TestHelper.continueTPromiseConvertCallbacks) * 2) * 2,
                exceptionCounter
            );

            resolvePromiseInt.Forget();
            rejectPromiseInt.Forget();
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_race_void()
        {
            var extraDeferred = Promise.NewDeferred<int>();
            var extraPromise = extraDeferred.Promise.Preserve();

            var resolveDeferred = Promise.NewDeferred();
            var rejectDeferred = Promise.NewDeferred();

            var resolvePromise = resolveDeferred.Promise.Preserve();
            var rejectPromise = rejectDeferred.Promise.Preserve();

            int exceptionCounter = 0;

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");
            Action<object> catcher = (object o) =>
            {
                Assert.IsInstanceOf<InvalidReturnException>(o);
                ++exceptionCounter;
            };

            Func<Promise, Promise> promiseToPromise = promise =>
            {
                promise.Catch(catcher).Forget();
                return Promise.Race(extraPromise, promise.ThenDuplicate().ThenDuplicate()).Catch(() => { });
            };

            Func<Promise<int>, Promise<int>> promiseToPromiseConvert = promise =>
            {
                promise.Catch(catcher).Forget();
                return Promise<int>.Race(extraPromise, promise.ThenDuplicate().ThenDuplicate()).Catch(() => 1);
            };

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };

            TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddCallbacks<int, bool, string>(resolvePromise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            TestHelper.AddCallbacks<int, string, string>(rejectPromise,
                onResolve: resolveAssert,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            resolveDeferred.Resolve();
            rejectDeferred.Reject("Fail value");

            Assert.AreEqual(
                (TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                (TestHelper.continueVoidPromiseVoidCallbacks + TestHelper.continueVoidPromiseConvertCallbacks) * 2) * 2,
                exceptionCounter
            );

            resolvePromise.Forget();
            rejectPromise.Forget();
            extraDeferred.Resolve(1);
            extraPromise.Forget();
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_race_T()
        {
            var extraDeferred = Promise.NewDeferred<int>();
            var extraPromise = extraDeferred.Promise.Preserve();

            var resolveDeferredInt = Promise.NewDeferred<int>();
            var rejectDeferredInt = Promise.NewDeferred<int>();

            var resolvePromiseInt = resolveDeferredInt.Promise.Preserve();
            var rejectPromiseInt = rejectDeferredInt.Promise.Preserve();

            int exceptionCounter = 0;

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");
            Action<object> catcher = (object o) =>
            {
                Assert.IsInstanceOf<InvalidReturnException>(o);
                ++exceptionCounter;
            };

            Func<Promise, Promise> promiseToPromise = promise =>
            {
                promise.Catch(catcher).Forget();
                return Promise.Race(promise.ThenDuplicate().ThenDuplicate(), extraPromise).Catch(() => { });
            };

            Func<Promise<int>, Promise<int>> promiseToPromiseConvert = promise =>
            {
                promise.Catch(catcher).Forget();
                return Promise<int>.Race(promise.ThenDuplicate().ThenDuplicate(), extraPromise).Catch(() => 1);
            };

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };

            TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddCallbacks<int, int, string, string>(resolvePromiseInt,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(resolvePromiseInt,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            TestHelper.AddCallbacks<int, int, string, string>(rejectPromiseInt,
                onResolve: _ => resolveAssert(),
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                promiseToPromiseT: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert,
                onCallbackAddedT: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseInt,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            resolveDeferredInt.Resolve(1);
            rejectDeferredInt.Reject("Fail value");

            Assert.AreEqual(
                (TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                (TestHelper.continueTPromiseVoidCallbacks + TestHelper.continueTPromiseConvertCallbacks) * 2) * 2,
                exceptionCounter
            );

            resolvePromiseInt.Forget();
            rejectPromiseInt.Forget();
            extraDeferred.Resolve(1);
            extraPromise.Forget();
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_first_void()
        {
            var extraDeferred = Promise.NewDeferred<int>();
            var extraPromise = extraDeferred.Promise.Preserve();

            var resolveDeferred = Promise.NewDeferred();
            var rejectDeferred = Promise.NewDeferred();

            var resolvePromise = resolveDeferred.Promise.Preserve();
            var rejectPromise = rejectDeferred.Promise.Preserve();

            int exceptionCounter = 0;

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");
            Action<object> catcher = (object o) =>
            {
                Assert.IsInstanceOf<InvalidReturnException>(o);
                ++exceptionCounter;
            };

            Func<Promise, Promise> promiseToPromise = promise =>
            {
                promise.Catch(catcher).Forget();
                return Promise.First(promise.ThenDuplicate().ThenDuplicate(), extraPromise).Catch(() => { });
            };

            Func<Promise<int>, Promise<int>> promiseToPromiseConvert = promise =>
            {
                promise.Catch(catcher).Forget();
                return Promise<int>.First(promise.ThenDuplicate().ThenDuplicate(), extraPromise).Catch(() => 1);
            };

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };

            TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddCallbacks<int, bool, string>(resolvePromise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            TestHelper.AddCallbacks<int, string, string>(rejectPromise,
                onResolve: resolveAssert,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            resolveDeferred.Resolve();
            rejectDeferred.Reject("Fail value");

            Assert.AreEqual(
                (TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                (TestHelper.continueVoidPromiseVoidCallbacks + TestHelper.continueVoidPromiseConvertCallbacks) * 2) * 2,
                exceptionCounter
            );

            resolvePromise.Forget();
            rejectPromise.Forget();
            extraDeferred.Resolve(1);
            extraPromise.Forget();
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_first_T()
        {
            var extraDeferred = Promise.NewDeferred<int>();
            var extraPromise = extraDeferred.Promise.Preserve();

            var resolveDeferredInt = Promise.NewDeferred<int>();
            var rejectDeferredInt = Promise.NewDeferred<int>();

            var resolvePromiseInt = resolveDeferredInt.Promise.Preserve();
            var rejectPromiseInt = rejectDeferredInt.Promise.Preserve();

            int exceptionCounter = 0;

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");
            Action<object> catcher = (object o) =>
            {
                Assert.IsInstanceOf<InvalidReturnException>(o);
                ++exceptionCounter;
            };

            Func<Promise, Promise> promiseToPromise = promise =>
            {
                promise.Catch(catcher).Forget();
                return Promise.First(extraPromise, promise.ThenDuplicate().ThenDuplicate()).Catch(() => { });
            };

            Func<Promise<int>, Promise<int>> promiseToPromiseConvert = promise =>
            {
                promise.Catch(catcher).Forget();
                return Promise<int>.First(extraPromise, promise.ThenDuplicate().ThenDuplicate()).Catch(() => 1);
            };

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };

            TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddCallbacks<int, int, string, string>(resolvePromiseInt,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(resolvePromiseInt,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            TestHelper.AddCallbacks<int, int, string, string>(rejectPromiseInt,
                onResolve: _ => resolveAssert(),
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                promiseToPromiseT: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert,
                onCallbackAddedT: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseInt,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            resolveDeferredInt.Resolve(1);
            rejectDeferredInt.Reject("Fail value");

            Assert.AreEqual(
                (TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                (TestHelper.continueTPromiseVoidCallbacks + TestHelper.continueTPromiseConvertCallbacks) * 2) * 2,
                exceptionCounter
            );

            resolvePromiseInt.Forget();
            rejectPromiseInt.Forget();
            extraDeferred.Resolve(1);
            extraPromise.Forget();
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_all_void()
        {
            var extraDeferred = Promise.NewDeferred<int>();
            var extraPromise = extraDeferred.Promise.Preserve();

            var resolveDeferred = Promise.NewDeferred();
            var rejectDeferred = Promise.NewDeferred();

            var resolvePromise = resolveDeferred.Promise.Preserve();
            var rejectPromise = rejectDeferred.Promise.Preserve();

            int exceptionCounter = 0;

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");
            Action<object> catcher = (object o) =>
            {
                Assert.IsInstanceOf<InvalidReturnException>(o);
                ++exceptionCounter;
            };

            Func<Promise, Promise> promiseToPromise = promise =>
            {
                promise.Catch(catcher).Forget();
                return Promise.All(promise.ThenDuplicate().ThenDuplicate(), extraPromise).Catch(() => { });
            };

            Func<Promise<int>, Promise<int>> promiseToPromiseConvert = promise =>
            {
                promise.Catch(catcher).Forget();
                return Promise<int>.All(promise.ThenDuplicate().ThenDuplicate(), extraPromise)
                    .Catch(() => { })
                    .Then(() => 1);
            };

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };

            TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddCallbacks<int, bool, string>(resolvePromise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            TestHelper.AddCallbacks<int, string, string>(rejectPromise,
                onResolve: resolveAssert,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            resolveDeferred.Resolve();
            rejectDeferred.Reject("Fail value");

            Assert.AreEqual(
                (TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                (TestHelper.continueVoidPromiseVoidCallbacks + TestHelper.continueVoidPromiseConvertCallbacks) * 2) * 2,
                exceptionCounter
            );

            resolvePromise.Forget();
            rejectPromise.Forget();
            extraDeferred.Resolve(1);
            extraPromise.Forget();
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_all_T()
        {
            var extraDeferred = Promise.NewDeferred<int>();
            var extraPromise = extraDeferred.Promise.Preserve();

            var resolveDeferredInt = Promise.NewDeferred<int>();
            var rejectDeferredInt = Promise.NewDeferred<int>();

            var resolvePromiseInt = resolveDeferredInt.Promise.Preserve();
            var rejectPromiseInt = rejectDeferredInt.Promise.Preserve();

            int exceptionCounter = 0;

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");
            Action<object> catcher = (object o) =>
            {
                Assert.IsInstanceOf<InvalidReturnException>(o);
                ++exceptionCounter;
            };

            Func<Promise, Promise> promiseToPromise = promise =>
            {
                promise.Catch(catcher).Forget();
                return Promise.All(extraPromise, promise.ThenDuplicate().ThenDuplicate())
                    .Catch(() => { });
            };

            Func<Promise<int>, Promise<int>> promiseToPromiseConvert = promise =>
            {
                promise.Catch(catcher).Forget();
                return Promise<int>.All(extraPromise, promise.ThenDuplicate().ThenDuplicate())
                    .Catch(() => { })
                    .Then(() => 1);
            };

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
            {
                var preserved = p = p.Preserve();
                preserved
                    .Catch(() => { })
                    .Finally(() => preserved.Forget())
                    .Forget();
            };

            TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddCallbacks<int, int, string, string>(resolvePromiseInt,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(resolvePromiseInt,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            TestHelper.AddCallbacks<int, int, string, string>(rejectPromiseInt,
                onResolve: _ => resolveAssert(),
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                promiseToPromiseT: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert,
                onCallbackAddedT: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseInt,
                promiseToPromise: promiseToPromise,
                promiseToPromiseConvert: promiseToPromiseConvert,
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            resolveDeferredInt.Resolve(1);
            rejectDeferredInt.Reject("Fail value");

            Assert.AreEqual(
                (TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                (TestHelper.continueTPromiseVoidCallbacks + TestHelper.continueTPromiseConvertCallbacks) * 2) * 2,
                exceptionCounter
            );

            resolvePromiseInt.Forget();
            rejectPromiseInt.Forget();
            extraDeferred.Resolve(1);
            extraPromise.Forget();
        }
#endif
    }
}