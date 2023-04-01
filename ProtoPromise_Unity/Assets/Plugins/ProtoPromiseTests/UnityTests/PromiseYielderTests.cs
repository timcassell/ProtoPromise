#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using Proto.Promises;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProtoPromiseTests.Unity
{
    public class PromiseYielderTestBehaviour : MonoBehaviour
    {

    }

    public class PromiseYielderTests
    {
        public enum RunnerType
        {
            Default,
            Custom
        }

        private class MoveNextCounterInstruction : CustomYieldInstruction
        {
            public int count;

            public override bool keepWaiting
            {
                get
                {
                    ++count;
                    return false;
                }
            }
        }

        private PromiseYielderTestBehaviour behaviour;

        [SetUp]
        public void Setup()
        {
            TestHelper.Setup();

            behaviour = new GameObject("PromiseYielderTestBehaviour").AddComponent<PromiseYielderTestBehaviour>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.Destroy(behaviour.gameObject);

            TestHelper.Cleanup();
        }

        private MonoBehaviour GetRunner(RunnerType runnerType)
        {
            return runnerType == RunnerType.Default ? null : behaviour;
        }

        public enum CancelType
        {
            None,
            Immediate,
            Delayed
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitOneFrame_WaitsOneFrame(
            [Values] RunnerType runnerType,
            [Values] CancelType cancelType)
        {
            int currentFrame = Time.frameCount;
            int continuedFrame = -1;
            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            var promise = PromiseYielder.WaitOneFrame(GetRunner(runnerType), cancelToken)
                .Finally(() => continuedFrame = Time.frameCount);
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                if (cancelType == CancelType.Delayed)
                {
                    cancelSource.Cancel();
                }
                yield return yieldInstruction;
                Assert.AreEqual(cancelType == CancelType.None ? Promise.State.Resolved : Promise.State.Canceled, yieldInstruction.State);
            }
            cancelSource.Dispose();
            Assert.AreEqual(currentFrame + (cancelType == CancelType.None ? 1 : 0), continuedFrame);
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitFor_WaitsOnce(
            [Values] RunnerType runnerType,
            [Values] CancelType cancelType)
        {
            var instruction = new MoveNextCounterInstruction();
            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            var promise = PromiseYielder.WaitFor(instruction, GetRunner(runnerType), cancelToken);
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                if (cancelType == CancelType.Delayed)
                {
                    cancelSource.Cancel();
                }
                yield return yieldInstruction;
                Assert.AreEqual(cancelType == CancelType.None ? Promise.State.Resolved : Promise.State.Canceled, yieldInstruction.State);
            }
            cancelSource.Dispose();
            // The yield instruction's keepWaiting is still called immediately, even though we cancel the promise before the coroutine completed on the next frame.
            Assert.AreEqual(cancelType == CancelType.Immediate ? 0 : 1, instruction.count);
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitFor_WaitsOnceOnEachRunner()
        {
            var instruction = new MoveNextCounterInstruction();

            var behaviour2 = new GameObject("PromiseYielderTestBehaviour2").AddComponent<PromiseYielderTestBehaviour>();

            try
            {
                var promise = PromiseYielder.WaitFor(instruction)
                  .Then(() => PromiseYielder.WaitFor(instruction, behaviour))
                  .Then(() => PromiseYielder.WaitFor(instruction, behaviour2));
                using (var yieldInstruction = promise.ToYieldInstruction())
                {
                    yield return yieldInstruction;
                }
                Assert.AreEqual(3, instruction.count);
            }
            finally
            {
                Object.Destroy(behaviour2.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitForFrames_WaitsCorrectFrameCount([Values] CancelType cancelType)
        {
            const int waitFramesCount = 10;
            int currentFrame = Time.frameCount;
            int continuedFrame = -1;
            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            var promise = PromiseYielder.WaitForFrames(waitFramesCount, cancelToken)
                .Finally(() => continuedFrame = Time.frameCount);
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                if (cancelType == CancelType.Delayed)
                {
                    // Wait for half the frames before canceling the token.
                    for (int i = 0; i < waitFramesCount / 2; ++i)
                    {
                        yield return null;
                    }
                    cancelSource.Cancel();
                }
                yield return yieldInstruction;
                Assert.AreEqual(cancelType == CancelType.None ? Promise.State.Resolved : Promise.State.Canceled, yieldInstruction.State);
            }
            cancelSource.Dispose();
            int expectedWaitFrames = cancelType == CancelType.None ? waitFramesCount
                : cancelType == CancelType.Immediate ? 0
                : waitFramesCount / 2;
            Assert.AreEqual(currentFrame + expectedWaitFrames, continuedFrame);
        }

#if PROMISE_PROGRESS
        [UnityTest]
        public IEnumerator PromiseYielderWaitForFrames_ReportsProgress()
        {
            const int waitFramesCount = 10;

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);

            PromiseYielder.WaitForFrames(waitFramesCount)
                .SubscribeProgress(progressHelper)
                .Forget();

            
            for (int currentFrame = 0; currentFrame < waitFramesCount; ++currentFrame)
            {
                yield return null;
                progressHelper.AssertCurrentProgress((float) currentFrame / waitFramesCount, false);
            }
            yield return null;
            progressHelper.AssertCurrentProgress(1f, false);
        }
#endif // PROMISE_PROGRESS
    }
}