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
            UnityEngine.Object.Destroy(behaviour.gameObject);

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
        public IEnumerator PromiseYielderWaitOneFrame_WaitsOneFrame()
        {
            int currentFrame = Time.frameCount;
            int continuedFrame = -1;

            var promise = PromiseYielder.WaitOneFrame().ToPromise()
                .Finally(() => continuedFrame = Time.frameCount);
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                yieldInstruction.GetResult();
            }
            Assert.AreEqual(currentFrame + 1, continuedFrame);
        }

        public class WaitOneFrameTestBehaviour : MonoBehaviour
        {
            private Func<Promise> _updateWaitOneFrameFunc;
            private Promise<ValueTuple<int, int>>.Deferred _updateDeferred;
            private Func<Promise> _eofWaitOneFrameFunc;
            private Promise<ValueTuple<int, int>>.Deferred _eofDeferred;

            public Promise<ValueTuple<int, int>> WaitOneFrameFromUpdate(Func<Promise> waitOneFrameFunc)
            {
                _updateWaitOneFrameFunc = waitOneFrameFunc;
                _updateDeferred = Promise<ValueTuple<int, int>>.NewDeferred();
                return _updateDeferred.Promise;
            }

            public Promise<ValueTuple<int, int>> WaitOneFrameFromEndOfFrame(Func<Promise> waitOneFrameFunc)
            {
                _eofWaitOneFrameFunc = waitOneFrameFunc;
                _eofDeferred = Promise<ValueTuple<int, int>>.NewDeferred();
                return _eofDeferred.Promise;
            }

            private IEnumerator Start()
            {
                var endOfFrame = new WaitForEndOfFrame();
                while (true)
                {
                    yield return endOfFrame;

                    if (_eofDeferred != default)
                    {
                        var deferred = _eofDeferred;
                        _eofDeferred = default;
                        _eofWaitOneFrameFunc()
                            .Then(ValueTuple.Create(deferred, Time.frameCount), cv => deferred.Resolve(ValueTuple.Create(cv.Item2, Time.frameCount)))
                            .Forget();
                    }
                }
            }

            private void Update()
            {
                if (_updateDeferred != default)
                {
                    var deferred = _updateDeferred;
                    _updateDeferred = default;
                    _updateWaitOneFrameFunc()
                        .Then(ValueTuple.Create(deferred, Time.frameCount), cv => deferred.Resolve(ValueTuple.Create(cv.Item2, Time.frameCount)))
                        .Forget();
                }
            }
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitOneFrame_WaitsOneFrame_FromUpdate()
        {
            // Update runs before the PromiseYielder frame processor, so this test makes sure WaitOneFrame doesn't resolve in the same frame.    
            var testBehaviour = behaviour.gameObject.AddComponent<WaitOneFrameTestBehaviour>();
            try
            {
                var promise = testBehaviour.WaitOneFrameFromUpdate(() => PromiseYielder.WaitOneFrame().ToPromise())
                    .Then(cv => Assert.AreEqual(cv.Item1 + 1, cv.Item2));
                using (var yieldInstruction = promise.ToYieldInstruction())
                {
                    yield return yieldInstruction;
                    yieldInstruction.GetResult();
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(testBehaviour);
            }
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitOneFrame_WaitsOneFrame_FromEndOfFrame()
        {
            // End of Frame runs after everything else, so this test makes sure WaitOneFrame resolves in the next frame (doesn't skip a frame).
            if (Application.isBatchMode)
            {
                Assert.Inconclusive("Application is running in batchmode, WaitForEndOfFrame will not run.");
                yield break;
            }

            var testBehaviour = behaviour.gameObject.AddComponent<WaitOneFrameTestBehaviour>();
            try
            {
                var promise = testBehaviour.WaitOneFrameFromEndOfFrame(() => PromiseYielder.WaitOneFrame().ToPromise())
                    .Then(cv => Assert.AreEqual(cv.Item1 + 1, cv.Item2));
                using (var yieldInstruction = promise.ToYieldInstruction())
                {
                    yield return yieldInstruction;
                    yieldInstruction.GetResult();
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(testBehaviour);
            }
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitOneFrame_WaitsOneFrameMultiple()
        {
            int initialFrame = Time.frameCount;

            var promise = PromiseYielder.WaitOneFrame().ToPromise()
                .Then(() =>
                {
                    Assert.AreEqual(1, Time.frameCount - initialFrame);
                    return PromiseYielder.WaitOneFrame().ToPromise();
                })
                .Then(() =>
                {
                    Assert.AreEqual(2, Time.frameCount - initialFrame);
                    return PromiseYielder.WaitOneFrame().ToPromise();
                })
                .Then(() =>
                {
                    Assert.AreEqual(3, Time.frameCount - initialFrame);
                    return PromiseYielder.WaitOneFrame().ToPromise();
                })
                .Then(() =>
                {
                    Assert.AreEqual(4, Time.frameCount - initialFrame);
                });
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
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
            if (cancelType == CancelType.Delayed)
            {
                cancelSource.Cancel();
            }
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                Assert.AreEqual(cancelType == CancelType.None ? Promise.State.Resolved : Promise.State.Canceled, yieldInstruction.State);
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
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
                    if (yieldInstruction.State == Promise.State.Rejected)
                        yieldInstruction.GetResult();
                }
                Assert.AreEqual(3, instruction.count);
            }
            finally
            {
                UnityEngine.Object.Destroy(behaviour2.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitForFrames_WaitsCorrectFrameCount(
            [Values] CancelType cancelType,
            [Values(0, 10)] int waitFramesCount)
        {
            int currentFrame = Time.frameCount;
            int continuedFrame = -1;
            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            var promise = PromiseYielder.WaitForFrames((uint) waitFramesCount).ToPromise(cancelToken)
                .Finally(() => continuedFrame = Time.frameCount);
            if (cancelType == CancelType.Delayed)
            {
                // Wait for half the frames before canceling the token.
                // Subtract 1 frame since this Coroutine runs after the PromiseYielder Coroutine.
                for (int i = 0; i < waitFramesCount / 2 - 1; ++i)
                {
                    yield return null;
                }
                cancelSource.Cancel();
            }
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                bool expectResolved = cancelType == CancelType.None || (waitFramesCount == 0f && cancelType != CancelType.Immediate);
                Assert.AreEqual(expectResolved ? Promise.State.Resolved : Promise.State.Canceled, yieldInstruction.State);
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
            cancelSource.Dispose();
            int expectedWaitFrames = cancelType == CancelType.None ? waitFramesCount
                : cancelType == CancelType.Immediate ? 0
                : waitFramesCount / 2;
            Assert.AreEqual(currentFrame + expectedWaitFrames, continuedFrame);
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitForTime_WaitsCorrectTime(
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

            var promise = PromiseYielder.WaitForTime(TimeSpan.FromSeconds(waitSeconds)).ToPromise(cancelToken)
                .Finally(() => continuedTime = Time.time);
            if (cancelType == CancelType.Delayed)
            {
                // Wait for half the time before canceling the token.
                PromiseYielder.WaitForTime(TimeSpan.FromSeconds(waitSeconds / 2f)).ToPromise()
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
                Assert.AreEqual(0f, continuedTime - startTime);
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

        [UnityTest]
        public IEnumerator PromiseYielderWaitForRealTime_WaitsCorrectTime(
            [Values] CancelType cancelType,
            [Values(0.5f, 1f, 2f)] float timeScale,
            [Values(0f, 2f)] float waitSeconds)
        {
            float oldTimeScale = Time.timeScale;
            Time.timeScale = timeScale;

            float startTime = Time.realtimeSinceStartup;
            float continuedTime = float.NaN;
            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            var promise = PromiseYielder.WaitForRealTime(TimeSpan.FromSeconds(waitSeconds)).ToPromise(cancelToken)
                .Finally(() => continuedTime = Time.realtimeSinceStartup);
            if (cancelType == CancelType.Delayed)
            {
                // Wait for half the time before canceling the token.
                PromiseYielder.WaitForRealTime(TimeSpan.FromSeconds(waitSeconds / 2f)).ToPromise()
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

        [UnityTest]
        public IEnumerator PromiseYielderWaitUntil_WaitsCorrectly([Values] CancelType cancelType)
        {
            bool keepWaiting = true;
            bool didContinue = false;

            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            var promise = PromiseYielder.WaitUntil(() => !keepWaiting).ToPromise(cancelToken)
                .Finally(() => didContinue = true);

            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            if (cancelType == CancelType.Delayed)
            {
                cancelSource.Cancel();
                // Continuation won't be invoked until the next cycle.
                Assert.IsFalse(didContinue);
            }
            else
            {
                Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            }

            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);
            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);

            yield return null;
            keepWaiting = false;

            yield return null;
            Assert.IsTrue(didContinue);

            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
            cancelSource.Dispose();
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitUntil_WaitsCorrectlyWithCaptureValue([Values] CancelType cancelType)
        {
            bool keepWaiting = true;
            bool didContinue = false;
            const int captureValue = 42;

            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            var promise = PromiseYielder.WaitUntil(captureValue, cv =>
                {
                    Assert.AreEqual(captureValue, cv);
                    return !keepWaiting;
                }).ToPromise(cancelToken)
                .Finally(() => didContinue = true);

            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            if (cancelType == CancelType.Delayed)
            {
                cancelSource.Cancel();
                // Continuation won't be invoked until the next cycle.
                Assert.IsFalse(didContinue);
            }
            else
            {
                Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            }

            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);
            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);

            yield return null;
            keepWaiting = false;

            yield return null;
            Assert.IsTrue(didContinue);

            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
            cancelSource.Dispose();
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitWhile_WaitsCorrectly([Values] CancelType cancelType)
        {
            bool keepWaiting = true;
            bool didContinue = false;

            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            var promise = PromiseYielder.WaitWhile(() => keepWaiting).ToPromise(cancelToken)
                .Finally(() => didContinue = true);

            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            if (cancelType == CancelType.Delayed)
            {
                cancelSource.Cancel();
                // Continuation won't be invoked until the next cycle.
                Assert.IsFalse(didContinue);
            }
            else
            {
                Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            }

            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);
            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);

            yield return null;
            keepWaiting = false;

            yield return null;
            Assert.IsTrue(didContinue);

            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
            cancelSource.Dispose();
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitWhile_WaitsCorrectlyWithCaptureValue([Values] CancelType cancelType)
        {
            bool keepWaiting = true;
            bool didContinue = false;
            const int captureValue = 42;

            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            var promise = PromiseYielder.WaitWhile(captureValue, cv =>
            {
                Assert.AreEqual(captureValue, cv);
                return keepWaiting;
            }).ToPromise(cancelToken)
                .Finally(() => didContinue = true);

            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            if (cancelType == CancelType.Delayed)
            {
                cancelSource.Cancel();
                // Continuation won't be invoked until the next cycle.
                Assert.IsFalse(didContinue);
            }
            else
            {
                Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            }

            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);
            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);

            yield return null;
            keepWaiting = false;

            yield return null;
            Assert.IsTrue(didContinue);

            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
            cancelSource.Dispose();
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitForFixedUpdate_CompletesInFixedUpdate()
        {
            var promise = PromiseYielder.WaitForFixedUpdate().ToPromise()
                .Finally(() => Assert.IsTrue(Time.inFixedTimeStep));

            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
        }

        // Not testing WaitForEndOfFrame as there is no way to assert that it is actually in that execution stage.
        // Not testing WaitForAsyncOperation as I don't want to have to load something for unit testing.

        [UnityTest]
        public IEnumerator PromiseYielder_ManyEarlyWaitUntils()
        {
            // Use the testBehaviour to start the waits from Update so that it is ran early (before the PromiseYielder processes the queues).
            var testBehaviour = behaviour.gameObject.AddComponent<WaitOneFrameTestBehaviour>();
            try
            {
                var promise = testBehaviour.WaitOneFrameFromUpdate(() =>
                {
                    // Wait for the internal queues to be processed twice to make sure nothing breaks.
                    const int waitFrameCount = 6;
                    // Testing implementation detail of the internal array growing in size. Initial size is 64, so we need to create at least 65 waits.
                    const int waitCount = 100;

                    int initialFrame = Time.frameCount;
                    Promise[] promises = new Promise[waitCount];
                    for (int i = 0; i < waitCount; i++)
                    {
                        promises[i] = PromiseYielder.WaitUntil(() => Time.frameCount >= initialFrame + waitFrameCount).ToPromise();
                    }
                    return Promise.All(promises);
                });
                using (var yieldInstruction = promise.ToYieldInstruction())
                {
                    yield return yieldInstruction;
                    yieldInstruction.GetResult();
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(testBehaviour);
            }
        }

        [UnityTest]
        public IEnumerator PromiseYielder_ManyLateWaitUntils()
        {
            if (Application.isBatchMode)
            {
                Assert.Inconclusive("Application is running in batchmode, WaitForEndOfFrame will not run.");
                yield break;
            }

            // Use the testBehaviour to start the waits from EndOfFrame so that it is ran early (before the PromiseYielder processes the queues).
            var testBehaviour = behaviour.gameObject.AddComponent<WaitOneFrameTestBehaviour>();
            try
            {
                var promise = testBehaviour.WaitOneFrameFromEndOfFrame(() =>
                {
                    // Wait for the internal queues to be processed twice to make sure nothing breaks.
                    const int waitFrameCount = 6;
                    // Testing implementation detail of the internal array growing in size. Initial size is 64, so we need to create at least 65 waits.
                    const int waitCount = 100;

                    int initialFrame = Time.frameCount;
                    Promise[] promises = new Promise[waitCount];
                    for (int i = 0; i < waitCount; i++)
                    {
                        // Use capture value to make sure the internal queue is not the same queue used as PromiseYielder_ManyEarlyWaitUntils().
                        promises[i] = PromiseYielder.WaitUntil(initialFrame, iFrame => Time.frameCount >= iFrame + waitFrameCount).ToPromise();
                    }
                    return Promise.All(promises);
                });
                using (var yieldInstruction = promise.ToYieldInstruction())
                {
                    yield return yieldInstruction;
                    yieldInstruction.GetResult();
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(testBehaviour);
            }
        }

        [UnityTest]
        public IEnumerator PromiseYielder_ManyWaitWhiles()
        {
            // Wait for the internal queues to be processed twice to make sure nothing breaks.
            const int waitFrameCount = 6;
            // Testing implementation detail of the internal array growing in size. Initial size is 64, so we need to create at least 65 waits.
            const int waitCount = 100;

            int initialFrame = Time.frameCount;
            Promise[] promises = new Promise[waitCount];
            for (int i = 0; i < waitCount; i++)
            {
                promises[i] = PromiseYielder.WaitWhile(() => Time.frameCount < initialFrame + waitFrameCount).ToPromise();
            }
            var promise = Promise.All(promises);
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                yieldInstruction.GetResult();
            }
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitForFramesWithProgress_ReportsProgress()
        {
            const int waitFramesCount = 10;

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            var progress = progressHelper.ToProgress();

            PromiseYielder.WaitForFrames(waitFramesCount, progress.Token).ToPromise()
                .Then(progress.DisposeAsync)
                .Forget();

            for (int currentFrame = 0; currentFrame < waitFramesCount; ++currentFrame)
            {
                progressHelper.AssertCurrentProgress((float) currentFrame / waitFramesCount, false);
                yield return null;
            }
            progressHelper.AssertCurrentProgress(1f, false);
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitForTimeWithProgress_ReportsProgress([Values(0.5f, 1f, 2f)] float timeScale)
        {
            float oldTimeScale = Time.timeScale;
            Time.timeScale = timeScale;

            const float waitSeconds = 2f;

            // Timing can be off a bit, so give it a large delta.
            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous, delta: 1f / 10f);
            var progress = progressHelper.ToProgress();

            var promise = PromiseYielder.WaitForTime(TimeSpan.FromSeconds(waitSeconds), progress.Token).ToPromise()
                .Then(progress.DisposeAsync);

            for (float currentTime = 0f; currentTime < waitSeconds; currentTime += Time.deltaTime)
            {
                progressHelper.AssertCurrentProgress(currentTime / waitSeconds, false);
                yield return null;
            }
            // Timing can be off a bit, so we explicitly wait for the promise to complete.
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                yieldInstruction.GetResult();
            }
            progressHelper.AssertCurrentProgress(1f, false);

            Time.timeScale = oldTimeScale;
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitForRealTimeWithProgress_ReportsProgress([Values(0.5f, 1f, 2f)] float timeScale)
        {
            float oldTimeScale = Time.timeScale;
            Time.timeScale = timeScale;

            const float waitSeconds = 2f;

            // Timing can be off a bit, so give it a large delta.
            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous, delta: 1f / 10f);
            var progress = progressHelper.ToProgress();

            var promise = PromiseYielder.WaitForRealTime(TimeSpan.FromSeconds(waitSeconds), progress.Token).ToPromise()
                .Then(progress.DisposeAsync);

            for (float currentTime = 0f; currentTime < waitSeconds; currentTime += Time.unscaledDeltaTime)
            {
                progressHelper.AssertCurrentProgress(currentTime / waitSeconds, false);
                yield return null;
            }
            // Timing can be off a bit, so we explicitly wait for the promise to complete.
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                yieldInstruction.GetResult();
            }
            progressHelper.AssertCurrentProgress(1f, false);

            Time.timeScale = oldTimeScale;
        }


        [UnityTest]
        public IEnumerator PromiseYielderWaitOneFrame_WaitsOneFrame_Async()
        {
            int currentFrame = Time.frameCount;
            int continuedFrame = -1;

            async Promise Func()
            {
                await PromiseYielder.WaitOneFrame();
            }

            var promise = Func()
                .Finally(() => continuedFrame = Time.frameCount);
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                yieldInstruction.GetResult();
            }
            Assert.AreEqual(currentFrame + 1, continuedFrame);
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitOneFrame_WaitsOneFrame_FromUpdate_Async()
        {
            // Update runs before the PromiseYielder frame processor, so this test makes sure WaitOneFrame doesn't resolve in the same frame.    
            var testBehaviour = behaviour.gameObject.AddComponent<WaitOneFrameTestBehaviour>();
            try
            {
                async Promise Func()
                {
                    await PromiseYielder.WaitOneFrame();
                }

                var promise = testBehaviour.WaitOneFrameFromUpdate(Func)
                    .Then(cv => Assert.AreEqual(cv.Item1 + 1, cv.Item2));
                using (var yieldInstruction = promise.ToYieldInstruction())
                {
                    yield return yieldInstruction;
                    yieldInstruction.GetResult();
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(testBehaviour);
            }
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitOneFrame_WaitsOneFrame_FromEndOfFrame_Async()
        {
            // End of Frame runs after everything else, so this test makes sure WaitOneFrame resolves in the next frame (doesn't skip a frame).
            if (Application.isBatchMode)
            {
                Assert.Inconclusive("Application is running in batchmode, WaitForEndOfFrame will not run.");
                yield break;
            }

            var testBehaviour = behaviour.gameObject.AddComponent<WaitOneFrameTestBehaviour>();
            try
            {
                async Promise Func()
                {
                    await PromiseYielder.WaitOneFrame();
                }

                var promise = testBehaviour.WaitOneFrameFromEndOfFrame(Func)
                    .Then(cv => Assert.AreEqual(cv.Item1 + 1, cv.Item2));
                using (var yieldInstruction = promise.ToYieldInstruction())
                {
                    yield return yieldInstruction;
                    yieldInstruction.GetResult();
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(testBehaviour);
            }
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitOneFrame_WaitsOneFrameMultiple_Async()
        {
            int initialFrame = Time.frameCount;

            async Promise Func()
            {
                await PromiseYielder.WaitOneFrame();
                Assert.AreEqual(1, Time.frameCount - initialFrame);

                await PromiseYielder.WaitOneFrame();
                Assert.AreEqual(2, Time.frameCount - initialFrame);

                await PromiseYielder.WaitOneFrame();
                Assert.AreEqual(3, Time.frameCount - initialFrame);

                await PromiseYielder.WaitOneFrame();
                Assert.AreEqual(4, Time.frameCount - initialFrame);
            }

            using (var yieldInstruction = Func().ToYieldInstruction())
            {
                yield return yieldInstruction;
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitForFrames_WaitsCorrectFrameCount_Async(
            [Values] CancelType cancelType,
            [Values(0, 10)] int waitFramesCount)
        {
            int currentFrame = Time.frameCount;
            int continuedFrame = -1;
            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            async Promise Func()
            {
                if (cancelType == CancelType.None)
                    await PromiseYielder.WaitForFrames((uint) waitFramesCount);
                else
                    await PromiseYielder.WaitForFrames((uint) waitFramesCount).WithCancelation(cancelToken);
            }

            var promise = Func()
                .Finally(() => continuedFrame = Time.frameCount);
            if (cancelType == CancelType.Delayed)
            {
                // Wait for half the frames before canceling the token.
                // Subtract 1 frame since this Coroutine runs after the PromiseYielder Coroutine.
                for (int i = 0; i < waitFramesCount / 2 - 1; ++i)
                {
                    yield return null;
                }
                cancelSource.Cancel();
            }
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                bool expectResolved = cancelType == CancelType.None || (waitFramesCount == 0f && cancelType != CancelType.Immediate);
                Assert.AreEqual(expectResolved ? Promise.State.Resolved : Promise.State.Canceled, yieldInstruction.State);
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
            cancelSource.Dispose();
            int expectedWaitFrames = cancelType == CancelType.None ? waitFramesCount
                : cancelType == CancelType.Immediate ? 0
                : waitFramesCount / 2;
            Assert.AreEqual(currentFrame + expectedWaitFrames, continuedFrame);
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitForTime_WaitsCorrectTime_Async(
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

            async Promise Func()
            {
                if (cancelType == CancelType.None)
                    await PromiseYielder.WaitForTime(TimeSpan.FromSeconds(waitSeconds));
                else
                    await PromiseYielder.WaitForTime(TimeSpan.FromSeconds(waitSeconds)).WithCancelation(cancelToken);
            }

            var promise = Func()
                .Finally(() => continuedTime = Time.time);
            if (cancelType == CancelType.Delayed)
            {
                // Wait for half the time before canceling the token.
                PromiseYielder.WaitForTime(TimeSpan.FromSeconds(waitSeconds / 2f)).ToPromise()
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
                Assert.AreEqual(0f, continuedTime - startTime);
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

        [UnityTest]
        public IEnumerator PromiseYielderWaitForRealTime_WaitsCorrectTime_Async(
            [Values] CancelType cancelType,
            [Values(0.5f, 1f, 2f)] float timeScale,
            [Values(0f, 2f)] float waitSeconds)
        {
            float oldTimeScale = Time.timeScale;
            Time.timeScale = timeScale;

            float startTime = Time.realtimeSinceStartup;
            float continuedTime = float.NaN;
            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            async Promise Func()
            {
                if (cancelType == CancelType.None)
                    await PromiseYielder.WaitForRealTime(TimeSpan.FromSeconds(waitSeconds));
                else
                    await PromiseYielder.WaitForRealTime(TimeSpan.FromSeconds(waitSeconds)).WithCancelation(cancelToken);
            }

            var promise = Func()
                .Finally(() => continuedTime = Time.realtimeSinceStartup);
            if (cancelType == CancelType.Delayed)
            {
                // Wait for half the time before canceling the token.
                PromiseYielder.WaitForRealTime(TimeSpan.FromSeconds(waitSeconds / 2f)).ToPromise()
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

        [UnityTest]
        public IEnumerator PromiseYielderWaitUntil_WaitsCorrectly_Async([Values] CancelType cancelType)
        {
            bool keepWaiting = true;
            bool didContinue = false;

            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            async Promise Func()
            {
                if (cancelType == CancelType.None)
                    await PromiseYielder.WaitUntil(() => !keepWaiting);
                else
                    await PromiseYielder.WaitUntil(() => !keepWaiting).WithCancelation(cancelToken);
            }

            var promise = Func()
                .Finally(() => didContinue = true);

            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            if (cancelType == CancelType.Delayed)
            {
                cancelSource.Cancel();
                // Continuation won't be invoked until the next cycle.
                Assert.IsFalse(didContinue);
            }
            else
            {
                Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            }

            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);
            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);

            yield return null;
            keepWaiting = false;

            yield return null;
            Assert.IsTrue(didContinue);

            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
            cancelSource.Dispose();
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitUntil_WaitsCorrectlyWithCaptureValue_Async([Values] CancelType cancelType)
        {
            bool keepWaiting = true;
            bool didContinue = false;
            const int captureValue = 42;

            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            async Promise Func()
            {
                var waitUntil = PromiseYielder.WaitUntil(captureValue, cv =>
                {
                    Assert.AreEqual(captureValue, cv);
                    return !keepWaiting;
                });
                if (cancelType == CancelType.None)
                    await waitUntil;
                else
                    await waitUntil.WithCancelation(cancelToken);
            }

            var promise = Func()
                .Finally(() => didContinue = true);

            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            if (cancelType == CancelType.Delayed)
            {
                cancelSource.Cancel();
                // Continuation won't be invoked until the next cycle.
                Assert.IsFalse(didContinue);
            }
            else
            {
                Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            }

            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);
            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);

            yield return null;
            keepWaiting = false;

            yield return null;
            Assert.IsTrue(didContinue);

            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
            cancelSource.Dispose();
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitWhile_WaitsCorrectly_Async([Values] CancelType cancelType)
        {
            bool keepWaiting = true;
            bool didContinue = false;

            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            async Promise Func()
            {
                if (cancelType == CancelType.None)
                    await PromiseYielder.WaitWhile(() => keepWaiting);
                else
                    await PromiseYielder.WaitWhile(() => keepWaiting).WithCancelation(cancelToken);
            }

            var promise = Func()
                .Finally(() => didContinue = true);

            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            if (cancelType == CancelType.Delayed)
            {
                cancelSource.Cancel();
                // Continuation won't be invoked until the next cycle.
                Assert.IsFalse(didContinue);
            }
            else
            {
                Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            }

            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);
            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);

            yield return null;
            keepWaiting = false;

            yield return null;
            Assert.IsTrue(didContinue);

            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
            cancelSource.Dispose();
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitWhile_WaitsCorrectlyWithCaptureValue_Async([Values] CancelType cancelType)
        {
            bool keepWaiting = true;
            bool didContinue = false;
            const int captureValue = 42;

            var cancelSource = CancelationSource.New();
            var cancelToken = cancelType == CancelType.Delayed ? cancelSource.Token
                : cancelType == CancelType.Immediate ? CancelationToken.Canceled()
                : CancelationToken.None;

            async Promise Func()
            {
                var waitUntil = PromiseYielder.WaitWhile(captureValue, cv =>
                {
                    Assert.AreEqual(captureValue, cv);
                    return keepWaiting;
                });
                if (cancelType == CancelType.None)
                    await waitUntil;
                else
                    await waitUntil.WithCancelation(cancelToken);
            }

            var promise = Func()
                .Finally(() => didContinue = true);

            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            yield return null;
            Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);

            yield return null;
            if (cancelType == CancelType.Delayed)
            {
                cancelSource.Cancel();
                // Continuation won't be invoked until the next cycle.
                Assert.IsFalse(didContinue);
            }
            else
            {
                Assert.AreEqual(cancelType == CancelType.Immediate, didContinue);
            }

            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);
            yield return null;
            Assert.AreEqual(cancelType != CancelType.None, didContinue);

            yield return null;
            keepWaiting = false;

            yield return null;
            Assert.IsTrue(didContinue);

            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
            cancelSource.Dispose();
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitForFixedUpdate_CompletesInFixedUpdate_Async()
        {
            async Promise Func()
            {
                await PromiseYielder.WaitForFixedUpdate();

                Assert.IsTrue(Time.inFixedTimeStep);
            }

            using (var yieldInstruction = Func().ToYieldInstruction())
            {
                yield return yieldInstruction;
                if (yieldInstruction.State == Promise.State.Rejected)
                    yieldInstruction.GetResult();
            }
        }

        // Not testing WaitForEndOfFrame as there is no way to assert that it is actually in that execution stage.
        // Not testing WaitForAsyncOperation as I don't want to have to load something for unit testing.

        [UnityTest]
        public IEnumerator PromiseYielderWaitForFramesWithProgress_ReportsProgress_Async()
        {
            const int waitFramesCount = 10;

            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            var progress = progressHelper.ToProgress();

            async Promise Func()
            {
                await PromiseYielder.WaitForFrames(waitFramesCount, progress.Token);
                await progress.DisposeAsync();
            }

            Func().Forget();

            for (int currentFrame = 0; currentFrame < waitFramesCount; ++currentFrame)
            {
                progressHelper.AssertCurrentProgress((float) currentFrame / waitFramesCount, false);
                yield return null;
            }
            progressHelper.AssertCurrentProgress(1f, false);
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitForTimeWithProgress_ReportsProgress_Async([Values(0.5f, 1f, 2f)] float timeScale)
        {
            float oldTimeScale = Time.timeScale;
            Time.timeScale = timeScale;

            const float waitSeconds = 2f;

            // Timing can be off a bit, so give it a large delta.
            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous, delta: 1f / 10f);
            var progress = progressHelper.ToProgress();

            async Promise Func()
            {
                await PromiseYielder.WaitForTime(TimeSpan.FromSeconds(waitSeconds), progress.Token);
                await progress.DisposeAsync();
            }

            var promise = Func();

            for (float currentTime = 0f; currentTime < waitSeconds; currentTime += Time.deltaTime)
            {
                progressHelper.AssertCurrentProgress(currentTime / waitSeconds, false);
                yield return null;
            }
            // Timing can be off a bit, so we explicitly wait for the promise to complete.
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                yieldInstruction.GetResult();
            }
            progressHelper.AssertCurrentProgress(1f, false);

            Time.timeScale = oldTimeScale;
        }

        [UnityTest]
        public IEnumerator PromiseYielderWaitForRealTimeWithProgress_ReportsProgress_Async([Values(0.5f, 1f, 2f)] float timeScale)
        {
            float oldTimeScale = Time.timeScale;
            Time.timeScale = timeScale;

            const float waitSeconds = 2f;

            // Timing can be off a bit, so give it a large delta.
            var progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous, delta: 1f / 10f);
            var progress = progressHelper.ToProgress();

            async Promise Func()
            {
                await PromiseYielder.WaitForRealTime(TimeSpan.FromSeconds(waitSeconds), progress.Token);
                await progress.DisposeAsync();
            }

            var promise = Func();

            for (float currentTime = 0f; currentTime < waitSeconds; currentTime += Time.unscaledDeltaTime)
            {
                progressHelper.AssertCurrentProgress(currentTime / waitSeconds, false);
                yield return null;
            }
            // Timing can be off a bit, so we explicitly wait for the promise to complete.
            using (var yieldInstruction = promise.ToYieldInstruction())
            {
                yield return yieldInstruction;
                yieldInstruction.GetResult();
            }
            progressHelper.AssertCurrentProgress(1f, false);

            Time.timeScale = oldTimeScale;
        }
    }
}