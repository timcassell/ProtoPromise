#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
# endif

using NUnit.Framework;
using Proto.Promises;
using ProtoPromiseTests.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ProtoPromiseTests.APIs
{
    public class UncaughtRejectionTests
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

            TestHelper.s_expectedUncaughtRejectValue = null;
        }

        private static IEnumerable<TestCaseData> GetExpectedRejections()
        {
            yield return new TestCaseData(null);
            yield return new TestCaseData(new object());
            yield return new TestCaseData(new System.InvalidOperationException());
            yield return new TestCaseData(42);
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void UncaughtRejectionIsSentToUncaughtRejectionHandler_void(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                System.Action throwExpected = () => { throw Promise.RejectException(expectedRejectionValue); };

                var cancelationSource = CancelationSource.New();
                var promiseToAwait = default(Promise);

                var actions = new System.Action<Promise>[][]
                {
                    TestHelper.ResolveActionsVoid(),
                    TestHelper.ThenActionsVoid(onRejected: throwExpected),
                    TestHelper.CatchActionsVoid(onRejected: throwExpected),
                    TestHelper.ContinueWithActionsVoid(throwExpected)
                }
                .SelectMany(x => x)
                .Concat(
                    new System.Func<Promise, CancelationToken, Promise>[][]
                    {
                        TestHelper.ResolveActionsVoidWithCancelation(),
                        TestHelper.ThenActionsVoidWithCancelation(onRejected: throwExpected),
                        TestHelper.CatchActionsVoidWithCancelation(onRejected: throwExpected),
                        TestHelper.ContinueWithActionsVoidWithCancelation(throwExpected)
                    }
                    .SelectMany(funcs =>
                        funcs.Select(func => (System.Action<Promise>) (promise => func.Invoke(promise, cancelationSource.Token).Forget()))
                    )
                )
                .Concat(
                    TestHelper.ActionsReturningPromiseVoid(() => promiseToAwait)
                    .Select(func =>
                        (System.Action<Promise>) (promise =>
                        {
                            promiseToAwait = promise;
                            func.Invoke().Forget();
                        })
                    )
                );

                var deferred = Promise.NewDeferred();
                var preservedPromise = deferred.Promise.Preserve();

                foreach (var callback in actions)
                {
                    foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
                    {
                        ++expectedCount;
                        callback.Invoke(promise);
                    }
                }

                preservedPromise.Forget();
                deferred.Reject(expectedRejectionValue);
                cancelationSource.Dispose();
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void UncaughtRejectionIsSentToUncaughtRejectionHandler_T(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                System.Action throwExpected = () => { throw Promise.RejectException(expectedRejectionValue); };

                var cancelationSource = CancelationSource.New();
                var promiseToAwait = default(Promise<int>);

                var actions = new System.Action<Promise<int>>[][]
                {
                    TestHelper.ResolveActions<int>(),
                    TestHelper.ThenActions<int>(onRejected: throwExpected),
                    TestHelper.CatchActions<int>(onRejected: throwExpected),
                    TestHelper.ContinueWithActions<int>(throwExpected)
                }
                .SelectMany(x => x)
                .Concat(
                    new System.Func<Promise<int>, CancelationToken, Promise>[][]
                    {
                        TestHelper.ResolveActionsWithCancelation<int>(),
                        TestHelper.ThenActionsWithCancelation<int>(onRejected: throwExpected),
                        TestHelper.CatchActionsWithCancelation<int>(onRejected: throwExpected),
                        TestHelper.ContinueWithActionsWithCancelation<int>(throwExpected)
                    }
                    .SelectMany(funcs =>
                        funcs.Select(func => (System.Action<Promise<int>>) (promise => func.Invoke(promise, cancelationSource.Token).Forget()))
                    )
                )
                .Concat(
                    TestHelper.ActionsReturningPromiseT<int>(() => promiseToAwait)
                    .Select(func =>
                        (System.Action<Promise<int>>) (promise =>
                        {
                            promiseToAwait = promise;
                            func.Invoke().Forget();
                        })
                    )
                );

                var deferred = Promise.NewDeferred<int>();
                var preservedPromise = deferred.Promise.Preserve();

                foreach (var callback in actions)
                {
                    foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
                    {
                        ++expectedCount;
                        callback.Invoke(promise);
                    }
                }

                preservedPromise.Forget();
                deferred.Reject(expectedRejectionValue);
                cancelationSource.Dispose();
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void WhenPromiseIsCanceled_UncaughtRejectionIsSentToUncaughtRejectionHandler_void(object expectedRejectionValue)
        {
            // Testing an implementation detail - when a promise is canceled and the previous promise is rejected, it counts as an uncaught rejection.
            // This behavior is subject to change.

            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var cancelationSource = CancelationSource.New();

                var actions = new System.Func<Promise, CancelationToken, Promise>[][]
                {
                    TestHelper.ResolveActionsVoidWithCancelation(),
                    TestHelper.ThenActionsVoidWithCancelation(onRejected: () => { }),
                    TestHelper.CatchActionsVoidWithCancelation(onRejected: () => { }),
                    TestHelper.ContinueWithActionsVoidWithCancelation(() => { }),
                    new System.Func<Promise, CancelationToken, Promise>[]
                    {
                        (promise, token) => promise.WaitAsync(token),
                        (promise, token) => promise.WaitAsync(SynchronizationOption.Foreground, cancelationToken: token)
                    }
                }
                .SelectMany(x => x);

                var deferred = Promise.NewDeferred();
                var preservedPromise = deferred.Promise.Preserve();

                foreach (var callback in actions)
                {
                    // We subtract 1 because the preserved promise will only report its unhandled rejection if none of the waiters suppress it. (In this case, the .Duplicate() does suppress it.)
                    --expectedCount;
                    foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
                    {
                        ++expectedCount;
                        callback.Invoke(promise, cancelationSource.Token).Forget();
                    }
                }

                preservedPromise.Forget();
                cancelationSource.Cancel();
                cancelationSource.Dispose();
                deferred.Reject(expectedRejectionValue);
                TestHelper.ExecuteForegroundCallbacks();
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void WhenPromiseIsCanceled_UncaughtRejectionIsSentToUncaughtRejectionHandler_T(object expectedRejectionValue)
        {
            // Testing an implementation detail - when a promise is canceled and the previous promise is rejected, it counts as an uncaught rejection.
            // This behavior is subject to change.

            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var cancelationSource = CancelationSource.New();

                var actions = new System.Func<Promise<int>, CancelationToken, Promise>[][]
                {
                    TestHelper.ResolveActionsWithCancelation<int>(),
                    TestHelper.ThenActionsWithCancelation<int>(onRejected: () => { }),
                    TestHelper.CatchActionsWithCancelation<int>(onRejected: () => { }),
                    TestHelper.ContinueWithActionsWithCancelation<int>(() => { }),
                    new System.Func<Promise<int>, CancelationToken, Promise>[]
                    {
                        (promise, token) => promise.WaitAsync(token),
                        (promise, token) => promise.WaitAsync(SynchronizationOption.Foreground, cancelationToken: token)
                    }
                }
                .SelectMany(x => x);

                var deferred = Promise.NewDeferred<int>();
                var preservedPromise = deferred.Promise.Preserve();

                foreach (var callback in actions)
                {
                    // We subtract 1 because the preserved promise will only report its unhandled rejection if none of the waiters suppress it. (In this case, the .Duplicate() does suppress it.)
                    --expectedCount;
                    foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
                    {
                        ++expectedCount;
                        callback.Invoke(promise, cancelationSource.Token).Forget();
                    }
                }

                preservedPromise.Forget();
                cancelationSource.Cancel();
                cancelationSource.Dispose();
                deferred.Reject(expectedRejectionValue);
                TestHelper.ExecuteForegroundCallbacks();
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void PromiseAll_UncaughtRejectionIsSentToUncaughtRejectionHandler_void(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                // Promise.All is only rejected with the first rejection, any rejections after that are reported as uncaught.

                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var deferred1 = Promise.NewDeferred();
                var deferred2 = Promise.NewDeferred();
                var preservedPromise1 = deferred1.Promise.Preserve();
                var preservedPromise2 = deferred2.Promise.Preserve();

                // Subtract 1 because the other testable promises suppress the preserved promise's rejection.
                --expectedCount;
                foreach (var promise2 in TestHelper.GetTestablePromises(preservedPromise2))
                {
                    ++expectedCount;
                    Promise.All(preservedPromise1, promise2)
                        .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                        .Forget();
                }

                // Run it again with a freshly preserved promise that isn't suppressed.
                ++expectedCount;
                var secondPreservedPromise2 = preservedPromise2.Preserve();
                preservedPromise2.Forget();

                Promise.All(preservedPromise1, secondPreservedPromise2)
                    .Catch(() => { })
                    .Forget();

                preservedPromise1.Forget();
                secondPreservedPromise2.Forget();
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void PromiseAll_UncaughtRejectionIsSentToUncaughtRejectionHandler_T(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                // Promise.All is only rejected with the first rejection, any rejections after that are reported as uncaught.

                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var deferred1 = Promise.NewDeferred<int>();
                var deferred2 = Promise.NewDeferred<int>();
                var preservedPromise1 = deferred1.Promise.Preserve();
                var preservedPromise2 = deferred2.Promise.Preserve();

                // Subtract 1 because the other testable promises suppress the preserved promise's rejection.
                --expectedCount;
                foreach (var promise2 in TestHelper.GetTestablePromises(preservedPromise2))
                {
                    ++expectedCount;
                    Promise<int>.All(preservedPromise1, promise2)
                        .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                        .Forget();
                }

                // Run it again with a freshly preserved promise that isn't suppressed.
                ++expectedCount;
                var secondPreservedPromise2 = preservedPromise2.Preserve();
                preservedPromise2.Forget();

                Promise<int>.All(preservedPromise1, secondPreservedPromise2)
                    .Catch(() => { })
                    .Forget();

                preservedPromise1.Forget();
                secondPreservedPromise2.Forget();
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void PromiseMerge_UncaughtRejectionIsSentToUncaughtRejectionHandler(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                // Promise.Merge is only rejected with the first rejection, any rejections after that are reported as uncaught.

                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var deferred1 = Promise.NewDeferred<int>();
                var deferred2 = Promise.NewDeferred<string>();
                var preservedPromise1 = deferred1.Promise.Preserve();
                var preservedPromise2 = deferred2.Promise.Preserve();

                // Subtract 1 because the other testable promises suppress the preserved promise's rejection.
                --expectedCount;
                foreach (var promise2 in TestHelper.GetTestablePromises(preservedPromise2))
                {
                    ++expectedCount;
                    Promise.Merge(preservedPromise1, promise2)
                        .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                        .Forget();
                }

                // Run it again with a freshly preserved promise that isn't suppressed.
                ++expectedCount;
                var secondPreservedPromise2 = preservedPromise2.Preserve();
                preservedPromise2.Forget();

                Promise.Merge(preservedPromise1, secondPreservedPromise2)
                    .Catch(() => { })
                    .Forget();

                preservedPromise1.Forget();
                secondPreservedPromise2.Forget();
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void PromiseRace_UncaughtRejectionIsSentToUncaughtRejectionHandler_void(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                // Promise.Race is only rejected with the first rejection, any rejections after that are reported as uncaught.

                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var deferred1 = Promise.NewDeferred();
                var deferred2 = Promise.NewDeferred();
                var preservedPromise1 = deferred1.Promise.Preserve();
                var preservedPromise2 = deferred2.Promise.Preserve();

                // Subtract 1 because the other testable promises suppress the preserved promise's rejection.
                --expectedCount;
                foreach (var promise2 in TestHelper.GetTestablePromises(preservedPromise2))
                {
                    ++expectedCount;
                    Promise.Race(preservedPromise1, promise2)
                        .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                        .Forget();
                }

                // Run it again with a freshly preserved promise that isn't suppressed.
                ++expectedCount;
                var secondPreservedPromise2 = preservedPromise2.Preserve();
                preservedPromise2.Forget();

                Promise.Race(preservedPromise1, secondPreservedPromise2)
                    .Catch(() => { })
                    .Forget();

                preservedPromise1.Forget();
                secondPreservedPromise2.Forget();
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void PromiseRace_UncaughtRejectionIsSentToUncaughtRejectionHandler_T(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                // Promise.Race is only rejected with the first rejection, any rejections after that are reported as uncaught.

                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var deferred1 = Promise.NewDeferred<int>();
                var deferred2 = Promise.NewDeferred<int>();
                var preservedPromise1 = deferred1.Promise.Preserve();
                var preservedPromise2 = deferred2.Promise.Preserve();

                // Subtract 1 because the other testable promises suppress the preserved promise's rejection.
                --expectedCount;
                foreach (var promise2 in TestHelper.GetTestablePromises(preservedPromise2))
                {
                    ++expectedCount;
                    Promise<int>.Race(preservedPromise1, promise2)
                        .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                        .Forget();
                }

                // Run it again with a freshly preserved promise that isn't suppressed.
                ++expectedCount;
                var secondPreservedPromise2 = preservedPromise2.Preserve();
                preservedPromise2.Forget();

                Promise<int>.Race(preservedPromise1, secondPreservedPromise2)
                    .Catch(() => { })
                    .Forget();

                preservedPromise1.Forget();
                secondPreservedPromise2.Forget();
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void PromiseRaceWithIndex_UncaughtRejectionIsSentToUncaughtRejectionHandler_void(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                // Promise.RaceWithIndex is only rejected with the first rejection, any rejections after that are reported as uncaught.

                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var deferred1 = Promise.NewDeferred();
                var deferred2 = Promise.NewDeferred();
                var preservedPromise1 = deferred1.Promise.Preserve();
                var preservedPromise2 = deferred2.Promise.Preserve();

                // Subtract 1 because the other testable promises suppress the preserved promise's rejection.
                --expectedCount;
                foreach (var promise2 in TestHelper.GetTestablePromises(preservedPromise2))
                {
                    ++expectedCount;
                    Promise.RaceWithIndex(preservedPromise1, promise2)
                        .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                        .Forget();
                }

                // Run it again with a freshly preserved promise that isn't suppressed.
                ++expectedCount;
                var secondPreservedPromise2 = preservedPromise2.Preserve();
                preservedPromise2.Forget();

                Promise.RaceWithIndex(preservedPromise1, secondPreservedPromise2)
                    .Catch(() => { })
                    .Forget();

                preservedPromise1.Forget();
                secondPreservedPromise2.Forget();
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void PromiseRaceWithIndex_UncaughtRejectionIsSentToUncaughtRejectionHandler_T(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                // Promise.RaceWithIndex is only rejected with the first rejection, any rejections after that are reported as uncaught.

                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var deferred1 = Promise.NewDeferred<int>();
                var deferred2 = Promise.NewDeferred<int>();
                var preservedPromise1 = deferred1.Promise.Preserve();
                var preservedPromise2 = deferred2.Promise.Preserve();

                // Subtract 1 because the other testable promises suppress the preserved promise's rejection.
                --expectedCount;
                foreach (var promise2 in TestHelper.GetTestablePromises(preservedPromise2))
                {
                    ++expectedCount;
                    Promise.RaceWithIndex(preservedPromise1, promise2)
                        .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                        .Forget();
                }

                // Run it again with a freshly preserved promise that isn't suppressed.
                ++expectedCount;
                var secondPreservedPromise2 = preservedPromise2.Preserve();
                preservedPromise2.Forget();

                Promise.RaceWithIndex(preservedPromise1, secondPreservedPromise2)
                    .Catch(() => { })
                    .Forget();

                preservedPromise1.Forget();
                secondPreservedPromise2.Forget();
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void PromiseFirst_UncaughtRejectionIsSuppressed_void(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                // Promise.First is only rejected with the first rejection, any rejections after that are suppressed.

                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    ++uncaughtCount;
                    Assert.Fail("Promise.First uncaught rejection was reported as unhandled. This should not happen.");
                };

                var deferred1 = Promise.NewDeferred();
                var deferred2 = Promise.NewDeferred();
                var preservedPromise1 = deferred1.Promise.Preserve();
                var preservedPromise2 = deferred2.Promise.Preserve();

                // Subtract 1 because the other testable promises suppress the preserved promise's rejection.
                foreach (var promise2 in TestHelper.GetTestablePromises(preservedPromise2))
                {
                    Promise.First(preservedPromise1, promise2)
                        .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                        .Forget();
                }

                // Run it again with a freshly preserved promise that isn't suppressed.
                var secondPreservedPromise2 = preservedPromise2.Preserve();
                preservedPromise2.Forget();

                Promise.First(preservedPromise1, secondPreservedPromise2)
                    .Catch(() => { })
                    .Forget();

                preservedPromise1.Forget();
                secondPreservedPromise2.Forget();
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(0, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void PromiseFirst_UncaughtRejectionIsSuppressed_T(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                // Promise.First is only rejected with the first rejection, any rejections after that are suppressed.

                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    ++uncaughtCount;
                    Assert.Fail("Promise.First uncaught rejection was reported as unhandled. This should not happen.");
                };

                var deferred1 = Promise.NewDeferred<int>();
                var deferred2 = Promise.NewDeferred<int>();
                var preservedPromise1 = deferred1.Promise.Preserve();
                var preservedPromise2 = deferred2.Promise.Preserve();

                // Subtract 1 because the other testable promises suppress the preserved promise's rejection.
                foreach (var promise2 in TestHelper.GetTestablePromises(preservedPromise2))
                {
                    Promise<int>.First(preservedPromise1, promise2)
                        .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                        .Forget();
                }

                // Run it again with a freshly preserved promise that isn't suppressed.
                var secondPreservedPromise2 = preservedPromise2.Preserve();
                preservedPromise2.Forget();

                Promise<int>.First(preservedPromise1, secondPreservedPromise2)
                    .Catch(() => { })
                    .Forget();

                preservedPromise1.Forget();
                secondPreservedPromise2.Forget();
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(0, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void PromiseFirstWithIndex_UncaughtRejectionIsSuppressed_void(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                // Promise.FirstWithIndex is only rejected with the first rejection, any rejections after that are suppressed.

                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    ++uncaughtCount;
                    Assert.Fail("Promise.FirstWithIndex uncaught rejection was reported as unhandled. This should not happen.");
                };

                var deferred1 = Promise.NewDeferred();
                var deferred2 = Promise.NewDeferred();
                var preservedPromise1 = deferred1.Promise.Preserve();
                var preservedPromise2 = deferred2.Promise.Preserve();

                // Subtract 1 because the other testable promises suppress the preserved promise's rejection.
                foreach (var promise2 in TestHelper.GetTestablePromises(preservedPromise2))
                {
                    Promise.FirstWithIndex(preservedPromise1, promise2)
                        .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                        .Forget();
                }

                // Run it again with a freshly preserved promise that isn't suppressed.
                var secondPreservedPromise2 = preservedPromise2.Preserve();
                preservedPromise2.Forget();

                Promise.FirstWithIndex(preservedPromise1, secondPreservedPromise2)
                    .Catch(() => { })
                    .Forget();

                preservedPromise1.Forget();
                secondPreservedPromise2.Forget();
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(0, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void PromiseFirstWithIndex_UncaughtRejectionIsSuppressed_T(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                // Promise.FirstWithIndex is only rejected with the first rejection, any rejections after that are suppressed.

                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    ++uncaughtCount;
                    Assert.Fail("Promise.FirstWithIndex uncaught rejection was reported as unhandled. This should not happen.");
                };

                var deferred1 = Promise.NewDeferred<int>();
                var deferred2 = Promise.NewDeferred<int>();
                var preservedPromise1 = deferred1.Promise.Preserve();
                var preservedPromise2 = deferred2.Promise.Preserve();

                // Subtract 1 because the other testable promises suppress the preserved promise's rejection.
                foreach (var promise2 in TestHelper.GetTestablePromises(preservedPromise2))
                {
                    Promise.FirstWithIndex(preservedPromise1, promise2)
                        .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                        .Forget();
                }

                // Run it again with a freshly preserved promise that isn't suppressed.
                var secondPreservedPromise2 = preservedPromise2.Preserve();
                preservedPromise2.Forget();

                Promise.FirstWithIndex(preservedPromise1, secondPreservedPromise2)
                    .Catch(() => { })
                    .Forget();

                preservedPromise1.Forget();
                secondPreservedPromise2.Forget();
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(0, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

#if !UNITY_WEBGL
        private static IEnumerable<TestCaseData> GetExpectedRejectionsAndTimeout()
        {
            object[] rejections = new object[4]
            {
                null,
                new object(),
                new System.InvalidOperationException(),
                42
            };
            int[] timeouts = new int[2] { 0, 1 };

            foreach (var rejection in rejections)
                foreach (var timeout in timeouts)
                {
                    yield return new TestCaseData(rejection, timeout);
                }
        }

        [Test, TestCaseSource("GetExpectedRejectionsAndTimeout")]
        public void PromiseWait_UncaughtRejectionIsSentToUncaughtRejectionHandler(object expectedRejectionValue, int timeout)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var deferred = Promise.NewDeferred();
                var preservedPromise = deferred.Promise.Preserve();

                foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
                {
                    ++expectedCount;
                    Assert.IsFalse(promise.Wait(System.TimeSpan.FromMilliseconds(timeout)));
                }

                // Run it again with a freshly preserved promise, because the initial promise will have had its rejection suppressed by the other promises.
                var secondPreservedPromise = preservedPromise.Preserve();
                preservedPromise.Forget();
                Assert.IsFalse(secondPreservedPromise.Wait(System.TimeSpan.FromMilliseconds(timeout)));

                secondPreservedPromise.Forget();
                deferred.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejectionsAndTimeout")]
        public void PromiseWaitForResult_UncaughtRejectionIsSentToUncaughtRejectionHandler(object expectedRejectionValue, int timeout)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var deferred = Promise.NewDeferred<int>();
                var preservedPromise = deferred.Promise.Preserve();

                int outResult;
                foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
                {
                    ++expectedCount;
                    Assert.IsFalse(promise.WaitForResult(System.TimeSpan.FromMilliseconds(timeout), out outResult));
                }

                // Run it again with a freshly preserved promise, because the initial promise will have had its rejection suppressed by the other promises.
                var secondPreservedPromise = preservedPromise.Preserve();
                preservedPromise.Forget();
                Assert.IsFalse(secondPreservedPromise.WaitForResult(System.TimeSpan.FromMilliseconds(timeout), out outResult));

                secondPreservedPromise.Forget();
                deferred.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }
#endif // !UNITY_WEBGL
    }
}