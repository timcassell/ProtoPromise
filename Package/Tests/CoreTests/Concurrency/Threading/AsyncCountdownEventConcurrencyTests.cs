#if !UNITY_WEBGL

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProtoPromiseTests.Concurrency.Threading
{
    public class AsyncCountdownEventConcurrencyTests
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
        public void AsyncCountdownEvent_WaitCalledConcurrently_AlreadySet()
        {
            int invokedCount = 0;
            var ce = new AsyncCountdownEvent(0);
            Action parallelAction = () =>
            {
                ce.Wait();
                Interlocked.Increment(ref invokedCount);
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                },
                // teardown
                () =>
                {
                    Assert.AreEqual(4, invokedCount);
                },
                // parallel actions, repeated to generate offsets
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction
            );
        }

        private static IEnumerable<TestCaseData> GetCountsAndSeparate()
        {
            yield return new TestCaseData(1, true);
            yield return new TestCaseData(2, true);
            yield return new TestCaseData(2, false);
        }

        [Test, TestCaseSource(nameof(GetCountsAndSeparate))]
        public void AsyncCountdownEvent_WaitAndSignalCalledConcurrently(int count, bool separateSignals)
        {
            int invokedCount = 0;
            var ce = new AsyncCountdownEvent(count);
            Action parallelAction = () =>
            {
                ce.Wait();
                Interlocked.Increment(ref invokedCount);
            };

            var actions = new List<Action>(6)
            {
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction
            };
            if (separateSignals)
            {
                for (int i = 0; i < count; ++i)
                {
                    actions.Add(() => ce.Signal());
                }
            }
            else
            {
                actions.Add(() => ce.Signal(count));
            }

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                    ce.Reset();
                },
                // teardown
                () =>
                {
                    Assert.AreEqual(4, invokedCount);
                },
                // parallel actions
                actions.ToArray()
            );
        }

        [Test]
        public void AsyncCountdownEvent_WaitAndCancelCalledConcurrently_AlreadySet()
        {
            int invokedCount = 0;
            var ce = new AsyncCountdownEvent(0);
            var cancelationSource = default(CancelationSource);
            Action parallelAction = () =>
            {
                ce.TryWait(cancelationSource.Token);
                Interlocked.Increment(ref invokedCount);
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                    cancelationSource = CancelationSource.New();
                },
                // teardown
                () =>
                {
                    Assert.AreEqual(4, invokedCount);
                    cancelationSource.Dispose();
                },
                // parallel actions, repeated to generate offsets
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction,
                () => cancelationSource.Cancel()
            );
        }

        [Test, TestCaseSource(nameof(GetCountsAndSeparate))]
        public void AsyncCountdownEvent_WaitAndSignalAndCancelCalledConcurrently(int count, bool separateSignals)
        {
            int invokedCount = 0;
            var ce = new AsyncCountdownEvent(count);
            var cancelationSource = default(CancelationSource);
            Action parallelAction = () =>
            {
                ce.TryWait(cancelationSource.Token);
                Interlocked.Increment(ref invokedCount);
            };

            var actions = new List<Action>(6)
            {
                parallelAction,
                parallelAction,
                parallelAction,
                () => cancelationSource.Cancel()
            };
            if (separateSignals)
            {
                for (int i = 0; i < count; ++i)
                {
                    actions.Add(() => ce.Signal());
                }
            }
            else
            {
                actions.Add(() => ce.Signal(count));
            }

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                    cancelationSource = CancelationSource.New();
                    ce.Reset();
                },
                // teardown
                () =>
                {
                    Assert.AreEqual(3, invokedCount);
                    cancelationSource.Dispose();
                },
                // parallel actions
                actions.ToArray()
            );
        }

        [Test]
        public void AsyncCountdownEvent_WaitAsyncCalledConcurrently_AlreadySet()
        {
            int invokedCount = 0;
            var ce = new AsyncCountdownEvent(0);
            Action parallelAction = () =>
            {
                ce.WaitAsync()
                    .Then(() => Interlocked.Increment(ref invokedCount))
                    .Forget();
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                },
                // teardown
                () =>
                {
                    Assert.AreEqual(4, invokedCount);
                },
                // parallel actions, repeated to generate offsets
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction
            );
        }

        [Test, TestCaseSource(nameof(GetCountsAndSeparate))]
        public void AsyncCountdownEvent_WaitAsyncAndSetCalledConcurrently(int count, bool separateSignals)
        {
            int invokedCount = 0;
            var ce = new AsyncCountdownEvent(count);
            Action parallelAction = () =>
            {
                ce.WaitAsync()
                    .Then(() => Interlocked.Increment(ref invokedCount))
                    .Forget();
            };

            var actions = new List<Action>(6)
            {
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction
            };
            if (separateSignals)
            {
                for (int i = 0; i < count; ++i)
                {
                    actions.Add(() => ce.Signal());
                }
            }
            else
            {
                actions.Add(() => ce.Signal(count));
            }

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                    ce.Reset();
                },
                // teardown
                () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    Assert.AreEqual(4, invokedCount);
                },
                // parallel actions, repeated to generate offsets
                actions.ToArray()
            );
        }

        [Test]
        public void AsyncCountdownEvent_WaitAsyncAndCancelCalledConcurrently_AlreadySet()
        {
            int invokedCount = 0;
            var ce = new AsyncCountdownEvent(0);
            var cancelationSource = default(CancelationSource);
            Action parallelAction = () =>
            {
                ce.TryWaitAsync(cancelationSource.Token)
                    .Then(() => Interlocked.Increment(ref invokedCount))
                    .Forget();
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                    cancelationSource = CancelationSource.New();
                },
                // teardown
                () =>
                {
                    Assert.AreEqual(4, invokedCount);
                    cancelationSource.Dispose();
                },
                // parallel actions, repeated to generate offsets
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction,
                () => cancelationSource.Cancel()
            );
        }

        [Test, TestCaseSource(nameof(GetCountsAndSeparate))]
        public void AsyncCountdownEvent_WaitAsyncAndSetAndCancelCalledConcurrently(int count, bool separateSignals)
        {
            int invokedCount = 0;
            var ce = new AsyncCountdownEvent(count);
            var cancelationSource = default(CancelationSource);
            Action parallelAction = () =>
            {
                ce.TryWaitAsync(cancelationSource.Token)
                    .Then(() => Interlocked.Increment(ref invokedCount))
                    .Forget();
            };

            var actions = new List<Action>(6)
            {
                parallelAction,
                parallelAction,
                parallelAction,
                () => cancelationSource.Cancel()
            };
            if (separateSignals)
            {
                for (int i = 0; i < count; ++i)
                {
                    actions.Add(() => ce.Signal());
                }
            }
            else
            {
                actions.Add(() => ce.Signal(count));
            }

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                    cancelationSource = CancelationSource.New();
                    ce.Reset();
                },
                // teardown
                () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    Assert.AreEqual(3, invokedCount);
                    cancelationSource.Dispose();
                },
                // parallel actions
                actions.ToArray()
            );
        }
    }
}

#endif // !UNITY_WEBGL