#if !UNITY_WEBGL

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Linq;
using System.Threading;

namespace ProtoPromise.Tests.Concurrency
{
    public class ApiWithCancelationTokenConcurrencyTests
    {
        const int rejectValue = 1;

        [SetUp]
        public void Setup()
        {
            // When a callback is canceled and the previous promise is rejected, the rejection is unhandled.
            // So we set the expected uncaught reject value.
            TestHelper.s_expectedUncaughtRejectValue = rejectValue;

            TestHelper.Setup();
        }

        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();

            TestHelper.s_expectedUncaughtRejectValue = null;
        }

        [Test]
        public void Then_PromiseMayBeResolvedAndCallbackCanceledConcurrently_void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            bool completed = false;

            var resolveActions = TestHelper.ResolveActionsVoidWithCancelation(() => { });
            var thenActions = TestHelper.ThenActionsVoidWithCancelation(() => { }, null);
            var continueActions = TestHelper.ContinueWithActionsVoidWithCancelation(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in resolveActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred();
                        completed = false;
                        action(deferred.Promise, cancelationSource.Token)
                            .Finally(() => completed = true) // State of the promise is indeterminable, just make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Resolve()
                );
            }
        }

        [Test]
        public void Then_PromiseMayBeResolvedAndCallbackCanceledConcurrently_T()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            bool completed = false;

            var resolveActions = TestHelper.ResolveActionsWithCancelation<int>(v => { });
            var thenActions = TestHelper.ThenActionsWithCancelation<int>(v => { }, null);
            var continueActions = TestHelper.ContinueWithActionsWithCancelation<int>(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in resolveActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred<int>();
                        completed = false;
                        action(deferred.Promise, cancelationSource.Token)
                            .Finally(() => completed = true) // State of the promise is indeterminable, just make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Resolve(1)
                );
            }
        }

        [Test]
        public void Catch_PromiseMayBeRejectedAndCallbackCanceledConcurrently_void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            bool completed = false;

            var catchActions = TestHelper.CatchActionsVoidWithCancelation(() => { });
            var thenActions = TestHelper.ThenActionsVoidWithCancelation(null, () => { });
            var continueActions = TestHelper.ContinueWithActionsVoidWithCancelation(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in catchActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred();
                        completed = false;
                        action(deferred.Promise, cancelationSource.Token)
                            .Finally(() => completed = true) // State of the promise is indeterminable, just make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Reject(rejectValue)
                );
            }
        }

        [Test]
        public void Catch_PromiseMayBeRejectedAndCallbackCanceledConcurrently_T()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            bool completed = false;

            var catchActions = TestHelper.CatchActionsWithCancelation<int>(() => { });
            var thenActions = TestHelper.ThenActionsWithCancelation<int>(null, () => { });
            var continueActions = TestHelper.ContinueWithActionsWithCancelation<int>(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in catchActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred<int>();
                        completed = false;
                        action(deferred.Promise, cancelationSource.Token)
                            .Finally(() => completed = true) // State of the promise is indeterminable, just make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Reject(rejectValue)
                );
            }
        }

        [Test]
        public void CatchCancelation_PromiseMayBeCanceledAndCallbackCanceledConcurrently_void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            bool completed = false;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise, CancelationToken, Promise>[]
                {
                    (promise, token) => promise.WaitAsync(token).CatchCancelation(() => { }),
                    (promise, token) => promise.WaitAsync(token).CatchCancelation(1, cv => { }),
                    (promise, token) => promise.WaitAsync(token).CatchCancelation(() => Promise.Resolved()),
                    (promise, token) => promise.WaitAsync(token).CatchCancelation(1, cv => Promise.Resolved()),
                })
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred();
                        action(deferred.Promise, cancelationSource.Token)
                            .Finally(() => completed = true) // Make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Cancel()
                );
            }
        }

        [Test]
        public void CatchCancelation_PromiseMayBeCanceledAndCallbackCanceledConcurrently_T()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            bool completed = false;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise<int>, CancelationToken, Promise<int>>[]
                {
                    (promise, token) => promise.WaitAsync(token).CatchCancelation(() => 1),
                    (promise, token) => promise.WaitAsync(token).CatchCancelation(1, cv => 1),
                    (promise, token) => promise.WaitAsync(token).CatchCancelation(() => Promise.Resolved(1)),
                    (promise, token) => promise.WaitAsync(token).CatchCancelation(1, cv => Promise.Resolved(1)),
                })
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred<int>();
                        action(deferred.Promise, cancelationSource.Token)
                            .Finally(() => completed = true) // Make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Cancel()
                );
            }
        }

        [Test]
        public void Then_PromiseMayBeResolvedAndAwaitedAndCallbackCanceledConcurrently_void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise);
            bool completed = false;

            Thread foregroundThread = Thread.CurrentThread;

            var resolveActions = TestHelper.ResolveActionsVoidWithCancelation(() => { });
            var thenActions = TestHelper.ThenActionsVoidWithCancelation(() => { }, null);
            var continueActions = TestHelper.ContinueWithActionsVoidWithCancelation(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in resolveActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred();
                        cancelationToken = cancelationSource.Token;
                        promise = deferred.Promise;
                        completed = false;
                    },
                    // Teardown
                    () =>
                    {
                        TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                        Assert.IsTrue(completed);
                        cancelationSource.Dispose();
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Resolve(),
                    () => action(promise, cancelationToken)
                        // State of the promise is indeterminable, just make sure it completes.
                        .Finally(() => completed = true)
                        .Forget()
                );
            }
        }

        [Test]
        public void Then_PromiseMayBeResolvedAndAwaitedAndCallbackCanceledConcurrently_T()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise<int>);
            bool completed = false;

            Thread foregroundThread = Thread.CurrentThread;

            var resolveActions = TestHelper.ResolveActionsWithCancelation<int>(v => { });
            var thenActions = TestHelper.ThenActionsWithCancelation<int>(v => { }, null);
            var continueActions = TestHelper.ContinueWithActionsWithCancelation<int>(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in resolveActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred<int>();
                        cancelationToken = cancelationSource.Token;
                        promise = deferred.Promise;
                        completed = false;
                    },
                    // Teardown
                    () =>
                    {
                        TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                        Assert.IsTrue(completed);
                        cancelationSource.Dispose();
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Resolve(1),
                    () => action(promise, cancelationToken)
                        // State of the promise is indeterminable, just make sure it completes.
                        .Finally(() => completed = true)
                        .Forget()
                );
            }
        }

        [Test]
        public void Catch_PromiseMayBeRejectedAndAwaitedAndCallbackCanceledConcurrently_void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise);
            bool completed = false;

            Thread foregroundThread = Thread.CurrentThread;

            var catchActions = TestHelper.CatchActionsVoidWithCancelation(() => { });
            var thenActions = TestHelper.ThenActionsVoidWithCancelation(null, () => { });
            var continueActions = TestHelper.ContinueWithActionsVoidWithCancelation(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in catchActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred();
                        cancelationToken = cancelationSource.Token;
                        promise = deferred.Promise;
                        completed = false;
                    },
                    // Teardown
                    () =>
                    {
                        TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                        Assert.IsTrue(completed);
                        cancelationSource.Dispose();
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Reject(rejectValue),
                    () => action(promise, cancelationToken)
                        // State of the promise is indeterminable, just make sure it completes.
                        .Finally(() => completed = true)
                        .Forget()
                );
            }
        }

        [Test]
        public void Catch_PromiseMayBeRejectedAndAwaitedAndCallbackCanceledConcurrently_T()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise<int>);
            bool completed = false;

            Thread foregroundThread = Thread.CurrentThread;

            var catchActions = TestHelper.CatchActionsWithCancelation<int>(() => { });
            var thenActions = TestHelper.ThenActionsWithCancelation<int>(null, () => { });
            var continueActions = TestHelper.ContinueWithActionsWithCancelation<int>(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in catchActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred<int>();
                        cancelationToken = cancelationSource.Token;
                        promise = deferred.Promise;
                        completed = false;
                    },
                    // Teardown
                    () =>
                    {
                        TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                        Assert.IsTrue(completed);
                        cancelationSource.Dispose();
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Reject(rejectValue),
                    () => action(promise, cancelationToken)
                        // State of the promise is indeterminable, just make sure it completes.
                        .Finally(() => completed = true)
                        .Forget()
                );
            }
        }

        [Test]
        public void CatchCancelation_PromiseMayBeCanceledAndAwaitedAndCallbackCanceledConcurrently_void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise);
            bool completed = false;

            Thread foregroundThread = Thread.CurrentThread;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise, CancelationToken, Promise>[]
                {
                    (p, token) => p.WaitAsync(token).CatchCancelation(() => { }),
                    (p, token) => p.WaitAsync(token).CatchCancelation(1, cv => { }),
                    (p, token) => p.WaitAsync(token).CatchCancelation(() => Promise.Resolved()),
                    (p, token) => p.WaitAsync(token).CatchCancelation(1, cv => Promise.Resolved()),
                })
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred();
                        cancelationToken = cancelationSource.Token;
                        promise = deferred.Promise;
                    },
                    // Teardown
                    () =>
                    {
                        TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                        Assert.IsTrue(completed);
                        cancelationSource.Dispose();
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Cancel(),
                    () => action(promise, cancelationToken)
                        // State of the promise is indeterminable, just make sure it completes.
                        .Finally(() => completed = true)
                        .Forget()
                );
            }
        }

        [Test]
        public void CatchCancelation_PromiseMayBeCanceledAndAwaitedAndCallbackCanceledConcurrently_T()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise<int>);
            bool completed = false;

            Thread foregroundThread = Thread.CurrentThread;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise<int>, CancelationToken, Promise<int>>[]
                {
                    (p, token) => p.WaitAsync(token).CatchCancelation(() => 1),
                    (p, token) => p.WaitAsync(token).CatchCancelation(1, cv => 1),
                    (p, token) => p.WaitAsync(token).CatchCancelation(() => Promise.Resolved(1)),
                    (p, token) => p.WaitAsync(token).CatchCancelation(1, cv => Promise.Resolved(1)),
                })
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred<int>();
                        cancelationToken = cancelationSource.Token;
                        promise = deferred.Promise;
                    },
                    // Teardown
                    () =>
                    {
                        TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                        Assert.IsTrue(completed);
                        cancelationSource.Dispose();
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Cancel(),
                    () => action(promise, cancelationToken)
                        // State of the promise is indeterminable, just make sure it completes.
                        .Finally(() => completed = true)
                        .Forget()
                );
            }
        }
    }
}

#endif // !UNITY_WEBGL