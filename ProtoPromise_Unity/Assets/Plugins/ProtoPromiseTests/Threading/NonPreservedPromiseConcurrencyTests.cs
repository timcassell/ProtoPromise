#if CSHARP_7_3_OR_NEWER && !UNITY_WEBGL

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Proto.Promises.Tests.Threading
{
    public class NonPreservedPromiseConcurrencyTests
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
        public void PromiseWithReferenceBacking_ForgetMayOnlyBeCalledOnce_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise;

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        promise.Forget();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );

            deferred.Resolve();
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void PromiseWithReferenceBacking_ForgetMayOnlyBeCalledOnce_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise;

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        promise.Forget();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );

            deferred.Resolve(1);
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void PromiseWithReferenceBacking_DuplicateMayOnlyBeCalledOnce_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise;

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        promise.Duplicate().Forget();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );

            deferred.Resolve();
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void PromiseWithReferenceBacking_DuplicateMayOnlyBeCalledOnce_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise;

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        promise.Duplicate().Forget();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );

            deferred.Resolve(1);
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void PromiseWithReferenceBacking_PreserveMayOnlyBeCalledOnce_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise;

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        promise.Preserve().Forget();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );

            deferred.Resolve();
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void PromiseWithReferenceBacking_PreserveMayOnlyBeCalledOnce_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise;

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        promise.Preserve().Forget();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );

            deferred.Resolve(1);
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void PromiseWithReferenceBacking_ThenMayOnlyBeCalledOnce_void()
        {
            var actions = new List<Action<Promise>>(TestHelper.ResolveActionsVoid(() => { }));
            actions.AddRange(TestHelper.ThenActionsVoid(() => { }, null));
            var threadHelper = new ThreadHelper();
            foreach (var action in actions)
            {
                int successCount = 0, invalidCount = 0;
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise;

                threadHelper.ExecuteMultiActionParallel(
                    () =>
                    {
                        try
                        {
                            action(promise);
                            Interlocked.Increment(ref successCount);
                        }
                        catch (InvalidOperationException)
                        {
                            Interlocked.Increment(ref invalidCount);
                        }
                    }
                );

                deferred.Resolve();

                Assert.AreEqual(1, successCount);
                Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
            }
        }

        [Test]
        public void PromiseWithReferenceBacking_ThenMayOnlyBeCalledOnce_T()
        {
            var actions = new List<Action<Promise<int>>>(TestHelper.ResolveActions<int>(v => { }));
            actions.AddRange(TestHelper.ThenActions<int>(v => { }, null));
            var threadHelper = new ThreadHelper();
            foreach (var action in actions)
            {
                int successCount = 0, invalidCount = 0;
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise;

                threadHelper.ExecuteMultiActionParallel(
                    () =>
                    {
                        try
                        {
                            action(promise);
                            Interlocked.Increment(ref successCount);
                        }
                        catch (InvalidOperationException)
                        {
                            Interlocked.Increment(ref invalidCount);
                        }
                    }
                );

                deferred.Resolve(1);

                Assert.AreEqual(1, successCount);
                Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
            }
        }

        [Test]
        public void PromiseWithReferenceBacking_CatchMayOnlyBeCalledOnce_void()
        {
            var actions = TestHelper.CatchActionsVoid(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in actions)
            {
                int successCount = 0, invalidCount = 0;
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise;

                threadHelper.ExecuteMultiActionParallel(
                    () =>
                    {
                        try
                        {
                            action(promise);
                            Interlocked.Increment(ref successCount);
                        }
                        catch (InvalidOperationException)
                        {
                            Interlocked.Increment(ref invalidCount);
                        }
                    }
                );

                deferred.Resolve();

                Assert.AreEqual(1, successCount);
                Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
            }
        }

        [Test]
        public void PromiseWithReferenceBacking_CatchMayOnlyBeCalledOnce_T()
        {
            var actions = TestHelper.CatchActions<int>(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in actions)
            {
                int successCount = 0, invalidCount = 0;
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise;

                threadHelper.ExecuteMultiActionParallel(
                    () =>
                    {
                        try
                        {
                            action(promise);
                            Interlocked.Increment(ref successCount);
                        }
                        catch (InvalidOperationException)
                        {
                            Interlocked.Increment(ref invalidCount);
                        }
                    }
                );

                deferred.Resolve(1);

                Assert.AreEqual(1, successCount);
                Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
            }
        }

        [Test]
        public void PromiseWithReferenceBacking_ContinueWithMayOnlyBeCalledOnce_void()
        {
            var actions = TestHelper.ContinueWithActionsVoid(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in actions)
            {
                int successCount = 0, invalidCount = 0;
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise;

                threadHelper.ExecuteMultiActionParallel(
                    () =>
                    {
                        try
                        {
                            action(promise);
                            Interlocked.Increment(ref successCount);
                        }
                        catch (InvalidOperationException)
                        {
                            Interlocked.Increment(ref invalidCount);
                        }
                    }
                );

                deferred.Resolve();

                Assert.AreEqual(1, successCount);
                Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
            }
        }

        [Test]
        public void PromiseWithReferenceBacking_ContinueWithMayOnlyBeCalledOnce_T()
        {
            var actions = TestHelper.ContinueWithActions<int>(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in actions)
            {
                int successCount = 0, invalidCount = 0;
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise;

                threadHelper.ExecuteMultiActionParallel(
                    () =>
                    {
                        try
                        {
                            action(promise);
                            Interlocked.Increment(ref successCount);
                        }
                        catch (InvalidOperationException)
                        {
                            Interlocked.Increment(ref invalidCount);
                        }
                    }
                );

                deferred.Resolve(1);

                Assert.AreEqual(1, successCount);
                Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
            }
        }

        [Test]
        public void PromiseWithReferenceBackingMayOnlyBeAwaitedOnce_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise;

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            await promise;
                            Interlocked.Increment(ref successCount);
                        }
                        catch (InvalidOperationException)
                        {
                            Interlocked.Increment(ref invalidCount);
                        }
                    }
                }
            );

            deferred.Resolve();

            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void PromiseWithReferenceBackingMayOnlyBeAwaitedOnce_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise;

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            await promise;
                            Interlocked.Increment(ref successCount);
                        }
                        catch (InvalidOperationException)
                        {
                            Interlocked.Increment(ref invalidCount);
                        }
                    }
                }
            );

            deferred.Resolve(1);

            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }
    }
}

#endif