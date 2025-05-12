#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;

namespace ProtoPromise.Tests.APIs
{
    public class MergeTests
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
        public void MergePromiseIsResolvedWhenAllPromisesAreResolved()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<string>();

            string success = "Success";
            bool resolved = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .Then(values =>
                {
                    resolved = true;

                    Assert.AreEqual(1, values.Item1);
                    Assert.AreEqual(success, values.Item2);
                })
                .Forget();

            deferred1.Resolve(1);
            deferred2.Resolve(success);

            Assert.IsTrue(resolved);
        }

        [Test]
        public void MergePromiseIsResolvedWhenAllPromisesAreAlreadyResolved()
        {
            string success = "Success";

            bool resolved = false;

            Promise.Merge(Promise.Resolved(1), Promise.Resolved(success))
                .Then(values =>
                {
                    resolved = true;

                    Assert.AreEqual(1, values.Item1);
                    Assert.AreEqual(success, values.Item2);
                })
                .Forget();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void MergePromiseIsRejectedWhenFirstPromiseIsRejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<string>();

            string expected = "Error";
            bool rejected = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();

            deferred1.Reject(expected);
            deferred2.Resolve("Success");

            Assert.IsTrue(rejected);
        }

        [Test]
        public void MergePromiseIsRejectedWhenSecondPromiseIsRejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<string>();

            string expected = "Error";
            bool rejected = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();

            deferred1.Resolve(2);
            deferred2.Reject(expected);

            Assert.IsTrue(rejected);
        }

        [Test]
        public void MergePromiseIsRejectedWhenBothPromisesAreRejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<string>();

            string reject1 = "Error 1";
            string reject2 = "Error 2";
            bool rejected = false;

            // The second rejection gets sent to the UncaughtRejectionHandler.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            bool uncaught = false;
            Promise.Config.UncaughtRejectionHandler = e =>
            {
                Assert.AreEqual(reject2, e.Value);
                uncaught = true;
            };

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .Catch((string e) =>
                {
                    Assert.AreEqual(reject1, e);
                    rejected = true;
                })
                .Forget();

            deferred1.Reject(reject1);
            deferred2.Reject(reject2);

            Assert.IsTrue(rejected);
            Assert.True(uncaught);

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void MergePromiseIsRejectedWhenAnyPromiseIsAlreadyRejected()
        {
            bool rejected = false;
            string expected = "Error";

            var deferred = Promise.NewDeferred<int>();

            Promise.Merge(deferred.Promise, Promise<int>.Rejected(expected))
                .Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();

            deferred.Resolve(0);

            Assert.IsTrue(rejected);
        }

        [Test]
        public void MergePromiseIsCanceledWhenFirstPromiseIsCanceled()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred1);
            var deferred2 = Promise.NewDeferred<string>();

            bool canceled = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            cancelationSource.Cancel();
            deferred2.Resolve("Success");

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void MergePromiseIsCanceledWhenSecondPromiseIsCanceled()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<string>();
            cancelationSource.Token.Register(deferred2);

            bool canceled = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            deferred1.Resolve(2);
            cancelationSource.Cancel();

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void MergePromiseIsCanceledWhenBothPromisesAreCanceled()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>();
            cancelationSource1.Token.Register(deferred1);
            CancelationSource cancelationSource2 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<string>();
            cancelationSource2.Token.Register(deferred2);

            bool canceled = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            cancelationSource1.Cancel();
            cancelationSource2.Cancel();

            Assert.IsTrue(canceled);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void MergePromiseIsCanceledWhenAnyPromiseIsAlreadyCanceled()
        {
            bool canceled = false;

            var deferred = Promise.NewDeferred<int>();

            Promise.Merge(deferred.Promise, Promise<int>.Canceled())
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            deferred.Resolve(0);

            Assert.IsTrue(canceled);
        }
    }
}