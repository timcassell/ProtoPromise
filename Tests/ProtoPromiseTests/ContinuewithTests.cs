#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using NUnit.Framework;

namespace Proto.Promises.Tests
{
    public class ContinuewithTests
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
        [Test]
        public void IfOnContinueIsNullThrow_void()
        {
            void Test()
            {
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.ContinueWith(default(Promise.ContinueAction));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.ContinueWith(default(Promise.ContinueFunc<int>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.ContinueWith(default(Promise.ContinueFunc<Promise>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.ContinueWith(default(Promise.ContinueFunc<Promise<int>>));
                });
                deferred.Resolve();
                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void IfOnContinueIsNullThrow_T()
        {
            void Test()
            {
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.ContinueWith(default(Promise<int>.ContinueAction));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.ContinueWith(default(Promise<int>.ContinueFunc<int>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.ContinueWith(default(Promise<int>.ContinueFunc<Promise>));
                });
                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.ContinueWith(default(Promise<int>.ContinueFunc<Promise<int>>));
                });
                deferred.Resolve(1);
                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }
#endif

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsResolved_void()
        {
            void Test()
            {
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                int finallyCount = 0;

                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: r => ++finallyCount
                );

                deferred.Resolve();

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(TestHelper.continueVoidCallbacks * 2, finallyCount);

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsResolved_T()
        {
            void Test()
            {
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                int finallyCount = 0;

                TestHelper.AddContinueCallbacks<int, int, string>(promise,
                    onContinue: r => ++finallyCount
                );

                deferred.Resolve(50);

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(TestHelper.continueTCallbacks * 2, finallyCount);

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueStateWhenPromiseIsResolved()
        {
            void Test()
            {
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: r => Assert.AreEqual(r.State, Promise.State.Resolved)
                );

                deferred.Resolve();

                Promise.Manager.HandleCompletes();

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueResultWhenPromiseIsResolved()
        {
            void Test()
            {
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                int expected = 50;

                TestHelper.AddContinueCallbacks<int, bool, string>(promise,
                    onContinue: r =>
                    {
                        Assert.AreEqual(r.State, Promise.State.Resolved);
                        Assert.AreEqual(expected, r.Result);
                    }
                );

                deferred.Resolve(expected);

                Promise.Manager.HandleCompletes();

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsRejected_void()
        {
            void Test()
            {
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                int finallyCount = 0;

                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: r => ++finallyCount
                );

                deferred.Reject("Reject");
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(TestHelper.continueVoidCallbacks * 2, finallyCount);

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsRejected_T()
        {
            void Test()
            {
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                int finallyCount = 0;

                TestHelper.AddContinueCallbacks<int, int, string>(promise,
                    onContinue: r => ++finallyCount
                );

                deferred.Reject("Reject");
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(TestHelper.continueTCallbacks * 2, finallyCount);

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueRejectReasonWhenPromiseIsRejected_void()
        {
            void Test()
            {
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                string rejection = "Reject";

                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: r =>
                    {
                        Assert.AreEqual(r.State, Promise.State.Rejected);
                        Assert.AreEqual(rejection, r.RejectContainer.Value);
                    }
                );

                deferred.Reject(rejection);
                Promise.Manager.HandleCompletes();

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueRejectReasonWhenPromiseIsRejected_T()
        {
            void Test()
            {
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                string rejection = "Reject";

                TestHelper.AddContinueCallbacks<int, int, string>(promise,
                    onContinue: r =>
                    {
                        Assert.AreEqual(r.State, Promise.State.Rejected);
                        Assert.AreEqual(rejection, r.RejectContainer.Value);
                    }
                );

                deferred.Reject(rejection);
                Promise.Manager.HandleCompletes();

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueRethrowRejectReasonWhenPromiseIsRejected_void()
        {
            void Test()
            {
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                int rejections = 0;
                string rejection = "Reject";

                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: r => r.RethrowIfRejected(),
                    onCallbackAdded: p => p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }).Forget(),
                    onCallbackAddedConvert: p => p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }).Forget()
                );

                deferred.Reject(rejection);

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    TestHelper.continueVoidCallbacks * 2,
                    rejections
                );

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueRethrowRejectReasonWhenPromiseIsRejected_T()
        {
            void Test()
            {
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                int rejections = 0;
                string rejection = "Reject";

                TestHelper.AddContinueCallbacks<int, int, string>(promise,
                    onContinue: r => r.RethrowIfRejected(),
                    onCallbackAdded: p => p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }).Forget(),
                    onCallbackAddedConvert: p => p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }).Forget()
                );

                deferred.Reject(rejection);

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    TestHelper.continueTCallbacks * 2,
                    rejections
                );

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsCanceled_void()
        {
            void Test()
            {
                bool repeat = true;
            Repeat:
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                int finallyCount = 0;

                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: r => ++finallyCount
                );

                if (repeat)
                {
                    cancelationSource.Cancel();
                }
                else
                {
                    cancelationSource.Cancel("Cancel");
                }

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(TestHelper.continueVoidCallbacks * 2, finallyCount);

                cancelationSource.Dispose();
                promise.Forget();

                if (repeat)
                {
                    repeat = false;
                    goto Repeat;
                }
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsCanceled_T()
        {
            void Test()
            {
                bool repeat = true;
            Repeat:
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                int finallyCount = 0;

                TestHelper.AddContinueCallbacks<int, int, string>(promise,
                    onContinue: r => ++finallyCount
                );

                if (repeat)
                {
                    cancelationSource.Cancel();
                }
                else
                {
                    cancelationSource.Cancel("Cancel");
                }

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(TestHelper.continueTCallbacks * 2, finallyCount);

                cancelationSource.Dispose();
                promise.Forget();

                if (repeat)
                {
                    repeat = false;
                    goto Repeat;
                }
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueCancelStateWhenPromiseIsCanceled_void()
        {
            void Test()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: r =>
                    {
                        Assert.AreEqual(r.State, Promise.State.Canceled);
                        Assert.AreEqual(null, r.CancelContainer.ValueType);
                    }
                );

                cancelationSource.Cancel();
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueCancelReasonWhenPromiseIsCanceled_void()
        {
            void Test()
            {
                string cancelation = "Cancel";
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: r =>
                    {
                        Assert.AreEqual(r.State, Promise.State.Canceled);
                        Assert.AreEqual(cancelation, r.CancelContainer.Value);
                    }
                );

                cancelationSource.Cancel(cancelation);
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueCancelStateWhenPromiseIsCanceled_T()
        {
            void Test()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                TestHelper.AddContinueCallbacks<int, int, string>(promise,
                    onContinue: r =>
                    {
                        Assert.AreEqual(r.State, Promise.State.Canceled);
                        Assert.AreEqual(null, r.CancelContainer.ValueType);
                    }
                );

                cancelationSource.Cancel();
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueCancelReasonWhenPromiseIsCanceled_T()
        {
            void Test()
            {
                string cancelation = "Cancel";
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                TestHelper.AddContinueCallbacks<int, int, string>(promise,
                    onContinue: r =>
                    {
                        Assert.AreEqual(r.State, Promise.State.Canceled);
                        Assert.AreEqual(cancelation, r.CancelContainer.Value);
                    }
                );

                cancelationSource.Cancel(cancelation);
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueRethrowCancelReasonWhenPromiseIsCanceled_void0()
        {
            void Test()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                int cancelCount = 0;

                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: r => r.RethrowIfCanceled(),
                    onCallbackAdded: p => p.CatchCancelation(e => { Assert.AreEqual(null, e.ValueType); ++cancelCount; }),
                    onCallbackAddedConvert: p => p.CatchCancelation(e => { Assert.AreEqual(null, e.ValueType); ++cancelCount; })
                );

                cancelationSource.Cancel();

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(
                    TestHelper.continueVoidCallbacks * 2,
                    cancelCount
                );

                cancelationSource.Dispose();
                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueRethrowCancelReasonWhenPromiseIsCanceled_void1()
        {
            void Test()
            {
                string cancelation = "Cancel";
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                int cancelCount = 0;

                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: r => r.RethrowIfCanceled(),
                    onCallbackAdded: p => p.CatchCancelation(e => { Assert.AreEqual(cancelation, e.Value); ++cancelCount; }),
                    onCallbackAddedConvert: p => p.CatchCancelation(e => { Assert.AreEqual(cancelation, e.Value); ++cancelCount; })
                );

                cancelationSource.Cancel(cancelation);

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(
                    TestHelper.continueVoidCallbacks * 2,
                    cancelCount
                );

                cancelationSource.Dispose();
                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueRethrowCancelReasonWhenPromiseIsCanceled_T0()
        {
            void Test()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                int cancelCount = 0;

                TestHelper.AddContinueCallbacks<int, int, string>(promise,
                    onContinue: r => r.RethrowIfCanceled(),
                    onCallbackAdded: p => p.CatchCancelation(e => { Assert.AreEqual(null, e.ValueType); ++cancelCount; }),
                    onCallbackAddedConvert: p => p.CatchCancelation(e => { Assert.AreEqual(null, e.ValueType); ++cancelCount; })
                );

                cancelationSource.Cancel();

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(
                    TestHelper.continueTCallbacks * 2,
                    cancelCount
                );

                cancelationSource.Dispose();
                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnContinueRethrowCancelReasonWhenPromiseIsCanceled_T1()
        {
            void Test()
            {
                string cancelation = "Cancel";
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                int cancelCount = 0;

                TestHelper.AddContinueCallbacks<int, int, string>(promise,
                    onContinue: r => r.RethrowIfCanceled(),
                    onCallbackAdded: p => p.CatchCancelation(e => { Assert.AreEqual(cancelation, e.Value); ++cancelCount; }),
                    onCallbackAddedConvert: p => p.CatchCancelation(e => { Assert.AreEqual(cancelation, e.Value); ++cancelCount; })
                );

                cancelationSource.Cancel(cancelation);

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(
                    TestHelper.continueTCallbacks * 2,
                    cancelCount
                );

                cancelationSource.Dispose();
                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }
    }
}