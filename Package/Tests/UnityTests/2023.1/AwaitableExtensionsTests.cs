#if UNITY_2023_1_OR_NEWER

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProtoPromise.Tests.Unity
{
    public class AwaitableExtensionsTests
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

        public enum CancelType
        {
            None,
            Immediate,
            Delayed
        }

        [UnityTest]
        public IEnumerator AwaitableToPromise_WaitsOneFrame(
            [Values] CancelType cancelType)
        {
            int currentFrame = Time.frameCount;
            int continuedFrame = -1;
            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            var promise = Awaitable.NextFrameAsync().ToPromise(cancelToken)
                .Finally(() =>
                {
                    continuedFrame = Time.frameCount;
                    if (cancelType == CancelType.Delayed)
                    {
                        cancelSource.Cancel();
                    }
                });
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                bool expectResolved = cancelType == CancelType.None || cancelType != CancelType.Immediate;
                Assert.AreEqual(expectResolved ? Promise.State.Resolved : Promise.State.Canceled, yieldInstruction.State);
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
            cancelSource.Dispose();
            Assert.AreEqual(cancelType == CancelType.Immediate ? currentFrame : currentFrame + 1, continuedFrame);
        }

        [UnityTest]
        public IEnumerator AwaitableToPromise_WaitsCorrectTime(
            [Values] CancelType cancelType,
            [Values(0.5f, 1f, 2f)] float timeScale,
            [Values(0f, 2f)] float waitSeconds)
        {
            float oldTimeScale = Time.timeScale;
            Time.timeScale = timeScale;

            float startTime = Time.time;
            float continuedTime = float.NaN;
            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            var promise = Awaitable.WaitForSecondsAsync(waitSeconds).ToPromise(cancelToken)
                .Finally(() => continuedTime = Time.time);
            if (cancelType == CancelType.Delayed)
            {
                // Wait for half the time before canceling the token.
                Awaitable.WaitForSecondsAsync(waitSeconds / 2f).ToPromise()
                    .Finally(cancelSource.Cancel)
                    .Forget();
            }
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                bool expectResolved = cancelType == CancelType.None || (waitSeconds == 0f && cancelType != CancelType.Immediate);
                Assert.AreEqual(expectResolved ? Promise.State.Resolved : Promise.State.Canceled, yieldInstruction.State);
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
            cancelSource.Dispose();

            // Fairly large delta due to engine frames. This expects at least 10 fps.
            const float delta = 1f / 10f;
            if (waitSeconds == 0f || cancelType == CancelType.Immediate)
            {
                // Awaitable.WaitForSecondsAsync always waits at least 1 frame, even if the time is 0.
                Assert.AreEqual(0f, continuedTime - startTime, delta);
            }
            else if (cancelType == CancelType.None)
            {
                Assert.GreaterOrEqual(delta + continuedTime - startTime, waitSeconds);
            }
            else if (cancelType == CancelType.Immediate)
            {
                Assert.GreaterOrEqual(delta + continuedTime - startTime, waitSeconds / 2f);
            }

            Time.timeScale = oldTimeScale;
        }
    }
}

#endif // UNITY_2023_1_OR_NEWER