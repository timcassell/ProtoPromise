#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class APlus_2_3_ThePromiseResolutionProcedure
    {
#if PROMISE_DEBUG
        [Test]
        public void _2_3_1_IfPromiseAndXReferToTheSameObjectRejectPromiseWithInvalidReturnExceptionAsTheReason()
        {
            var resolveDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, resolveDeferred.State);
            var rejectDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, rejectDeferred.State);
            var resolveDeferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, resolveDeferredInt.State);
            var rejectDeferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, rejectDeferredInt.State);

            int exceptionCounter = 0;

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");
            Action<object> catcher = (object o) =>
            {
                Assert.IsInstanceOf<InvalidReturnException>(o);
                ++exceptionCounter;
            };

            TestHelper.AddResolveCallbacks<int, string>(resolveDeferred.Promise,
                promiseToPromise: p => { p.Catch(catcher); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p; }
            );
            TestHelper.AddCallbacks<int, bool, string>(resolveDeferred.Promise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: p => { p.Catch(catcher); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p; }
            );
            TestHelper.AddCompleteCallbacks<int, string>(resolveDeferred.Promise,
                promiseToPromise: p => { p.Catch(catcher); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p; }
            );

            TestHelper.AddResolveCallbacks<int, bool, string>(resolveDeferredInt.Promise,
                promiseToPromise: p => { p.Catch(catcher); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p; }
            );
            TestHelper.AddCallbacks<int, bool, string, string>(resolveDeferredInt.Promise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: p => { p.Catch(catcher); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p; },
                promiseToPromiseT: p => { p.Catch(catcher); return p; }
            );
            TestHelper.AddCompleteCallbacks<bool, string>(resolveDeferredInt.Promise,
                promiseToPromise: p => { p.Catch(catcher); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p; }
            );

            TestHelper.AddCallbacks<int, string, string>(rejectDeferred.Promise,
                onResolve: resolveAssert,
                promiseToPromise: p => { p.Catch(catcher); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p; }
            );
            TestHelper.AddCompleteCallbacks<int, string>(rejectDeferred.Promise,
                promiseToPromise: p => { p.Catch(catcher); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p; }
            );

            TestHelper.AddCallbacks<int, bool, string, string>(rejectDeferredInt.Promise,
                onResolve: _ => resolveAssert(),
                promiseToPromise: p => { p.Catch(catcher); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p; },
                promiseToPromiseT: p => { p.Catch(catcher); return p; }
            );
            TestHelper.AddCompleteCallbacks<bool, string>(rejectDeferredInt.Promise,
                promiseToPromise: p => { p.Catch(catcher); return p; },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p; }
            );

            resolveDeferred.Resolve();
            resolveDeferredInt.Resolve(0);
            rejectDeferred.Reject("Fail value");
            rejectDeferredInt.Reject("Fail value");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(
                TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                (TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks) * 4,
                exceptionCounter);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }
#endif

        public class _2_3_2_IfXIsAPromiseAdoptItsState
        {
            [Test]
            public void _2_3_2_1_IfXIsPendingPromiseMustRemainPendingUntilXIsFulfilledOrRejected()
            {
                int expectedCompleteCount = 0;
                int completeCounter = 0;

                var resolveDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, resolveDeferred.State);
                var rejectDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, rejectDeferred.State);
                var resolveDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, resolveDeferredInt.State);
                var rejectDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, rejectDeferredInt.State);

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                var resolveWaitDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, resolveWaitDeferred.State);
                var resolveWaitDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, resolveWaitDeferredInt.State);
                var rejectWaitDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, rejectWaitDeferred.State);
                var rejectWaitDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, rejectWaitDeferredInt.State);

                TestHelper.AddResolveCallbacks<int, string>(resolveDeferred.Promise,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCallbacks<int, object, string>(resolveDeferred.Promise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(resolveDeferred.Promise,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferredInt.Promise;
                    }
                );

                TestHelper.AddResolveCallbacks<int, string>(resolveDeferred.Promise,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCallbacks<int, bool, string>(resolveDeferred.Promise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(resolveDeferred.Promise,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferredInt.Promise;
                    }
                );

                resolveDeferred.Resolve();
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                TestHelper.AddResolveCallbacks<int, int, string>(resolveDeferredInt.Promise,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCallbacks<int, int, object, string>(resolveDeferredInt.Promise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(resolveDeferredInt.Promise,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferredInt.Promise;
                    }
                );

                TestHelper.AddResolveCallbacks<int, int, string>(resolveDeferredInt.Promise,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCallbacks<int, int, object, string>(resolveDeferredInt.Promise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferredInt.Promise;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(resolveDeferredInt.Promise,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferredInt.Promise;
                    }
                );

                resolveDeferredInt.Resolve(0);
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                TestHelper.AddCallbacks<int, object, string>(rejectDeferred.Promise,
                    onResolve: resolveAssert,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(rejectDeferred.Promise,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferredInt.Promise;
                    }
                );

                TestHelper.AddCallbacks<int, object, string>(rejectDeferred.Promise,
                    onResolve: resolveAssert,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(rejectDeferred.Promise,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferredInt.Promise;
                    }
                );

                rejectDeferred.Reject("Fail outer");
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                TestHelper.AddCallbacks<int, int, object, string>(rejectDeferredInt.Promise,
                    onResolve: _ => resolveAssert(),
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferredInt.Promise;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(rejectDeferredInt.Promise,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return resolveWaitDeferredInt.Promise;
                    }
                );

                TestHelper.AddCallbacks<int, int, object, string>(rejectDeferredInt.Promise,
                    onResolve: _ => resolveAssert(),
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferredInt.Promise;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(rejectDeferredInt.Promise,
                    promiseToPromise: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Complete(() => ++completeCounter);
                        return rejectWaitDeferredInt.Promise;
                    }
                );

                rejectDeferredInt.Reject("Fail outer");
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferred.Resolve();
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveTPromiseVoidCallbacks +
                    TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectTPromiseVoidCallbacks +
                    (TestHelper.completePromiseVoidCallbacks * 4);
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                resolveWaitDeferredInt.Resolve(0);
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += TestHelper.resolveVoidPromiseConvertCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                    TestHelper.rejectVoidPromiseConvertCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                    (TestHelper.completePromiseConvertCallbacks * 4);
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferred.Reject("Fail inner");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveTPromiseVoidCallbacks +
                    TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectTPromiseVoidCallbacks +
                    (TestHelper.completePromiseVoidCallbacks * 4);
                Assert.AreEqual(expectedCompleteCount, completeCounter);


                rejectWaitDeferredInt.Reject("Fail inner");
                Promise.Manager.HandleCompletes();
                expectedCompleteCount += TestHelper.resolveVoidPromiseConvertCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                    TestHelper.rejectVoidPromiseConvertCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                    (TestHelper.completePromiseConvertCallbacks * 4);
                Assert.AreEqual(expectedCompleteCount, completeCounter);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void _2_3_2_2_IfWhenXIsFulfilledFulfillPromiseWithTheSameValue()
            {
                var resolveDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, resolveDeferred.State);
                resolveDeferred.Retain();
                resolveDeferred.Resolve();
                Assert.AreEqual(Promise.State.Resolved, resolveDeferred.State);

                var rejectDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, rejectDeferred.State);
                rejectDeferred.Retain();
                rejectDeferred.Reject("Fail value");
                Assert.AreEqual(Promise.State.Rejected, rejectDeferred.State);

                var resolveDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, resolveDeferredInt.State);
                resolveDeferredInt.Retain();
                resolveDeferredInt.Resolve(0);
                Assert.AreEqual(Promise.State.Resolved, resolveDeferredInt.State);

                var rejectDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, rejectDeferredInt.State);
                rejectDeferredInt.Retain();
                rejectDeferredInt.Reject("Fail value");
                Assert.AreEqual(Promise.State.Rejected, rejectDeferredInt.State);

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
                Assert.AreEqual(Promise.State.Pending, resolveWaitDeferredInt.State);
                resolveWaitDeferredInt.Retain();

                // Test pending -> resolved and already resolved.
                bool firstRun = true;
            RunAgain:
                resolveCounter = 0;

                TestHelper.AddResolveCallbacks<int, string>(resolveDeferred.Promise,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved);
                        return resolveWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCallbacks<int, object, string>(resolveDeferred.Promise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved);
                        return resolveWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(resolveDeferred.Promise,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved);
                        return resolveWaitDeferredInt.Promise;
                    }
                );

                TestHelper.AddResolveCallbacks<int, int, string>(resolveDeferredInt.Promise,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved);
                        return resolveWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCallbacks<int, int, object, string>(resolveDeferredInt.Promise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved);
                        return resolveWaitDeferredInt.Promise;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.Then(v =>
                        {
                            Assert.AreEqual(resolveValue, v);
                            ++resolveCounter;
                        });
                        return resolveWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(resolveDeferredInt.Promise,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved);
                        return resolveWaitDeferredInt.Promise;
                    }
                );


                TestHelper.AddCallbacks<int, object, string>(rejectDeferred.Promise,
                    onResolve: resolveAssert,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved);
                        return resolveWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(rejectDeferred.Promise,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved);
                        return resolveWaitDeferredInt.Promise;
                    }
                );

                TestHelper.AddCallbacks<int, int, object, string>(rejectDeferredInt.Promise,
                    onResolve: _ => resolveAssert(),
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved);
                        return resolveWaitDeferredInt.Promise;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.Then(onResolved);
                        return resolveWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(rejectDeferredInt.Promise,
                    promiseToPromiseConvert: p =>
                    {
                        p.Then(onResolved);
                        return resolveWaitDeferredInt.Promise;
                    }
                );

                Assert.AreEqual(0, resolveCounter);


                if (firstRun)
                {
                    resolveWaitDeferredInt.Resolve(resolveValue);
                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(TestHelper.resolveVoidPromiseConvertCallbacks + TestHelper.resolveTPromiseConvertCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                        TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks + (TestHelper.completePromiseConvertCallbacks * 4),
                        resolveCounter);
                    firstRun = false;
                    goto RunAgain;
                }

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(TestHelper.resolveVoidPromiseConvertCallbacks + TestHelper.resolveTPromiseConvertCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                    TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks + (TestHelper.completePromiseConvertCallbacks * 4),
                    resolveCounter);

                resolveDeferred.Release();
                resolveDeferredInt.Release();
                rejectDeferred.Release();
                rejectDeferredInt.Release();

                resolveWaitDeferredInt.Release();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void _2_3_2_3_IfWhenXIsRejectedRejectPromiseWithTheSameReason()
            {
                var resolveDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, resolveDeferred.State);
                resolveDeferred.Retain();
                resolveDeferred.Resolve();
                Assert.AreEqual(Promise.State.Resolved, resolveDeferred.State);

                var rejectDeferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, rejectDeferred.State);
                rejectDeferred.Retain();
                rejectDeferred.Reject("Fail value");
                Assert.AreEqual(Promise.State.Rejected, rejectDeferred.State);

                var resolveDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, resolveDeferredInt.State);
                resolveDeferredInt.Retain();
                resolveDeferredInt.Resolve(0);
                Assert.AreEqual(Promise.State.Resolved, resolveDeferredInt.State);

                var rejectDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, rejectDeferredInt.State);
                rejectDeferredInt.Retain();
                rejectDeferredInt.Reject("Fail value");
                Assert.AreEqual(Promise.State.Rejected, rejectDeferredInt.State);

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
                Assert.AreEqual(Promise.State.Pending, rejectWaitDeferred.State);
                rejectWaitDeferred.Retain();
                var rejectWaitDeferredInt = Promise.NewDeferred<int>();
                Assert.AreEqual(Promise.State.Pending, rejectWaitDeferredInt.State);
                rejectWaitDeferredInt.Retain();

                // Test pending -> rejected and already rejected.
                bool firstRun = true;
            RunAgain:
                rejectCounter = 0;

                TestHelper.AddResolveCallbacks<int, string>(resolveDeferred.Promise,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCallbacks<int, object, string>(resolveDeferred.Promise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(resolveDeferred.Promise,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferredInt.Promise;
                    }
                );

                TestHelper.AddResolveCallbacks<int, int, string>(resolveDeferredInt.Promise,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCallbacks<int, int, object, string>(resolveDeferredInt.Promise,
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferredInt.Promise;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(resolveDeferredInt.Promise,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferredInt.Promise;
                    }
                );


                TestHelper.AddCallbacks<int, object, string>(rejectDeferred.Promise,
                    onResolve: resolveAssert,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(rejectDeferred.Promise,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferredInt.Promise;
                    }
                );

                TestHelper.AddCallbacks<int, int, object, string>(rejectDeferredInt.Promise,
                    onResolve: _ => resolveAssert(),
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferredInt.Promise;
                    },
                    promiseToPromiseT: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferredInt.Promise;
                    }
                );
                TestHelper.AddCompleteCallbacks<int, string>(rejectDeferredInt.Promise,
                    promiseToPromise: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferred.Promise;
                    },
                    promiseToPromiseConvert: p =>
                    {
                        p.Catch(onRejected);
                        return rejectWaitDeferredInt.Promise;
                    }
                );

                Assert.AreEqual(0, rejectCounter);


                if (firstRun)
                {
                    rejectWaitDeferred.Reject(rejectValue);
                    rejectWaitDeferredInt.Reject(rejectValue);
                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                        TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                        TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                        TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks +
                        TestHelper.rejectTPromiseTCallbacks + ((TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks) * 4),
                        rejectCounter);
                    firstRun = false;
                    goto RunAgain;
                }

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                    TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                    TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                    TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks +
                    TestHelper.rejectTPromiseTCallbacks + ((TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks) * 4),
                    rejectCounter);

                resolveDeferred.Release();
                resolveDeferredInt.Release();
                rejectDeferred.Release();
                rejectDeferredInt.Release();

                rejectWaitDeferred.Release();
                rejectWaitDeferredInt.Release();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        // Not supported. You may alternatively "return Promise.New(deferred => {...});".
        // 2.3.3 if X is a function...

        [Test]
        public void _2_3_4_IfOnResolvedOrOnRejectedReturnsSuccessfullyResolvePromise()
        {
            var resolveDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, resolveDeferred.State);
            var rejectDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, rejectDeferred.State);
            var resolveDeferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, resolveDeferredInt.State);
            var rejectDeferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, rejectDeferredInt.State);

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

            int resolveCounter = 0;

            Action<Promise> callback = p => p.Then(() => ++resolveCounter);

            TestHelper.AddResolveCallbacks<string, string>(resolveDeferred.Promise,
                promiseToVoid: callback
            );
            TestHelper.AddCallbacks<string, object, string>(resolveDeferred.Promise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToVoid: callback
            );
            TestHelper.AddCompleteCallbacks<string, string>(resolveDeferred.Promise,
                promiseToVoid: callback
            );

            TestHelper.AddResolveCallbacks<int, string, string>(resolveDeferredInt.Promise,
                promiseToVoid: callback
            );
            TestHelper.AddCallbacks<int, string, object, string>(resolveDeferredInt.Promise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToVoid: callback
            );
            TestHelper.AddCompleteCallbacks<string, string>(resolveDeferredInt.Promise,
                promiseToVoid: callback
            );

            TestHelper.AddCallbacks<string, object, string>(rejectDeferred.Promise,
                onResolve: resolveAssert,
                promiseToVoid: callback
            );
            TestHelper.AddCompleteCallbacks<string, string>(rejectDeferred.Promise,
                promiseToVoid: callback
            );

            TestHelper.AddCallbacks<int, string, object, string>(rejectDeferredInt.Promise,
                onResolve: _ => resolveAssert(),
                promiseToVoid: callback
            );
            TestHelper.AddCompleteCallbacks<string, string>(rejectDeferredInt.Promise,
                promiseToVoid: callback
            );

            resolveDeferred.Resolve();
            resolveDeferredInt.Resolve(0);
            rejectDeferred.Reject("Fail value");
            rejectDeferredInt.Reject("Fail value");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.resolveVoidVoidCallbacks +
                TestHelper.resolveTVoidCallbacks +
                TestHelper.rejectVoidVoidCallbacks +
                TestHelper.rejectTVoidCallbacks +
                (TestHelper.completeVoidCallbacks * 4),
                resolveCounter);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void _2_3_4_IfXIsNotAPromiseOrAFunctionFulfillPromiseWithX()
        {
            var resolveDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, resolveDeferred.State);
            var rejectDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, rejectDeferred.State);
            var resolveDeferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, resolveDeferredInt.State);
            var rejectDeferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, rejectDeferredInt.State);

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
                });
                return expected;
            };

            TestHelper.AddResolveCallbacks<int, string>(resolveDeferred.Promise,
                promiseToConvert: callback
            );
            TestHelper.AddCallbacks<int, object, string>(resolveDeferred.Promise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToConvert: callback
            );
            TestHelper.AddCompleteCallbacks<int, string>(resolveDeferred.Promise,
                promiseToConvert: callback
            );

            TestHelper.AddResolveCallbacks<int, int, string>(resolveDeferredInt.Promise,
                promiseToConvert: callback
            );
            TestHelper.AddCallbacks<int, int, object, string>(resolveDeferredInt.Promise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToConvert: callback
            );
            TestHelper.AddCompleteCallbacks<int, string>(resolveDeferredInt.Promise,
                promiseToConvert: callback
            );

            TestHelper.AddCallbacks<int, object, string>(rejectDeferred.Promise,
                onResolve: resolveAssert,
                promiseToConvert: callback
            );
            TestHelper.AddCompleteCallbacks<int, string>(rejectDeferred.Promise,
                promiseToConvert: callback
            );

            TestHelper.AddCallbacks<int, int, object, string>(rejectDeferredInt.Promise,
                onResolve: _ => resolveAssert(),
                promiseToConvert: callback,
                promiseToT: callback
            );
            TestHelper.AddCompleteCallbacks<int, string>(rejectDeferredInt.Promise,
                promiseToConvert: callback
            );

            resolveDeferred.Resolve();
            resolveDeferredInt.Resolve(0);
            rejectDeferred.Reject("Fail value");
            rejectDeferredInt.Reject("Fail value");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.resolveVoidConvertCallbacks +
                TestHelper.resolveTConvertCallbacks +
                TestHelper.rejectVoidConvertCallbacks +
                TestHelper.rejectTConvertCallbacks + TestHelper.rejectTTCallbacks +
                (TestHelper.completeConvertCallbacks * 4),
                resolveCounter);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        // If a promise is resolved with a thenable that participates in a circular thenable chain, such that the recursive
        // nature of[[Resolve]](promise, thenable) eventually causes[[Resolve]](promise, thenable) to be
        // called again, following the above algorithm will lead to infinite recursion.Implementations are encouraged, but
        // not required, to detect such recursion and reject promise with an informative Exception as the reason.

#if PROMISE_DEBUG
        [Test]
        public void _2_3_5_IfXIsAPromiseAndItResultsInACircularPromiseChainRejectPromiseWithInvalidReturnExceptionAsTheReason()
        {
            var resolveDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, resolveDeferred.State);
            var rejectDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, rejectDeferred.State);
            var resolveDeferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, resolveDeferredInt.State);
            var rejectDeferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, rejectDeferredInt.State);

            int exceptionCounter = 0;

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");
            Action<object> catcher = (object o) =>
            {
                Assert.IsInstanceOf<InvalidReturnException>(o);
                ++exceptionCounter;
            };

            TestHelper.AddResolveCallbacks<int, string>(resolveDeferred.Promise,
                promiseToPromise: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => 0); }
            );
            TestHelper.AddCallbacks<int, bool, string>(resolveDeferred.Promise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => 0); }
            );
            TestHelper.AddCompleteCallbacks<int, string>(resolveDeferred.Promise,
                promiseToPromise: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => 0); }
            );

            TestHelper.AddResolveCallbacks<int, bool, string>(resolveDeferredInt.Promise,
                promiseToPromise: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => false); }
            );
            TestHelper.AddCallbacks<int, bool, string, string>(resolveDeferredInt.Promise,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert,
                promiseToPromise: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => false); },
                promiseToPromiseT: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => 0); }
            );
            TestHelper.AddCompleteCallbacks<bool, string>(resolveDeferredInt.Promise,
                promiseToPromise: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => false); }
            );

            TestHelper.AddCallbacks<int, string, string>(rejectDeferred.Promise,
                onResolve: resolveAssert,
                promiseToPromise: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => 0); }
            );
            TestHelper.AddCompleteCallbacks<int, string>(rejectDeferred.Promise,
                promiseToPromise: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => 0); }
            );

            TestHelper.AddCallbacks<int, bool, string, string>(rejectDeferredInt.Promise,
                onResolve: _ => resolveAssert(),
                promiseToPromise: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => false); },
                promiseToPromiseT: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => 0); }
            );
            TestHelper.AddCompleteCallbacks<bool, string>(rejectDeferredInt.Promise,
                promiseToPromise: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => { }); },
                promiseToPromiseConvert: p => { p.Catch(catcher); return p.ThenDuplicate().ThenDuplicate().Catch(() => false); }
            );

            resolveDeferred.Resolve();
            resolveDeferredInt.Resolve(0);
            rejectDeferred.Reject("Fail value");
            rejectDeferredInt.Reject("Fail value");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(
                TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks + TestHelper.rejectTPromiseTCallbacks +
                (TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks) * 4,
                exceptionCounter);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }
#endif
    }
}