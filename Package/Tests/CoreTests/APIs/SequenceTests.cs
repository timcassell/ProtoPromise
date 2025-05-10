#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Linq;

namespace ProtoPromise.Tests.APIs
{
    public class SequenceTests
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
        public void SequencePromiseIsResolvedWhenAllPromisesAreResolved()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool completed = false;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise)
                .Then(() => { completed = true; })
                .Forget();

            Assert.IsFalse(completed);

            deferred1.Resolve();

            Assert.IsFalse(completed);

            deferred2.Resolve();

            Assert.IsTrue(completed);
        }

        [Test]
        public void SequencePromiseIsResolvedIfThereAreNoDelegates()
        {
            bool completed = false;

            Promise.Sequence(Enumerable.Empty<Func<Promise>>())
                .Then(() => { completed = true; })
                .Forget();

            Assert.IsTrue(completed);
        }

        [Test]
        public void SequencePromiseIsResolvedWhenAllPromisesAreAlreadyResolved()
        {
            bool completed = false;

            Promise.Sequence(() => Promise.Resolved(), () => Promise.Resolved())
                .Then(() => { completed = true; })
                .Forget();

            Assert.IsTrue(completed);
        }

        [Test]
        public void SequencePromiseIsRejectedWhenFirstPromiseIsRejected()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise)
                .Catch((string e) => { invoked = true; })
                .Forget();

            Assert.IsFalse(invoked);

            deferred1.Reject("Error");

            Assert.IsTrue(invoked);

            deferred2.Resolve();

            Assert.IsTrue(invoked);
            deferred2.Promise.Forget(); // Need to forget this promise because it was never awaited due to the rejection.
        }

        [Test]
        public void SequencePromiseIsRejectedWhenSecondPromiseIsRejected()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise)
                .Catch((string e) => { invoked = true; })
                .Forget();

            Assert.IsFalse(invoked);

            deferred1.Resolve();

            Assert.IsFalse(invoked);

            deferred2.Reject("Error");

            Assert.IsTrue(invoked);
        }

        [Test]
        public void SequenceDelegatesStopGettingInvokedWhenAPromiseIsRejected()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            int invokes = 0;

            Promise.Sequence(
                () => { ++invokes; return deferred1.Promise; },
                () => { ++invokes; return deferred2.Promise; }
            )
                .Catch(() => { })
                .Forget();

            Assert.AreEqual(1, invokes);

            deferred1.Reject("Error");

            Assert.AreEqual(1, invokes);

            deferred2.Resolve();

            Assert.AreEqual(1, invokes);
            deferred2.Promise.Forget(); // Need to forget this promise because it was never awaited due to the rejection.
        }

        [Test]
        public void SequencePromiseIsRejectedWhenAnyPromiseIsAlreadyRejected()
        {
            bool invoked = false;
            string rejection = "Error";

            var deferred = Promise.NewDeferred<int>();

            Promise.Sequence(() => deferred.Promise, () => Promise<int>.Rejected(rejection))
                .Catch((string ex) =>
                {
                    Assert.AreEqual(rejection, ex);
                    invoked = true;
                })
                .Forget();

            Assert.IsFalse(invoked);

            deferred.Resolve(1);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void SequencePromiseIsCanceledWhenFirstPromiseIsCanceled()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred1);
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise)
                .CatchCancelation(() => invoked = true)
                .Forget();

            Assert.IsFalse(invoked);

            cancelationSource.Cancel();

            Assert.IsTrue(invoked);

            deferred2.Resolve();

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
            deferred2.Promise.Forget(); // Need to forget this promise because it was never awaited due to the cancelation.
        }

        [Test]
        public void SequencePromiseIsCanceledWhenSecondPromiseIsCanceled()
        {
            var deferred1 = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred2);

            bool invoked = false;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise)
                .CatchCancelation(() => invoked = true)
                .Forget();

            Assert.IsFalse(invoked);

            deferred1.Resolve();

            Assert.IsFalse(invoked);

            cancelationSource.Cancel();

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
        }

        [Test]
        public void SequenceDelegatesStopGettingInvokedWhenAPromiseIsCanceled()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred1);
            var deferred2 = Promise.NewDeferred();

            int invokes = 0;

            Promise.Sequence(
                () => { ++invokes; return deferred1.Promise; },
                () => { ++invokes; return deferred2.Promise; }
            )
                .Forget();

            Assert.AreEqual(1, invokes);

            cancelationSource.Cancel();

            Assert.AreEqual(1, invokes);

            deferred2.Resolve();

            Assert.AreEqual(1, invokes);

            cancelationSource.Dispose();
            deferred2.Promise.Forget(); // Need to forget this promise because it was never awaited due to the cancelation.
        }

        [Test]
        public void SequencePromiseIsCanceledWhenAnyPromiseIsAlreadyCanceled()
        {
            bool invoked = false;

            var deferred = Promise.NewDeferred<int>();

            Promise.Sequence(() => deferred.Promise, () => Promise<int>.Canceled())
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Forget();

            Assert.IsFalse(invoked);

            deferred.Resolve(1);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void SequencePromiseIsCanceledWhenTokenIsCanceled0()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            bool canceled = false;
            int invokedIndex = -1;

            cancelationSource.Cancel();

            Promise.Sequence(
                cancelationSource.Token,
                () =>
                {
                    invokedIndex = 0;
                    return Promise.Resolved();
                },
                () =>
                {
                    invokedIndex = 1;
                    return Promise.Resolved();
                }
            )
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            Assert.AreEqual(-1, invokedIndex);
            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void SequencePromiseIsCanceledWhenTokenIsCanceled1()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            int invokedIndex = -1;

            var deferred = Promise.NewDeferred();

            Promise.Sequence(
                cancelationSource.Token,
                () =>
                {
                    invokedIndex = 0;
                    return deferred.Promise;
                },
                () =>
                {
                    invokedIndex = 1;
                    return Promise.Resolved();
                }
            )
                .Forget();

            cancelationSource.Cancel();
            deferred.Resolve();

            Assert.AreEqual(0, invokedIndex);

            cancelationSource.Dispose();
        }

        [Test]
        public void SequencePromiseIsCanceledWhenTokenIsCanceled2()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            int invokedIndex = -1;

            Promise.Sequence(
                cancelationSource.Token,
                () =>
                {
                    invokedIndex = 0;
                    cancelationSource.Cancel();
                    return Promise.Resolved();
                },
                () =>
                {
                    invokedIndex = 1;
                    return Promise.Resolved();
                }
            )
                .Forget();

            Assert.AreEqual(0, invokedIndex);

            cancelationSource.Dispose();
        }

        [Test]
        public void SequencePromiseIsNotCanceledWhenTokenIsCanceledAfterAllCallbacksHaveBeenInvoked()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            bool canceled = false;
            int invokedIndex = -1;

            Promise.Sequence(
                cancelationSource.Token,
                () =>
                {
                    invokedIndex = 0;
                    return Promise.Resolved();
                },
                () =>
                {
                    invokedIndex = 1;
                    return Promise.Resolved();
                },
                () =>
                {
                    invokedIndex = 2;
                    cancelationSource.Cancel();
                    return Promise.Resolved();
                }
            )
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            Assert.AreEqual(2, invokedIndex);
            Assert.IsFalse(canceled);

            cancelationSource.Dispose();
        }
    }
}