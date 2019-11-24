using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class All
    {
        [Test]
        public void combined_promise_is_resolved_when_children_are_resolved()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var completed = 0;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(values => {
                    ++completed;

                    Assert.AreEqual(2, values.Count);
                    Assert.AreEqual(1, values[0]);
                    Assert.AreEqual(2, values[1]);
                });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => ++completed);

            deferred1.Resolve(1);
            deferred2.Resolve(2);

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, completed);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void combined_promise_is_rejected_when_first_promise_is_rejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var errors = 0;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => { ++errors; });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => { ++errors; });

            deferred1.Reject("Error!");
            deferred2.Resolve(2);

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, errors);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void combined_promise_is_rejected_when_second_promise_is_rejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var errors = 0;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => { ++errors; });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => { ++errors; });

            deferred1.Resolve(2);
            deferred2.Reject("Error!");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, errors);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void combined_promise_is_rejected_when_both_promises_are_rejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var errors = 0;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => { ++errors; });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => { ++errors; });

            deferred1.Reject("Error!");
            deferred2.Reject("Error!");

            // Only 1 rejection is caught, so expect an unhandled throw.
            Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

            Assert.AreEqual(2, errors);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void combined_promise_is_resolved_if_there_are_no_promises()
        {
            var completed = 0;

            Promise.All(Enumerable.Empty<Promise<int>>())
                .Then(v => {
                    ++completed;

                    Assert.IsEmpty(v);
                });

            Promise.All(Enumerable.Empty<Promise>())
                .Then(() => ++completed);

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, completed);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void combined_promise_is_resolved_when_all_promises_are_already_resolved()
        {
            var promise1 = Promise.Resolved(1);
            var promise2 = Promise.Resolved(1);

            promise1.Retain();
            promise2.Retain();
            Promise.Manager.HandleCompletes();

            var completed = 0;

            Promise.All(promise1, promise2)
                .Then(v => ++completed);

            Promise.All((Promise) promise1, promise2)
                .Then(() => ++completed);

            promise1.Release();
            promise2.Release();
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, completed);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void all_with_rejected_promise()
        {
            int rejected = 0;
            string rejection = "Error!";

            var deferred = Promise.NewDeferred<int>();
            var promise = Promise.Rejected<int, string>(rejection);

            Promise.All(deferred.Promise, promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(ex => {
                    Assert.AreEqual(rejection, ex);
                    ++rejected;
                });

            Promise.All((Promise) deferred.Promise, promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(ex => {
                    Assert.AreEqual(rejection, ex);
                    ++rejected;
                });

            deferred.Resolve(0);

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, rejected);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllProgressIsNormalized()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.All(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 8f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3f / 8f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(4f / 8f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(5f / 8f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(6f / 8f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(7f / 8f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(8f / 8f, progress, TestHelper.progressEpsilon);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
    }
}