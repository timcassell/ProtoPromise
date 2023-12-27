#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if CSHARP_7_3_OR_NEWER

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

#pragma warning disable IDE0062 // Make local function 'static'
#pragma warning disable CS0618 // Type or member is obsolete

using NUnit.Framework;
using Proto.Promises;
using System;

namespace ProtoPromiseTests.APIs
{
    public class AsyncFunctionTests
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
        public void AsyncPromiseIsResolvedFromPromise()
        {
            var deferred = Promise.NewDeferred();

            bool resolved = false;

            async Promise Func()
            {
                await deferred.Promise;
            }

            Func()
                .Then(() => resolved = true)
                .Forget();

            Assert.IsFalse(resolved);
            deferred.Resolve();
            Assert.IsTrue(resolved);
        }

        [Test]
        public void AsyncPromiseIsResolvedFromPromiseWithValue()
        {
            var deferred = Promise.NewDeferred<int>();

            int expected = 50;
            bool resolved = false;

            async Promise<int> Func()
            {
                return await deferred.Promise;
            }

            Func()
                .Then(value =>
                {
                    Assert.AreEqual(expected, value);
                    resolved = true;
                })
                .Forget();

            Assert.IsFalse(resolved);
            deferred.Resolve(expected);
            Assert.IsTrue(resolved);
        }

        [Test]
        public void AsyncPromiseIsResolvedWithoutAwait()
        {
            bool resolved = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                return;
            }

            Func()
                .Then(() => resolved = true)
                .Forget();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void AsyncPromiseIsResolvedWithValueWithoutAwait()
        {
            int expected = 50;
            bool resolved = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise<int> Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                return expected;
            }

            Func()
                .Then(value =>
                {
                    Assert.AreEqual(expected, value);
                    resolved = true;
                })
                .Forget();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void AsyncPromiseIsRejectedFromPromise1()
        {
            var deferred = Promise.NewDeferred();

            NullReferenceException expected = new NullReferenceException();
            bool rejected = false;

            async Promise Func()
            {
                await deferred.Promise;
            }

            Func()
                .Catch((NullReferenceException e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();

            Assert.IsFalse(rejected);
            deferred.Reject(expected);
            Assert.IsTrue(rejected);
        }

        [Test]
        public void AsyncPromiseIsRejectedFromPromise2()
        {
            var deferred = Promise.NewDeferred<int>();

            NullReferenceException expected = new NullReferenceException();
            bool rejected = false;

            async Promise<int> Func()
            {
                return await deferred.Promise;
            }

            Func()
                .Catch((NullReferenceException e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();

            Assert.IsFalse(rejected);
            deferred.Reject(expected);
            Assert.IsTrue(rejected);
        }

        [Test]
        public void AsyncPromiseIsRejectedFromThrow1()
        {
            NullReferenceException expected = new NullReferenceException();
            bool rejected = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                throw expected;
            }

            Func()
                .Catch((NullReferenceException e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();

            Assert.IsTrue(rejected);
        }

        [Test]
        public void AsyncPromiseIsRejectedFromThrow2()
        {
            NullReferenceException expected = new NullReferenceException();
            bool rejected = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise<int> Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                throw expected;
            }

            Func()
                .Catch((NullReferenceException e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();

            Assert.IsTrue(rejected);
        }

        [Test]
        public void AsyncPromiseIsRejectedFromThrow3()
        {
            NullReferenceException expected = new NullReferenceException();
            bool rejected = false;

            async Promise Func()
            {
                await Promise.Resolved();
                throw expected;
            }

            Func()
                .Catch((NullReferenceException e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();

            Assert.IsTrue(rejected);
        }

        [Test]
        public void AsyncPromiseIsRejectedFromThrow4()
        {
            NullReferenceException expected = new NullReferenceException();
            bool rejected = false;

            async Promise<int> Func()
            {
                await Promise.Resolved();
                throw expected;
            }

            Func()
                .Catch((NullReferenceException e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();

            Assert.IsTrue(rejected);
        }

        [Test]
        public void AsyncPromiseIsCanceledFromPromise1()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred);

            //System.Diagnostics.Debugger.Launch();
            bool canceled = false;

            async Promise Func()
            {
                await deferred.Promise;
            }

            Func()
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            Assert.IsFalse(canceled);
            cancelationSource.Cancel();
            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncPromiseIsCanceledFromPromise2()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred);

            bool canceled = false;

            async Promise<int> Func()
            {
                return await deferred.Promise;
            }

            Func()
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            Assert.IsFalse(canceled);
            cancelationSource.Cancel();
            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow1()
        {
            bool canceled = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                throw new OperationCanceledException();
            }

            Func()
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            Assert.IsTrue(canceled);
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow2()
        {
            bool canceled = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise<int> Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                throw new OperationCanceledException();
            }

            Func()
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            Assert.IsTrue(canceled);
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow3()
        {
            bool canceled = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                throw Promise.CancelException();
            }

            Func()
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            Assert.IsTrue(canceled);
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow4()
        {
            bool canceled = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise<int> Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                throw Promise.CancelException();
            }

            Func()
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            Assert.IsTrue(canceled);
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow5()
        {
            bool canceled = false;

            async Promise Func()
            {
                await Promise.Resolved();
                throw new OperationCanceledException();
            }

            Func()
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            Assert.IsTrue(canceled);
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow6()
        {
            bool canceled = false;

            async Promise<int> Func()
            {
                await Promise.Resolved();
                throw new OperationCanceledException();
            }

            Func()
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            Assert.IsTrue(canceled);
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow7()
        {
            bool canceled = false;

            async Promise Func()
            {
                await Promise.Resolved();
                throw Promise.CancelException();
            }

            Func()
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            Assert.IsTrue(canceled);
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow8()
        {
            bool canceled = false;

            async Promise<int> Func()
            {
                await Promise.Resolved();
                throw Promise.CancelException();
            }

            Func()
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            Assert.IsTrue(canceled);
        }

        [Test]
        public void AsyncPromiseCanMultipleAwait_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool continued1 = false;
            bool continued2 = false;
            bool resolved = false;

            async Promise Func()
            {
                await deferred1.Promise;
                continued1 = true;
                await deferred2.Promise;
                continued2 = true;
            }

            Func()
                .Then(() => resolved = true)
                .Forget();

            Assert.IsFalse(continued1);
            Assert.IsFalse(continued2);
            Assert.IsFalse(resolved);

            deferred1.Resolve();
            Assert.IsTrue(continued1);
            Assert.IsFalse(continued2);
            Assert.IsFalse(resolved);

            deferred2.Resolve();
            Assert.IsTrue(continued1);
            Assert.IsTrue(continued2);
            Assert.IsTrue(resolved);
        }

        [Test]
        public void AsyncPromiseCanMultipleAwait_T()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred<string>();

            bool continued1 = false;
            bool continued2 = false;
            bool resolved = false;
            int expected = 50;

            async Promise<int> Func()
            {
                await deferred1.Promise;
                continued1 = true;
                var value = await deferred2.Promise;
                continued2 = true;
                return expected;
            }

            Func()
                .Then(value =>
                {
                    Assert.AreEqual(expected, value);
                    resolved = true;
                })
                .Forget();

            Assert.IsFalse(continued1);
            Assert.IsFalse(continued2);
            Assert.IsFalse(resolved);

            deferred1.Resolve();
            Assert.IsTrue(continued1);
            Assert.IsFalse(continued2);
            Assert.IsFalse(resolved);

            deferred2.Resolve("Some string");
            Assert.IsTrue(continued1);
            Assert.IsTrue(continued2);
            Assert.IsTrue(resolved);
        }

#if PROMISE_PROGRESS
        [Test]
        public void AsyncPromiseWillHaveProgressScaledProperly_MinMax_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            async Promise Func()
            {
                await deferred1.Promise.AwaitWithProgress(0f, 0.3f);
                (await deferred2.Promise.AwaitWithProgressNoThrow(0.5f, 1f)).RethrowIfRejectedOrCanceled();
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.3f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred1, 0.5f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.5f, 1f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred2, 1f);

            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseWillHaveProgressScaledProperly_Max_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            async Promise Func()
            {
                await deferred1.Promise.AwaitWithProgress(0.3f);
                (await deferred2.Promise.AwaitWithProgressNoThrow(1f)).RethrowIfRejectedOrCanceled();
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.3f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred1, 0.3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.3f, 1f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred2, 1f);

            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseWillHaveProgressScaledProperly_MinMax_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            async Promise<int> Func()
            {
                await deferred1.Promise.AwaitWithProgress(0f, 0.3f);
                var resultContainer = await deferred2.Promise.AwaitWithProgressNoThrow(0.5f, 1f);
                resultContainer.RethrowIfRejectedOrCanceled();
                return resultContainer.Value;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.3f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred1, 1, 0.5f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.5f, 1f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred2, 2, 1f);

            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseWillHaveProgressScaledProperly_Max_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            async Promise<int> Func()
            {
                await deferred1.Promise.AwaitWithProgress(0.3f);
                var resultContainer = await deferred2.Promise.AwaitWithProgressNoThrow(1f);
                resultContainer.RethrowIfRejectedOrCanceled();
                return resultContainer.Value;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.3f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred1, 1, 0.3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.3f, 1f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred2, 2, 1f);

            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseNestedWillHaveProgressScaledProperly_MinMax_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            async Promise Func()
            {
                await Func1().AwaitWithProgress(0f, 0.3f);
                (await Func2().AwaitWithProgressNoThrow(0.5f, 1f)).RethrowIfRejectedOrCanceled();
            }

            async Promise Func1()
            {
                await deferred1.Promise.AwaitWithProgress(0f, 1f);
            }

            async Promise Func2()
            {
                (await deferred2.Promise.AwaitWithProgressNoThrow(0f, 1f)).RethrowIfRejectedOrCanceled();
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.3f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred1, 0.5f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.5f, 1f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred2, 1f);

            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseNestedWillHaveProgressScaledProperly_Max_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            async Promise Func()
            {
                await Func1().AwaitWithProgress(0.3f);
                (await Func2().AwaitWithProgressNoThrow(1f)).RethrowIfRejectedOrCanceled();
            }

            async Promise Func1()
            {
                await deferred1.Promise.AwaitWithProgress(0f, 1f);
            }

            async Promise Func2()
            {
                (await deferred2.Promise.AwaitWithProgressNoThrow(0f, 1f)).RethrowIfRejectedOrCanceled();
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.3f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred1, 0.3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.3f, 1f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred2, 1f);

            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseNestedWillHaveProgressScaledProperly_MinMax_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            async Promise<int> Func()
            {
                await Func1().AwaitWithProgress(0f, 0.3f);
                var resultContainer = await Func2().AwaitWithProgressNoThrow(0.5f, 1f);
                resultContainer.RethrowIfRejectedOrCanceled();
                return resultContainer.Value;
            }

            async Promise<int> Func1()
            {
                return await deferred1.Promise.AwaitWithProgress(0f, 1f);
            }

            async Promise<int> Func2()
            {
                var resultContainer = await deferred2.Promise.AwaitWithProgressNoThrow(0f, 1f);
                resultContainer.RethrowIfRejectedOrCanceled();
                return resultContainer.Value;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.3f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred1, 1, 0.5f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.5f, 1f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred2, 2, 1f);

            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseNestedWillHaveProgressScaledProperly_Max_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            async Promise<int> Func()
            {
                await Func1().AwaitWithProgress(0.3f);
                var resultContainer = await Func2().AwaitWithProgressNoThrow(1f);
                resultContainer.RethrowIfRejectedOrCanceled();
                return resultContainer.Value;
            }

            async Promise<int> Func1()
            {
                return await deferred1.Promise.AwaitWithProgress(0f, 1f);
            }

            async Promise<int> Func2()
            {
                var resultContainer = await deferred2.Promise.AwaitWithProgressNoThrow(0f, 1f);
                resultContainer.RethrowIfRejectedOrCanceled();
                return resultContainer.Value;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.3f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred1, 1, 0.3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.3f, 1f, 0.5f));
            progressHelper.ResolveAndAssertResult(deferred2, 2, 1f);

            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseWontReportProgressFromCanceledPromiseChain_MinMax_void0()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var cancelationSource1 = CancelationSource.New();
            var cancelationSource2 = CancelationSource.New();

            async Promise Func()
            {
                await deferred1.Promise
                    .ThenDuplicate(cancelationSource1.Token)
                    .CatchCancelation(() => { })
                    .AwaitWithProgress(0f, 0.3f);
                (await deferred2.Promise
                    .ThenDuplicate(cancelationSource2.Token)
                    .CatchCancelation(() => { })
                    .AwaitWithProgressNoThrow(0.5f, 1f)).RethrowIfRejectedOrCanceled();
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.3f, 0.5f));
            progressHelper.CancelAndAssertResult(cancelationSource1, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, 0.5f, false);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.5f, 1f, 0.5f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.7f, TestHelper.Lerp(0.5f, 1f, 0.5f), false);
            progressHelper.CancelAndAssertResult(cancelationSource2, 1f);
            Assert.IsTrue(complete);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.8f, 1f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 1f, false);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void AsyncPromiseWontReportProgressFromCanceledPromiseChain_Max_void0()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var cancelationSource1 = CancelationSource.New();
            var cancelationSource2 = CancelationSource.New();

            async Promise Func()
            {
                await deferred1.Promise
                    .ThenDuplicate(cancelationSource1.Token)
                    .CatchCancelation(() => { })
                    .AwaitWithProgress(0.3f);
                (await deferred2.Promise
                    .ThenDuplicate(cancelationSource2.Token)
                    .CatchCancelation(() => { })
                    .AwaitWithProgressNoThrow(1f)).RethrowIfRejectedOrCanceled();
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.3f, 0.5f));
            progressHelper.CancelAndAssertResult(cancelationSource1, 0.3f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, 0.3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.3f, 1f, 0.5f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.7f, TestHelper.Lerp(0.3f, 1f, 0.5f), false);
            progressHelper.CancelAndAssertResult(cancelationSource2, 1f);
            Assert.IsTrue(complete);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.8f, 1f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 1f, false);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void AsyncPromiseWontReportProgressFromCanceledPromiseChain_MinMax_void1()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var cancelationSource1 = CancelationSource.New();
            var cancelationSource2 = CancelationSource.New();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            async Promise Func()
            {
                await deferred1.Promise
                    .ThenDuplicate(cancelationSource1.Token)
                    .CatchCancelation(() => deferred3.Promise)
                    .AwaitWithProgress(0f, 0.3f);
                (await deferred2.Promise
                    .ThenDuplicate(cancelationSource2.Token)
                    .CatchCancelation(() => deferred4.Promise)
                    .AwaitWithProgressNoThrow(0.5f, 1f)).RethrowIfRejectedOrCanceled();
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous, delta: TestHelper.progressEpsilon * 2); // Increase delta to accommodate for internal scaling operations with loss of precision.
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.1f, TestHelper.Lerp(0f, 0.3f, 0.1f / 2f));
            progressHelper.CancelAndAssertResult(cancelationSource1, TestHelper.Lerp(0f, 0.3f, 1f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.2f, TestHelper.Lerp(0f, 0.3f, 1f / 2f), false);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, TestHelper.Lerp(0f, 0.3f, 1.5f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.3f, TestHelper.Lerp(0f, 0.3f, 1.5f / 2f), false);
            progressHelper.ResolveAndAssertResult(deferred3, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.4f, 0.5f, false);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.5f, 1f, 0.5f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.4f, TestHelper.Lerp(0.5f, 1f, 0.5f / 2f), false);
            progressHelper.CancelAndAssertResult(cancelationSource2, TestHelper.Lerp(0.5f, 1f, 1f / 2f));

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.6f, TestHelper.Lerp(0.5f, 1f, 1f / 2f), false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0.5f, 1f, 1f / 2f), false);
            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, TestHelper.Lerp(0.5f, 1f, 1.5f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, TestHelper.Lerp(0.5f, 1f, 1.5f / 2f), false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, TestHelper.Lerp(0.5f, 1f, 1.5f / 2f), false);

            progressHelper.ResolveAndAssertResult(deferred4, 1f);
            Assert.IsTrue(complete);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 1f, false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.7f, 1f, false);

            progressHelper.ResolveAndAssertResult(deferred2, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 1f, false);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void AsyncPromiseWontReportProgressFromCanceledPromiseChain_Max_void1()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var cancelationSource1 = CancelationSource.New();
            var cancelationSource2 = CancelationSource.New();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            async Promise Func()
            {
                await deferred1.Promise
                    .ThenDuplicate(cancelationSource1.Token)
                    .CatchCancelation(() => deferred3.Promise)
                    .AwaitWithProgress(0.3f);
                (await deferred2.Promise
                    .ThenDuplicate(cancelationSource2.Token)
                    .CatchCancelation(() => deferred4.Promise)
                    .AwaitWithProgressNoThrow(1f)).RethrowIfRejectedOrCanceled();
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous, delta: TestHelper.progressEpsilon * 2); // Increase delta to accommodate for internal scaling operations with loss of precision.
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.1f, TestHelper.Lerp(0f, 0.3f, 0.1f / 2f));
            progressHelper.CancelAndAssertResult(cancelationSource1, TestHelper.Lerp(0f, 0.3f, 1f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.2f, TestHelper.Lerp(0f, 0.3f, 1f / 2f), false);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, TestHelper.Lerp(0f, 0.3f, 1.5f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.3f, TestHelper.Lerp(0f, 0.3f, 1.5f / 2f), false);
            progressHelper.ResolveAndAssertResult(deferred3, 0.3f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.4f, 0.3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.3f, 1f, 0.5f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.4f, TestHelper.Lerp(0.3f, 1f, 0.5f / 2f), false);
            progressHelper.CancelAndAssertResult(cancelationSource2, TestHelper.Lerp(0.3f, 1f, 1f / 2f));

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.6f, TestHelper.Lerp(0.3f, 1f, 1f / 2f), false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0.3f, 1f, 1f / 2f), false);
            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, TestHelper.Lerp(0.3f, 1f, 1.5f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, TestHelper.Lerp(0.3f, 1f, 1.5f / 2f), false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, TestHelper.Lerp(0.3f, 1f, 1.5f / 2f), false);

            progressHelper.ResolveAndAssertResult(deferred4, 1f);
            Assert.IsTrue(complete);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 1f, false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.7f, 1f, false);

            progressHelper.ResolveAndAssertResult(deferred2, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 1f, false);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void AsyncPromiseWontReportProgressFromCanceledPromiseChain_MinMax_T0()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var cancelationSource1 = CancelationSource.New();
            var cancelationSource2 = CancelationSource.New();

            async Promise<int> Func()
            {
                await deferred1.Promise
                    .ThenDuplicate(cancelationSource1.Token)
                    .CatchCancelation(() => 2)
                    .AwaitWithProgress(0f, 0.3f);
                var resultContainer = await deferred2.Promise
                    .ThenDuplicate(cancelationSource2.Token)
                    .CatchCancelation(() => 2)
                    .AwaitWithProgressNoThrow(0.5f, 1f);
                resultContainer.RethrowIfRejectedOrCanceled();
                return resultContainer.Value;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.3f, 0.5f));
            progressHelper.CancelAndAssertResult(cancelationSource1, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, 0.5f, false);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.5f, 1f, 0.5f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.7f, TestHelper.Lerp(0.5f, 1f, 0.5f), false);
            progressHelper.CancelAndAssertResult(cancelationSource2, 1f);
            Assert.IsTrue(complete);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.8f, 1f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 2, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f, false);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void AsyncPromiseWontReportProgressFromCanceledPromiseChain_Max_T0()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var cancelationSource1 = CancelationSource.New();
            var cancelationSource2 = CancelationSource.New();

            async Promise<int> Func()
            {
                await deferred1.Promise
                    .ThenDuplicate(cancelationSource1.Token)
                    .CatchCancelation(() => 2)
                    .AwaitWithProgress(0.3f);
                var resultContainer = await deferred2.Promise
                    .ThenDuplicate(cancelationSource2.Token)
                    .CatchCancelation(() => 2)
                    .AwaitWithProgressNoThrow(1f);
                resultContainer.RethrowIfRejectedOrCanceled();
                return resultContainer.Value;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.3f, 0.5f));
            progressHelper.CancelAndAssertResult(cancelationSource1, 0.3f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, 0.3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.3f, 1f, 0.5f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.7f, TestHelper.Lerp(0.3f, 1f, 0.5f), false);
            progressHelper.CancelAndAssertResult(cancelationSource2, 1f);
            Assert.IsTrue(complete);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.8f, 1f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 2, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f, false);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void AsyncPromiseWontReportProgressFromCanceledPromiseChain_MinMax_T1()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var cancelationSource1 = CancelationSource.New();
            var cancelationSource2 = CancelationSource.New();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            async Promise<int> Func()
            {
                await deferred1.Promise
                    .ThenDuplicate(cancelationSource1.Token)
                    .CatchCancelation(() => deferred3.Promise)
                    .AwaitWithProgress(0f, 0.3f);
                var resultContainer = await deferred2.Promise
                    .ThenDuplicate(cancelationSource2.Token)
                    .CatchCancelation(() => deferred4.Promise)
                    .AwaitWithProgressNoThrow(0.5f, 1f);
                resultContainer.RethrowIfRejectedOrCanceled();
                return resultContainer.Value;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous, delta: TestHelper.progressEpsilon * 2); // Increase delta to accommodate for internal scaling operations with loss of precision.
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.1f, TestHelper.Lerp(0f, 0.3f, 0.1f / 2f));
            progressHelper.CancelAndAssertResult(cancelationSource1, TestHelper.Lerp(0f, 0.3f, 1f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.2f, TestHelper.Lerp(0f, 0.3f, 1f / 2f), false);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, TestHelper.Lerp(0f, 0.3f, 1.5f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.3f, TestHelper.Lerp(0f, 0.3f, 1.5f / 2f), false);
            progressHelper.ResolveAndAssertResult(deferred3, 3, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.4f, 0.5f, false);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.5f, 1f, 0.5f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.4f, TestHelper.Lerp(0.5f, 1f, 0.5f / 2f), false);
            progressHelper.CancelAndAssertResult(cancelationSource2, TestHelper.Lerp(0.5f, 1f, 1f / 2f));

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.6f, TestHelper.Lerp(0.5f, 1f, 1f / 2f), false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0.5f, 1f, 1f / 2f), false);
            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, TestHelper.Lerp(0.5f, 1f, 1.5f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, TestHelper.Lerp(0.5f, 1f, 1.5f / 2f), false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, TestHelper.Lerp(0.5f, 1f, 1.5f / 2f), false);

            progressHelper.ResolveAndAssertResult(deferred4, 4, 1f);
            Assert.IsTrue(complete);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 1f, false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.7f, 1f, false);

            progressHelper.ResolveAndAssertResult(deferred2, 2, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f, false);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void AsyncPromiseWontReportProgressFromCanceledPromiseChain_Max_T1()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var cancelationSource1 = CancelationSource.New();
            var cancelationSource2 = CancelationSource.New();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            async Promise<int> Func()
            {
                await deferred1.Promise
                    .ThenDuplicate(cancelationSource1.Token)
                    .CatchCancelation(() => deferred3.Promise)
                    .AwaitWithProgress(0.3f);
                var resultContainer = await deferred2.Promise
                    .ThenDuplicate(cancelationSource2.Token)
                    .CatchCancelation(() => deferred4.Promise)
                    .AwaitWithProgressNoThrow(1f);
                resultContainer.RethrowIfRejectedOrCanceled();
                return resultContainer.Value;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous, delta: TestHelper.progressEpsilon * 2); // Increase delta to accommodate for internal scaling operations with loss of precision.
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.1f, TestHelper.Lerp(0f, 0.3f, 0.1f / 2f));
            progressHelper.CancelAndAssertResult(cancelationSource1, TestHelper.Lerp(0f, 0.3f, 1f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.2f, TestHelper.Lerp(0f, 0.3f, 1f / 2f), false);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, TestHelper.Lerp(0f, 0.3f, 1.5f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.3f, TestHelper.Lerp(0f, 0.3f, 1.5f / 2f), false);
            progressHelper.ResolveAndAssertResult(deferred3, 3, 0.3f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.4f, 0.3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0.3f, 1f, 0.5f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.4f, TestHelper.Lerp(0.3f, 1f, 0.5f / 2f), false);
            progressHelper.CancelAndAssertResult(cancelationSource2, TestHelper.Lerp(0.3f, 1f, 1f / 2f));

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.6f, TestHelper.Lerp(0.3f, 1f, 1f / 2f), false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0.3f, 1f, 1f / 2f), false);
            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, TestHelper.Lerp(0.3f, 1f, 1.5f / 2f));
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, TestHelper.Lerp(0.3f, 1f, 1.5f / 2f), false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, TestHelper.Lerp(0.3f, 1f, 1.5f / 2f), false);

            progressHelper.ResolveAndAssertResult(deferred4, 4, 1f);
            Assert.IsTrue(complete);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 1f, false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.7f, 1f, false);

            progressHelper.ResolveAndAssertResult(deferred2, 2, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f, false);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void AsyncPromiseWillReportProgress_WhenAnotherAwaitableIsAwaitedWithoutProgress_MinMax_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            async Promise Func()
            {
                await deferred1.Promise.AwaitWithProgress(0f, 0.5f);
                await deferred2.Promise;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f));
            // Implementation detail - progress is not reported after awaited promise is resolved, until another promise is awaited with progress.
            progressHelper.ResolveAndAssertResult(deferred1, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ResolveAndAssertResult(deferred2, 1f, true);
            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseWillReportProgress_WhenAnotherAwaitableIsAwaitedWithoutProgress_MinMax_NoThrow_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            async Promise Func()
            {
                (await deferred1.Promise.AwaitWithProgressNoThrow(0f, 0.5f)).RethrowIfRejectedOrCanceled();
                await deferred2.Promise;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f));
            // Implementation detail - progress is not reported after awaited promise is resolved, until another promise is awaited with progress.
            progressHelper.ResolveAndAssertResult(deferred1, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ResolveAndAssertResult(deferred2, 1f, true);
            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseWillReportProgress_WhenAnotherAwaitableIsAwaitedWithoutProgress_Max_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            async Promise Func()
            {
                await deferred1.Promise.AwaitWithProgress(0.5f);
                await deferred2.Promise;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f));
            // Implementation detail - progress is not reported after awaited promise is resolved, until another promise is awaited with progress.
            progressHelper.ResolveAndAssertResult(deferred1, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ResolveAndAssertResult(deferred2, 1f, true);
            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseWillReportProgress_WhenAnotherAwaitableIsAwaitedWithoutProgress_Max_NoThrow_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            async Promise Func()
            {
                (await deferred1.Promise.AwaitWithProgressNoThrow(0.5f)).RethrowIfRejectedOrCanceled();
                await deferred2.Promise;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f));
            // Implementation detail - progress is not reported after awaited promise is resolved, until another promise is awaited with progress.
            progressHelper.ResolveAndAssertResult(deferred1, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ResolveAndAssertResult(deferred2, 1f, true);
            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseWillReportProgress_WhenAnotherAwaitableIsAwaitedWithoutProgress_MinMax_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            async Promise<int> Func()
            {
                await deferred1.Promise.AwaitWithProgress(0f, 0.5f);
                return await deferred2.Promise;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f));
            // Implementation detail - progress is not reported after awaited promise is resolved, until another promise is awaited with progress.
            progressHelper.ResolveAndAssertResult(deferred1, 1, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ResolveAndAssertResult(deferred2, 2, 1f, true);
            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseWillReportProgress_WhenAnotherAwaitableIsAwaitedWithoutProgress_MinMax_NoThrow_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            async Promise<int> Func()
            {
                (await deferred1.Promise.AwaitWithProgressNoThrow(0f, 0.5f)).RethrowIfRejectedOrCanceled();
                return await deferred2.Promise;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f));
            // Implementation detail - progress is not reported after awaited promise is resolved, until another promise is awaited with progress.
            progressHelper.ResolveAndAssertResult(deferred1, 1, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ResolveAndAssertResult(deferred2, 2, 1f, true);
            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseWillReportProgress_WhenAnotherAwaitableIsAwaitedWithoutProgress_Max_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            async Promise<int> Func()
            {
                await deferred1.Promise.AwaitWithProgress(0.5f);
                return await deferred2.Promise;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f));
            // Implementation detail - progress is not reported after awaited promise is resolved, until another promise is awaited with progress.
            progressHelper.ResolveAndAssertResult(deferred1, 1, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ResolveAndAssertResult(deferred2, 2, 1f, true);
            Assert.IsTrue(complete);
        }

        [Test]
        public void AsyncPromiseWillReportProgress_WhenAnotherAwaitableIsAwaitedWithoutProgress_Max_NoThrow_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            async Promise<int> Func()
            {
                (await deferred1.Promise.AwaitWithProgressNoThrow(0.5f)).RethrowIfRejectedOrCanceled();
                return await deferred2.Promise;
            }

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            bool complete = false;

            Func()
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Then(() => complete = true)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f));
            // Implementation detail - progress is not reported after awaited promise is resolved, until another promise is awaited with progress.
            progressHelper.ResolveAndAssertResult(deferred1, 1, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, TestHelper.Lerp(0f, 0.5f, 0.5f), false);
            progressHelper.ResolveAndAssertResult(deferred2, 2, 1f, true);
            Assert.IsTrue(complete);
        }
#endif // PROMISE_PROGRESS

#if PROMISE_DEBUG
        [Test]
        public void IfTheAwaitedPromiseResultsInACircularPromiseChain_ThrowInvalidOperationException_void0()
        {
            var deferred = Promise.NewDeferred();
            var selfPromise = default(Promise);

            bool thrown = false;

            async Promise Func()
            {
                await deferred.Promise;
                try
                {
                    await selfPromise;
                }
                catch (Proto.Promises.InvalidOperationException)
                {
                    thrown = true;
                }
            }

            selfPromise = Func();
            selfPromise.Forget();

            deferred.Resolve();

            Assert.IsTrue(thrown);
        }

        [Test]
        public void IfTheAwaitedPromiseResultsInACircularPromiseChain_ThrowInvalidOperationException_void1()
        {
            var deferred = Promise.NewDeferred();
            var selfPromise = default(Promise);

            bool thrown = false;

            async Promise Func()
            {
                await deferred.Promise;
                await selfPromise
                    .ThenDuplicate()
                    .ThenDuplicate()
                    .Then(() => Promise.Resolved())
                    .Catch((Proto.Promises.InvalidOperationException e) => thrown = true);
            }

            selfPromise = Func();

            deferred.Resolve();

            Assert.IsTrue(thrown);
        }

        [Test]
        public void IfTheAwaitedPromiseResultsInACircularPromiseChain_ThrowInvalidOperationException_void2()
        {
            var deferred = Promise.NewDeferred();
            var selfPromise = default(Promise);

            bool thrown = false;

            async Promise Func()
            {
                await deferred.Promise;
                try
                {
                    await selfPromise
                        .ThenDuplicate()
                        .ThenDuplicate()
                        .Then(() => Promise.Resolved());
                }
                catch (Proto.Promises.InvalidOperationException)
                {
                    thrown = true;
                }
            }

            selfPromise = Func();

            deferred.Resolve();

            Assert.IsTrue(thrown);
        }

        [Test]
        public void IfTheAwaitedPromiseResultsInACircularPromiseChain_ThrowInvalidOperationException_void3()
        {
            var deferred = Promise.NewDeferred();
            var selfPromise = default(Promise);

            bool thrown = false;

            async Promise Func()
            {
                await Func2();
            }

            async Promise Func2()
            {
                await deferred.Promise;
                try
                {
                    await selfPromise;
                }
                catch (Proto.Promises.InvalidOperationException)
                {
                    thrown = true;
                }
            }

            selfPromise = Func();

            deferred.Resolve();

            Assert.IsTrue(thrown);
        }

        [Test]
        public void IfTheAwaitedPromiseResultsInACircularPromiseChain_ThrowInvalidOperationException_void4()
        {
            var deferred = Promise.NewDeferred();
            var selfPromise = default(Promise);

            bool thrown = false;

            async Promise Func()
            {
                try
                {
                    await Func2();
                }
                catch (Proto.Promises.InvalidOperationException)
                {
                    thrown = true;
                }
            }

            async Promise Func2()
            {
                await deferred.Promise;
                await selfPromise;
            }

            selfPromise = Func();

            deferred.Resolve();

            Assert.IsTrue(thrown);
        }

        [Test]
        public void IfTheAwaitedPromiseResultsInACircularPromiseChain_ThrowInvalidOperationException_T0()
        {
            var deferred = Promise.NewDeferred<int>();
            var selfPromise = default(Promise<int>);

            bool thrown = false;

            async Promise<int> Func()
            {
                await deferred.Promise;
                try
                {
                    return await selfPromise;
                }
                catch (Proto.Promises.InvalidOperationException)
                {
                    thrown = true;
                    return 2;
                }
            }

            selfPromise = Func();

            deferred.Resolve(1);

            Assert.IsTrue(thrown);
        }

        [Test]
        public void IfTheAwaitedPromiseResultsInACircularPromiseChain_ThrowInvalidOperationException_T1()
        {
            var deferred = Promise.NewDeferred<int>();
            var selfPromise = default(Promise<int>);

            bool thrown = false;

            async Promise<int> Func()
            {
                await deferred.Promise;
                return await selfPromise
                    .ThenDuplicate()
                    .ThenDuplicate()
                    .Then(() => Promise.Resolved(2))
                    .Catch((Proto.Promises.InvalidOperationException e) =>
                    {
                        thrown = true;
                        return 3;
                    });
            }

            selfPromise = Func();

            deferred.Resolve(1);

            Assert.IsTrue(thrown);
        }

        [Test]
        public void IfTheAwaitedPromiseResultsInACircularPromiseChain_ThrowInvalidOperationException_T2()
        {
            var deferred = Promise.NewDeferred<int>();
            var selfPromise = default(Promise<int>);

            bool thrown = false;

            async Promise<int> Func()
            {
                await deferred.Promise;
                try
                {
                    return await selfPromise
                        .ThenDuplicate()
                        .ThenDuplicate()
                        .Then(() => Promise.Resolved(2));
                }
                catch (Proto.Promises.InvalidOperationException)
                {
                    thrown = true;
                    return 3;
                }
            }

            selfPromise = Func();

            deferred.Resolve(1);

            Assert.IsTrue(thrown);
        }

        [Test]
        public void IfTheAwaitedPromiseResultsInACircularPromiseChain_ThrowInvalidOperationException_T3()
        {
            var deferred = Promise.NewDeferred<int>();
            var selfPromise = default(Promise<int>);

            bool thrown = false;

            async Promise<int> Func()
            {
                return await Func2();
            }

            async Promise<int> Func2()
            {
                await deferred.Promise;
                try
                {
                    return await selfPromise;
                }
                catch (Proto.Promises.InvalidOperationException)
                {
                    thrown = true;
                    return 2;
                }
            }

            selfPromise = Func();

            deferred.Resolve(1);

            Assert.IsTrue(thrown);
        }

        [Test]
        public void IfTheAwaitedPromiseResultsInACircularPromiseChain_ThrowInvalidOperationException_T4()
        {
            var deferred = Promise.NewDeferred<int>();
            var selfPromise = default(Promise<int>);

            bool thrown = false;

            async Promise<int> Func()
            {
                try
                {
                    return await Func2();
                }
                catch (Proto.Promises.InvalidOperationException)
                {
                    thrown = true;
                    return 2;
                }
            }

            async Promise<int> Func2()
            {
                await deferred.Promise;
                return await selfPromise;
            }

            selfPromise = Func();

            deferred.Resolve(1);

            Assert.IsTrue(thrown);
        }
#endif // PROMISE_DEBUG
    }

#if !NET_LEGACY
    public class AsyncLocalTests
    {
        [SetUp]
        public void Setup()
        {
            TestHelper.Setup();
            Promise.Config.AsyncFlowExecutionContextEnabled = true;
        }

        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();
        }

        private System.Threading.AsyncLocal<int> _asyncLocal;
        private Promise _promise;

        [Test]
        public void AsyncLocalWorks_void([Values] bool isPending)
        {
            _asyncLocal = new System.Threading.AsyncLocal<int>();
            var deferred = isPending ? Promise.NewDeferred() : default(Promise.Deferred);
            _promise = isPending ? deferred.Promise : Promise.Resolved();
            FuncVoid().Forget();
            deferred.TryResolve();
        }

        private async Promise FuncVoid()
        {
            _asyncLocal.Value = 1;

            await FuncVoidNested();

            Assert.AreEqual(1, _asyncLocal.Value);
        }

        private async Promise FuncVoidNested()
        {
            Assert.AreEqual(1, _asyncLocal.Value);
            _asyncLocal.Value = 2;

            await _promise;

            Assert.AreEqual(2, _asyncLocal.Value);
        }

        [Test]
        public void AsyncLocalWorks_T([Values] bool isPending)
        {
            _asyncLocal = new System.Threading.AsyncLocal<int>();
            var deferred = isPending ? Promise.NewDeferred() : default(Promise.Deferred);
            _promise = isPending ? deferred.Promise : Promise.Resolved();
            FuncT().Forget();
            deferred.TryResolve();
        }

        private async Promise<int> FuncT()
        {
            _asyncLocal.Value = 1;

            int result = await FuncTNested();

            Assert.AreEqual(1, _asyncLocal.Value);
            return result;
        }

        private async Promise<int> FuncTNested()
        {
            Assert.AreEqual(1, _asyncLocal.Value);
            _asyncLocal.Value = 2;

            await _promise;

            Assert.AreEqual(2, _asyncLocal.Value);
            return 3;
        }
    }
#endif
}

#endif