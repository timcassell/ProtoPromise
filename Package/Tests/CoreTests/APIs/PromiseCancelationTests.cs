#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Collections.Generic;

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
                deferred = Promise.NewDeferred();
                cancelationSource.Token.Register(deferred);
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
                deferred = Promise.NewDeferred();
                cancelationSource.Token.Register(deferred);
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
                deferred = Promise.NewDeferred<int>();
                cancelationSource.Token.Register(deferred);
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
                deferred = Promise.NewDeferred<int>();
                cancelationSource.Token.Register(deferred);
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
                var deferred = Promise.NewDeferred();

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
                Assert.IsFalse(deferred.TryCancel());
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Cancel());

                Assert.IsTrue(resolved);
            }

            [Test]
            public void MustNotTransitionToAnyOtherState_T()
            {
                var deferred = Promise.NewDeferred<int>();

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
                Assert.IsFalse(deferred.TryCancel());
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Cancel());

                Assert.IsTrue(resolved);
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
                var deferred = Promise.NewDeferred();

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
                Assert.IsFalse(deferred.TryCancel());
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Cancel());

                Assert.IsTrue(rejected);
            }

            [Test]
            public void MustNotTransitionToAnyOtherState_T()
            {
                var deferred = Promise.NewDeferred<int>();

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
                Assert.IsFalse(deferred.TryCancel());
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Cancel());

                Assert.IsTrue(rejected);
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
                var deferred = Promise.NewDeferred();
                cancelationSource.Token.Register(deferred);

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
                var deferred = Promise.NewDeferred<int>();
                cancelationSource.Token.Register(deferred);

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
            var deferred = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred);

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
            var deferred = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred);

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
                var deferred = Promise.NewDeferred<int>();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    TestHelper.AddCancelCallbacks<float>(promiseRetainer.WaitAsync(),
                        onCancel: () =>
                        {
                            canceled = true;
                        }
                    );
                    TestHelper.AddCancelCallbacks<int, float>(promiseRetainer.WaitAsync(),
                        onCancel: () =>
                        {
                            canceled = true;
                        }
                    );

                    deferred.Cancel();
                    Assert.True(canceled);
                }
            }

            [Test]
            public void ItMustNotBeCalledBeforePromiseIsCanceled_void()
            {
                var canceled = false;
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred();
                cancelationSource.Token.Register(deferred);

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
                var deferred = Promise.NewDeferred<int>();
                cancelationSource.Token.Register(deferred);

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
                var deferred = Promise.NewDeferred();
                cancelationSource.Token.Register(deferred);
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
                var deferred = Promise.NewDeferred<int>();
                cancelationSource.Token.Register(deferred);
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
            var deferred = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred);

            bool canceled = false;
            TestHelper.AddCancelCallbacks<float>(deferred.Promise,
                onCancel: () => canceled = true,
                continuationOptions: new ContinuationOptions(SynchronizationOption.Foreground, forceAsync: true)
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
            var deferred = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred);

            bool canceled = false;
            TestHelper.AddCancelCallbacks<int, float>(deferred.Promise,
                onCancel: () => canceled = true,
                continuationOptions: new ContinuationOptions(SynchronizationOption.Foreground, forceAsync: true)
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
            public void IfWhenPromiseCanceled_AllRespectiveOnCanceledCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToCatchCancelation_void()
            {
                var deferred = Promise.NewDeferred();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    int counter = 0;

                    for (int i = 0; i < 10; ++i)
                    {
                        int index = i;
                        promiseRetainer.WaitAsync()
                            .CatchCancelation(() => Assert.AreEqual(index, counter++))
                            .Forget();
                    }

                    deferred.Cancel();
                    Assert.AreEqual(10, counter);
                }
            }

            [Test]
            public void IfWhenPromiseIsCanceled_AllRespectiveOnCanceledCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToCatchCancelation_T()
            {
                var deferred = Promise.NewDeferred<int>();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    int counter = 0;

                    for (int i = 0; i < 10; ++i)
                    {
                        int index = i;
                        promiseRetainer.WaitAsync()
                            .CatchCancelation(() => Assert.AreEqual(index, counter++))
                            .Forget();
                    }

                    deferred.Cancel();
                    Assert.AreEqual(10, counter);
                }
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

                var deferred = Promise.NewDeferred();
                var promiseQueue = new Queue<Promise>();

                TestHelper.AddCancelCallbacks<float>(deferred.Promise,
                    onAdoptCallbackAdded: (ref Promise p) =>
                    {
                        promiseQueue.Enqueue(p);
                        p = Promise.Resolved();
                    },
                    promiseToPromise: p => promiseQueue.Dequeue()
                );

                deferred.Cancel();

                Assert.AreEqual(
                    TestHelper.onCancelPromiseCallbacks * 2,
                    exceptionCounter
                );

                Promise.Config.UncaughtRejectionHandler = currentHandler;
            }

            [Test]
            public void IfPromiseAndXReferToTheSameObject_RejectPromiseWithInvalidReturnExceptionAsTheReason_T()
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

                var deferred = Promise.NewDeferred<int>();
                var promiseQueue = new Queue<Promise<int>>();

                TestHelper.AddCancelCallbacks<int, float>(deferred.Promise,
                    onAdoptCallbackAdded: (ref Promise<int> p) =>
                    {
                        promiseQueue.Enqueue(p);
                        p = Promise.Resolved(1);
                    },
                    promiseToPromise: p => promiseQueue.Dequeue()
                );

                deferred.Cancel();

                Assert.AreEqual(
                    TestHelper.onCancelPromiseCallbacks * 2,
                    exceptionCounter
                );

                Promise.Config.UncaughtRejectionHandler = currentHandler;
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
                    var cancelPromiseRetainer = cancelDeferred.Promise.GetRetainer();

                    var resolveWaitDeferred = Promise.NewDeferred();
                    var rejectWaitDeferred = Promise.NewDeferred();
                    var cancelWaitDeferred = Promise.NewDeferred();

                    var resolveWaitPromiseRetainer = resolveWaitDeferred.Promise.GetRetainer();
                    var rejectWaitPromiseRetainer = rejectWaitDeferred.Promise.GetRetainer();
                    var cancelWaitPromiseRetainer = cancelWaitDeferred.Promise.GetRetainer();

                    TestAction<Promise> onAdoptCallbackAdded = (ref Promise p) =>
                    {
                        p = p.Finally(() => ++completeCounter)
                            .Catch(() => { });
                    };

                    TestHelper.AddCancelCallbacks<float>(cancelPromiseRetainer.WaitAsync(),
                        promiseToPromise: p => resolveWaitPromiseRetainer.WaitAsync(),
                        onAdoptCallbackAdded: onAdoptCallbackAdded
                    );
                    TestHelper.AddCancelCallbacks<float>(cancelPromiseRetainer.WaitAsync(),
                        promiseToPromise: p => rejectWaitPromiseRetainer.WaitAsync(),
                        onAdoptCallbackAdded: onAdoptCallbackAdded
                    );
                    TestHelper.AddCancelCallbacks<float>(cancelPromiseRetainer.WaitAsync(),
                        promiseToPromise: p => cancelWaitPromiseRetainer.WaitAsync(),
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

                    cancelPromiseRetainer.Dispose();
                    resolveWaitPromiseRetainer.Dispose();
                    rejectWaitPromiseRetainer.Dispose();
                    cancelWaitPromiseRetainer.Dispose();
                }

                [Test]
                public void IfXIsPending_PromiseMustRemainPendingUntilXIsFulfilledOrRejectedOrCanceled_T()
                {
                    int expectedCompleteCount = 0;
                    int completeCounter = 0;

                    var cancelDeferred = Promise.NewDeferred<int>();
                    var cancelPromiseRetainer = cancelDeferred.Promise.GetRetainer();

                    var resolveWaitDeferred = Promise.NewDeferred<int>();
                    var rejectWaitDeferred = Promise.NewDeferred<int>();
                    var cancelWaitDeferred = Promise.NewDeferred<int>();

                    var resolveWaitPromiseRetainer = resolveWaitDeferred.Promise.GetRetainer();
                    var rejectWaitPromiseRetainer = rejectWaitDeferred.Promise.GetRetainer();
                    var cancelWaitPromiseRetainer = cancelWaitDeferred.Promise.GetRetainer();

                    TestAction<Promise<int>> onAdoptCallbackAdded = (ref Promise<int> p) =>
                    {
                        p = p.Finally(() => ++completeCounter)
                            .Catch(() => 1);
                    };

                    TestHelper.AddCancelCallbacks<int, float>(cancelPromiseRetainer.WaitAsync(),
                        promiseToPromise: p => resolveWaitPromiseRetainer.WaitAsync(),
                        onAdoptCallbackAdded: onAdoptCallbackAdded
                    );
                    TestHelper.AddCancelCallbacks<int, float>(cancelPromiseRetainer.WaitAsync(),
                        promiseToPromise: p => rejectWaitPromiseRetainer.WaitAsync(),
                        onAdoptCallbackAdded: onAdoptCallbackAdded
                    );
                    TestHelper.AddCancelCallbacks<int, float>(cancelPromiseRetainer.WaitAsync(),
                        promiseToPromise: p => cancelWaitPromiseRetainer.WaitAsync(),
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

                    cancelPromiseRetainer.Dispose();
                    resolveWaitPromiseRetainer.Dispose();
                    rejectWaitPromiseRetainer.Dispose();
                    cancelWaitPromiseRetainer.Dispose();
                }

                [Test]
                public void IfWhenXIsFulfilled_FulfillPromiseWithTheSameValue_void()
                {
                    var cancelDeferred = Promise.NewDeferred();
                    cancelDeferred.Cancel();

                    var cancelPromiseRetainer = cancelDeferred.Promise.GetRetainer();

                    int resolveCounter = 0;

                    TestAction<Promise> onAdoptCallbackAdded = (ref Promise p) =>
                    {
                        p = p.Then(() =>
                        {
                            ++resolveCounter;
                        });
                    };

                    var resolveWaitDeferred = Promise.NewDeferred();
                    var resolveWaitPromiseRetainer = resolveWaitDeferred.Promise.GetRetainer();

                    Func<Promise, Promise> promiseToPromise = p => resolveWaitPromiseRetainer.WaitAsync();

                    // Test pending -> resolved and already resolved.
                    bool firstRun = true;
                RunAgain:
                    resolveCounter = 0;

                    TestHelper.AddCancelCallbacks<float>(cancelPromiseRetainer.WaitAsync(),
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

                    cancelPromiseRetainer.Dispose();
                    resolveWaitPromiseRetainer.Dispose();
                }

                [Test]
                public void IfWhenXIsFulfilled_FulfillPromiseWithTheSameValue_T()
                {
                    var cancelDeferred = Promise.NewDeferred<int>();
                    cancelDeferred.Cancel();

                    var cancelPromiseRetainer = cancelDeferred.Promise.GetRetainer();

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
                    var resolveWaitPromiseRetainer = resolveWaitDeferred.Promise.GetRetainer();

                    Func<Promise<int>, Promise<int>> promiseToPromise = p => resolveWaitPromiseRetainer.WaitAsync();

                    // Test pending -> resolved and already resolved.
                    bool firstRun = true;
                RunAgain:
                    resolveCounter = 0;

                    TestHelper.AddCancelCallbacks<int, float>(cancelPromiseRetainer.WaitAsync(),
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

                    cancelPromiseRetainer.Dispose();
                    resolveWaitPromiseRetainer.Dispose();
                }

                [Test]
                public void IfWhenXIsRejected_RejectPromiseWithTheSameReason_void()
                {
                    var cancelDeferred = Promise.NewDeferred();
                    cancelDeferred.Cancel();

                    var cancelPromiseRetainer = cancelDeferred.Promise.GetRetainer();

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
                    var rejectWaitPromiseRetainer = rejectWaitDeferred.Promise.GetRetainer();

                    Func<Promise, Promise> promiseToPromise = p => rejectWaitPromiseRetainer.WaitAsync();

                    // Test pending -> rejected and already rejected.
                    bool firstRun = true;
                RunAgain:
                    rejectCounter = 0;

                    TestHelper.AddCancelCallbacks<float>(cancelPromiseRetainer.WaitAsync(),
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

                    cancelPromiseRetainer.Dispose();
                    rejectWaitPromiseRetainer.Dispose();
                }

                [Test]
                public void IfWhenXIsRejected_RejectPromiseWithTheSameReason_T()
                {
                    var cancelDeferred = Promise.NewDeferred<int>();
                    cancelDeferred.Cancel();

                    var cancelPromiseRetainer = cancelDeferred.Promise.GetRetainer();

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
                    var rejectWaitPromiseRetainer = rejectWaitDeferred.Promise.GetRetainer();

                    Func<Promise<int>, Promise<int>> promiseToPromise = p => rejectWaitPromiseRetainer.WaitAsync();

                    // Test pending -> rejected and already rejected.
                    bool firstRun = true;
                RunAgain:
                    rejectCounter = 0;

                    TestHelper.AddCancelCallbacks<int, float>(cancelPromiseRetainer.WaitAsync(),
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

                    cancelPromiseRetainer.Dispose();
                    rejectWaitPromiseRetainer.Dispose();
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
                string expectedMessage = "Circular Promise chain detected.";
                var deferred = Promise.NewDeferred();

                int exceptionCounter = 0;

                Action<object> catcher = (object o) =>
                {
                    Assert.IsInstanceOf<InvalidReturnException>(o);
                    Assert.AreEqual(expectedMessage, o.UnsafeAs<Exception>().Message);
                    ++exceptionCounter;
                };

                Func<Promise, Promise> promiseToPromise = p =>
                {
                    using (var promiseRetainer = p.GetRetainer())
                    {
                        promiseRetainer.WaitAsync().Catch(catcher).Forget();
                        return promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate().Catch(() => { });
                    }
                };

                bool adopted = false;
                TestAction<Promise> onAdoptCallbackAdded = (ref Promise p) => adopted = true;
                TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                {
                    if (!adopted)
                    {
                        p.Forget();
                    }
                    adopted = false;
                };

                TestHelper.AddCancelCallbacks<float>(deferred.Promise,
                    promiseToPromise: promiseToPromise,
                    onAdoptCallbackAdded: onAdoptCallbackAdded,
                    onCallbackAdded: onCallbackAdded
                );

                deferred.Cancel();

                Assert.AreEqual(TestHelper.onCancelCallbacks, exceptionCounter);
            }

            [Test]
            public void IfXIsAPromiseAndItResultsInACircularPromiseChain_RejectPromiseWithInvalidReturnExceptionAsTheReason_T()
            {
                string expectedMessage = "Circular Promise chain detected.";
                var deferred = Promise.NewDeferred<int>();

                int exceptionCounter = 0;

                Action<object> catcher = (object o) =>
                {
                    Assert.IsInstanceOf<InvalidReturnException>(o);
                    Assert.AreEqual(expectedMessage, o.UnsafeAs<Exception>().Message);
                    ++exceptionCounter;
                };

                Func<Promise<int>, Promise<int>> promiseToPromise = p =>
                {
                    using (var promiseRetainer = p.GetRetainer())
                    {
                        promiseRetainer.WaitAsync().Catch(catcher).Forget();
                        return promiseRetainer.WaitAsync().ThenDuplicate().ThenDuplicate().Catch(() => 1);
                    }
                };

                bool adopted = false;
                TestAction<Promise<int>> onAdoptCallbackAdded = (ref Promise<int> p) => adopted = true;
                TestAction<Promise<int>> onCallbackAdded = (ref Promise<int> p) =>
                {
                    if (!adopted)
                    {
                        p.Forget();
                    }
                    adopted = false;
                };

                TestHelper.AddCancelCallbacks<int, float>(deferred.Promise,
                    promiseToPromise: promiseToPromise,
                    onAdoptCallbackAdded: onAdoptCallbackAdded,
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
            var deferred = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred);

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
            var deferred = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred);

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
            var deferred = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred);

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
            var promiseRetainer = deferred.Promise.GetRetainer();

            promiseRetainer.WaitAsync()
                .Then(() => { }, cancelationSource.Token)
                .CatchCancelation(captureValue, cv => Assert.AreEqual(captureValue, cv))
                .Forget();
            promiseRetainer.WaitAsync()
                .Then(() => 1f, cancelationSource.Token)
                .CatchCancelation(captureValue, cv => Assert.AreEqual(captureValue, cv))
                .Forget();
            cancelationSource.Cancel();

            deferred.Resolve();

            cancelationSource.Dispose();
            promiseRetainer.Dispose();
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

                var deferred = Promise.NewDeferred();
                cancelationSource.Token.Register(deferred);

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

                var deferred = Promise.NewDeferred();
                Proto.Promises.CancelationToken.Canceled().Register(deferred);

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
                var deferred = Promise.NewDeferred();
                cancelationSource.Token.Register(deferred);

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
                var deferred = Promise.NewDeferred<int>();
                cancelationSource.Token.Register(deferred);

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
                var deferred = Promise.NewDeferred();
                deferredCancelationSource.Token.Register(deferred);

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
                var deferred = Promise.NewDeferred<int>();
                deferredCancelationSource.Token.Register(deferred);

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
                var promiseRetainer = deferred.Promise.GetRetainer();

                TestHelper.AddResolveCallbacksWithCancelation<float, string>(
                    promiseRetainer.WaitAsync(),
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promiseRetainer.WaitAsync(),
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                cancelationSource.Cancel();
                deferred.Resolve();

                cancelationSource.Dispose();
                promiseRetainer.Dispose();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsCanceled_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>();
                var promiseRetainer = deferred.Promise.GetRetainer();

                TestHelper.AddResolveCallbacksWithCancelation<int, float, string>(
                    promiseRetainer.WaitAsync(),
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promiseRetainer.WaitAsync(),
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                cancelationSource.Cancel();
                deferred.Resolve(1);

                cancelationSource.Dispose();
                promiseRetainer.Dispose();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsAlreadyCanceled_void0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred();
                var promiseRetainer = deferred.Promise.GetRetainer();

                TestHelper.AddResolveCallbacksWithCancelation<float, string>(
                    promiseRetainer.WaitAsync(),
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promiseRetainer.WaitAsync(),
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                deferred.Resolve();

                cancelationSource.Dispose();
                promiseRetainer.Dispose();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsAlreadyCanceled_void1()
            {
                var deferred = Promise.NewDeferred();
                var promiseRetainer = deferred.Promise.GetRetainer();

                TestHelper.AddResolveCallbacksWithCancelation<float, string>(
                    promiseRetainer.WaitAsync(),
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: Proto.Promises.CancelationToken.Canceled()
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promiseRetainer.WaitAsync(),
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: Proto.Promises.CancelationToken.Canceled()
                );

                deferred.Resolve();
                promiseRetainer.Dispose();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsAlreadyCanceled_T0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred<int>();
                var promiseRetainer = deferred.Promise.GetRetainer();

                TestHelper.AddResolveCallbacksWithCancelation<int, float, string>(
                    promiseRetainer.WaitAsync(),
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promiseRetainer.WaitAsync(),
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                deferred.Resolve(1);

                cancelationSource.Dispose();
                promiseRetainer.Dispose();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsAlreadyCanceled_T1()
            {
                var deferred = Promise.NewDeferred<int>();
                var promiseRetainer = deferred.Promise.GetRetainer();

                TestHelper.AddResolveCallbacksWithCancelation<int, float, string>(
                    promiseRetainer.WaitAsync(),
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: Proto.Promises.CancelationToken.Canceled()
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promiseRetainer.WaitAsync(),
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: Proto.Promises.CancelationToken.Canceled()
                );

                deferred.Resolve(1);
                promiseRetainer.Dispose();
            }

            public class Reject
            {

                private const string expectedRejection = "Reject";

                [SetUp]
                public void Setup()
                {
                    // When a callback is canceled and the previous promise is rejected, the rejection is unhandled.
                    // So we set the expected uncaught reject value.
                    TestHelper.s_expectedUncaughtRejectValue = expectedRejection;

                    TestHelper.Setup();
                }

                [TearDown]
                public void Teardown()
                {
                    TestHelper.Cleanup();

                    TestHelper.s_expectedUncaughtRejectValue = null;
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
                    deferred.Reject(expectedRejection);

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
                    deferred.Reject(expectedRejection);

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

                    deferred.Reject(expectedRejection);

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

                    deferred.Reject(expectedRejection);
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

                    deferred.Reject(expectedRejection);

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

                    deferred.Reject(expectedRejection);
                }
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
                var promiseRetainer = deferred.Promise.GetRetainer();

                int cancelCallbacks = 0;

                TestHelper.AddResolveCallbacksWithCancelation<float, string>(
                    promiseRetainer.WaitAsync(),
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promiseRetainer.WaitAsync(),
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                    promiseRetainer.WaitAsync(),
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
                promiseRetainer.Dispose();
            }

            [Test]
            public void PromiseIsCanceledFromToken_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();

                var deferred = Promise.NewDeferred<int>();
                var promiseRetainer = deferred.Promise.GetRetainer();

                int cancelCallbacks = 0;

                TestHelper.AddResolveCallbacksWithCancelation<int, float, string>(
                    promiseRetainer.WaitAsync(),
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promiseRetainer.WaitAsync(),
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                    promiseRetainer.WaitAsync(),
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
                promiseRetainer.Dispose();
            }

            [Test]
            public void PromiseIsCanceledFromAlreadyCanceledToken_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred();
                var promiseRetainer = deferred.Promise.GetRetainer();

                int cancelCallbacks = 0;

                TestHelper.AddResolveCallbacksWithCancelation<float, string>(
                    promiseRetainer.WaitAsync(),
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promiseRetainer.WaitAsync(),
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                    promiseRetainer.WaitAsync(),
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );

                deferred.Resolve();

                Assert.AreEqual(
                    TestHelper.cancelVoidCallbacks,
                    cancelCallbacks
                );

                cancelationSource.Dispose();
                promiseRetainer.Dispose();
            }

            [Test]
            public void PromiseIsCanceledFromAlreadyCanceledToken_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred<int>();
                var promiseRetainer = deferred.Promise.GetRetainer();

                int cancelCallbacks = 0;

                TestHelper.AddResolveCallbacksWithCancelation<int, float, string>(
                    promiseRetainer.WaitAsync(),
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promiseRetainer.WaitAsync(),
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                    promiseRetainer.WaitAsync(),
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks
                );

                deferred.Resolve(1);

                Assert.AreEqual(
                    TestHelper.cancelTCallbacks,
                    cancelCallbacks
                );

                cancelationSource.Dispose();
                promiseRetainer.Dispose();
            }
        }
    }
}