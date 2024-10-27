#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using ProtoPromiseTests.Concurrency;
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

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    foreach (var callback in actions)
                    {
                        foreach (var promise in TestHelper.GetTestablePromises(promiseRetainer))
                        {
                            ++expectedCount;
                            callback.Invoke(promise);
                        }
                    }
                }

#if !PROMISE_DEBUG
                expectedCount -= 40;
#endif
                deferred.Reject(expectedRejectionValue);
                cancelationSource.Dispose();
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    foreach (var callback in actions)
                    {
                        foreach (var promise in TestHelper.GetTestablePromises(promiseRetainer))
                        {
                            ++expectedCount;
                            callback.Invoke(promise);
                        }
                    }
                }

#if !PROMISE_DEBUG
                expectedCount -= 40;
#endif
                deferred.Reject(expectedRejectionValue);
                cancelationSource.Dispose();
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        // In RELEASE mode, PromiseRetainer is used directly, and Duplicate returns itself.
        // In DEBUG mode, a duplicate promise backing reference is used for each WaitAsync.
#if PROMISE_DEBUG
        private const int expectedSubtraction = 1;
#else
        private const int expectedSubtraction = 2;
#endif

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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

                var actions = TestHelper.ResolveActionsVoidWithCancelation()
                    .Concat(TestHelper.ThenActionsVoidWithCancelation(onRejected: () => { }))
                    .Concat(TestHelper.CatchActionsVoidWithCancelation(onRejected: () => { }))
                    .Concat(TestHelper.ContinueWithActionsVoidWithCancelation(() => { }))
                    .Append((promise, token) => promise.WaitAsync(token));

                var deferred = Promise.NewDeferred();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    foreach (var callback in actions)
                    {
                        // Subtract expected because the other testable promises suppress the preserved/retained promise's rejection.
                        expectedCount -= expectedSubtraction;
                        foreach (var promise in TestHelper.GetTestablePromises(promiseRetainer))
                        {
                            ++expectedCount;
                            callback.Invoke(promise, cancelationSource.Token).Forget();
                        }
                    }
                }

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

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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

                var actions = TestHelper.ResolveActionsWithCancelation<int>()
                    .Concat(TestHelper.ThenActionsWithCancelation<int>(onRejected: () => { }))
                    .Concat(TestHelper.CatchActionsWithCancelation<int>(onRejected: () => { }))
                    .Concat(TestHelper.ContinueWithActionsWithCancelation<int>(() => { }))
                    .Append((promise, token) => promise.WaitAsync(token));

                var deferred = Promise.NewDeferred<int>();
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    foreach (var callback in actions)
                    {
                        // Subtract expected because the other testable promises suppress the preserved/retained promise's rejection.
                        expectedCount -= expectedSubtraction;
                        foreach (var promise in TestHelper.GetTestablePromises(promiseRetainer))
                        {
                            ++expectedCount;
                            callback.Invoke(promise, cancelationSource.Token).Forget();
                        }
                    }
                }
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

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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
                using (var promiseRetainer1 = deferred1.Promise.GetRetainer())
                {
                    using (var promiseRetainer2 = deferred2.Promise.GetRetainer())
                    {
                        // Subtract expected because the other testable promises suppress the preserved/retained promise's rejection.
                        expectedCount -= expectedSubtraction;
                        foreach (var promise2 in TestHelper.GetTestablePromises(promiseRetainer2))
                        {
                            ++expectedCount;
                            Promise.All(promiseRetainer1.WaitAsync(), promise2)
                                .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                                .Forget();
                        }

                        // Run it again with a freshly retained promise that isn't suppressed.
                        ++expectedCount;
                        using (var secondPromiseRetainer2 = promiseRetainer2.WaitAsync().GetRetainer())
                        {
                            Promise.All(promiseRetainer1.WaitAsync(), secondPromiseRetainer2.WaitAsync())
                                .Catch(() => { })
                                .Forget();
                        }
                    }
                }
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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
                using (var promiseRetainer1 = deferred1.Promise.GetRetainer())
                {
                    using (var promiseRetainer2 = deferred2.Promise.GetRetainer())
                    {
                        // Subtract expected because the other testable promises suppress the preserved/retained promise's rejection.
                        expectedCount -= expectedSubtraction;
                        foreach (var promise2 in TestHelper.GetTestablePromises(promiseRetainer2))
                        {
                            ++expectedCount;
                            Promise<int>.All(promiseRetainer1.WaitAsync(), promise2)
                                .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                                .Forget();
                        }

                        // Run it again with a freshly retained promise that isn't suppressed.
                        ++expectedCount;
                        using (var secondPromiseRetainer2 = promiseRetainer2.WaitAsync().GetRetainer())
                        {
                            Promise<int>.All(promiseRetainer1.WaitAsync(), secondPromiseRetainer2.WaitAsync())
                                .Catch(() => { })
                                .Forget();
                        }
                    }
                }
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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
                using (var promiseRetainer1 = deferred1.Promise.GetRetainer())
                {
                    using (var promiseRetainer2 = deferred2.Promise.GetRetainer())
                    {
                        // Subtract expected because the other testable promises suppress the preserved/retained promise's rejection.
                        expectedCount -= expectedSubtraction;
                        foreach (var promise2 in TestHelper.GetTestablePromises(promiseRetainer2))
                        {
                            ++expectedCount;
                            Promise.Merge(promiseRetainer1.WaitAsync(), promise2)
                                .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                                .Forget();
                        }

                        // Run it again with a freshly retained promise that isn't suppressed.
                        ++expectedCount;
                        using (var secondPromiseRetainer2 = promiseRetainer2.WaitAsync().GetRetainer())
                        {
                            Promise.Merge(promiseRetainer1.WaitAsync(), secondPromiseRetainer2.WaitAsync())
                                .Catch(() => { })
                                .Forget();
                        }
                    }
                }
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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
                using (var promiseRetainer1 = deferred1.Promise.GetRetainer())
                {
                    using (var promiseRetainer2 = deferred2.Promise.GetRetainer())
                    {
                        // Subtract expected because the other testable promises suppress the preserved/retained promise's rejection.
                        expectedCount -= expectedSubtraction;
                        foreach (var promise2 in TestHelper.GetTestablePromises(promiseRetainer2))
                        {
                            ++expectedCount;
                            Promise.Race(promiseRetainer1.WaitAsync(), promise2)
                                .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                                .Forget();
                        }

                        // Run it again with a freshly retained promise that isn't suppressed.
                        ++expectedCount;
                        using (var secondPromiseRetainer2 = promiseRetainer2.WaitAsync().GetRetainer())
                        {
                            Promise.Race(promiseRetainer1.WaitAsync(), secondPromiseRetainer2.WaitAsync())
                                .Catch(() => { })
                                .Forget();
                        }
                    }
                }
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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
                using (var promiseRetainer1 = deferred1.Promise.GetRetainer())
                {
                    using (var promiseRetainer2 = deferred2.Promise.GetRetainer())
                    {
                        // Subtract expected because the other testable promises suppress the preserved/retained promise's rejection.
                        expectedCount -= expectedSubtraction;
                        foreach (var promise2 in TestHelper.GetTestablePromises(promiseRetainer2))
                        {
                            ++expectedCount;
                            Promise<int>.Race(promiseRetainer1.WaitAsync(), promise2)
                                .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                                .Forget();
                        }

                        // Run it again with a freshly retained promise that isn't suppressed.
                        ++expectedCount;
                        using (var secondPromiseRetainer2 = promiseRetainer2.WaitAsync().GetRetainer())
                        {
                            Promise<int>.Race(promiseRetainer1.WaitAsync(), secondPromiseRetainer2.WaitAsync())
                                .Catch(() => { })
                                .Forget();
                        }
                    }
                }
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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
                using (var promiseRetainer1 = deferred1.Promise.GetRetainer())
                {
                    using (var promiseRetainer2 = deferred2.Promise.GetRetainer())
                    {
                        // Subtract expected because the other testable promises suppress the preserved/retained promise's rejection.
                        expectedCount -= expectedSubtraction;
                        foreach (var promise2 in TestHelper.GetTestablePromises(promiseRetainer2))
                        {
                            ++expectedCount;
                            Promise.RaceWithIndex(promiseRetainer1.WaitAsync(), promise2)
                                .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                                .Forget();
                        }

                        // Run it again with a freshly retained promise that isn't suppressed.
                        ++expectedCount;
                        using (var secondPromiseRetainer2 = promiseRetainer2.WaitAsync().GetRetainer())
                        {
                            Promise.RaceWithIndex(promiseRetainer1.WaitAsync(), secondPromiseRetainer2.WaitAsync())
                                .Catch(() => { })
                                .Forget();
                        }
                    }
                }
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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
                using (var promiseRetainer1 = deferred1.Promise.GetRetainer())
                {
                    using (var promiseRetainer2 = deferred2.Promise.GetRetainer())
                    {
                        // Subtract expected because the other testable promises suppress the preserved/retained promise's rejection.
                        expectedCount -= expectedSubtraction;
                        foreach (var promise2 in TestHelper.GetTestablePromises(promiseRetainer2))
                        {
                            ++expectedCount;
                            Promise.RaceWithIndex(promiseRetainer1.WaitAsync(), promise2)
                                .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                                .Forget();
                        }

                        // Run it again with a freshly retained promise that isn't suppressed.
                        ++expectedCount;
                        using (var secondPromiseRetainer2 = promiseRetainer2.WaitAsync().GetRetainer())
                        {
                            Promise.RaceWithIndex(promiseRetainer1.WaitAsync(), secondPromiseRetainer2.WaitAsync())
                                .Catch(() => { })
                                .Forget();
                        }
                    }
                }
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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
                using (var promiseRetainer1 = deferred1.Promise.GetRetainer())
                {
                    using (var promiseRetainer2 = deferred2.Promise.GetRetainer())
                    {
                        foreach (var promise2 in TestHelper.GetTestablePromises(promiseRetainer2))
                        {
                            Promise.First(promiseRetainer1.WaitAsync(), promise2)
                                .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                                .Forget();
                        }

                        // Run it again with a freshly retained promise that isn't suppressed.
                        using (var secondPromiseRetainer2 = promiseRetainer2.WaitAsync().GetRetainer())
                        {
                            Promise.First(promiseRetainer1.WaitAsync(), secondPromiseRetainer2.WaitAsync())
                                .Catch(() => { })
                                .Forget();
                        }
                    }
                }
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(0, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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
                using (var promiseRetainer1 = deferred1.Promise.GetRetainer())
                {
                    using (var promiseRetainer2 = deferred2.Promise.GetRetainer())
                    {
                        foreach (var promise2 in TestHelper.GetTestablePromises(promiseRetainer2))
                        {
                            Promise<int>.First(promiseRetainer1.WaitAsync(), promise2)
                                .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                                .Forget();
                        }

                        // Run it again with a freshly retained promise that isn't suppressed.
                        using (var secondPromiseRetainer2 = promiseRetainer2.WaitAsync().GetRetainer())
                        {
                            Promise<int>.First(promiseRetainer1.WaitAsync(), secondPromiseRetainer2.WaitAsync())
                                .Catch(() => { })
                                .Forget();
                        }
                    }
                }
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(0, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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
                using (var promiseRetainer1 = deferred1.Promise.GetRetainer())
                {
                    using (var promiseRetainer2 = deferred2.Promise.GetRetainer())
                    {
                        foreach (var promise2 in TestHelper.GetTestablePromises(promiseRetainer2))
                        {
                            Promise.FirstWithIndex(promiseRetainer1.WaitAsync(), promise2)
                                .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                                .Forget();
                        }

                        // Run it again with a freshly retained promise that isn't suppressed.
                        using (var secondPromiseRetainer2 = promiseRetainer2.WaitAsync().GetRetainer())
                        {
                            Promise.FirstWithIndex(promiseRetainer1.WaitAsync(), secondPromiseRetainer2.WaitAsync())
                                .Catch(() => { })
                                .Forget();
                        }
                    }
                }
                deferred1.Reject(expectedRejectionValue);
                deferred2.Reject(expectedRejectionValue);
                Assert.AreEqual(0, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource(nameof(GetExpectedRejections))]
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
                using (var promiseRetainer1 = deferred1.Promise.GetRetainer())
                {
                    using (var promiseRetainer2 = deferred2.Promise.GetRetainer())
                    {
                        foreach (var promise2 in TestHelper.GetTestablePromises(promiseRetainer2))
                        {
                            Promise.FirstWithIndex(promiseRetainer1.WaitAsync(), promise2)
                                .Catch(() => { }) // We catch the first rejection, the second will be reported as uncaught.
                                .Forget();
                        }

                        // Run it again with a freshly retained promise that isn't suppressed.
                        using (var secondPromiseRetainer2 = promiseRetainer2.WaitAsync().GetRetainer())
                        {
                            Promise.FirstWithIndex(promiseRetainer1.WaitAsync(), secondPromiseRetainer2.WaitAsync())
                                .Catch(() => { })
                                .Forget();
                        }
                    }
                }
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

        [Test, TestCaseSource(nameof(GetExpectedRejectionsAndTimeout))]
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
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    foreach (var promise in TestHelper.GetTestablePromises(promiseRetainer))
                    {
                        ++expectedCount;
                        Assert.IsFalse(promise.TryWait(System.TimeSpan.FromMilliseconds(timeout)));
                    }

#if !PROMISE_DEBUG
                    // Run it again with a freshly retained promise, because the initial promise will have had its rejection suppressed by the other promises.
                    using (var secondPromiseRetainer = promiseRetainer.WaitAsync().GetRetainer())
                    {
                        Assert.IsFalse(secondPromiseRetainer.WaitAsync().TryWait(System.TimeSpan.FromMilliseconds(timeout)));
                    }
#endif

                    // Run it again with a freshly preserved promise, because the preserved testabled promise will have had its rejection suppressed by the other promises.
#pragma warning disable CS0618 // Type or member is obsolete
                    var secondPreservedPromise = promiseRetainer.WaitAsync().Preserve();
#pragma warning restore CS0618 // Type or member is obsolete
                    Assert.IsFalse(secondPreservedPromise.TryWait(System.TimeSpan.FromMilliseconds(timeout)));
                    secondPreservedPromise.Forget();
                }
                deferred.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource(nameof(GetExpectedRejectionsAndTimeout))]
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
                using (var promiseRetainer = deferred.Promise.GetRetainer())
                {
                    foreach (var promise in TestHelper.GetTestablePromises(promiseRetainer))
                    {
                        ++expectedCount;
                        Assert.IsFalse(promise.TryWaitForResult(System.TimeSpan.FromMilliseconds(timeout), out _));
                    }

#if !PROMISE_DEBUG
                    // Run it again with a freshly retained promise, because the retained promise will have had its rejection suppressed by the other promises.
                    using (var secondPromiseRetainer = promiseRetainer.WaitAsync().GetRetainer())
                    {
                        Assert.IsFalse(secondPromiseRetainer.WaitAsync().TryWaitForResult(System.TimeSpan.FromMilliseconds(timeout), out _));
                    }
#endif

                    // Run it again with a freshly preserved promise, because the preserved testabled promise will have had its rejection suppressed by the other promises.
#pragma warning disable CS0618 // Type or member is obsolete
                    var secondPreservedPromise = promiseRetainer.WaitAsync().Preserve();
#pragma warning restore CS0618 // Type or member is obsolete
                    Assert.IsFalse(secondPreservedPromise.TryWaitForResult(System.TimeSpan.FromMilliseconds(timeout), out _));
                    secondPreservedPromise.Forget();
                }
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