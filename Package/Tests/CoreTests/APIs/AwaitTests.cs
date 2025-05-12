#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System.Threading.Tasks;

namespace ProtoPromise.Tests.APIs
{
    public class AwaitTests
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
        public void ResolveAwaitedPromiseContinuesExecution()
        {
            var deferred = Promise.NewDeferred();

            bool continued = false;

            async void Func()
            {
                await deferred.Promise;
                continued = true;
            }

            Func();
            Assert.IsFalse(continued);

            deferred.Resolve();
            Assert.IsTrue(continued);
        }

        [Test]
        public void ResolveAwaitedPromiseContinuesExecution_NoThrow()
        {
            var deferred = Promise.NewDeferred();

            bool continued = false;

            async void Func()
            {
                (await deferred.Promise.AwaitNoThrow()).RethrowIfRejectedOrCanceled();
                continued = true;
            }

            Func();
            Assert.IsFalse(continued);

            deferred.Resolve();
            Assert.IsTrue(continued);
        }

        [Test]
        public void ResolveAwaitedPromiseReturnsValueAndContinuesExecution()
        {
            var deferred = Promise.NewDeferred<int>();

            int expected = 50;
            bool continued = false;

            async void Func()
            {
                int value = await deferred.Promise;
                Assert.AreEqual(expected, value);
                continued = true;
            }

            Func();
            Assert.IsFalse(continued);

            deferred.Resolve(expected);
            Assert.IsTrue(continued);
        }

        [Test]
        public void ResolveAwaitedPromiseReturnsValueAndContinuesExecution_NoThrow()
        {
            var deferred = Promise.NewDeferred<int>();

            int expected = 50;
            bool continued = false;

            async void Func()
            {
                var resultContainer = await deferred.Promise.AwaitNoThrow();
                resultContainer.RethrowIfRejectedOrCanceled();
                Assert.AreEqual(expected, resultContainer.Value);
                continued = true;
            }

            Func();
            Assert.IsFalse(continued);

            deferred.Resolve(expected);
            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitAlreadyResolvedPromiseContinuesExecution()
        {
            bool continued = false;

            async void Func()
            {
                await Promise.Resolved();
                continued = true;
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);

            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitAlreadyResolvedPromiseContinuesExecution_NoThrow()
        {
            bool continued = false;

            async void Func()
            {
                (await Promise.Resolved().AwaitNoThrow()).RethrowIfRejectedOrCanceled();
                continued = true;
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);

            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitAlreadyResolvedPromiseReturnsValueAndContinuesExecution()
        {
            int expected = 50;
            bool continued = false;

            async void Func()
            {
                int value = await Promise.Resolved(expected);
                Assert.AreEqual(expected, value);
                continued = true;
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);

            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitAlreadyResolvedPromiseReturnsValueAndContinuesExecution_NoThrow()
        {
            int expected = 50;
            bool continued = false;

            async void Func()
            {
                var resultContainer = await Promise.Resolved(expected).AwaitNoThrow();
                resultContainer.RethrowIfRejectedOrCanceled();
                Assert.AreEqual(expected, resultContainer.Value);
                continued = true;
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);

            Assert.IsTrue(continued);
        }

        [Test]
        public void RejectAwaitedPromiseThrows_void()
        {
            var deferred = Promise.NewDeferred();

            bool continued = false;
            string rejectValue = "Reject";

            async void Func()
            {
                try
                {
                    await deferred.Promise;
                }
                catch (UnhandledException e)
                {
                    Assert.AreEqual(rejectValue, e.Value);
                    continued = true;
                }
            }

            Func();
            Assert.IsFalse(continued);

            deferred.Reject(rejectValue);
            Assert.IsTrue(continued);
        }

        [Test]
        public void RejectAwaitedPromiseNoThrow_IsRejectedWithReason_void()
        {
            var deferred = Promise.NewDeferred();

            bool continued = false;
            string rejectValue = "Reject";

            async void Func()
            {
                var resultContainer = await deferred.Promise.AwaitNoThrow();
                Assert.AreEqual(Promise.State.Rejected, resultContainer.State);
                Assert.AreEqual(rejectValue, resultContainer.Reason);
                continued = true;
            }

            Func();
            Assert.IsFalse(continued);

            deferred.Reject(rejectValue);
            Assert.IsTrue(continued);
        }

        [Test]
        public void RejectAwaitedPromiseThrows_T()
        {
            var deferred = Promise.NewDeferred<int>();

            bool continued = false;
            string rejectValue = "Reject";

            async void Func()
            {
                try
                {
                    int value = await deferred.Promise;
                }
                catch (UnhandledException e)
                {
                    Assert.AreEqual(rejectValue, e.Value);
                    continued = true;
                }
            }

            Func();
            Assert.IsFalse(continued);

            deferred.Reject(rejectValue);
            Assert.IsTrue(continued);
        }

        [Test]
        public void RejectAwaitedPromiseNoThrow_IsRejectedWithReason_T()
        {
            var deferred = Promise.NewDeferred<int>();

            bool continued = false;
            string rejectValue = "Reject";

            async void Func()
            {
                var resultContainer = await deferred.Promise.AwaitNoThrow();
                Assert.AreEqual(Promise.State.Rejected, resultContainer.State);
                Assert.AreEqual(rejectValue, resultContainer.Reason);
                continued = true;
            }

            Func();
            Assert.IsFalse(continued);

            deferred.Reject(rejectValue);
            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitAlreadyRejectedPromiseThrows_void()
        {
            bool continued = false;
            string rejectValue = "Reject";

            async void Func()
            {
                try
                {
                    await Promise.Rejected(rejectValue);
                }
                catch (UnhandledException e)
                {
                    Assert.AreEqual(rejectValue, e.Value);
                    continued = true;
                }
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitNoThrowAlreadyRejectedPromise_IsRejectedWithReason_void()
        {
            bool continued = false;
            string rejectValue = "Reject";

            async void Func()
            {
                var resultContainer = await Promise.Rejected(rejectValue).AwaitNoThrow();
                Assert.AreEqual(Promise.State.Rejected, resultContainer.State);
                Assert.AreEqual(rejectValue, resultContainer.Reason);
                continued = true;
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitAlreadyRejectedPromiseThrows_T()
        {
            bool continued = false;
            string rejectValue = "Reject";

            async void Func()
            {
                try
                {
                    int value = await Promise<int>.Rejected(rejectValue);
                }
                catch (UnhandledException e)
                {
                    Assert.AreEqual(rejectValue, e.Value);
                    continued = true;
                }
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitNoThrowAlreadyRejectedPromise_IsRejectedWithReason_T()
        {
            bool continued = false;
            string rejectValue = "Reject";

            async void Func()
            {
                var resultContainer = await Promise<int>.Rejected(rejectValue).AwaitNoThrow();
                Assert.AreEqual(Promise.State.Rejected, resultContainer.State);
                Assert.AreEqual(rejectValue, resultContainer.Reason);
                continued = true;
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);
        }

        [Test]
        public void CancelAwaitedPromiseThrowsOperationCanceled_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred);

            bool continued = false;

            async void Func()
            {
                try
                {
                    await deferred.Promise;
                }
                catch (CanceledException)
                {
                    continued = true;
                }
            }

            Func();
            Assert.IsFalse(continued);

            cancelationSource.Cancel();
            Assert.IsTrue(continued);

            cancelationSource.Dispose();
        }

        [Test]
        public void CancelAwaitedPromiseNoThrow_IsCanceled_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred);

            bool continued = false;

            async void Func()
            {
                var resultContainer = await deferred.Promise.AwaitNoThrow();
                Assert.AreEqual(Promise.State.Canceled, resultContainer.State);
                continued = true;
            }

            Func();
            Assert.IsFalse(continued);

            cancelationSource.Cancel();
            Assert.IsTrue(continued);

            cancelationSource.Dispose();
        }

        [Test]
        public void CancelAwaitedPromiseThrowsOperationCanceled_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred);

            bool continued = false;

            async void Func()
            {
                try
                {
                    int value = await deferred.Promise;
                }
                catch (CanceledException)
                {
                    continued = true;
                }
            }

            Func();
            Assert.IsFalse(continued);

            cancelationSource.Cancel();
            Assert.IsTrue(continued);

            cancelationSource.Dispose();
        }

        [Test]
        public void CancelAwaitedPromiseNoThrow_IsCanceled_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred);

            bool continued = false;

            async void Func()
            {
                var resultContainer = await deferred.Promise.AwaitNoThrow();
                Assert.AreEqual(Promise.State.Canceled, resultContainer.State);
                continued = true;
            }

            Func();
            Assert.IsFalse(continued);

            cancelationSource.Cancel();
            Assert.IsTrue(continued);

            cancelationSource.Dispose();
        }

        [Test]
        public void AwaitAlreadyCanceledPromiseThrowsOperationCanceled_void()
        {
            bool continued = false;

            async void Func()
            {
                try
                {
                    await Promise.Canceled();
                }
                catch (CanceledException)
                {
                    continued = true;
                }
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);

            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitNoThrowAlreadyCanceledPromise_IsCanceled_void()
        {
            bool continued = false;

            async void Func()
            {
                var resultContainer = await Promise.Canceled().AwaitNoThrow();
                Assert.AreEqual(Promise.State.Canceled, resultContainer.State);
                continued = true;
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitAlreadyCanceledPromiseThrowsOperationCanceled_T()
        {
            bool continued = false;

            async void Func()
            {
                try
                {
                    int value = await Promise<int>.Canceled();
                }
                catch (CanceledException)
                {
                    continued = true;
                }
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitNoThrowAlreadyCanceledPromise_IsCanceled_T()
        {
            bool continued = false;

            async void Func()
            {
                var resultContainer = await Promise<int>.Canceled().AwaitNoThrow();
                Assert.AreEqual(Promise.State.Canceled, resultContainer.State);
                continued = true;
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);
        }

        [Test]
        public void ResolveDoubleAwaitedRetainedPromiseContinuesExecution()
        {
            var deferred = Promise.NewDeferred();
            using (var promiseRetainer = deferred.Promise.GetRetainer())
            {
                int continuedCount = 0;

                async void Func()
                {
                    await promiseRetainer;
                    ++continuedCount;
                }

                Func();
                Func();
                Assert.AreEqual(0, continuedCount);

                deferred.Resolve();
                Assert.AreEqual(2, continuedCount);
            }
        }

        [Test]
        public void ResolveDoubleAwaitedRetainedPromiseReturnsValueAndContinuesExecution()
        {
            var deferred = Promise.NewDeferred<int>();
            using (var promiseRetainer = deferred.Promise.GetRetainer())
            {
                int expected = 50;
                int continuedCount = 0;

                async void Func()
                {
                    int value = await promiseRetainer;
                    Assert.AreEqual(expected, value);
                    ++continuedCount;
                }

                Func();
                Func();
                Assert.AreEqual(0, continuedCount);

                deferred.Resolve(expected);
                Assert.AreEqual(2, continuedCount);
            }
        }

        [Test]
        public void DoubleAwaitAlreadyResolvedRetainedPromiseContinuesExecution()
        {
            using (var promiseRetainer = Promise.Resolved().GetRetainer())
            {
                int continuedCount = 0;

                async void Func()
                {
                    await promiseRetainer;
                    ++continuedCount;
                }

                Assert.AreEqual(0, continuedCount);
                Func();
                Func();
                Assert.AreEqual(2, continuedCount);

                Assert.AreEqual(2, continuedCount);
            }
        }

        [Test]
        public void DoubleAwaitAlreadyResolvedRetainedPromiseReturnsValueAndContinuesExecution()
        {
            int expected = 50;
            using (var promiseRetainer = Promise.Resolved(expected).GetRetainer())
            {
                int continuedCount = 0;

                async void Func()
                {
                    int value = await promiseRetainer;
                    Assert.AreEqual(expected, value);
                    ++continuedCount;
                }

                Assert.AreEqual(0, continuedCount);
                Func();
                Func();
                Assert.AreEqual(2, continuedCount);

                Assert.AreEqual(2, continuedCount);
            }
        }

        [Test]
        public void RejectDoubleAwaitedRetainedPromiseThrows_void()
        {
            var deferred = Promise.NewDeferred();
            using (var promiseRetainer = deferred.Promise.GetRetainer())
            {
                string rejectValue = "Reject";
                int continuedCount = 0;

                async void Func()
                {
                    try
                    {
                        await promiseRetainer;
                    }
                    catch (UnhandledException e)
                    {
                        Assert.AreEqual(rejectValue, e.Value);
                        ++continuedCount;
                    }
                }

                Func();
                Func();
                Assert.AreEqual(0, continuedCount);

                deferred.Reject(rejectValue);
                Assert.AreEqual(2, continuedCount);
            }
        }

        [Test]
        public void RejectDoubleAwaitedRetainedPromiseThrows_T()
        {
            var deferred = Promise.NewDeferred<int>();
            using (var promiseRetainer = deferred.Promise.GetRetainer())
            {
                string rejectValue = "Reject";
                int continuedCount = 0;

                async void Func()
                {
                    try
                    {
                        int value = await promiseRetainer;
                    }
                    catch (UnhandledException e)
                    {
                        Assert.AreEqual(rejectValue, e.Value);
                        ++continuedCount;
                    }
                }

                Func();
                Func();
                Assert.AreEqual(0, continuedCount);

                deferred.Reject(rejectValue);
                Assert.AreEqual(2, continuedCount);
            }
        }

        [Test]
        public void DoubleAwaitAlreadyRejectedRetainedPromiseThrows_void()
        {
            string rejectValue = "Reject";
            using (var promiseRetainer = Promise.Rejected(rejectValue).GetRetainer())
            {
                int continuedCount = 0;

                async void Func()
                {
                    try
                    {
                        await promiseRetainer;
                    }
                    catch (UnhandledException e)
                    {
                        Assert.AreEqual(rejectValue, e.Value);
                        ++continuedCount;
                    }
                }

                Assert.AreEqual(0, continuedCount);
                Func();
                Func();
                Assert.AreEqual(2, continuedCount);

                Assert.AreEqual(2, continuedCount);
            }
        }

        [Test]
        public void DoubleAwaitAlreadyRejectedRetainedPromiseThrows_T()
        {
            string rejectValue = "Reject";
            using (var promiseRetainer = Promise<int>.Rejected(rejectValue).GetRetainer())
            {
                int continuedCount = 0;

                async void Func()
                {
                    try
                    {
                        int value = await promiseRetainer;
                    }
                    catch (UnhandledException e)
                    {
                        Assert.AreEqual(rejectValue, e.Value);
                        ++continuedCount;
                    }
                }

                Assert.AreEqual(0, continuedCount);
                Func();
                Func();
                Assert.AreEqual(2, continuedCount);

                Assert.AreEqual(2, continuedCount);
            }
        }

        [Test]
        public void CancelDoubleAwaitedRetainedPromiseThrowsOperationCanceled_void()
        {
            var deferred = Promise.NewDeferred();
            using (var promiseRetainer = deferred.Promise.GetRetainer())
            {
                int continuedCount = 0;

                async void Func()
                {
                    try
                    {
                        await promiseRetainer;
                    }
                    catch (CanceledException)
                    {
                        ++continuedCount;
                    }
                }

                Func();
                Func();
                Assert.AreEqual(0, continuedCount);

                deferred.Cancel();
                Assert.AreEqual(2, continuedCount);
            }
        }

        [Test]
        public void CancelDoubleAwaitedRetainedPromiseThrowsOperationCanceled_T()
        {
            var deferred = Promise.NewDeferred<int>();
            using (var promiseRetainer = deferred.Promise.GetRetainer())
            {
                int continuedCount = 0;

                async void Func()
                {
                    try
                    {
                        int value = await promiseRetainer;
                    }
                    catch (CanceledException)
                    {
                        ++continuedCount;
                    }
                }

                Func();
                Func();
                Assert.AreEqual(0, continuedCount);

                deferred.Cancel();
                Assert.AreEqual(2, continuedCount);
            }
        }

        [Test]
        public void DoubleAwaitAlreadyCanceledRetainedPromiseThrowsOperationCanceled_void()
        {
            using (var promiseRetainer = Promise.Canceled().GetRetainer())
            {
                int continuedCount = 0;

                async void Func()
                {
                    try
                    {
                        await promiseRetainer;
                    }
                    catch (CanceledException)
                    {
                        ++continuedCount;
                    }
                }

                Assert.AreEqual(0, continuedCount);
                Func();
                Func();
                Assert.AreEqual(2, continuedCount);

                Assert.AreEqual(2, continuedCount);
            }
        }

        [Test]
        public void DoubleAwaitAlreadyCanceledRetainedPromiseThrowsOperationCanceled_T()
        {
            using (var promiseRetainer = Promise<int>.Canceled().GetRetainer())
            {
                int continuedCount = 0;

                async void Func()
                {
                    try
                    {
                        int value = await promiseRetainer;
                    }
                    catch (CanceledException)
                    {
                        ++continuedCount;
                    }
                }

                Assert.AreEqual(0, continuedCount);
                Func();
                Func();
                Assert.AreEqual(2, continuedCount);

                Assert.AreEqual(2, continuedCount);
            }
        }

        [Test]
        public void PromiseToTaskIsResolvedProperly_void([Values] bool isPending)
        {
            Promise.Deferred deferred = isPending ? Promise.NewDeferred() : default(Promise.Deferred);
            var promise = isPending ? deferred.Promise : Promise.Resolved();

            bool completed = false;

            Func();
            if (isPending)
            {
                deferred.Resolve();
            }

            Assert.IsTrue(completed);

            async void Func()
            {
                await promise.ToTask();
                completed = true;
            }
        }

        [Test]
        public void PromiseToTaskIsResolvedProperly_T([Values] bool isPending)
        {
            Promise<int>.Deferred deferred = isPending ? Promise.NewDeferred<int>() : default(Promise<int>.Deferred);
            var promise = isPending ? deferred.Promise : Promise.Resolved(1);

            bool completed = false;

            Func();
            if (isPending)
            {
                deferred.Resolve(1);
            }

            Assert.IsTrue(completed);

            async void Func()
            {
                int result = await promise.ToTask();
                Assert.AreEqual(1, result);
                completed = true;
            }
        }

        [Test]
        public void TaskToPromiseIsResolvedProperly_void([Values] bool isPending)
        {
            TaskCompletionSource<bool> taskCompletionSource = isPending ? new TaskCompletionSource<bool>() : null;
            Task task = isPending ? taskCompletionSource.Task : Task.CompletedTask;

            bool completed = false;

            Func();
            if (isPending)
            {
                taskCompletionSource.SetResult(true);
            }

            Assert.IsTrue(completed);

            async void Func()
            {
                await task.ToPromise();
                completed = true;
            }
        }

        [Test]
        public void TaskToPromiseIsResolvedProperly_T([Values] bool isPending)
        {
            TaskCompletionSource<int> taskCompletionSource = isPending ? new TaskCompletionSource<int>() : null;
            Task<int> task = isPending ? taskCompletionSource.Task : Task.FromResult(1);

            bool completed = false;

            Func();
            if (isPending)
            {
                taskCompletionSource.SetResult(1);
            }

            Assert.IsTrue(completed);

            async void Func()
            {
                int result = await task.ToPromise();
                Assert.AreEqual(1, result);
                completed = true;
            }
        }

#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
        [Test]
        public void PromiseAsValueTaskIsResolvedProperly_void([Values] bool isPending)
        {
            Promise.Deferred deferred = isPending ? Promise.NewDeferred() : default(Promise.Deferred);
            var promise = isPending ? deferred.Promise : Promise.Resolved();

            bool completed = false;

            Func();
            if (isPending)
            {
                deferred.Resolve();
            }
            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();

            Assert.IsTrue(completed);

            async void Func()
            {
                await promise.AsValueTask();
                completed = true;
            }
        }

        [Test]
        public void PromiseAsValueTaskIsResolvedProperly_T([Values] bool isPending)
        {
            Promise<int>.Deferred deferred = isPending ? Promise.NewDeferred<int>() : default(Promise<int>.Deferred);
            var promise = isPending ? deferred.Promise : Promise.Resolved(1);

            bool completed = false;

            Func();
            if (isPending)
            {
                deferred.Resolve(1);
            }
            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();

            Assert.IsTrue(completed);

            async void Func()
            {
                int result = await promise.AsValueTask();
                Assert.AreEqual(1, result);
                completed = true;
            }
        }

        [Test]
        public void ValueTaskToPromiseIsResolvedProperly_void([Values] bool isPending)
        {
            TaskCompletionSource<bool> taskCompletionSource = isPending ? new TaskCompletionSource<bool>() : null;
            ValueTask task = isPending ? new ValueTask(taskCompletionSource.Task) : new ValueTask();

            bool completed = false;

            Func();
            if (isPending)
            {
                taskCompletionSource.SetResult(true);
            }

            Assert.IsTrue(completed);

            async void Func()
            {
                await task.ToPromise();
                completed = true;
            }
        }

        [Test]
        public void ValueTaskToPromiseIsResolvedProperly_T([Values] bool isPending)
        {
            TaskCompletionSource<int> taskCompletionSource = isPending ? new TaskCompletionSource<int>() : null;
            ValueTask<int> task = isPending ? new ValueTask<int>(taskCompletionSource.Task) : new ValueTask<int>(1);

            bool completed = false;

            Func();
            if (isPending)
            {
                taskCompletionSource.SetResult(1);
            }

            Assert.IsTrue(completed);

            async void Func()
            {
                int result = await task.ToPromise();
                Assert.AreEqual(1, result);
                completed = true;
            }
        }
#endif
    }
}