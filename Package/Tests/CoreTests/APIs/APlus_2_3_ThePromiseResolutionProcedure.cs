#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE1006 // Naming Styles

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Collections.Generic;

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
            string expectedMessage = "A Promise cannot wait on itself.";
            int exceptionCounter = 0;

            // When the promise awaits itself, it becomes rejected and invalidated, so the InvalidReturnException gets sent to the UncaughtRejectionHandler.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e =>
            {
                Assert.IsAssignableFrom<InvalidReturnException>(e.Value);
                Assert.AreEqual(expectedMessage, e.Value.UnsafeAs<Exception>().Message);
                ++exceptionCounter;
            };

            var resolveDeferred = Promise.NewDeferred();
            var rejectDeferred = Promise.NewDeferred();
            using (var resolvePromiseRetainer = resolveDeferred.Promise.GetRetainer())
            {
                using (var rejectPromiseRetainer = rejectDeferred.Promise.GetRetainer())
                {
                    int expectedCount = 0;
                    var voidPromiseQueue = new Queue<Promise>();
                    var intPromiseQueue = new Queue<Promise<int>>();
                    Func<Promise> voidReturnProvider = () => voidPromiseQueue.Dequeue();
                    Func<Promise<int>> intReturnProvider = () => intPromiseQueue.Dequeue();

                    foreach (var (func, adoptLocation) in TestHelper.GetFunctionsAdoptingPromiseVoidToVoid<string>(voidReturnProvider))
                    {
                        if (adoptLocation == AdoptLocation.Reject) continue;

                        ++expectedCount;
                        voidPromiseQueue.Enqueue(func.Invoke(resolvePromiseRetainer.WaitAsync()));
                    }
                    foreach (var (func, adoptLocation) in TestHelper.GetFunctionsAdoptingPromiseVoidToT<int, string>(intReturnProvider))
                    {
                        if (adoptLocation == AdoptLocation.Reject) continue;

                        ++expectedCount;
                        intPromiseQueue.Enqueue(func.Invoke(resolvePromiseRetainer.WaitAsync()));
                    }
                    foreach (var (func, adoptLocation) in TestHelper.GetFunctionsAdoptingPromiseVoidToVoid<string>(voidReturnProvider))
                    {
                        if (adoptLocation == AdoptLocation.Resolve) continue;

                        ++expectedCount;
                        voidPromiseQueue.Enqueue(func.Invoke(rejectPromiseRetainer.WaitAsync()));
                    }
                    foreach (var (func, adoptLocation) in TestHelper.GetFunctionsAdoptingPromiseVoidToT<int, string>(intReturnProvider))
                    {
                        if (adoptLocation == AdoptLocation.Resolve) continue;

                        ++expectedCount;
                        intPromiseQueue.Enqueue(func.Invoke(rejectPromiseRetainer.WaitAsync()));
                    }

                    resolveDeferred.Resolve();
                    rejectDeferred.Reject("Fail value");

                    Assert.AreEqual(expectedCount, exceptionCounter);
                }
            }

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void _2_3_1_IfPromiseAndXReferToTheSameObject_RejectPromiseWithInvalidReturnExceptionAsTheReason_T()
        {
            string expectedMessage = "A Promise cannot wait on itself.";
            int exceptionCounter = 0;

            // When the promise awaits itself, it becomes rejected and invalidated, so the InvalidReturnException gets sent to the UncaughtRejectionHandler.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e =>
            {
                Assert.IsAssignableFrom<InvalidReturnException>(e.Value);
                Assert.AreEqual(expectedMessage, e.Value.UnsafeAs<Exception>().Message);
                ++exceptionCounter;
            };

            var resolveDeferred = Promise.NewDeferred<int>();
            var rejectDeferred = Promise.NewDeferred<int>();
            using (var resolvePromiseRetainer = resolveDeferred.Promise.GetRetainer())
            {
                using (var rejectPromiseRetainer = rejectDeferred.Promise.GetRetainer())
                {
                    int expectedCount = 0;
                    var voidPromiseQueue = new Queue<Promise>();
                    var intPromiseQueue = new Queue<Promise<int>>();
                    Func<Promise> voidReturnProvider = () => voidPromiseQueue.Dequeue();
                    Func<Promise<int>> intReturnProvider = () => intPromiseQueue.Dequeue();

                    foreach (var (func, adoptLocation) in TestHelper.GetFunctionsAdoptingPromiseTToVoid<int, string>(voidReturnProvider))
                    {
                        if (adoptLocation == AdoptLocation.Reject) continue;

                        ++expectedCount;
                        voidPromiseQueue.Enqueue(func.Invoke(resolvePromiseRetainer.WaitAsync()));
                    }
                    foreach (var (func, adoptLocation) in TestHelper.GetFunctionsAdoptingPromiseTToT<int, string>(intReturnProvider))
                    {
                        if (adoptLocation == AdoptLocation.Reject) continue;

                        ++expectedCount;
                        intPromiseQueue.Enqueue(func.Invoke(resolvePromiseRetainer.WaitAsync()));
                    }
                    foreach (var (func, adoptLocation) in TestHelper.GetFunctionsAdoptingPromiseTToVoid<int, string>(voidReturnProvider))
                    {
                        if (adoptLocation == AdoptLocation.Resolve) continue;

                        ++expectedCount;
                        voidPromiseQueue.Enqueue(func.Invoke(rejectPromiseRetainer.WaitAsync()));
                    }
                    foreach (var (func, adoptLocation) in TestHelper.GetFunctionsAdoptingPromiseTToT<int, string>(intReturnProvider))
                    {
                        if (adoptLocation == AdoptLocation.Resolve) continue;

                        ++expectedCount;
                        intPromiseQueue.Enqueue(func.Invoke(rejectPromiseRetainer.WaitAsync()));
                    }

                    resolveDeferred.Resolve(42);
                    rejectDeferred.Reject("Fail value");

                    Assert.AreEqual(expectedCount, exceptionCounter);
                }
            }

            Promise.Config.UncaughtRejectionHandler = currentHandler;
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

                using (var resolvePromiseRetainer = resolveDeferred.Promise.GetRetainer())
                {
                    using (var rejectPromiseRetainer = rejectDeferred.Promise.GetRetainer())
                    {
                        Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                        Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                        var resolveWaitDeferred = Promise.NewDeferred();
                        var resolveWaitDeferredInt = Promise.NewDeferred<int>();
                        var rejectWaitDeferred = Promise.NewDeferred();
                        var rejectWaitDeferredInt = Promise.NewDeferred<int>();

                        using (var resolveWaitPromiseRetainer = resolveWaitDeferred.Promise.GetRetainer())
                        {
                            using (var rejectWaitPromiseRetainer = rejectWaitDeferred.Promise.GetRetainer())
                            {
                                using (var resolveWaitPromiseIntRetainer = resolveWaitDeferredInt.Promise.GetRetainer())
                                {
                                    using (var rejectWaitPromiseIntRetainer = rejectWaitDeferredInt.Promise.GetRetainer())
                                    {
                                        bool adopted = false;
                                        TestAction<Promise> onAdoptCallbackAdded = (ref Promise p) => adopted = true;
                                        TestAction<Promise<int>> onAdoptCallbackAddedConvert = (ref Promise<int> p) => adopted = true;
                                        TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                                        {
                                            if (!adopted)
                                            {
                                                p.Forget();
                                            }
                                            adopted = false;
                                        };
                                        TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                                        {
                                            if (!adopted)
                                            {
                                                p.Forget();
                                            }
                                            adopted = false;
                                        };

                                        TestHelper.AddResolveCallbacks<int, string>(resolvePromiseRetainer.WaitAsync(),
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: onAdoptCallbackAdded,
                                            onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                                        );
                                        TestHelper.AddCallbacks<int, object, string>(resolvePromiseRetainer.WaitAsync(),
                                            onReject: _ => rejectAssert(),
                                            onUnknownRejection: rejectAssert,
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: (ref Promise p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Reject,
                                            onAdoptCallbackAddedConvert: (ref Promise<int> p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Reject
                                        );
                                        TestHelper.AddContinueCallbacks<int, string>(resolvePromiseRetainer.WaitAsync(),
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: onAdoptCallbackAdded,
                                            onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                                        );

                                        TestHelper.AddResolveCallbacks<int, string>(resolvePromiseRetainer.WaitAsync(),
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: onAdoptCallbackAdded,
                                            onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                                        );
                                        TestHelper.AddCallbacks<int, bool, string>(resolvePromiseRetainer.WaitAsync(),
                                            onReject: _ => rejectAssert(),
                                            onUnknownRejection: rejectAssert,
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: (ref Promise p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Reject,
                                            onAdoptCallbackAddedConvert: (ref Promise<int> p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Reject
                                        );
                                        TestHelper.AddContinueCallbacks<int, string>(resolvePromiseRetainer.WaitAsync(),
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: onAdoptCallbackAdded,
                                            onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                                        );

                                        resolveDeferred.Resolve();
                                        Assert.AreEqual(expectedCompleteCount, completeCounter);


                                        TestHelper.AddCallbacks<int, object, string>(rejectPromiseRetainer.WaitAsync(),
                                            onResolve: resolveAssert,
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: (ref Promise p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Resolve,
                                            onAdoptCallbackAddedConvert: (ref Promise<int> p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Resolve,
                                            onAdoptCallbackAddedCatch: onAdoptCallbackAdded
                                        );
                                        TestHelper.AddContinueCallbacks<int, string>(rejectPromiseRetainer.WaitAsync(),
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: onAdoptCallbackAdded,
                                            onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                                        );

                                        TestHelper.AddCallbacks<int, object, string>(rejectPromiseRetainer.WaitAsync(),
                                            onResolve: resolveAssert,
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: (ref Promise p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Resolve,
                                            onAdoptCallbackAddedConvert: (ref Promise<int> p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Resolve,
                                            onAdoptCallbackAddedCatch: onAdoptCallbackAdded
                                        );
                                        TestHelper.AddContinueCallbacks<int, string>(rejectPromiseRetainer.WaitAsync(),
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: onAdoptCallbackAdded,
                                            onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
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
                                    }
                                }
                            }
                        }
                    }
                }
            }

            [Test]
            public void _2_3_2_1_IfXIsPending_PromiseMustRemainPendingUntilXIsFulfilledOrRejected_T()
            {
                int expectedCompleteCount = 0;
                int completeCounter = 0;

                var resolveDeferredInt = Promise.NewDeferred<int>();
                var rejectDeferredInt = Promise.NewDeferred<int>();

                using (var resolvePromiseIntRetainer = resolveDeferredInt.Promise.GetRetainer())
                {
                    using (var rejectPromiseIntRetainer = rejectDeferredInt.Promise.GetRetainer())
                    {
                        Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                        Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                        var resolveWaitDeferred = Promise.NewDeferred();
                        var resolveWaitDeferredInt = Promise.NewDeferred<int>();
                        var rejectWaitDeferred = Promise.NewDeferred();
                        var rejectWaitDeferredInt = Promise.NewDeferred<int>();

                        using (var resolveWaitPromiseRetainer = resolveWaitDeferred.Promise.GetRetainer())
                        {
                            using (var resolveWaitPromiseIntRetainer = resolveWaitDeferredInt.Promise.GetRetainer())
                            {
                                using (var rejectWaitPromiseRetainer = rejectWaitDeferred.Promise.GetRetainer())
                                {
                                    using (var rejectWaitPromiseIntRetainer = rejectWaitDeferredInt.Promise.GetRetainer())
                                    {
                                        bool adopted = false;
                                        TestAction<Promise> onAdoptCallbackAdded = (ref Promise p) => adopted = true;
                                        TestAction<Promise<int>> onAdoptCallbackAddedConvert = (ref Promise<int> p) => adopted = true;
                                        TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                                        {
                                            if (!adopted)
                                            {
                                                p.Forget();
                                            }
                                            adopted = false;
                                        };
                                        TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                                        {
                                            if (!adopted)
                                            {
                                                p.Forget();
                                            }
                                            adopted = false;
                                        };

                                        TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseIntRetainer.WaitAsync(),
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: onAdoptCallbackAdded,
                                            onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                                        );
                                        TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseIntRetainer.WaitAsync(),
                                            onReject: _ => rejectAssert(),
                                            onUnknownRejection: rejectAssert,
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: (ref Promise p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Reject,
                                            onAdoptCallbackAddedConvert: (ref Promise<int> p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Reject
                                        );
                                        TestHelper.AddContinueCallbacks<int, string>(resolvePromiseIntRetainer.WaitAsync(),
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: onAdoptCallbackAdded,
                                            onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                                        );

                                        TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseIntRetainer.WaitAsync(),
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: onAdoptCallbackAdded,
                                            onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                                        );
                                        TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseIntRetainer.WaitAsync(),
                                            onReject: _ => rejectAssert(),
                                            onUnknownRejection: rejectAssert,
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            promiseToPromiseT: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: (ref Promise p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Reject,
                                            onAdoptCallbackAddedConvert: (ref Promise<int> p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Reject
                                        );
                                        TestHelper.AddContinueCallbacks<int, string>(resolvePromiseIntRetainer.WaitAsync(),
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: onAdoptCallbackAdded,
                                            onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                                        );

                                        resolveDeferredInt.Resolve(1);
                                        Assert.AreEqual(expectedCompleteCount, completeCounter);


                                        TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseIntRetainer.WaitAsync(),
                                            onResolve: _ => resolveAssert(),
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            promiseToPromiseT: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onCallbackAddedT: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: (ref Promise p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Resolve,
                                            onAdoptCallbackAddedConvert: (ref Promise<int> p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Resolve,
                                            onAdoptCallbackAddedCatch: onAdoptCallbackAddedConvert
                                        );
                                        TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseIntRetainer.WaitAsync(),
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Forget();
                                                return resolveWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: onAdoptCallbackAdded,
                                            onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                                        );

                                        TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseIntRetainer.WaitAsync(),
                                            onResolve: _ => resolveAssert(),
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            promiseToPromiseT: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onCallbackAddedT: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: (ref Promise p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Resolve,
                                            onAdoptCallbackAddedConvert: (ref Promise<int> p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Resolve,
                                            onAdoptCallbackAddedCatch: onAdoptCallbackAddedConvert
                                        );
                                        TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseIntRetainer.WaitAsync(),
                                            promiseToPromise: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseRetainer.WaitAsync();
                                            },
                                            promiseToPromiseConvert: p =>
                                            {
                                                p.Finally(() => ++completeCounter).Catch(() => { }).Forget();
                                                return rejectWaitPromiseIntRetainer.WaitAsync();
                                            },
                                            onCallbackAdded: onCallbackAdded,
                                            onCallbackAddedConvert: onCallbackAddedConvert,
                                            onAdoptCallbackAdded: onAdoptCallbackAdded,
                                            onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
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
                                    }
                                }
                            }
                        }
                    }
                }
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

                using (var resolvePromiseRetainer = resolveDeferred.Promise.GetRetainer())
                {
                    using (var rejectPromiseRetainer = rejectDeferred.Promise.GetRetainer())
                    {
                        using (var resolvePromiseIntRetainer = resolveDeferredInt.Promise.GetRetainer())
                        {
                            using (var rejectPromiseIntRetainer = rejectDeferredInt.Promise.GetRetainer())
                            {
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
                                using (var resolveWaitPromiseIntRetainer = resolveWaitDeferredInt.Promise.GetRetainer())
                                {
                                    Func<Promise<int>, Promise<int>> promiseToPromiseConvert = p => resolveWaitPromiseIntRetainer.WaitAsync();

                                    // Test pending -> resolved and already resolved.
                                    bool firstRun = true;
                                RunAgain:
                                    resolveCounter = 0;

                                    TestHelper.AddResolveCallbacks<int, string>(resolvePromiseRetainer.WaitAsync(),
                                        promiseToPromiseConvert: promiseToPromiseConvert,
                                        onCallbackAddedConvert: onCallbackAddedConvert
                                    );
                                    TestHelper.AddCallbacks<int, object, string>(resolvePromiseRetainer.WaitAsync(),
                                        onReject: _ => rejectAssert(),
                                        onUnknownRejection: rejectAssert,
                                        promiseToPromiseConvert: promiseToPromiseConvert,
                                        onCallbackAddedConvert: onCallbackAddedConvert
                                    );
                                    TestHelper.AddContinueCallbacks<int, string>(resolvePromiseRetainer.WaitAsync(),
                                        promiseToPromiseConvert: promiseToPromiseConvert,
                                        onCallbackAddedConvert: onCallbackAddedConvert
                                    );

                                    TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseIntRetainer.WaitAsync(),
                                        promiseToPromiseConvert: promiseToPromiseConvert,
                                        onCallbackAddedConvert: onCallbackAddedConvert
                                    );
                                    TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseIntRetainer.WaitAsync(),
                                        onReject: _ => rejectAssert(),
                                        onUnknownRejection: rejectAssert,
                                        promiseToPromiseConvert: promiseToPromiseConvert,
                                        onCallbackAddedConvert: onCallbackAddedConvert
                                    );
                                    TestHelper.AddContinueCallbacks<int, string>(resolvePromiseIntRetainer.WaitAsync(),
                                        promiseToPromiseConvert: promiseToPromiseConvert,
                                        onCallbackAddedConvert: onCallbackAddedConvert
                                    );


                                    TestHelper.AddCallbacks<int, object, string>(rejectPromiseRetainer.WaitAsync(),
                                        onResolve: resolveAssert,
                                        promiseToPromiseConvert: promiseToPromiseConvert,
                                        onCallbackAddedConvert: onCallbackAddedConvert
                                    );
                                    TestHelper.AddContinueCallbacks<int, string>(rejectPromiseRetainer.WaitAsync(),
                                        promiseToPromiseConvert: promiseToPromiseConvert,
                                        onCallbackAddedConvert: onCallbackAddedConvert
                                    );

                                    TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseIntRetainer.WaitAsync(),
                                        onResolve: _ => resolveAssert(),
                                        promiseToPromiseConvert: promiseToPromiseConvert,
                                        promiseToPromiseT: promiseToPromiseConvert,
                                        onCallbackAddedConvert: onCallbackAddedConvert,
                                        onCallbackAddedT: onCallbackAddedConvert
                                    );
                                    TestHelper.AddContinueCallbacks<int, string>(rejectPromiseIntRetainer.WaitAsync(),
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
                                }
                            }
                        }
                    }
                }
            }

            [Test]
            public void _2_3_2_3_IfWhenXIsRejected_RejectPromiseWithTheSameReason_void()
            {
                var resolveDeferred = Promise.NewDeferred();
                var rejectDeferred = Promise.NewDeferred();

                resolveDeferred.Resolve();
                rejectDeferred.Reject("Fail value");

                using (var resolvePromiseRetainer = resolveDeferred.Promise.GetRetainer())
                {
                    using (var rejectPromiseRetainer = rejectDeferred.Promise.GetRetainer())
                    {
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

                        using (var rejectWaitPromiseRetainer = rejectWaitDeferred.Promise.GetRetainer())
                        {
                            using (var rejectWaitPromiseIntRetainer = rejectWaitDeferredInt.Promise.GetRetainer())
                            {
                                Func<Promise, Promise> promiseToPromise = p => rejectWaitPromiseRetainer.WaitAsync();
                                Func<Promise<int>, Promise<int>> promiseToPromiseConvert = p => rejectWaitPromiseIntRetainer.WaitAsync();

                                // Test pending -> rejected and already rejected.
                                bool firstRun = true;
                            RunAgain:
                                rejectCounter = 0;

                                TestHelper.AddResolveCallbacks<int, string>(resolvePromiseRetainer.WaitAsync(),
                                    promiseToPromise: promiseToPromise,
                                    onCallbackAdded: onCallbackAdded,
                                    promiseToPromiseConvert: promiseToPromiseConvert,
                                    onCallbackAddedConvert: onCallbackAddedConvert
                                );
                                TestHelper.AddCallbacks<int, object, string>(resolvePromiseRetainer.WaitAsync(),
                                    onReject: _ => rejectAssert(),
                                    onUnknownRejection: rejectAssert,
                                    promiseToPromise: promiseToPromise,
                                    onCallbackAdded: onCallbackAdded,
                                    promiseToPromiseConvert: promiseToPromiseConvert,
                                    onCallbackAddedConvert: onCallbackAddedConvert
                                );
                                TestHelper.AddContinueCallbacks<int, string>(resolvePromiseRetainer.WaitAsync(),
                                    promiseToPromise: promiseToPromise,
                                    onCallbackAdded: onCallbackAdded,
                                    promiseToPromiseConvert: promiseToPromiseConvert,
                                    onCallbackAddedConvert: onCallbackAddedConvert
                                );

                                TestHelper.AddCallbacks<int, object, string>(rejectPromiseRetainer.WaitAsync(),
                                    onResolve: resolveAssert,
                                    promiseToPromise: promiseToPromise,
                                    onCallbackAdded: onCallbackAdded,
                                    promiseToPromiseConvert: promiseToPromiseConvert,
                                    onCallbackAddedConvert: onCallbackAddedConvert
                                );
                                TestHelper.AddContinueCallbacks<int, string>(rejectPromiseRetainer.WaitAsync(),
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
                            }
                        }
                    }
                }
            }

            [Test]
            public void _2_3_2_3_IfWhenXIsRejected_RejectPromiseWithTheSameReason_T()
            {
                var resolveDeferredInt = Promise.NewDeferred<int>();
                var rejectDeferredInt = Promise.NewDeferred<int>();

                resolveDeferredInt.Resolve(1);
                rejectDeferredInt.Reject("Fail value");

                using (var resolvePromiseIntRetainer = resolveDeferredInt.Promise.GetRetainer())
                {
                    using (var rejectPromiseIntRetainer = rejectDeferredInt.Promise.GetRetainer())
                    {
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

                        using (var rejectWaitPromiseRetainer = rejectWaitDeferred.Promise.GetRetainer())
                        {
                            using (var rejectWaitPromiseIntRetainer = rejectWaitDeferredInt.Promise.GetRetainer())
                            {
                                Func<Promise, Promise> promiseToPromise = p => rejectWaitPromiseRetainer.WaitAsync();
                                Func<Promise<int>, Promise<int>> promiseToPromiseConvert = p => rejectWaitPromiseIntRetainer.WaitAsync();

                                // Test pending -> rejected and already rejected.
                                bool firstRun = true;
                            RunAgain:
                                rejectCounter = 0;

                                TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseIntRetainer.WaitAsync(),
                                    promiseToPromise: promiseToPromise,
                                    onCallbackAdded: onCallbackAdded,
                                    promiseToPromiseConvert: promiseToPromiseConvert,
                                    onCallbackAddedConvert: onCallbackAddedConvert
                                );
                                TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseIntRetainer.WaitAsync(),
                                    onReject: _ => rejectAssert(),
                                    onUnknownRejection: rejectAssert,
                                    promiseToPromise: promiseToPromise,
                                    onCallbackAdded: onCallbackAdded,
                                    promiseToPromiseConvert: promiseToPromiseConvert,
                                    onCallbackAddedConvert: onCallbackAddedConvert,
                                    promiseToPromiseT: promiseToPromiseConvert,
                                    onCallbackAddedT: onCallbackAddedConvert
                                );
                                TestHelper.AddContinueCallbacks<int, int, string>(resolvePromiseIntRetainer.WaitAsync(),
                                    promiseToPromise: promiseToPromise,
                                    onCallbackAdded: onCallbackAdded,
                                    promiseToPromiseConvert: promiseToPromiseConvert,
                                    onCallbackAddedConvert: onCallbackAddedConvert
                                );

                                TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseIntRetainer.WaitAsync(),
                                    onResolve: _ => resolveAssert(),
                                    promiseToPromise: promiseToPromise,
                                    onCallbackAdded: onCallbackAdded,
                                    promiseToPromiseConvert: promiseToPromiseConvert,
                                    onCallbackAddedConvert: onCallbackAddedConvert,
                                    promiseToPromiseT: promiseToPromiseConvert,
                                    onCallbackAddedT: onCallbackAddedConvert
                                );
                                TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseIntRetainer.WaitAsync(),
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
                            }
                        }
                    }
                }
            }
        }

        // Not supported. You may alternatively "return Promise.New(deferred => {...});".
        // 2.3.3 if X is a function...

        [Test]
        public void _2_3_4_IfOnResolvedOrOnRejectedReturnsSuccessfully_ResolvePromise_void()
        {
            var resolveDeferred = Promise.NewDeferred();
            var rejectDeferred = Promise.NewDeferred();

            using (var resolvePromiseRetainer = resolveDeferred.Promise.GetRetainer())
            {
                using (var rejectPromiseRetainer = rejectDeferred.Promise.GetRetainer())
                {
                    Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                    Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                    int resolveCounter = 0;

                    TestAction<Promise> onCallbackAdded = (ref Promise p) => p.Then(() => ++resolveCounter).Forget();
                    TestAction<Promise<string>> onCallbackAddedConvert = (ref Promise<string> p) => p.Then(() => ++resolveCounter).Forget();

                    TestHelper.AddResolveCallbacks<string, string>(resolvePromiseRetainer.WaitAsync(),
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert
                    );
                    TestHelper.AddCallbacks<string, object, string>(resolvePromiseRetainer.WaitAsync(),
                        onReject: _ => rejectAssert(),
                        onUnknownRejection: rejectAssert,
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert
                    );
                    TestHelper.AddContinueCallbacks<string, string>(resolvePromiseRetainer.WaitAsync(),
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert
                    );

                    TestHelper.AddCallbacks<string, object, string>(rejectPromiseRetainer.WaitAsync(),
                        onResolve: resolveAssert,
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert
                    );
                    TestHelper.AddContinueCallbacks<string, string>(rejectPromiseRetainer.WaitAsync(),
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
                }
            }
        }

        [Test]
        public void _2_3_4_IfOnResolvedOrOnRejectedReturnsSuccessfully_ResolvePromise_T()
        {
            var resolveDeferredInt = Promise.NewDeferred<int>();
            var rejectDeferredInt = Promise.NewDeferred<int>();

            using (var resolvePromiseIntRetainer = resolveDeferredInt.Promise.GetRetainer())
            {
                using (var rejectPromiseIntRetainer = rejectDeferredInt.Promise.GetRetainer())
                {
                    Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                    Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                    int resolveCounter = 0;

                    TestAction<Promise> onCallbackAdded = (ref Promise p) => p.Then(() => ++resolveCounter).Forget();
                    TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) => p.Then(() => ++resolveCounter).Forget();

                    TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseIntRetainer.WaitAsync(),
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert
                    );
                    TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseIntRetainer.WaitAsync(),
                        onReject: _ => rejectAssert(),
                        onUnknownRejection: rejectAssert,
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        onCallbackAddedT: onCallbackAddedConvert
                    );
                    TestHelper.AddContinueCallbacks<int, int, string>(resolvePromiseIntRetainer.WaitAsync(),
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert
                    );

                    TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseIntRetainer.WaitAsync(),
                        onResolve: _ => resolveAssert(),
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        onCallbackAddedT: onCallbackAddedConvert
                    );
                    TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseIntRetainer.WaitAsync(),
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
                }
            }
        }

        [Test]
        public void _2_3_4_IfXIsNotAPromiseOrAFunction_FulfillPromiseWithX_void()
        {
            var resolveDeferred = Promise.NewDeferred();
            var rejectDeferred = Promise.NewDeferred();

            using (var resolvePromiseRetainer = resolveDeferred.Promise.GetRetainer())
            {
                using (var rejectPromiseRetainer = rejectDeferred.Promise.GetRetainer())
                {
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

                    TestHelper.AddResolveCallbacks<int, string>(resolvePromiseRetainer.WaitAsync(),
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        convertValue: expected
                    );
                    TestHelper.AddCallbacks<int, object, string>(resolvePromiseRetainer.WaitAsync(),
                        onReject: _ => rejectAssert(),
                        onUnknownRejection: rejectAssert,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        convertValue: expected
                    );
                    TestHelper.AddContinueCallbacks<int, string>(resolvePromiseRetainer.WaitAsync(),
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        convertValue: expected
                    );

                    TestHelper.AddCallbacks<int, object, string>(rejectPromiseRetainer.WaitAsync(),
                        onResolve: resolveAssert,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        convertValue: expected
                    );
                    TestHelper.AddContinueCallbacks<int, string>(rejectPromiseRetainer.WaitAsync(),
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
                }
            }
        }

        [Test]
        public void _2_3_4_IfXIsNotAPromiseOrAFunction_FulfillPromiseWithX_T()
        {
            var resolveDeferredInt = Promise.NewDeferred<int>();
            var rejectDeferredInt = Promise.NewDeferred<int>();

            using (var resolvePromiseIntRetainer = resolveDeferredInt.Promise.GetRetainer())
            {
                using (var rejectPromiseIntRetainer = rejectDeferredInt.Promise.GetRetainer())
                {
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

                    TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseIntRetainer.WaitAsync(),
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        convertValue: expected
                    );
                    TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseIntRetainer.WaitAsync(),
                        onReject: _ => rejectAssert(),
                        onUnknownRejection: rejectAssert,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        convertValue: expected
                    );
                    TestHelper.AddContinueCallbacks<int, string>(resolvePromiseIntRetainer.WaitAsync(),
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        convertValue: expected
                    );

                    TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseIntRetainer.WaitAsync(),
                        onResolve: _ => resolveAssert(),
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        onCallbackAddedT: onCallbackAddedConvert,
                        convertValue: expected,
                        TValue: expected
                    );
                    TestHelper.AddContinueCallbacks<int, string>(rejectPromiseIntRetainer.WaitAsync(),
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
                }
            }
        }

        // If a promise is resolved with a thenable that participates in a circular thenable chain, such that the recursive
        // nature of[[Resolve]](promise, thenable) eventually causes[[Resolve]](promise, thenable) to be
        // called again, following the above algorithm will lead to infinite recursion.Implementations are encouraged, but
        // not required, to detect such recursion and reject promise with an informative Exception as the reason.

#if PROMISE_DEBUG
        private static void TestCircularAwait_void(Func<Promise.Retainer, Promise> circularPromiseVoidGetter, Func<Promise<int>.Retainer, Promise<int>> circularPromiseTGetter)
        {
            string expectedMessage = "Circular Promise chain detected.";
            var resolveDeferred = Promise.NewDeferred();
            var rejectDeferred = Promise.NewDeferred();

            using (var resolvePromiseRetainer = resolveDeferred.Promise.GetRetainer())
            {
                using (var rejectPromiseRetainer = rejectDeferred.Promise.GetRetainer())
                {
                    int exceptionCounter = 0;

                    Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                    Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");
                    Action<object> catcher = (object o) =>
                    {
                        Assert.IsInstanceOf<InvalidReturnException>(o);
                        Assert.AreEqual(expectedMessage, o.UnsafeAs<Exception>().Message);
                        ++exceptionCounter;
                    };

                    Func<Promise, Promise> promiseToPromise = promise =>
                    {
                        using (var promiseRetainer = promise.GetRetainer())
                        {
                            promiseRetainer.WaitAsync().Catch(catcher).Forget();
                            return circularPromiseVoidGetter(promiseRetainer);
                        }
                    };

                    Func<Promise<int>, Promise<int>> promiseToPromiseConvert = promise =>
                    {
                        using (var promiseRetainer = promise.GetRetainer())
                        {
                            promiseRetainer.WaitAsync().Catch(catcher).Forget();
                            return circularPromiseTGetter(promiseRetainer);
                        }
                    };

                    bool adopted = false;
                    TestAction<Promise> onAdoptCallbackAdded = (ref Promise p) => adopted = true;
                    TestAction<Promise<int>> onAdoptCallbackAddedConvert = (ref Promise<int> p) => adopted = true;
                    TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                    {
                        if (!adopted)
                        {
                            p.Forget();
                        }
                        adopted = false;
                    };
                    TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                    {
                        if (!adopted)
                        {
                            p.Forget();
                        }
                        adopted = false;
                    };

                    TestHelper.AddResolveCallbacks<int, string>(resolvePromiseRetainer.WaitAsync(),
                        promiseToPromise: promiseToPromise,
                        promiseToPromiseConvert: promiseToPromiseConvert,
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        onAdoptCallbackAdded: onAdoptCallbackAdded,
                        onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                    );
                    TestHelper.AddCallbacks<int, bool, string>(resolvePromiseRetainer.WaitAsync(),
                        onReject: _ => rejectAssert(),
                        onUnknownRejection: rejectAssert,
                        promiseToPromise: promiseToPromise,
                        promiseToPromiseConvert: promiseToPromiseConvert,
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        onAdoptCallbackAdded: (ref Promise p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Reject,
                        onAdoptCallbackAddedConvert: (ref Promise<int> p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Reject
                    );
                    TestHelper.AddContinueCallbacks<int, string>(resolvePromiseRetainer.WaitAsync(),
                        promiseToPromise: promiseToPromise,
                        promiseToPromiseConvert: promiseToPromiseConvert,
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        onAdoptCallbackAdded: onAdoptCallbackAdded,
                        onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                    );

                    TestHelper.AddCallbacks<int, string, string>(rejectPromiseRetainer.WaitAsync(),
                        onResolve: resolveAssert,
                        promiseToPromise: promiseToPromise,
                        promiseToPromiseConvert: promiseToPromiseConvert,
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        onAdoptCallbackAdded: (ref Promise p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Resolve,
                        onAdoptCallbackAddedConvert: (ref Promise<int> p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Resolve,
                        onAdoptCallbackAddedCatch: onAdoptCallbackAdded
                    );
                    TestHelper.AddContinueCallbacks<int, string>(rejectPromiseRetainer.WaitAsync(),
                        promiseToPromise: promiseToPromise,
                        promiseToPromiseConvert: promiseToPromiseConvert,
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        onAdoptCallbackAdded: onAdoptCallbackAdded,
                        onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                    );

                    resolveDeferred.Resolve();
                    rejectDeferred.Reject("Fail value");

                    Assert.AreEqual(
                        (TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                        TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                        (TestHelper.continueVoidPromiseVoidCallbacks + TestHelper.continueVoidPromiseConvertCallbacks) * 2) * 2,
                        exceptionCounter
                    );
                }
            }
        }

        private static void TestCircularAwait_T(Func<Promise.Retainer, Promise> circularPromiseVoidGetter, Func<Promise<int>.Retainer, Promise<int>> circularPromiseTGetter)
        {
            string expectedMessage = "Circular Promise chain detected.";
            var resolveDeferredInt = Promise.NewDeferred<int>();
            var rejectDeferredInt = Promise.NewDeferred<int>();

            using (var resolvePromiseIntRetainer = resolveDeferredInt.Promise.GetRetainer())
            {
                using (var rejectPromiseIntRetainer = rejectDeferredInt.Promise.GetRetainer())
                {
                    int exceptionCounter = 0;

                    Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                    Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");
                    Action<object> catcher = (object o) =>
                    {
                        Assert.IsInstanceOf<InvalidReturnException>(o);
                        Assert.AreEqual(expectedMessage, o.UnsafeAs<Exception>().Message);
                        ++exceptionCounter;
                    };

                    Func<Promise, Promise> promiseToPromise = promise =>
                    {
                        using (var promiseRetainer = promise.GetRetainer())
                        {
                            promiseRetainer.WaitAsync().Catch(catcher).Forget();
                            return circularPromiseVoidGetter(promiseRetainer);
                        }
                    };

                    Func<Promise<int>, Promise<int>> promiseToPromiseConvert = promise =>
                    {
                        using (var promiseRetainer = promise.GetRetainer())
                        {
                            promiseRetainer.WaitAsync().Catch(catcher).Forget();
                            return circularPromiseTGetter(promiseRetainer);
                        }
                    };

                    bool adopted = false;
                    TestAction<Promise> onAdoptCallbackAdded = (ref Promise p) => adopted = true;
                    TestAction<Promise<int>> onAdoptCallbackAddedConvert = (ref Promise<int> p) => adopted = true;
                    TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                    {
                        if (!adopted)
                        {
                            p.Forget();
                        }
                        adopted = false;
                    };
                    TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                    {
                        if (!adopted)
                        {
                            p.Forget();
                        }
                        adopted = false;
                    };

                    TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseIntRetainer.WaitAsync(),
                        promiseToPromise: promiseToPromise,
                        promiseToPromiseConvert: promiseToPromiseConvert,
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        onAdoptCallbackAdded: onAdoptCallbackAdded,
                        onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                    );
                    TestHelper.AddCallbacks<int, int, string, string>(resolvePromiseIntRetainer.WaitAsync(),
                        onReject: _ => rejectAssert(),
                        onUnknownRejection: rejectAssert,
                        promiseToPromise: promiseToPromise,
                        promiseToPromiseConvert: promiseToPromiseConvert,
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        onAdoptCallbackAdded: (ref Promise p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Reject,
                        onAdoptCallbackAddedConvert: (ref Promise<int> p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Reject
                    );
                    TestHelper.AddContinueCallbacks<int, int, string>(resolvePromiseIntRetainer.WaitAsync(),
                        promiseToPromise: promiseToPromise,
                        promiseToPromiseConvert: promiseToPromiseConvert,
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        onAdoptCallbackAdded: onAdoptCallbackAdded,
                        onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                    );

                    TestHelper.AddCallbacks<int, int, string, string>(rejectPromiseIntRetainer.WaitAsync(),
                        onResolve: _ => resolveAssert(),
                        promiseToPromise: promiseToPromise,
                        promiseToPromiseConvert: promiseToPromiseConvert,
                        promiseToPromiseT: promiseToPromiseConvert,
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        onCallbackAddedT: onCallbackAddedConvert,
                        onAdoptCallbackAdded: (ref Promise p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Resolve,
                        onAdoptCallbackAddedConvert: (ref Promise<int> p, AdoptLocation adoptLocation) => adopted = adoptLocation != AdoptLocation.Resolve,
                        onAdoptCallbackAddedCatch: onAdoptCallbackAddedConvert
                    );
                    TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseIntRetainer.WaitAsync(),
                        promiseToPromise: promiseToPromise,
                        promiseToPromiseConvert: promiseToPromiseConvert,
                        onCallbackAdded: onCallbackAdded,
                        onCallbackAddedConvert: onCallbackAddedConvert,
                        onAdoptCallbackAdded: onAdoptCallbackAdded,
                        onAdoptCallbackAddedConvert: onAdoptCallbackAddedConvert
                    );

                    resolveDeferredInt.Resolve(1);
                    rejectDeferredInt.Reject("Fail value");

                    Assert.AreEqual(
                        (TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                        TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                        (TestHelper.continueTPromiseVoidCallbacks + TestHelper.continueTPromiseConvertCallbacks) * 2) * 2,
                        exceptionCounter
                    );
                }
            }
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_void()
        {
            TestCircularAwait_void(
                promiseRetainer => promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate().Catch(() => { }),
                promiseRetainer => promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate().Catch(() => 1)
            );
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_T()
        {
            TestCircularAwait_T(
                promiseRetainer => promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate().Catch(() => { }),
                promiseRetainer => promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate().Catch(() => 1)
            );
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_race_void()
        {
            var extraDeferred = Promise.NewDeferred<int>();
            using (var extraPromiseRetainer = extraDeferred.Promise.GetRetainer())
            {
                TestCircularAwait_void(
                    promiseRetainer => Promise.Race(
                        promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate(),
                        extraPromiseRetainer.WaitAsync()
                    ).Catch(() => { }),
                    promiseRetainer => Promise<int>.Race(
                        promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate(),
                        extraPromiseRetainer.WaitAsync()
                    ).Catch(() => 1)
                );
                extraDeferred.Resolve(1);
            }
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_race_T()
        {
            var extraDeferred = Promise.NewDeferred<int>();
            using (var extraPromiseRetainer = extraDeferred.Promise.GetRetainer())
            {
                TestCircularAwait_T(
                    promiseRetainer => Promise.Race(
                        promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate(),
                        extraPromiseRetainer.WaitAsync()
                    ).Catch(() => { }),
                    promiseRetainer => Promise<int>.Race(
                        promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate(),
                        extraPromiseRetainer.WaitAsync()
                    ).Catch(() => 1)
                );
                extraDeferred.Resolve(1);
            }
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_first_void()
        {
            var extraDeferred = Promise.NewDeferred<int>();
            using (var extraPromiseRetainer = extraDeferred.Promise.GetRetainer())
            {
                TestCircularAwait_void(
                    promiseRetainer => Promise.First(
                        promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate(),
                        extraPromiseRetainer.WaitAsync()
                    ).Catch(() => { }),
                    promiseRetainer => Promise<int>.First(
                        promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate(),
                        extraPromiseRetainer.WaitAsync()
                    ).Catch(() => 1)
                );
                extraDeferred.Resolve(1);
            }
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_first_T()
        {
            var extraDeferred = Promise.NewDeferred<int>();
            using (var extraPromiseRetainer = extraDeferred.Promise.GetRetainer())
            {
                TestCircularAwait_T(
                    promiseRetainer => Promise.First(
                        promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate(),
                        extraPromiseRetainer.WaitAsync()
                    ).Catch(() => { }),
                    promiseRetainer => Promise<int>.First(
                        promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate(),
                        extraPromiseRetainer.WaitAsync()
                    ).Catch(() => 1)
                );
                extraDeferred.Resolve(1);
            }
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_all_void()
        {
            var extraDeferred = Promise.NewDeferred<int>();
            using (var extraPromiseRetainer = extraDeferred.Promise.GetRetainer())
            {
                TestCircularAwait_void(
                    promiseRetainer => Promise.All(
                        promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate(),
                        extraPromiseRetainer.WaitAsync()
                    ).Catch(() => { }),
                    promiseRetainer => Promise<int>.All(
                        promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate(),
                        extraPromiseRetainer.WaitAsync()
                    ).Catch(() => { }).Then(() => 1)
                );
                extraDeferred.Resolve(1);
            }
        }

        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_all_T()
        {
            var extraDeferred = Promise.NewDeferred<int>();
            using (var extraPromiseRetainer = extraDeferred.Promise.GetRetainer())
            {
                TestCircularAwait_T(
                    promiseRetainer => Promise.All(
                        promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate(),
                        extraPromiseRetainer.WaitAsync()
                    ).Catch(() => { }),
                    promiseRetainer => Promise<int>.All(
                        promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate(),
                        extraPromiseRetainer.WaitAsync()
                    ).Catch(() => { }).Then(() => 1)
                );
                extraDeferred.Resolve(1);
            }
        }
#endif
    }
}