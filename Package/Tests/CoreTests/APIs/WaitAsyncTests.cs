#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using ProtoPromiseTests.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProtoPromiseTests.APIs
{
    public class WaitAsyncTests
    {
        public enum WaitAsyncCancelType
        {
            DefaultToken,
            AlreadyCanceled,
            CancelableToken_NoCancel,
            CancelBeforeComplete,
            CancelAfterComplete
        }

        const string rejectValue = "Fail";

        [SetUp]
        public void Setup()
        {
            // When a promise is canceled, the previous rejected promise is unhandled, so its rejection is sent to the UncaughtRejectionHandler.
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

        private static IEnumerable<TestCaseData> GetArgs()
        {
            foreach (CompleteType completeType in Enum.GetValues(typeof(CompleteType)))
            foreach (WaitAsyncCancelType cancelType in Enum.GetValues(typeof(WaitAsyncCancelType)))
            foreach (bool alreadyComplete in new[] { true, false })
            {
                if (alreadyComplete && cancelType == WaitAsyncCancelType.CancelBeforeComplete) continue;

                yield return new TestCaseData(completeType, cancelType, alreadyComplete);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs))]
        public void WaitAsync_CancelationToken_void(CompleteType completeType, WaitAsyncCancelType cancelType, bool alreadyComplete)
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelType == WaitAsyncCancelType.DefaultToken ? default(CancelationToken)
                : cancelType == WaitAsyncCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : cancelationSource.Token;

            bool cancelationExpected = cancelType == WaitAsyncCancelType.AlreadyCanceled || cancelType == WaitAsyncCancelType.CancelBeforeComplete;
            var expectedCompleteState = cancelationExpected ? Promise.State.Canceled : (Promise.State) completeType;

            var promise = TestHelper.BuildPromise(completeType, alreadyComplete, rejectValue, out var tryCompleter)
                .WaitAsync(cancelationToken)
                .ContinueWith(container =>
                {
                    Assert.AreEqual(expectedCompleteState, container.State);
                });
            if (cancelType == WaitAsyncCancelType.CancelBeforeComplete)
            {
                cancelationSource.Cancel();
            }
            tryCompleter();
            if (cancelType == WaitAsyncCancelType.CancelAfterComplete)
            {
                cancelationSource.Cancel();
            }

            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }

        [Test, TestCaseSource(nameof(GetArgs))]
        public void WaitAsync_CancelationToken_T(CompleteType completeType, WaitAsyncCancelType cancelType, bool alreadyComplete)
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelType == WaitAsyncCancelType.DefaultToken ? default(CancelationToken)
                : cancelType == WaitAsyncCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : cancelationSource.Token;

            bool cancelationExpected = cancelType == WaitAsyncCancelType.AlreadyCanceled || cancelType == WaitAsyncCancelType.CancelBeforeComplete;
            var expectedCompleteState = cancelationExpected ? Promise.State.Canceled : (Promise.State) completeType;
            const int resolveValue = 1;

            var promise = TestHelper.BuildPromise(completeType, alreadyComplete, resolveValue, rejectValue, out var tryCompleter)
                .WaitAsync(cancelationToken)
                .ContinueWith(container =>
                {
                    Assert.AreEqual(expectedCompleteState, container.State);
                    if (expectedCompleteState == Promise.State.Resolved)
                    {
                        Assert.AreEqual(resolveValue, container.Value);
                    }
                });
            if (cancelType == WaitAsyncCancelType.CancelBeforeComplete)
            {
                cancelationSource.Cancel();
            }
            tryCompleter();
            if (cancelType == WaitAsyncCancelType.CancelAfterComplete)
            {
                cancelationSource.Cancel();
            }

            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }
    }
}