#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Threading;

namespace ProtoPromiseTests.APIs
{
    public class CaptureTests
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

#if PROMISE_PROGRESS
        [Test]
        public void IfOnProgressIsNullThrow_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Progress(100, default(Action<int, float>));
            });

            deferred.Resolve();
            promise.Forget();
        }

        [Test]
        public void IfOnProgressIsNullThrow_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Progress(100, default(Action<int, float>));
            });

            deferred.Resolve(1);
            promise.Forget();
        }
#endif

        [Test]
        public void IfOnCanceledIsNullThrow_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.CatchCancelation(100, default(Action<int>));
            });

            deferred.Resolve();
            promise.Forget();
        }

        [Test]
        public void IfOnCanceledIsNullThrow_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.CatchCancelation(100, default(Action<int>));
            });

            deferred.Resolve(1);
            promise.Forget();
        }

        [Test]
        public void IfOnFinallyIsNullThrow_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Finally(100, default(Action<int>));
            });

            deferred.Resolve();

            promise.Forget();
        }

        [Test]
        public void IfOnFinallyIsNullThrow_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Finally(100, default(Action<int>));
            });

            deferred.Resolve(1);

            promise.Forget();
        }

        [Test]
        public void IfOnContinueIsNullThrow_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.ContinueWith(100, default(Promise.ContinueAction<int>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.ContinueWith(100, default(Promise.ContinueFunc<int, bool>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.ContinueWith(100, default(Promise.ContinueFunc<int, Promise>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.ContinueWith(100, default(Promise.ContinueFunc<int, Promise<bool>>));
            });

            deferred.Resolve();

            promise.Forget();
        }

        [Test]
        public void IfOnContinueIsNullThrow_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.ContinueWith(100, default(Promise<int>.ContinueAction<int>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.ContinueWith(100, default(Promise<int>.ContinueFunc<int, bool>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.ContinueWith(100, default(Promise<int>.ContinueFunc<int, Promise>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.ContinueWith(100, default(Promise<int>.ContinueFunc<int, Promise<bool>>));
            });

            deferred.Resolve(1);

            promise.Forget();
        }

        [Test]
        public void IfOnFulfilledIsNullThrow_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Action<int>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, bool>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise<bool>>));
            });


            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Action<int>), () => { });
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Action<int>), (string failValue) => { });
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, bool>), () => default(bool));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, bool>), (string failValue) => default(bool));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise>), () => default(Promise));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise>), (string failValue) => default(Promise));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise<bool>>), () => default(Promise<bool>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise<bool>>), (string failValue) => default(Promise<bool>));
            });


            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Action<int>), () => default(Promise));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Action<int>), (string failValue) => default(Promise));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, bool>), () => default(Promise<bool>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, bool>), (string failValue) => default(Promise<bool>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise>), () => { });
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise>), (string failValue) => { });
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise<bool>>), () => default(bool));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise<bool>>), (string failValue) => default(bool));
            });

            deferred.Resolve();
            promise.Forget();
        }

        [Test]
        public void IfOnFulfilledIsNullThrow_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Action<bool, int>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, int>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, Promise>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, Promise<int>>));
            });


            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Action<bool, int>), () => { });
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Action<bool, int>), (string failValue) => { });
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, int>), () => default(int));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, int>), (string failValue) => default(int));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, Promise>), () => default(Promise));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, Promise>), (string failValue) => default(Promise));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, Promise<int>>), () => default(Promise<int>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, Promise<int>>), (string failValue) => default(Promise<int>));
            });


            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Action<bool, int>), () => default(Promise));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Action<bool, int>), (string failValue) => default(Promise));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, int>), () => default(Promise<int>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, int>), (string failValue) => default(Promise<int>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, Promise>), () => { });
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, Promise>), (string failValue) => { });
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, Promise<int>>), () => default(int));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(true, default(Func<bool, int, Promise<int>>), (string failValue) => default(int));
            });

            deferred.Resolve(1);
            promise.Forget();
        }

        [Test]
        public void IfOnRejectedIsNullThrow_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Catch(100, default(Action<int>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Catch(100, default(Action<int, string>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Catch(100, default(Func<int, Promise>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Catch(100, default(Func<int, string, Promise>));
            });


            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => { }, 100, default(Action<int>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => { }, 100, default(Action<int, string>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise), 100, default(Func<int, Promise>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise), 100, default(Func<int, string, Promise>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => "string", 100, default(Func<int, string>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => "string", 100, default(Func<int, Exception, string>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise<string>), 100, default(Func<int, Promise<string>>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise<string>), 100, default(Func<int, Exception, Promise<string>>));
            });


            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise), 100, default(Action<int>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise), 100, default(Action<int, string>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => { }, 100, default(Func<int, Promise>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => { }, 100, default(Func<int, string, Promise>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise<string>), 100, default(Func<int, string>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise<string>), 100, default(Func<int, Exception, string>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => "string", 100, default(Func<int, Promise<string>>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then(() => "string", 100, default(Func<int, Exception, Promise<string>>));
            });

            deferred.Resolve();
            promise.Forget();
        }

        [Test]
        public void IfOnRejectedIsNullThrow_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Catch(true, default(Func<bool, int>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Catch(true, default(Func<bool, string, int>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Catch(true, default(Func<bool, Promise<int>>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Catch(true, default(Func<bool, string, Promise<int>>));
            });


            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => { }, true, default(Action<bool>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => { }, true, default(Action<bool, string>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => default(Promise), true, default(Func<bool, Promise>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => default(Promise), true, default(Func<bool, string, Promise>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => "string", true, default(Func<bool, string>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => "string", true, default(Func<bool, Exception, string>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => default(Promise<string>), true, default(Func<bool, Promise<string>>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => default(Promise<string>), true, default(Func<bool, Exception, Promise<string>>));
            });


            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => default(Promise), true, default(Action<bool>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => default(Promise), true, default(Action<bool, string>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => { }, true, default(Func<bool, Promise>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => { }, true, default(Func<bool, string, Promise>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => default(Promise<string>), true, default(Func<bool, string>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => default(Promise<string>), true, default(Func<bool, Exception, string>));
            });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => "string", true, default(Func<bool, Promise<string>>));
            });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Then((int x) => "string", true, default(Func<bool, Exception, Promise<string>>));
            });

            deferred.Resolve(1);
            promise.Forget();
        }
#endif

#if PROMISE_PROGRESS
        [Test]
        public void OnProgressWillBeInvokedWithCapturedValue_void([Values] SynchronizationOption synchronizationOption)
        {
            string expected = "expected";
            bool invoked = false;

            var deferred = Promise.NewDeferred();

            deferred.Promise
                .Progress(expected, (cv, progress) =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                    Thread.MemoryBarrier();
                }, synchronizationOption)
                .Forget();

            deferred.ReportProgress(0.5f);
            deferred.Resolve();

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.True(invoked);
        }

        [Test]
        public void OnProgressWillBeInvokedWithCapturedValue_T([Values] SynchronizationOption synchronizationOption)
        {
            string expected = "expected";
            bool invoked = false;

            var deferred = Promise.NewDeferred<int>();

            deferred.Promise
                .Progress(expected, (cv, progress) =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                    Thread.MemoryBarrier();
                }, synchronizationOption)
                .Forget();

            deferred.ReportProgress(0.5f);
            deferred.Resolve(1);

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.True(invoked);
        }
#endif

        [Test]
        public void OnCanceledWillBeInvokedWithCapturedValue_void()
        {
            string expected = "expected";
            bool invoked = false;

            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            deferred.Promise
                .CatchCancelation(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel();

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnCanceledWillBeInvokedWithCapturedValue_T()
        {
            string expected = "expected";
            bool invoked = false;

            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            deferred.Promise
                .CatchCancelation(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel();

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnFinallyWillBeInvokedWithCapturedValue_resolved_void()
        {
            var deferred = Promise.NewDeferred();

            string expected = "expected";
            bool invoked = false;

            deferred.Promise
                .Finally(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                })
                .Forget();

            deferred.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void OnFinallyWillBeInvokedWithCapturedValue_resolved_T()
        {
            var deferred = Promise.NewDeferred<int>();

            string expected = "expected";
            bool invoked = false;

            deferred.Promise
                .Finally(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                })
                .Forget();

            deferred.Resolve(1);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void OnFinallyWillBeInvokedWithCapturedValue_rejected_void()
        {
            var deferred = Promise.NewDeferred();

            string expected = "expected";
            string rejection = "Reject";
            bool invoked = false;

            deferred.Promise
                .Finally(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                })
                .Catch((string e) => Assert.AreEqual(rejection, e))
                .Forget();

            deferred.Reject(rejection);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void OnFinallyWillBeInvokedWithCapturedValue_rejected_T()
        {
            var deferred = Promise.NewDeferred<int>();

            string expected = "expected";
            string rejection = "Reject";
            bool invoked = false;

            deferred.Promise
                .Finally(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                })
                .Catch((string e) => Assert.AreEqual(rejection, e))
                .Forget();

            deferred.Reject(rejection);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void OnFinallyWillBeInvokedWithCapturedValue_canceled_void()
        {
            string expected = "expected";
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            bool invoked = false;

            deferred.Promise
                .Finally(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel();
            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnFinallyWillBeInvokedWithCapturedValue_canceled_T()
        {
            string expected = "expected";
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            bool invoked = false;

            deferred.Promise
                .Finally(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel();
            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
        }

        [Test]
        public void PromiseIsRejectedWithThrownExceptionWhenOnFinallyWithCapturedValueThrows_resolve_void()
        {
            var deferred = Promise.NewDeferred();

            bool invoked = false;
            Exception expected = new Exception();

            deferred.Promise
                .Finally(100, cv => { throw expected; })
                .Catch((Exception e) =>
                {
                    Assert.AreEqual(expected, e);
                    invoked = true;
                })
                .Forget();

            deferred.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void PromiseIsRejectedWithThrownExceptionWhenOnFinallyWithCapturedValueThrows_resolve_T()
        {
            var deferred = Promise.NewDeferred<int>();

            bool invoked = false;
            Exception expected = new Exception();

            deferred.Promise
                .Finally(100, cv => { throw expected; })
                .Catch((Exception e) =>
                {
                    Assert.AreEqual(expected, e);
                    invoked = true;
                })
                .Forget();

            deferred.Resolve(1);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void PromiseIsRejectedWithThrownExceptionWhenOnFinallyWithCapturedValueThrows_reject_void()
        {
            var deferred = Promise.NewDeferred();

            bool invoked = false;
            string rejectValue = "Reject";
            Exception expected = new Exception();

            deferred.Promise
                .Finally(100, cv => { throw expected; })
                .Catch((Exception e) =>
                {
                    Assert.AreEqual(expected, e);
                    invoked = true;
                })
                .Forget();

            // When the exception thrown in onFinally overwrites the current rejection, the current rejection gets sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it actually gets sent to it.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            bool uncaughtHandled = false;
            Promise.Config.UncaughtRejectionHandler = e =>
            {
                Assert.AreEqual(rejectValue, e.Value);
                uncaughtHandled = true;
            };

            deferred.Reject(rejectValue);

            Assert.IsTrue(invoked);
            Assert.IsTrue(uncaughtHandled);
            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void PromiseIsRejectedWithThrownExceptionWhenOnFinallyWithCapturedValueThrows_reject_T()
        {
            var deferred = Promise.NewDeferred<int>();

            bool invoked = false;
            string rejectValue = "Reject";
            Exception expected = new Exception();

            deferred.Promise
                .Finally(100, cv => { throw expected; })
                .Catch((Exception e) =>
                {
                    Assert.AreEqual(expected, e);
                    invoked = true;
                })
                .Forget();

            // When the exception thrown in onFinally overwrites the current rejection, the current rejection gets sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it actually gets sent to it.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            bool uncaughtHandled = false;
            Promise.Config.UncaughtRejectionHandler = e =>
            {
                Assert.AreEqual(rejectValue, e.Value);
                uncaughtHandled = true;
            };

            deferred.Reject(rejectValue);

            Assert.IsTrue(invoked);
            Assert.IsTrue(uncaughtHandled);
            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void PromiseIsRejectedWithThrownExceptionWhenOnFinallyWithCapturedValueThrows_cancel_void()
        {
            Exception expected = new Exception();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            bool invoked = false;

            deferred.Promise
                .Finally(100, cv => { throw expected; })
                .Catch((Exception e) =>
                {
                    Assert.AreEqual(expected, e);
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel();
            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
        }

        [Test]
        public void PromiseIsRejectedWithThrownExceptionWhenOnFinallyWithCapturedValueThrows_cancel_T()
        {
            Exception expected = new Exception();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            bool invoked = false;

            deferred.Promise
                .Finally(100, cv => { throw expected; })
                .Catch((Exception e) =>
                {
                    Assert.AreEqual(expected, e);
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel();
            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnContinueWillBeInvokedWithCapturedValue_resolved_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            string expected = "expected";
            bool invoked = false;

            TestHelper.AddContinueCallbacks<int, string>(promise,
                captureValue: expected,
                onContinueCapture: (cv, r) =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Resolve();

            Assert.IsTrue(invoked);

            promise.Forget();
        }

        [Test]
        public void OnContinueWillBeInvokedWithCapturedValue_resolved_T()
        {
            string expected = "expected";
            bool invoked = false;

            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            TestHelper.AddContinueCallbacks<int, int, string>(promise,
                captureValue: expected,
                onContinueCapture: (cv, r) =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Resolve(50);

            Assert.IsTrue(invoked);

            promise.Forget();
        }

        [Test]
        public void OnContinueWillBeInvokedWithCapturedValue_rejected_void()
        {
            string expected = "expected";
            bool invoked = false;

            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            TestHelper.AddContinueCallbacks<int, string>(promise,
                captureValue: expected,
                onContinueCapture: (cv, r) =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Reject("Reject");

            Assert.IsTrue(invoked);

            promise.Forget();
        }

        [Test]
        public void OnContinueWillBeInvokedWithCapturedValue_rejected_T()
        {
            string expected = "expected";
            bool invoked = false;

            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            TestHelper.AddContinueCallbacks<int, int, string>(promise,
                captureValue: expected,
                onContinueCapture: (cv, r) =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Reject("Reject");

            Assert.IsTrue(invoked);

            promise.Forget();
        }

        [Test]
        public void OnContinueWillBeInvokedWithCapturedValue_canceled_void()
        {
            string expected = "expected";
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            bool invoked = false;

            TestHelper.AddContinueCallbacks<int, string>(promise,
                captureValue: expected,
                onContinueCapture: (cv, r) =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            cancelationSource.Cancel();
            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
            promise.Forget();
        }

        [Test]
        public void OnContinueWillBeInvokedWithCapturedValue_canceled_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            string expected = "expected";
            bool invoked = false;

            TestHelper.AddContinueCallbacks<int, int, string>(promise,
                captureValue: expected,
                onContinueCapture: (cv, r) =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            cancelationSource.Cancel();
            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
            promise.Forget();
        }

        [Test]
        public void OnResolvedWillBeInvokedWithCapturedValue_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            string expected = "expected";
            bool invoked = false;

            TestHelper.AddResolveCallbacks<int, string>(promise,
                captureValue: expected,
                onResolveCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );
            TestHelper.AddCallbacks<int, object, string>(promise,
                captureValue: expected,
                onResolveCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Resolve();

            Assert.IsTrue(invoked);

            promise.Forget();
        }

        [Test]
        public void OnResolvedWillBeInvokedWithCapturedValue_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            string expected = "expected";
            bool invoked = false;

            TestHelper.AddResolveCallbacks<int, bool, string>(promise,
                captureValue: expected,
                onResolveCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );
            TestHelper.AddCallbacks<int, bool, object, string>(promise,
                captureValue: expected,
                onResolveCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Resolve(50);

            Assert.IsTrue(invoked);

            promise.Forget();
        }

        [Test]
        public void OnRejectedWillBeInvokedWithCapturedValue_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            string expected = "expected";
            bool invoked = false;

            TestHelper.AddCallbacks<int, object, string>(promise,
                captureValue: expected,
                onRejectCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                },
                onUnknownRejectionCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Reject("Reject");

            Assert.IsTrue(invoked);

            promise.Forget();
        }

        [Test]
        public void OnRejectedWillBeInvokedWithCapturedValue_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            string expected = "expected";
            bool invoked = false;

            TestHelper.AddCallbacks<int, bool, object, string>(promise,
                captureValue: expected,
                onResolveCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                },
                onUnknownRejectionCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Reject("Reject");

            Assert.IsTrue(invoked);

            promise.Forget();
        }
    }
}