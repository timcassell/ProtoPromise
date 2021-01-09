#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE1006 // Naming Styles

using System;
using NUnit.Framework;

namespace Proto.Promises.Tests
{
    public class APlus_2_3_ThePromiseResolutionProcedure
    {
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
            Action<object> catcher = (object o) =>
            {
                Assert.IsInstanceOf<InvalidReturnException>(o);
                ++exceptionCounter;
            };

            TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p; }
            );
            TestHelper.AddCallbacks<int, bool, string>(resolvePromise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p; }
            );
            TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p; }
            );

            TestHelper.AddCallbacks<int, string, string>(rejectPromise,
                onResolve: resolveAssert,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p; }
            );
            TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p; }
            );

            resolveDeferred.Resolve();
            rejectDeferred.Reject("Fail value");

            Promise.Manager.HandleCompletes();

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
            Action<object> catcher = (object o) =>
            {
                Assert.IsInstanceOf<InvalidReturnException>(o);
                ++exceptionCounter;
            };

            TestHelper.AddResolveCallbacks<int, bool, string>(resolvePromise,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p; }
            );
            TestHelper.AddCallbacks<int, bool, string, string>(resolvePromise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p; },
                promiseToPromiseT: p => { p.Catch(catcher).Forget(); return p; }
            );
            TestHelper.AddContinueCallbacks<int, bool, string>(resolvePromise,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p; }
            );

            TestHelper.AddCallbacks<int, bool, string, string>(rejectPromise,
                onResolve: _ => resolveAssert(),
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p; },
                promiseToPromiseT: p => { p.Catch(catcher).Forget(); return p; }
            );
            TestHelper.AddContinueCallbacks<int, bool, string>(rejectPromise,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p; }
            );

            resolveDeferred.Resolve(1);
            rejectDeferred.Reject("Fail value");

            Promise.Manager.HandleCompletes();

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
                var rejectWaitPromise = resolveWaitDeferredInt.Promise.Preserve();
                var resolveWaitPromiseInt = rejectWaitDeferred.Promise.Preserve();
                var rejectWaitPromiseInt = rejectWaitDeferredInt.Promise.Preserve();

                TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromise;
                    }
                );
                TestHelper.AddCallbacks<int, object, string>(resolvePromise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromise;
                    }
                );
                TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromise;
                    }
                );

                TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromiseInt;
                    }
                );
                TestHelper.AddCallbacks<int, bool, string>(resolvePromise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromiseInt;
                    }
                );
                TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromiseInt;
                    }
                );

                resolveDeferred.Resolve();
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                TestHelper.AddCallbacks<int, object, string>(rejectPromise,
                    onResolve: resolveAssert,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromise;
                    }
                );
                TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromise;
                    }
                );

                TestHelper.AddCallbacks<int, object, string>(rejectPromise,
                    onResolve: resolveAssert,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromiseInt;
                    }
                );
                TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromiseInt;
                    }
                );

                rejectDeferred.Reject("Fail outer");
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferred.Resolve();
                Promise.Manager.HandleCompletes();
                expectedCompleteCount +=
                    (TestHelper.resolveVoidPromiseVoidCallbacks +
                    TestHelper.rejectVoidPromiseVoidCallbacks +
                    (TestHelper.continueVoidPromiseVoidCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferredInt.Resolve(1);
                Promise.Manager.HandleCompletes();
                expectedCompleteCount +=
                    (TestHelper.resolveVoidPromiseConvertCallbacks +
                    TestHelper.rejectVoidPromiseConvertCallbacks +
                    (TestHelper.continueVoidPromiseConvertCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferred.Reject("Fail inner");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount +=
                    (TestHelper.resolveVoidPromiseVoidCallbacks +
                    TestHelper.rejectVoidPromiseVoidCallbacks +
                    (TestHelper.continueVoidPromiseVoidCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferredInt.Reject("Fail inner");
                Promise.Manager.HandleCompletes();
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
                var rejectWaitPromise = resolveWaitDeferredInt.Promise.Preserve();
                var resolveWaitPromiseInt = rejectWaitDeferred.Promise.Preserve();
                var rejectWaitPromiseInt = rejectWaitDeferredInt.Promise.Preserve();

                TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromise;
                    }
                );
                TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseInt,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromise;
                    }
                );
                TestHelper.AddContinueCallbacks<int, string>(resolvePromiseInt,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromise;
                    }
                );

                TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromiseInt;
                    }
                );
                TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseInt,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromiseInt;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromiseInt;
                    }
                );
                TestHelper.AddContinueCallbacks<int, string>(resolvePromiseInt,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromiseInt;
                    }
                );

                resolveDeferredInt.Resolve(1);
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseInt,
                    onResolve: _ => resolveAssert(),
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromise;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromise;
                    }
                );
                TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseInt,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromise;
                    }
                );

                TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseInt,
                    onResolve: _ => resolveAssert(),
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromiseInt;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromiseInt;
                    }
                );
                TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseInt,
                    promiseToPromise: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.ContinueWith(r => ++completeCounter).Forget();
                        return rejectWaitPromiseInt;
                    }
                );

                rejectDeferredInt.Reject("Fail outer");
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferred.Resolve();
                Promise.Manager.HandleCompletes();
                expectedCompleteCount +=
                    (TestHelper.resolveTPromiseVoidCallbacks +
                    TestHelper.rejectTPromiseVoidCallbacks +
                    (TestHelper.continueTPromiseVoidCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferredInt.Resolve(1);
                Promise.Manager.HandleCompletes();
                expectedCompleteCount +=
                    (TestHelper.resolveTPromiseConvertCallbacks +
                    TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                    (TestHelper.continueTPromiseConvertCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferred.Reject("Fail inner");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount +=
                    (TestHelper.resolveTPromiseVoidCallbacks +
                    TestHelper.rejectTPromiseVoidCallbacks +
                    (TestHelper.continueTPromiseVoidCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferredInt.Reject("Fail inner");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount +=
                    (TestHelper.resolveTPromiseConvertCallbacks +
                    TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                    (TestHelper.continueTPromiseConvertCallbacks * 2)) * 2;
                Assert.AreEqual(expectedCompleteCount, completeCounter);

                resolvePromiseInt.Forget();
                rejectPromiseInt.Forget();
                resolveWaitPromise.Forget();
                rejectWaitPromise.Forget();
                resolveWaitPromiseInt.Forget();
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
                Action<int> onResolved = v =>
                {
                    Assert.AreEqual(resolveValue, v);
                    ++resolveCounter;
                };

                var resolveWaitDeferredInt = Promise.NewDeferred<int>();
                var resolveWaitPromiseInt = resolveWaitDeferredInt.Promise.Preserve();

                // Test pending -> resolved and already resolved.
                bool firstRun = true;
            RunAgain:
                resolveCounter = 0;

                TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved).Forget();
                        return resolveWaitPromiseInt;
                    }
                );
                TestHelper.AddCallbacks<int, object, string>(resolvePromise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved).Forget();
                        return resolveWaitPromiseInt;
                    }
                );
                TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved).Forget();
                        return resolveWaitPromiseInt;
                    }
                );

                TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved).Forget();
                        return resolveWaitPromiseInt;
                    }
                );
                TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseInt,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.Then(v =>
                        {
                            Assert.AreEqual(resolveValue, v);
                            ++resolveCounter;
                        }).Forget();
                        return resolveWaitPromiseInt;
                    }
                );
                TestHelper.AddContinueCallbacks<int, string>(resolvePromiseInt,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved).Forget();
                        return resolveWaitPromiseInt;
                    }
                );


                TestHelper.AddCallbacks<int, object, string>(rejectPromise,
                    onResolve: resolveAssert,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved).Forget();
                        return resolveWaitPromiseInt;
                    }
                );
                TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved).Forget();
                        return resolveWaitPromiseInt;
                    }
                );

                TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseInt,
                    onResolve: _ => resolveAssert(),
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.Then(onResolved).Forget();
                        return resolveWaitPromiseInt;
                    }
                );
                TestHelper.AddContinueCallbacks<int, string>(rejectPromiseInt,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved).Forget();
                        return resolveWaitPromiseInt;
                    }
                );

                Assert.AreEqual(0, resolveCounter);


                if (firstRun)
                {
                    resolveWaitDeferredInt.Resolve(resolveValue);
                }

                Promise.Manager.HandleCompletes();
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
                Action<string> onRejected = rej =>
                {
                    Assert.AreEqual(rejectValue, rej);
                    ++rejectCounter;
                };

                var rejectWaitDeferred = Promise.NewDeferred();
                var rejectWaitDeferredInt = Promise.NewDeferred<int>();

                var resolveWaitPromise = rejectWaitDeferred.Promise.Preserve();
                var resolveWaitPromiseInt = rejectWaitDeferredInt.Promise.Preserve();

                // Test pending -> rejected and already rejected.
                bool firstRun = true;
            RunAgain:
                rejectCounter = 0;

                TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromiseInt;
                    }
                );
                TestHelper.AddCallbacks<int, object, string>(resolvePromise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromiseInt;
                    }
                );
                TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromiseInt;
                    }
                );

                TestHelper.AddCallbacks<int, object, string>(rejectPromise,
                    onResolve: resolveAssert,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromiseInt;
                    }
                );
                TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromiseInt;
                    }
                );

                Assert.AreEqual(0, rejectCounter);


                if (firstRun)
                {
                    rejectWaitDeferred.Reject(rejectValue);
                    rejectWaitDeferredInt.Reject(rejectValue);
                }

                Promise.Manager.HandleCompletes();
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

                resolveWaitPromise.Forget();
                resolveWaitPromiseInt.Forget();
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
                Action<string> onRejected = rej =>
                {
                    Assert.AreEqual(rejectValue, rej);
                    ++rejectCounter;
                };

                var rejectWaitDeferred = Promise.NewDeferred();
                var rejectWaitDeferredInt = Promise.NewDeferred<int>();

                var resolveWaitPromise = rejectWaitDeferred.Promise.Preserve();
                var resolveWaitPromiseInt = rejectWaitDeferredInt.Promise.Preserve();

                // Test pending -> rejected and already rejected.
                bool firstRun = true;
            RunAgain:
                rejectCounter = 0;

                TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromiseInt;
                    }
                );
                TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseInt,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromiseInt;
                    }
                );
                TestHelper.AddContinueCallbacks<int, int, string>(resolvePromiseInt,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromiseInt;
                    }
                );

                TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseInt,
                    onResolve: _ => resolveAssert(),
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromiseInt;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromiseInt;
                    }
                );
                TestHelper.AddContinueCallbacks<int, int, string>(rejectPromiseInt,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected).Forget();
                        return resolveWaitPromiseInt;
                    }
                );

                Assert.AreEqual(0, rejectCounter);


                if (firstRun)
                {
                    rejectWaitDeferred.Reject(rejectValue);
                    rejectWaitDeferredInt.Reject(rejectValue);
                }

                Promise.Manager.HandleCompletes();
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

                resolveWaitPromise.Forget();
                resolveWaitPromiseInt.Forget();
            }
        }

        // Not supported. You may alternatively "return Promise.New(deferred => {...});".
        // 2.3.3 if X is a function...

        [Test]
        public void _2_3_4_IfOnResolvedOrOnRejectedReturnsSuccessfully_ResolvePromise()
        {
            var resolveDeferred = Promise.NewDeferred();
            var rejectDeferred = Promise.NewDeferred();
            var resolveDeferredInt = Promise.NewDeferred<int>();
            var rejectDeferredInt = Promise.NewDeferred<int>();

            var resolvePromise = resolveDeferred.Promise.Preserve();
            var rejectPromise = rejectDeferred.Promise.Preserve();
            var resolvePromiseInt = resolveDeferredInt.Promise.Preserve();
            var rejectPromiseInt = rejectDeferredInt.Promise.Preserve();

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

            int resolveCounter = 0;

            Action<Promise> callback = p => p.Then(() => ++resolveCounter).Forget();

            TestHelper.AddResolveCallbacks<string, string>(resolvePromise,
                promiseToVoid: callback
            );
            TestHelper.AddCallbacks<string, object, string>(resolvePromise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToVoid: callback
            );
            TestHelper.AddContinueCallbacks<string, string>(resolvePromise,
                promiseToVoid: callback
            );

            TestHelper.AddResolveCallbacks<int, string, string>(resolvePromiseInt,
                promiseToVoid: callback
            );
            TestHelper.AddCallbacks<int, string, object, string>(resolvePromiseInt,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToVoid: callback
            );
            TestHelper.AddContinueCallbacks<int, string, string>(resolvePromiseInt,
                promiseToVoid: callback
            );

            TestHelper.AddCallbacks<string, object, string>(rejectPromise,
                onResolve: resolveAssert,
                promiseToVoid: callback
            );
            TestHelper.AddContinueCallbacks<string, string>(rejectPromise,
                promiseToVoid: callback
            );

            TestHelper.AddCallbacks<int, string, object, string>(rejectPromiseInt,
                onResolve: _ => resolveAssert(),
                promiseToVoid: callback
            );
            TestHelper.AddContinueCallbacks<int, string, string>(rejectPromiseInt,
                promiseToVoid: callback
            );

            resolveDeferred.Resolve();
            resolveDeferredInt.Resolve(1);
            rejectDeferred.Reject("Fail value");
            rejectDeferredInt.Reject("Fail value");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(
                (TestHelper.resolveVoidVoidCallbacks +
                TestHelper.resolveTVoidCallbacks +
                TestHelper.rejectVoidVoidCallbacks +
                TestHelper.rejectTVoidCallbacks +
                ((TestHelper.continueVoidVoidCallbacks + TestHelper.continueTVoidCallbacks) * 2)) * 2,
                resolveCounter
            );

            resolvePromise.Forget();
            rejectPromise.Forget();
            resolvePromiseInt.Forget();
            rejectPromiseInt.Forget();
        }

        [Test]
        public void _2_3_4_IfXIsNotAPromiseOrAFunction_FulfillPromiseWithX()
        {
            var resolveDeferred = Promise.NewDeferred();
            var rejectDeferred = Promise.NewDeferred();
            var resolveDeferredInt = Promise.NewDeferred<int>();
            var rejectDeferredInt = Promise.NewDeferred<int>();

            var resolvePromise = resolveDeferred.Promise.Preserve();
            var rejectPromise = rejectDeferred.Promise.Preserve();
            var resolvePromiseInt = resolveDeferredInt.Promise.Preserve();
            var rejectPromiseInt = rejectDeferredInt.Promise.Preserve();

            int expected = 100;

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

            int resolveCounter = 0;

            Func<Promise<int>, int> callback = p =>
            {
                p.Then(v =>
                {
                    Assert.AreEqual(expected, v);
                    ++resolveCounter;
                }).Forget();
                return expected;
            };

            TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                promiseToConvert: callback
            );
            TestHelper.AddCallbacks<int, object, string>(resolvePromise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToConvert: callback
            );
            TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                promiseToConvert: callback
            );

            TestHelper.AddResolveCallbacks<int, int, string>(resolvePromiseInt,
                promiseToConvert: callback
            );
            TestHelper.AddCallbacks<int, int, object, string>(resolvePromiseInt,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToConvert: callback
            );
            TestHelper.AddContinueCallbacks<int, string>(resolvePromiseInt,
                promiseToConvert: callback
            );

            TestHelper.AddCallbacks<int, object, string>(rejectPromise,
                onResolve: resolveAssert,
                promiseToConvert: callback
            );
            TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                promiseToConvert: callback
            );

            TestHelper.AddCallbacks<int, int, object, string>(rejectPromiseInt,
                onResolve: _ => resolveAssert(),
                promiseToConvert: callback,
                promiseToT: callback
            );
            TestHelper.AddContinueCallbacks<int, string>(rejectPromiseInt,
                promiseToConvert: callback
            );

            resolveDeferred.Resolve();
            resolveDeferredInt.Resolve(1);
            rejectDeferred.Reject("Fail value");
            rejectDeferredInt.Reject("Fail value");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(
                (TestHelper.resolveVoidConvertCallbacks +
                TestHelper.resolveTConvertCallbacks +
                TestHelper.rejectVoidConvertCallbacks +
                TestHelper.rejectTConvertCallbacks + TestHelper.rejectTTCallbacks +
                ((TestHelper.continueVoidConvertCallbacks + TestHelper.continueTConvertCallbacks) * 2)) * 2,
                resolveCounter
            );

            resolvePromise.Forget();
            rejectPromise.Forget();
            resolvePromiseInt.Forget();
            rejectPromiseInt.Forget();
        }

        // If a promise is resolved with a thenable that participates in a circular thenable chain, such that the recursive
        // nature of[[Resolve]](promise, thenable) eventually causes[[Resolve]](promise, thenable) to be
        // called again, following the above algorithm will lead to infinite recursion.Implementations are encouraged, but
        // not required, to detect such recursion and reject promise with an informative Exception as the reason.

#if PROMISE_DEBUG
        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason()
        {
            var resolveDeferred = Promise.NewDeferred();
            var rejectDeferred = Promise.NewDeferred();
            var resolveDeferredInt = Promise.NewDeferred<int>();
            var rejectDeferredInt = Promise.NewDeferred<int>();

            var resolvePromise = resolveDeferred.Promise.Preserve();
            var rejectPromise = rejectDeferred.Promise.Preserve();
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

            TestHelper.AddResolveCallbacks<int, string>(resolvePromise,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => 0); }
            );
            TestHelper.AddCallbacks<int, bool, string>(resolvePromise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => 0); }
            );
            TestHelper.AddContinueCallbacks<int, string>(resolvePromise,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => 0); }
            );

            TestHelper.AddResolveCallbacks<int, bool, string>(resolvePromiseInt,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => false); }
            );
            TestHelper.AddCallbacks<int, bool, string, string>(resolvePromiseInt,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => false); },
                promiseToPromiseT: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => 0); }
            );
            TestHelper.AddContinueCallbacks<int, bool, string>(resolvePromiseInt,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => false); }
            );

            TestHelper.AddCallbacks<int, string, string>(rejectPromise,
                onResolve: resolveAssert,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => 0); }
            );
            TestHelper.AddContinueCallbacks<int, string>(rejectPromise,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => 0); }
            );

            TestHelper.AddCallbacks<int, bool, string, string>(rejectPromiseInt,
                onResolve: _ => resolveAssert(),
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => false); },
                promiseToPromiseT: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => 0); }
            );
            TestHelper.AddContinueCallbacks<int, bool, string>(rejectPromiseInt,
                promiseToPromise: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher).Forget(); return p.ThenDuplicate().ThenDuplicate().Catch(() => false); }
            );

            resolveDeferred.Resolve();
            resolveDeferredInt.Resolve(1);
            rejectDeferred.Reject("Fail value");
            rejectDeferredInt.Reject("Fail value");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(
                (TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                (TestHelper.continueVoidPromiseVoidCallbacks + TestHelper.continueVoidPromiseConvertCallbacks +
                TestHelper.continueTPromiseVoidCallbacks + TestHelper.continueTPromiseConvertCallbacks) * 2) * 2,
                exceptionCounter
            );

            resolvePromise.Forget();
            rejectPromise.Forget();
            resolvePromiseInt.Forget();
            rejectPromiseInt.Forget();
        }
#endif
    }
}