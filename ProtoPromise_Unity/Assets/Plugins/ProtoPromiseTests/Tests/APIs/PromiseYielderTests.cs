#if UNITY_5_5_OR_NEWER

using NUnit.Framework;
using Proto.Promises;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProtoPromiseTests.APIs
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
            Object.Destroy(behaviour);

            TestHelper.Cleanup();
        }

        private MonoBehaviour GetRunner(RunnerType runnerType)
        {
            return runnerType == RunnerType.Default ? null : behaviour;
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitOneFrame_WaitsOneFrame([Values] RunnerType runnerType)
        {
            int currentFrame = Time.frameCount;
            int continuedFrame = -1;

            var promise = PromiseYielder.WaitOneFrame(GetRunner(runnerType))
                .Then(() => continuedFrame = Time.frameCount);
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
            }
            Assert.AreEqual(currentFrame + 1, continuedFrame);
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitFor_WaitsOnce([Values] RunnerType runnerType)
        {
            var instruction = new MoveNextCounterInstruction();

            var promise = PromiseYielder.WaitFor(instruction, GetRunner(runnerType));
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
            }
            Assert.AreEqual(1, instruction.count);
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitFor_WaitsOnceOnEachRunner()
        {
            var instruction = new MoveNextCounterInstruction();

            var promise = PromiseYielder.WaitFor(instruction)
                .Then(() => PromiseYielder.WaitFor(instruction, behaviour));
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
            }
            Assert.AreEqual(2, instruction.count);
        }
    }
}

#endif // UNITY_5_5_OR_NEWER