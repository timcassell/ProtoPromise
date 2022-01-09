#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;

namespace ProtoPromiseTests.APIs
{
    public class PromiseCancelationTests
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

        public class WhenPendingAPromise
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
            public void MayTransitionToEitherTheFulfilledOrRejectedOrCanceledState_void()
            {
                string Resolved = "Resolved", Rejected = "Rejected", Canceled = "Canceled";
                string state = null;

                var deferred = Promise.NewDeferred();
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .Then(() => state = Resolved, () => state = Rejected)
                    .CatchCancelation(() => state = Canceled)
                    .Forget();
                Assert.IsNull(state);

                deferred.Resolve();
                Assert.IsFalse(deferred.IsValidAndPending);

                Assert.AreEqual(Resolved, state);

                state = null;
                deferred = Promise.NewDeferred();
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .Then(() => state = Resolved, () => state = Rejected)
                    .CatchCancelation(() => state = Canceled)
                    .Forget();
                Assert.IsNull(state);

                deferred.Reject("Fail Value");
                Assert.IsFalse(deferred.IsValidAndPending);

                state = null;
                CancelationSource cancelationSource = CancelationSource.New();
                deferred = Promise.NewDeferred(cancelationSource.Token);
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .Then(() => state = Resolved, () => state = Rejected)
                    .CatchCancelation(() => state = Canceled)
                    .Forget();
                Assert.IsNull(state);

                cancelationSource.Cancel();
                Assert.IsFalse(deferred.IsValidAndPending);

                Assert.AreEqual(Canceled, state);
                cancelationSource.Dispose();

                state = null;
                cancelationSource = CancelationSource.New();
                deferred = Promise.NewDeferred(cancelationSource.Token);
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .Then(() => state = Resolved, () => state = Rejected)
                    .CatchCancelation(() => state = Canceled)
                    .Forget();
                Assert.IsNull(state);

                cancelationSource.Cancel();
                Assert.IsFalse(deferred.IsValidAndPending);

                Assert.AreEqual(Canceled, state);
                cancelationSource.Dispose();
            }

            [Test]
            public void MayTransitionToEitherTheFulfilledOrRejectedOrCanceledState_T()
            {
                string Resolved = "Resolved", Rejected = "Rejected", Canceled = "Canceled";
                string state = null;

                var deferred = Promise.NewDeferred<int>();
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .Then(v => state = Resolved, () => state = Rejected)
                    .CatchCancelation(() => state = Canceled)
                    .Forget();
                Assert.IsNull(state);

                deferred.Resolve(1);
                Assert.IsFalse(deferred.IsValidAndPending);

                Assert.AreEqual(Resolved, state);

                state = null;
                deferred = Promise.NewDeferred<int>();
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .Then(v => state = Resolved, () => state = Rejected)
                    .CatchCancelation(() => state = Canceled)
                    .Forget();
                Assert.IsNull(state);

                deferred.Reject("Fail Value");
                Assert.IsFalse(deferred.IsValidAndPending);

                state = null;
                CancelationSource cancelationSource = CancelationSource.New();
                deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .Then(v => state = Resolved, () => state = Rejected)
                    .CatchCancelation(() => state = Canceled)
                    .Forget();
                Assert.IsNull(state);

                cancelationSource.Cancel();
                Assert.IsFalse(deferred.IsValidAndPending);

                Assert.AreEqual(Canceled, state);
                cancelationSource.Dispose();

                state = null;
                cancelationSource = CancelationSource.New();
                deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .Then(v => state = Resolved, () => state = Rejected)
                    .CatchCancelation(() => state = Canceled)
                    .Forget();
                Assert.IsNull(state);

                cancelationSource.Cancel();
                Assert.IsFalse(deferred.IsValidAndPending);

                Assert.AreEqual(Canceled, state);
                cancelationSource.Dispose();
            }
        }

        public class WhenFulfilledAPromise
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
            public void MustNotTransitionToAnyOtherState_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);

                bool resolved = false;

                deferred.Promise
                    .Then(() => { resolved = true; })
                    .Catch(() => Assert.Fail("Promise was rejected when it was already resolved."))
                    .CatchCancelation(() => Assert.Fail("Promise was canceled when it was already resolved."))
                    .Forget();

                deferred.Resolve();

                Assert.IsFalse(deferred.TryResolve());
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Resolve());
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Reject("Fail value"));

                cancelationSource.Cancel();

                Assert.IsTrue(resolved);

                cancelationSource.Dispose();
            }

            [Test]
            public void MustNotTransitionToAnyOtherState_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

                bool resolved = false;

                deferred.Promise
                    .Then(v => { resolved = true; })
                    .Catch(() => Assert.Fail("Promise was rejected when it was already resolved."))
                    .CatchCancelation(() => Assert.Fail("Promise was canceled when it was already resolved."))
                    .Forget();

                deferred.Resolve(1);

                Assert.IsFalse(deferred.TryResolve(1));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Resolve(1));
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Reject("Fail value"));

                cancelationSource.Cancel();

                Assert.IsTrue(resolved);

                cancelationSource.Dispose();
            }
        }

        public class WhenRejectedAPromise
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
            public void MustNotTransitionToAnyOtherState_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);

                bool rejected = false;

                deferred.Promise
                    .Then(() => Assert.Fail("Promise was resolved when it was already rejected."))
                    .Catch(() => { rejected = true; })
                    .CatchCancelation(() => Assert.Fail("Promise was canceled when it was already rejected."))
                    .Forget();

                deferred.Reject("Fail Value");

                Assert.IsFalse(deferred.TryResolve());
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Resolve());
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Reject("Fail value"));

                cancelationSource.Cancel();

                Assert.IsTrue(rejected);

                cancelationSource.Dispose();
            }

            [Test]
            public void MustNotTransitionToAnyOtherState_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

                bool rejected = false;

                deferred.Promise
                    .Then(() => Assert.Fail("Promise was resolved when it was already rejected."))
                    .Catch(() => { rejected = true; })
                    .CatchCancelation(() => Assert.Fail("Promise was canceled when it was already rejected."))
                    .Forget();

                deferred.Reject("Fail Value");

                Assert.IsFalse(deferred.TryResolve(1));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Resolve(1));
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Reject("Fail value"));

                cancelationSource.Cancel();

                Assert.IsTrue(rejected);

                cancelationSource.Dispose();
            }
        }

        public class WhenCanceledAPromise
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
            public void MustNotTransitionToAnyOtherState_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);

                bool canceled = false;

                deferred.Promise
                    .Then(() => Assert.Fail("Promise was resolved when it was already canceled."))
                    .Catch(() => Assert.Fail("Promise was rejected when it was already canceled."))
                    .CatchCancelation(() => { canceled = true; })
                    .Forget();

                cancelationSource.Cancel();

                Assert.IsFalse(deferred.TryResolve());
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Resolve());
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Reject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => cancelationSource.Cancel());

                Assert.IsTrue(canceled);

                cancelationSource.Dispose();
            }

            [Test]
            public void MustNotTransitionToAnyOtherState_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

                bool canceled = false;

                deferred.Promise
                    .Then(v => Assert.Fail("Promise was resolved when it was already canceled."))
                    .Catch(() => Assert.Fail("Promise was rejected when it was already canceled."))
                    .CatchCancelation(() => { canceled = true; })
                    .Forget();

                cancelationSource.Cancel();

                Assert.IsFalse(deferred.TryResolve(1));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Resolve(1));
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Reject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => cancelationSource.Cancel());

                Assert.IsTrue(canceled);

                cancelationSource.Dispose();
            }
        }

#if PROMISE_DEBUG
        [Test]
        public void IfOnCanceledIsNullThrow_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                deferred.Promise.CatchCancelation(default(Action));
            });

            cancelationSource.Cancel();
            cancelationSource.Dispose();
            deferred.Promise.Forget();
        }

        [Test]
        public void IfOnCanceledIsNullThrow_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                deferred.Promise.CatchCancelation(default(Action));
            });

            cancelationSource.Cancel();
            deferred.Promise.Forget();
            cancelationSource.Dispose();
        }
#endif

        public class IfOnCanceledIsAFunction
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
            public void ItMustBeCalledAfterPromiseIsCanceled()
            {
                var canceled = false;
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                TestHelper.AddCancelCallbacks<float>(promise,
                    onCancel: () =>
                    {
                        canceled = true;
                    }
                );
                TestHelper.AddCancelCallbacks<int, float>(promise,
                    onCancel: () =>
                    {
                        canceled = true;
                    }
                );
                cancelationSource.Cancel();

                Assert.True(canceled);

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void ItMustNotBeCalledBeforePromiseIsCanceled_void()
            {
                var canceled = false;
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);

                TestHelper.AddCancelCallbacks<float>(deferred.Promise,
                    onCancel: () => canceled = true
                );

                Assert.False(canceled);

                cancelationSource.Cancel();

                Assert.True(canceled);

                cancelationSource.Dispose();
            }

            [Test]
            public void ItMustNotBeCalledBeforePromiseIsCanceled_T()
            {
                var canceled = false;
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

                TestHelper.AddCancelCallbacks<int, float>(deferred.Promise,
                    onCancel: () => canceled = true
                );

                Assert.False(canceled);

                cancelationSource.Cancel();

                Assert.True(canceled);

                cancelationSource.Dispose();
            }

            [Test]
            public void ItMustNotBeCalledMoreThanOnce_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                var cancelCount = 0;

                TestHelper.AddCancelCallbacks<float>(deferred.Promise,
                    onCancel: () => ++cancelCount,
                    onCancelCapture: cv => ++cancelCount
                );
                cancelationSource.Cancel();

                Assert.Throws<Proto.Promises.InvalidOperationException>(() =>
                    cancelationSource.Cancel()
                );

                Assert.AreEqual(TestHelper.onCancelCallbacks * 2, cancelCount);

                cancelationSource.Dispose();
            }

            [Test]
            public void ItMustNotBeCalledMoreThanOnce_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                var cancelCount = 0;

                TestHelper.AddCancelCallbacks<int, float>(deferred.Promise,
                    onCancel: () => ++cancelCount,
                    onCancelCapture: cv => ++cancelCount
                );
                cancelationSource.Cancel();

                Assert.Throws<Proto.Promises.InvalidOperationException>(() =>
                    cancelationSource.Cancel()
                );

                Assert.AreEqual(TestHelper.onCancelCallbacks * 2, cancelCount);

                cancelationSource.Dispose();
            }
        }

        [Test]
        public void OnCanceledMustNotBeCalledUntilTheExecutionContextStackContainsOnlyPlatformCode_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            bool canceled = false;
            TestHelper.AddCancelCallbacks<float>(deferred.Promise,
                onCancel: () => canceled = true,
                configureAwaitType: ConfigureAwaitType.Foreground
            );
            cancelationSource.Cancel();
            Assert.False(canceled);

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnCanceledMustNotBeCalledUntilTheExecutionContextStackContainsOnlyPlatformCode_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            bool canceled = false;
            TestHelper.AddCancelCallbacks<int, float>(deferred.Promise,
                onCancel: () => canceled = true,
                configureAwaitType: ConfigureAwaitType.Foreground
            );
            cancelationSource.Cancel();
            Assert.False(canceled);

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(canceled);

            cancelationSource.Dispose();
        }

        public class CatchCancelationMayBeCalledMultipleTimesOnTheSamePromise
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
            public void IfWhenPromiseCancelationIsCanceled_AllRespectiveOnCanceledCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToCatchCancelation_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                int order = 0;
                int counter = 0;

                Action<int> callback = expected =>
                {
                    Assert.AreEqual(expected, order);
                    if (++counter == TestHelper.onCancelCallbacks * 2)
                    {
                        counter = 0;
                        ++order;
                    }
                };

                TestHelper.AddCancelCallbacks<float>(promise,
                    onCancel: () => callback(0),
                    onCancelCapture: cv => callback(0)
                );
                TestHelper.AddCancelCallbacks<float>(promise,
                    onCancel: () => callback(1),
                    onCancelCapture: cv => callback(1)
                );
                TestHelper.AddCancelCallbacks<float>(promise,
                    onCancel: () => callback(2),
                    onCancelCapture: cv => callback(2)
                );

                cancelationSource.Cancel();

                Assert.AreEqual(3, order);

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void IfWhenPromiseCancelationIsCanceled_AllRespectiveOnCanceledCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToCatchCancelation_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                int order = 0;
                int counter = 0;

                Action<int> callback = expected =>
                {
                    Assert.AreEqual(expected, order);
                    if (++counter == TestHelper.onCancelCallbacks * 2)
                    {
                        counter = 0;
                        ++order;
                    }
                };

                TestHelper.AddCancelCallbacks<int, float>(promise,
                    onCancel: () => callback(0),
                    onCancelCapture: cv => callback(0)
                );
                TestHelper.AddCancelCallbacks<int, float>(promise,
                    onCancel: () => callback(1),
                    onCancelCapture: cv => callback(1)
                );
                TestHelper.AddCancelCallbacks<int, float>(promise,
                    onCancel: () => callback(2),
                    onCancelCapture: cv => callback(2)
                );

                cancelationSource.Cancel();

                Assert.AreEqual(3, order);

                cancelationSource.Dispose();
                promise.Forget();
            }
        }

        [Test]
        public void IfOnCanceledThrowsAnExceptionE_Promise2MustBeRejectedWithEAsTheReason_void()
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

            TestHelper.AddCancelCallbacks<float>(deferred.Promise,
                onCancel: () => { throw expected; },
                onCancelCapture: cv => { throw expected; },
                onCallbackAdded: (ref Promise p) => catchCallback(p)
            );

            deferred.Cancel();

            Assert.AreEqual(TestHelper.onCancelCallbacks * 2, exceptionCount);
        }

        [Test]
        public void IfOnCanceledThrowsAnExceptionE_Promise2MustBeRejectedWithEAsTheReason_T()
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

            TestHelper.AddCancelCallbacks<int, float>(deferred.Promise,
                onCancel: () => { throw expected; },
                onCancelCapture: cv => { throw expected; },
                onCallbackAdded: (ref Promise<int> p) => catchCallback(p)
            );

            deferred.Cancel();

            Assert.AreEqual(TestHelper.onCancelCallbacks * 2, exceptionCount);
        }

        [Test]
        public void IfPromiseIsResolved_ReturnedPromiseMustBeResolvedWithTheSameValue_void0()
        {
            var deferred = Promise.NewDeferred();

            bool resolved = false;

            deferred.Promise
                .CatchCancelation(() => Assert.Fail("Promise was canceled when it should have been resolved."))
                .Then(() => resolved = true, () => Assert.Fail("Promise was rejected when it should have been resolved."))
                .Forget();

            deferred.Resolve();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void IfPromiseIsResolved_ReturnedPromiseMustBeResolvedWithTheSameValue_void1()
        {
            var deferred = Promise.NewDeferred();

            bool resolved = false;

            deferred.Promise
                .CatchCancelation(() => { Assert.Fail("Promise was canceled when it should have been resolved."); return Promise.Canceled(); })
                .Then(() => resolved = true, () => Assert.Fail("Promise was rejected when it should have been resolved."))
                .Forget();

            deferred.Resolve();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void IfPromiseIsResolved_ReturnedPromiseMustBeResolvedWithTheSameValue_T0()
        {
            var deferred = Promise.NewDeferred<int>();

            int expected = 10;
            int actual = -1;

            deferred.Promise
                .CatchCancelation(() => { Assert.Fail("Promise was canceled when it should have been resolved."); return -10; })
                .Then(v => { actual = v; }, () => { Assert.Fail("Promise was rejected when it should have been resolved."); })
                .Forget();

            deferred.Resolve(expected);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void IfPromiseIsResolved_ReturnedPromiseMustBeResolvedWithTheSameValue_T1()
        {
            var deferred = Promise.NewDeferred<int>();

            int expected = 10;
            int actual = -1;

            deferred.Promise
                .CatchCancelation(() => { Assert.Fail("Promise was canceled when it should have been resolved."); return Promise<int>.Canceled(); })
                .Then(v => { actual = v; }, () => { Assert.Fail("Promise was rejected when it should have been resolved."); })
                .Forget();

            deferred.Resolve(expected);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void IfPromiseIsRejected_ReturnedPromiseMustBeRejectedWithTheSameReason_void0()
        {
            var deferred = Promise.NewDeferred();

            float expected = 1.5f;
            float actual = -1f;

            deferred.Promise
                .CatchCancelation(() => Assert.Fail("Promise was canceled when it should have been rejected."))
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."), (float rejectReason) => { actual = rejectReason; })
                .Forget();

            deferred.Reject(expected);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void IfPromiseIsRejected_ReturnedPromiseMustBeRejectedWithTheSameReason_void1()
        {
            var deferred = Promise.NewDeferred();

            float expected = 1.5f;
            float actual = -1f;

            deferred.Promise
                .CatchCancelation(() => { Assert.Fail("Promise was canceled when it should have been rejected."); return Promise.Canceled(); })
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."), (float rejectReason) => { actual = rejectReason; })
                .Forget();

            deferred.Reject(expected);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void IfPromiseIsRejected_ReturnedPromiseMustBeRejectedWithTheSameReason_T0()
        {
            var deferred = Promise.NewDeferred<int>();

            float expected = 1.5f;
            float actual = -1f;

            deferred.Promise
                .CatchCancelation(() => { Assert.Fail("Promise was canceled when it should have been rejected."); return -10; })
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."), (float rejectReason) => { actual = rejectReason; })
                .Forget();

            deferred.Reject(expected);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void IfPromiseIsRejected_ReturnedPromiseMustBeRejectedWithTheSameReason_T1()
        {
            var deferred = Promise.NewDeferred<int>();

            float expected = 1.5f;
            float actual = -1f;

            deferred.Promise
                .CatchCancelation(() => { Assert.Fail("Promise was canceled when it should have been rejected."); return Promise<int>.Canceled(); })
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."), (float rejectReason) => { actual = rejectReason; })
                .Forget();

            deferred.Reject(expected);

            Assert.AreEqual(expected, actual);
        }

        public class ThePromiseResolutionProcedure
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
            public void IfPromiseAndXReferToTheSameObject_RejectPromiseWithInvalidReturnExceptionAsTheReason_void()
            {
                int exceptionCounter = 0;

                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

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

                TestHelper.AddCancelCallbacks<float>(promise,
                    promiseToPromise: p => p,
                    onCallbackAdded: catchCallback
                );
                TestHelper.AddContinueCallbacks<int, float>(promise,
                    promiseToPromise: p => p,
                    promiseToPromiseConvert: p => p,
                    onCallbackAdded: catchCallback,
                    onCallbackAddedConvert: catchCallbackConvert
                );

                deferred.Cancel();

                Assert.AreEqual(
                    (TestHelper.onCancelCallbacks * 2) + TestHelper.continueVoidPromiseVoidCallbacks + TestHelper.continueVoidPromiseConvertCallbacks,
                    exceptionCounter
                );

                promise.Forget();
            }

            [Test]
            public void IfPromiseAndXReferToTheSameObject_RejectPromiseWithInvalidReturnExceptionAsTheReason_T()
            {
                int exceptionCounter = 0;

                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

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

                TestHelper.AddCancelCallbacks<int, float>(promise,
                    promiseToPromise: p => p,
                    onCallbackAdded: catchCallbackConvert
                );
                TestHelper.AddContinueCallbacks<int, int, float>(promise,
                    promiseToPromise: p => p,
                    promiseToPromiseConvert: p => p,
                    onCallbackAdded: catchCallback,
                    onCallbackAddedConvert: catchCallbackConvert
                );

                deferred.Cancel();

                Assert.AreEqual(
                    (TestHelper.onCancelCallbacks * 2) + TestHelper.continueVoidPromiseVoidCallbacks + TestHelper.continueVoidPromiseConvertCallbacks,
                    exceptionCounter
                );

                promise.Forget();
            }
#endif

            public class IfXIsAPromiseAdoptItsState
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
                public void IfXIsPending_PromiseMustRemainPendingUntilXIsFulfilledOrRejectedOrCanceled_void()
                {
                    int expectedCompleteCount = 0;
                    int completeCounter = 0;

                    var cancelDeferred = Promise.NewDeferred();
                    var cancelPromise = cancelDeferred.Promise.Preserve();

                    var resolveWaitDeferred = Promise.NewDeferred();
                    var rejectWaitDeferred = Promise.NewDeferred();
                    var cancelWaitDeferred = Promise.NewDeferred();

                    var resolveWaitPromise = resolveWaitDeferred.Promise.Preserve();
                    var rejectWaitPromise = rejectWaitDeferred.Promise.Preserve();
                    var cancelWaitPromise = cancelWaitDeferred.Promise.Preserve();

                    TestAction<Promise> onAdoptCallbackAdded = (ref Promise p) =>
                    {
                        p = p.Finally(() => ++completeCounter)
                            .Catch(() => { });
                    };

                    TestHelper.AddCancelCallbacks<float>(cancelPromise,
                        promiseToPromise: p => resolveWaitPromise,
                        onAdoptCallbackAdded: onAdoptCallbackAdded
                    );
                    TestHelper.AddCancelCallbacks<float>(cancelPromise,
                        promiseToPromise: p => rejectWaitPromise,
                        onAdoptCallbackAdded: onAdoptCallbackAdded
                    );
                    TestHelper.AddCancelCallbacks<float>(cancelPromise,
                        promiseToPromise: p => cancelWaitPromise,
                        onAdoptCallbackAdded: onAdoptCallbackAdded
                    );

                    cancelDeferred.Cancel();
                    Assert.AreEqual(expectedCompleteCount, completeCounter);

                    resolveWaitDeferred.Resolve();
                    expectedCompleteCount += TestHelper.onCancelCallbacks;
                    Assert.AreEqual(expectedCompleteCount, completeCounter);

                    rejectWaitDeferred.Reject("Reject");
                    expectedCompleteCount += TestHelper.onCancelCallbacks;
                    Assert.AreEqual(expectedCompleteCount, completeCounter);

                    cancelWaitDeferred.Cancel();
                    expectedCompleteCount += TestHelper.onCancelCallbacks;
                    Assert.AreEqual(expectedCompleteCount, completeCounter);

                    cancelPromise.Forget();
                    resolveWaitPromise.Forget();
                    rejectWaitPromise.Forget();
                    cancelWaitPromise.Forget();
                }

                [Test]
                public void IfXIsPending_PromiseMustRemainPendingUntilXIsFulfilledOrRejectedOrCanceled_T()
                {
                    int expectedCompleteCount = 0;
                    int completeCounter = 0;

                    var cancelDeferred = Promise.NewDeferred<int>();
                    var cancelPromise = cancelDeferred.Promise.Preserve();

                    var resolveWaitDeferred = Promise.NewDeferred<int>();
                    var rejectWaitDeferred = Promise.NewDeferred<int>();
                    var cancelWaitDeferred = Promise.NewDeferred<int>();

                    var resolveWaitPromise = resolveWaitDeferred.Promise.Preserve();
                    var rejectWaitPromise = rejectWaitDeferred.Promise.Preserve();
                    var cancelWaitPromise = cancelWaitDeferred.Promise.Preserve();

                    TestAction<Promise<int>> onAdoptCallbackAdded = (ref Promise<int> p) =>
                    {
                        p = p.Finally(() => ++completeCounter)
                            .Catch(() => 1);
                    };

                    TestHelper.AddCancelCallbacks<int, float>(cancelPromise,
                        promiseToPromise: p => resolveWaitPromise,
                        onAdoptCallbackAdded: onAdoptCallbackAdded
                    );
                    TestHelper.AddCancelCallbacks<int, float>(cancelPromise,
                        promiseToPromise: p => rejectWaitPromise,
                        onAdoptCallbackAdded: onAdoptCallbackAdded
                    );
                    TestHelper.AddCancelCallbacks<int, float>(cancelPromise,
                        promiseToPromise: p => cancelWaitPromise,
                        onAdoptCallbackAdded: onAdoptCallbackAdded
                    );

                    cancelDeferred.Cancel();
                    Assert.AreEqual(expectedCompleteCount, completeCounter);

                    resolveWaitDeferred.Resolve(1);
                    expectedCompleteCount += TestHelper.onCancelCallbacks;
                    Assert.AreEqual(expectedCompleteCount, completeCounter);

                    rejectWaitDeferred.Reject("Reject");
                    expectedCompleteCount += TestHelper.onCancelCallbacks;
                    Assert.AreEqual(expectedCompleteCount, completeCounter);

                    cancelWaitDeferred.Cancel();
                    expectedCompleteCount += TestHelper.onCancelCallbacks;
                    Assert.AreEqual(expectedCompleteCount, completeCounter);

                    cancelPromise.Forget();
                    resolveWaitPromise.Forget();
                    rejectWaitPromise.Forget();
                    cancelWaitPromise.Forget();
                }

                [Test]
                public void IfWhenXIsFulfilled_FulfillPromiseWithTheSameValue_void()
                {
                    var cancelDeferred = Promise.NewDeferred();
                    cancelDeferred.Cancel();

                    var cancelPromise = cancelDeferred.Promise.Preserve();

                    int resolveCounter = 0;

                    TestAction<Promise> onAdoptCallbackAdded = (ref Promise p) =>
                    {
                        p = p.Then(() =>
                        {
                            ++resolveCounter;
                        });
                    };

                    var resolveWaitDeferred = Promise.NewDeferred();
                    var resolveWaitPromise = resolveWaitDeferred.Promise.Preserve();

                    Func<Promise, Promise> promiseToPromise = p => resolveWaitPromise;

                    // Test pending -> resolved and already resolved.
                    bool firstRun = true;
                RunAgain:
                    resolveCounter = 0;

                    TestHelper.AddCancelCallbacks<float>(cancelPromise,
                        promiseToPromise: promiseToPromise,
                        onAdoptCallbackAdded: onAdoptCallbackAdded
                    );

                    if (firstRun)
                    {
                        Assert.AreEqual(0, resolveCounter);
                        resolveWaitDeferred.Resolve();
                    }

                    Assert.AreEqual(TestHelper.onCancelCallbacks, resolveCounter);

                    if (firstRun)
                    {
                        firstRun = false;
                        goto RunAgain;
                    }

                    cancelPromise.Forget();
                    resolveWaitPromise.Forget();
                }

                [Test]
                public void IfWhenXIsFulfilled_FulfillPromiseWithTheSameValue_T()
                {
                    var cancelDeferred = Promise.NewDeferred<int>();
                    cancelDeferred.Cancel();

                    var cancelPromise = cancelDeferred.Promise.Preserve();

                    int resolveValue = 100;
                    int resolveCounter = 0;

                    TestAction<Promise<int>> onAdoptCallbackAdded = (ref Promise<int> p) =>
                    {
                        p = p.Then(v =>
                        {
                            Assert.AreEqual(resolveValue, v);
                            ++resolveCounter;
                            return v;
                        });
                    };

                    var resolveWaitDeferred = Promise.NewDeferred<int>();
                    var resolveWaitPromise = resolveWaitDeferred.Promise.Preserve();

                    Func<Promise<int>, Promise<int>> promiseToPromise = p => resolveWaitPromise;

                    // Test pending -> resolved and already resolved.
                    bool firstRun = true;
                RunAgain:
                    resolveCounter = 0;

                    TestHelper.AddCancelCallbacks<int, float>(cancelPromise,
                        promiseToPromise: promiseToPromise,
                        onAdoptCallbackAdded: onAdoptCallbackAdded
                    );

                    if (firstRun)
                    {
                        Assert.AreEqual(0, resolveCounter);
                        resolveWaitDeferred.Resolve(resolveValue);
                    }

                    Assert.AreEqual(TestHelper.onCancelCallbacks, resolveCounter);

                    if (firstRun)
                    {
                        firstRun = false;
                        goto RunAgain;
                    }

                    cancelPromise.Forget();
                    resolveWaitPromise.Forget();
                }

                [Test]
                public void IfWhenXIsRejected_RejectPromiseWithTheSameReason_void()
                {
                    var cancelDeferred = Promise.NewDeferred();
                    cancelDeferred.Cancel();

                    var cancelPromise = cancelDeferred.Promise.Preserve();

                    float rejectReason = 1.5f;
                    int rejectCounter = 0;

                    TestAction<Promise> onAdoptCallbackAdded = (ref Promise p) =>
                    {
                        p = p.Catch((float reason) =>
                        {
                            Assert.AreEqual(rejectReason, reason);
                            ++rejectCounter;
                        });
                    };

                    var rejectWaitDeferred = Promise.NewDeferred();
                    var rejectWaitPromise = rejectWaitDeferred.Promise.Preserve();

                    Func<Promise, Promise> promiseToPromise = p => rejectWaitPromise;

                    // Test pending -> rejected and already rejected.
                    bool firstRun = true;
                RunAgain:
                    rejectCounter = 0;

                    TestHelper.AddCancelCallbacks<float>(cancelPromise,
                        promiseToPromise: promiseToPromise,
                        onAdoptCallbackAdded: onAdoptCallbackAdded
                    );

                    if (firstRun)
                    {
                        Assert.AreEqual(0, rejectCounter);
                        rejectWaitDeferred.Reject(rejectReason);
                    }

                    Assert.AreEqual(TestHelper.onCancelCallbacks, rejectCounter);

                    if (firstRun)
                    {
                        firstRun = false;
                        goto RunAgain;
                    }

                    cancelPromise.Forget();
                    rejectWaitPromise.Forget();
                }

                [Test]
                public void IfWhenXIsRejected_RejectPromiseWithTheSameReason_T()
                {
                    var cancelDeferred = Promise.NewDeferred<int>();
                    cancelDeferred.Cancel();

                    var cancelPromise = cancelDeferred.Promise.Preserve();

                    float rejectReason = 1.5f;
                    int rejectCounter = 0;

                    TestAction<Promise<int>> onAdoptCallbackAdded = (ref Promise<int> p) =>
                    {
                        p = p.Catch((float reason) =>
                        {
                            Assert.AreEqual(rejectReason, reason);
                            ++rejectCounter;
                            return 1;
                        });
                    };

                    var rejectWaitDeferred = Promise.NewDeferred<int>();
                    var rejectWaitPromise = rejectWaitDeferred.Promise.Preserve();

                    Func<Promise<int>, Promise<int>> promiseToPromise = p => rejectWaitPromise;

                    // Test pending -> rejected and already rejected.
                    bool firstRun = true;
                RunAgain:
                    rejectCounter = 0;

                    TestHelper.AddCancelCallbacks<int, float>(cancelPromise,
                        promiseToPromise: promiseToPromise,
                        onAdoptCallbackAdded: onAdoptCallbackAdded
                    );

                    if (firstRun)
                    {
                        Assert.AreEqual(0, rejectCounter);
                        rejectWaitDeferred.Reject(rejectReason);
                    }

                    Assert.AreEqual(TestHelper.onCancelCallbacks, rejectCounter);

                    if (firstRun)
                    {
                        firstRun = false;
                        goto RunAgain;
                    }

                    cancelPromise.Forget();
                    rejectWaitPromise.Forget();
                }
            }

            [Test]
            public void IfOnCanceledReturnsSuccessfully_ResolvePromise_void()
            {
                var deferred = Promise.NewDeferred();

                int resolveCounter = 0;

                TestAction<Promise> onCallbackAdded = (ref Promise p) => p.Then(() => ++resolveCounter).Forget();

                TestHelper.AddCancelCallbacks<float>(deferred.Promise,
                    onCancel: () => { },
                    onCancelCapture: cv => { },
                    onCallbackAdded: onCallbackAdded
                );

                deferred.Cancel();

                Assert.AreEqual(
                    TestHelper.onCancelCallbacks * 2,
                    resolveCounter
                );
            }

            [Test]
            public void IfOnResolvedOrOnRejectedReturnsSuccessfully_ResolvePromise_T()
            {
                var deferred = Promise.NewDeferred<int>();

                int expected = 100;
                int resolveCounter = 0;

                TestAction<Promise<int>> onCallbackAdded = (ref Promise<int> p) => p.Then(v =>
                {
                    Assert.AreEqual(expected, v);
                    ++resolveCounter;
                }).Forget();

                TestHelper.AddCancelCallbacks<int, float>(deferred.Promise,
                    TValue: expected,
                    onCancel: () => { },
                    onCancelCapture: cv => { },
                    onCallbackAdded: onCallbackAdded
                );

                deferred.Cancel();

                Assert.AreEqual(
                    TestHelper.onCancelCallbacks * 2,
                    resolveCounter
                );
            }

            // If a promise is resolved with a thenable that participates in a circular thenable chain, such that the recursive
            // nature of[[Resolve]](promise, thenable) eventually causes[[Resolve]](promise, thenable) to be
            // called again, following the above algorithm will lead to infinite recursion.Implementations are encouraged, but
            // not required, to detect such recursion and reject promise with an informative Exception as the reason.

#if PROMISE_DEBUG
            [Test]
            public void IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_void()
            {
                var deferred = Promise.NewDeferred();

                int exceptionCounter = 0;

                Action<object> catcher = (object o) =>
                {
                    Assert.IsInstanceOf<InvalidReturnException>(o);
                    ++exceptionCounter;
                };

                Func<Promise, Promise> promiseToPromise = p =>
                {
                    p.Catch(catcher).Forget();
                    return p.ThenDuplicate().ThenDuplicate().Catch(() => { });
                };

                TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                {
                    var preserved = p = p.Preserve();
                    preserved
                        .Catch(() => { })
                        .Finally(() => preserved.Forget())
                        .Forget();
                };

                TestHelper.AddCancelCallbacks<float>(deferred.Promise,
                    promiseToPromise: promiseToPromise,
                    onCallbackAdded: onCallbackAdded
                );

                deferred.Cancel();

                Assert.AreEqual(TestHelper.onCancelCallbacks, exceptionCounter);
            }

            [Test]
            public void IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_T()
            {
                var deferred = Promise.NewDeferred<int>();

                int exceptionCounter = 0;

                Action<object> catcher = (object o) =>
                {
                    Assert.IsInstanceOf<InvalidReturnException>(o);
                    ++exceptionCounter;
                };

                Func<Promise<int>, Promise<int>> promiseToPromise = p =>
                {
                    p.Catch(catcher).Forget();
                    return p.ThenDuplicate().ThenDuplicate().Catch(() => 1);
                };

                TestAction<Promise<int>> onCallbackAdded = (ref Promise<int> p) =>
                {
                    var preserved = p = p.Preserve();
                    preserved
                        .Catch(() => { })
                        .Finally(() => preserved.Forget())
                        .Forget();
                };

                TestHelper.AddCancelCallbacks<int, float>(deferred.Promise,
                    promiseToPromise: promiseToPromise,
                    onCallbackAdded: onCallbackAdded
                );

                deferred.Cancel();

                Assert.AreEqual(TestHelper.onCancelCallbacks, exceptionCounter);
            }
#endif
        }

        [Test]
        public void IfPromiseIsCanceled_OnResolveAndOnRejectedMustNotBeInvoked_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been canceled.");

            TestHelper.AddCallbacks<int, object, string>(deferred.Promise,
                onResolve: () => Assert.Fail("Promise was resolved when it should have been canceled."),
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert);

            cancelationSource.Cancel();

            cancelationSource.Dispose();
        }

        [Test]
        public void IfPromiseIsCanceled_OnResolveAndOnRejectedMustNotBeInvoked_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been canceled.");

            TestHelper.AddCallbacks<int, bool, object, string>(deferred.Promise,
                onResolve: v => Assert.Fail("Promise was resolved when it should have been canceled."),
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert);

            cancelationSource.Cancel();

            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationsDoNotPropagateToRoot()
        {
            var deferred = Promise.NewDeferred();

            CancelationSource cancelationSource = CancelationSource.New();

            bool resolved = false;

            deferred.Promise
                .Then(() => resolved = true)
                .Then(_ => Assert.Fail("Promise was resolved when it should have been canceled."), cancelationSource.Token)
                .Finally(cancelationSource.Dispose)
                .Forget();

            cancelationSource.Cancel();
            deferred.Resolve();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void CancelationsPropagateToBranches()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            bool invoked = false;

            deferred.Promise
                .Then(() => { })
                .CatchCancelation(() => invoked = true)
                .Forget();

            cancelationSource.Cancel();

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
        }

        [Test]
        public void IfPromiseIsCanceledAndAPreviousPromiseIsAlsoCanceled_onCanceledMustBeCalled()
        {
            var deferred = Promise.NewDeferred();

            bool invoked = false;

            CancelationSource cancelationSource = CancelationSource.New();

            deferred.Promise
                .Then(() => { }, cancelationSource.Token)
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Finally(cancelationSource.Dispose)
                .Forget();

            cancelationSource.Cancel();
            deferred.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void APromiseMayBeCanceledWhenItIsPending()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();

            bool canceled = false;

            deferred.Promise
                .Then(() => cancelationSource.Cancel())
                .Then(() => { }, cancelationSource.Token)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."),
                    () => Assert.Fail("Promise was rejected when it should have been canceled."))
                .CatchCancelation(() => canceled = true)
                .Finally(cancelationSource.Dispose)
                .Forget();

            deferred.Resolve();
            Assert.IsTrue(canceled);
        }

        [Test]
        public void CatchCancelationCaptureValue()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            int captureValue = 100;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            promise
                .Then(() => { }, cancelationSource.Token)
                .CatchCancelation(captureValue, cv => Assert.AreEqual(captureValue, cv))
                .Forget();
            promise
                .Then(() => 1f, cancelationSource.Token)
                .CatchCancelation(captureValue, cv => Assert.AreEqual(captureValue, cv))
                .Forget();
            cancelationSource.Cancel();

            deferred.Resolve();

            cancelationSource.Dispose();
            promise.Forget();
        }

        public class CancelationToken
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
            public void DeferredIsCanceledFromAlreadyCanceledToken_0()
            {
                var canceled = false;
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred(cancelationSource.Token);

                deferred.Promise
                    .CatchCancelation(() =>
                    {
                        canceled = true;
                    })
                    .Forget();

                Assert.True(canceled);

                cancelationSource.Dispose();
            }

            [Test]
            public void DeferredIsCanceledFromAlreadyCanceledToken_1()
            {
                var canceled = false;
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred(Proto.Promises.CancelationToken.Canceled());

                deferred.Promise
                    .CatchCancelation(() =>
                    {
                        canceled = true;
                    })
                    .Forget();

                Assert.True(canceled);

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsCanceled_void0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                cancelationSource.Cancel();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsCanceled_T0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                cancelationSource.Cancel();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsCanceled_void1()
            {
                CancelationSource deferredCancelationSource = CancelationSource.New();
                CancelationSource catchCancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(deferredCancelationSource.Token);

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), catchCancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), catchCancelationSource.Token)
                    .Forget();

                catchCancelationSource.Cancel();
                deferredCancelationSource.Cancel();

                catchCancelationSource.Dispose();
                deferredCancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsCanceled_T1()
            {
                CancelationSource deferredCancelationSource = CancelationSource.New();
                CancelationSource catchCancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(deferredCancelationSource.Token);

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), catchCancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), catchCancelationSource.Token)
                    .Forget();

                catchCancelationSource.Cancel();
                deferredCancelationSource.Cancel();

                catchCancelationSource.Dispose();
                deferredCancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsAlreadyCanceled_0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                Promise.Canceled()
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();
                Promise.Canceled<int>()
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsAlreadyCanceled_1()
            {
                Promise.Canceled()
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), Proto.Promises.CancelationToken.Canceled())
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), Proto.Promises.CancelationToken.Canceled())
                    .Forget();
                Promise.Canceled<int>()
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), Proto.Promises.CancelationToken.Canceled())
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), Proto.Promises.CancelationToken.Canceled())
                    .Forget();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfPromiseIsResolved_void0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred();

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                deferred.Resolve();
                cancelationSource.Cancel();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfPromiseIsResolved_T0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>();

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                deferred.Resolve(1);
                cancelationSource.Cancel();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfPromiseIsResolved_void1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred();

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                deferred.Resolve();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfPromiseIsResolved_T1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>();

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                deferred.Resolve(1);

                cancelationSource.Dispose();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsCanceled_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                TestHelper.AddResolveCallbacksWithCancelation<float, string>(
                    promise,
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promise,
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                cancelationSource.Cancel();
                deferred.Resolve();

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsCanceled_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                TestHelper.AddResolveCallbacksWithCancelation<int, float, string>(
                    promise,
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promise,
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                cancelationSource.Cancel();
                deferred.Resolve(1);

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsAlreadyCanceled_void0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                TestHelper.AddResolveCallbacksWithCancelation<float, string>(
                    promise,
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promise,
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                deferred.Resolve();

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsAlreadyCanceled_void1()
            {
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                TestHelper.AddResolveCallbacksWithCancelation<float, string>(
                    promise,
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: Proto.Promises.CancelationToken.Canceled()
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promise,
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: Proto.Promises.CancelationToken.Canceled()
                );

                deferred.Resolve();

                promise.Forget();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsAlreadyCanceled_T0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                TestHelper.AddResolveCallbacksWithCancelation<int, float, string>(
                    promise,
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promise,
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                deferred.Resolve(1);

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsAlreadyCanceled_T1()
            {
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                TestHelper.AddResolveCallbacksWithCancelation<int, float, string>(
                    promise,
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: Proto.Promises.CancelationToken.Canceled()
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promise,
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: Proto.Promises.CancelationToken.Canceled()
                );

                deferred.Resolve(1);

                promise.Forget();
            }

            [Test]
            public void OnRejectedIsNotInvokedIfTokenIsCanceled_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();

                var deferred = Promise.NewDeferred();

                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    deferred.Promise,
                    onReject: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejection: () => Assert.Fail("OnRejected was invoked."),
                    onRejectCapture: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejectionCapture: _ => Assert.Fail("OnRejected was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                cancelationSource.Cancel();
                deferred.Reject("Reject");

                cancelationSource.Dispose();
            }

            [Test]
            public void OnRejectedIsNotInvokedIfTokenIsCanceled_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();

                var deferred = Promise.NewDeferred<int>();

                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    deferred.Promise,
                    onReject: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejection: () => Assert.Fail("OnRejected was invoked."),
                    onRejectCapture: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejectionCapture: _ => Assert.Fail("OnRejected was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                cancelationSource.Cancel();
                deferred.Reject("Reject");

                cancelationSource.Dispose();
            }

            [Test]
            public void OnRejectedIsNotInvokedIfTokenIsAlreadyCanceled_void0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred();

                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    deferred.Promise,
                    onReject: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejection: () => Assert.Fail("OnRejected was invoked."),
                    onRejectCapture: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejectionCapture: _ => Assert.Fail("OnRejected was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                deferred.Reject("Reject");

                cancelationSource.Dispose();
            }

            [Test]
            public void OnRejectedIsNotInvokedIfTokenIsAlreadyCanceled_void1()
            {
                var deferred = Promise.NewDeferred();

                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    deferred.Promise,
                    onReject: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejection: () => Assert.Fail("OnRejected was invoked."),
                    onRejectCapture: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejectionCapture: _ => Assert.Fail("OnRejected was invoked."),
                    cancelationToken: Proto.Promises.CancelationToken.Canceled()
                );

                deferred.Reject("Reject");
            }

            [Test]
            public void OnRejectedIsNotInvokedIfTokenIsAlreadyCanceled_T0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred<int>();

                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    deferred.Promise,
                    onReject: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejection: () => Assert.Fail("OnRejected was invoked."),
                    onRejectCapture: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejectionCapture: _ => Assert.Fail("OnRejected was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                deferred.Reject("Reject");

                cancelationSource.Dispose();
            }

            [Test]
            public void OnRejectedIsNotInvokedIfTokenIsAlreadyCanceled_T1()
            {
                var deferred = Promise.NewDeferred<int>();

                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    deferred.Promise,
                    onReject: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejection: () => Assert.Fail("OnRejected was invoked."),
                    onRejectCapture: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejectionCapture: _ => Assert.Fail("OnRejected was invoked."),
                    cancelationToken: Proto.Promises.CancelationToken.Canceled()
                );

                deferred.Reject("Reject");
            }

            [Test]
            public void OnContinueIsNotInvokedIfTokenIsCanceled_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();

                var deferred = Promise.NewDeferred();

                TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                    deferred.Promise,
                    onContinue: _ => Assert.Fail("OnContinue was invoked."),
                    onContinueCapture: (_, __) => Assert.Fail("OnContinue was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                cancelationSource.Cancel();
                deferred.Resolve();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnContinueIsNotInvokedIfTokenIsCanceled_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();

                var deferred = Promise.NewDeferred<int>();

                TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                    deferred.Promise,
                    onContinue: _ => Assert.Fail("OnContinue was invoked."),
                    onContinueCapture: (_, __) => Assert.Fail("OnContinue was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                cancelationSource.Cancel();
                deferred.Resolve(1);

                cancelationSource.Dispose();
            }

            [Test]
            public void OnContinueIsNotInvokedIfTokenIsAlreadyCanceled_void0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred();

                TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                    deferred.Promise,
                    onContinue: _ => Assert.Fail("OnContinue was invoked."),
                    onContinueCapture: (_, __) => Assert.Fail("OnContinue was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                deferred.Resolve();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnContinueIsNotInvokedIfTokenIsAlreadyCanceled_void1()
            {
                var deferred = Promise.NewDeferred();

                TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                    deferred.Promise,
                    onContinue: _ => Assert.Fail("OnContinue was invoked."),
                    onContinueCapture: (_, __) => Assert.Fail("OnContinue was invoked."),
                    cancelationToken: Proto.Promises.CancelationToken.Canceled()
                );

                deferred.Resolve();
            }

            [Test]
            public void OnContinueIsNotInvokedIfTokenIsAlreadyCanceled_T0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred<int>();

                TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                    deferred.Promise,
                    onContinue: _ => Assert.Fail("OnContinue was invoked."),
                    onContinueCapture: (_, __) => Assert.Fail("OnContinue was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                deferred.Resolve(1);

                cancelationSource.Dispose();
            }

            [Test]
            public void OnContinueIsNotInvokedIfTokenIsAlreadyCanceled_T1()
            {
                var deferred = Promise.NewDeferred<int>();

                TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                    deferred.Promise,
                    onContinue: _ => Assert.Fail("OnContinue was invoked."),
                    onContinueCapture: (_, __) => Assert.Fail("OnContinue was invoked."),
                    cancelationToken: Proto.Promises.CancelationToken.Canceled()
                );

                deferred.Resolve(1);
            }

            [Test]
            public void PromiseIsCanceledFromToken_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();

                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                int cancelCallbacks = 0;

                TestHelper.AddResolveCallbacksWithCancelation<float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );

                cancelationSource.Cancel();
                deferred.Resolve();

                Assert.AreEqual(
                    TestHelper.cancelVoidCallbacks,
                    cancelCallbacks
                );

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void PromiseIsCanceledFromToken_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();

                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                int cancelCallbacks = 0;

                TestHelper.AddResolveCallbacksWithCancelation<int, float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );

                cancelationSource.Cancel();
                deferred.Resolve(1);

                Assert.AreEqual(
                    TestHelper.cancelTCallbacks,
                    cancelCallbacks
                );

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void PromiseIsCanceledFromAlreadyCanceledToken_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                int cancelCallbacks = 0;

                TestHelper.AddResolveCallbacksWithCancelation<float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );

                deferred.Resolve();

                Assert.AreEqual(
                    TestHelper.cancelVoidCallbacks,
                    cancelCallbacks
                );

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void PromiseIsCanceledFromAlreadyCanceledToken_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                int cancelCallbacks = 0;

                TestHelper.AddResolveCallbacksWithCancelation<int, float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );

                deferred.Resolve(1);

                Assert.AreEqual(
                    TestHelper.cancelTCallbacks,
                    cancelCallbacks
                );

                cancelationSource.Dispose();
                promise.Forget();
            }
        }
    }
}