﻿using Proto.Promises;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace ProtoPromise.Tests.Concurrency
{
    public class ThreadHelper
    {
        private sealed class ThreadRunner
        {
            private static readonly SendOrPostCallback _threadCallback = ThreadAction;

            private Action _autoAction;
            private Action<Action> _manualAction;
            bool didWait = false;
            private int _offset;
            private ThreadMerger _merger;

            private readonly Action WaitAction;

            private ThreadRunner()
            {
                WaitAction = WaitForBarrier;
            }

            private void WaitForBarrier()
            {
                didWait = true;
                _merger._barrier.SignalAndWait(); // Try to make actions run in lock-step to increase likelihood of breaking race conditions.
                for (int j = _offset; j > 0; --j) { } // Just spin in a loop for the offset.
            }

            public static void Run(Action action, int offset, ThreadMerger merger)
            {
                Run(action, null, offset, merger);
            }

            public static void Run(Action<Action> action, int offset, ThreadMerger merger)
            {
                Run(null, action, offset, merger);
            }

            private static void Run(Action autoAction, Action<Action> manualAction, int offset, ThreadMerger merger)
            {
                ThreadRunner runner = new ThreadRunner()
                {
                    _autoAction = autoAction,
                    _manualAction = manualAction,
                    _offset = offset,
                    _merger = merger
                };
                TestHelper._backgroundContext.Post(_threadCallback, runner);
            }

            private static void ThreadAction(object state)
            {
                ((ThreadRunner) state).Execute();
            }

            private void Execute()
            {
                ThreadMerger merger = _merger;
                try
                {
                    if (_autoAction != null)
                    {
                        WaitForBarrier();
                        _autoAction.Invoke();
                    }
                    else
                    {
                        didWait = false;
                        _manualAction.Invoke(WaitAction);
                        if (!didWait)
                        {
                            throw new System.InvalidOperationException("Wait action was not invoked.");
                        }
                    }
                }
                catch (Exception e)
                {
                    // Only reporting one exception instead of aggregate.
                    merger._executionExceptionInfo = ExceptionDispatchInfo.Capture(e);
                }
                Interlocked.Decrement(ref merger._currentParticipants);
            }
        }

        // Used to wait for all threads and propagate exceptions without using Tasks (for old runtime).
        private sealed class ThreadMerger
        {
            public readonly Barrier _barrier = new Barrier(1);
            volatile public int _currentParticipants = 0;
            // Only reporting one exception instead of aggregate.
            // This is so we don't have to wait on all actions to complete when 1 has errored, and also so we don't overload the test error output.
            volatile public ExceptionDispatchInfo _executionExceptionInfo = null;
        }

        public static readonly int multiExecutionCount = Math.Min(Environment.ProcessorCount * 100, 32766); // Maximum participants Barrier allows is 32767 (15 bits), subtract 1 for main/test thread.
        private static readonly int[] offsets = new int[] { 0, 10, 100 }; // Only using 3 values to not explode test times.

        private readonly object _locker = new object();
        private ThreadMerger _threadMerger = new ThreadMerger();

        /// <summary>
        /// Execute the action multiple times in parallel threads.
        /// </summary>
        public void ExecuteMultiActionParallel(Action action)
        {
            for (int i = 0; i < multiExecutionCount; ++i)
            {
                AddParallelAction(action);
            }
            ExecutePendingParallelActions();
        }

        /// <summary>
        /// Execute the action once in a separate thread.
        /// </summary>
        public void ExecuteSingleAction(Action action)
        {
            AddParallelAction(action);
            ExecutePendingParallelActions(Timeout.InfiniteTimeSpan);
        }

        public void ExecuteSynchronousOrOnThread(Action action, bool synchronous)
        {
            if (synchronous)
            {
                action();
            }
            else
            {
                ExecuteSingleAction(action);
            }
        }

        /// <summary>
        /// Add an action to be run in parallel.
        /// </summary>
        public void AddParallelAction(Action action, int offset = 0)
        {
            lock (_locker)
            {
                ++_threadMerger._currentParticipants;
                _threadMerger._barrier.AddParticipant();
                ThreadRunner.Run(action, offset, _threadMerger);
            }
        }

        /// <summary>
        /// Add an action and immediately run it in parallel. Call the Action provided to wait for the next Barrier.
        /// </summary>
        public void AddParallelActionSetup(Action<Action> action, int offset = 0)
        {
            lock (_locker)
            {
                ++_threadMerger._currentParticipants;
                _threadMerger._barrier.AddParticipant();
                ThreadRunner.Run(action, offset, _threadMerger);
            }
        }

        /// <summary>
        /// Runs the pending actions in parallel, attempting to run them in lock-step.
        /// Throws a <see cref="TimeoutException"/> if <paramref name="timeoutPerAction"/> times the number of actions is exceeded (default 1s).
        /// </summary>
        public void ExecutePendingParallelActions(TimeSpan timeoutPerAction = default(TimeSpan))
        {
            if (timeoutPerAction == default(TimeSpan))
            {
                timeoutPerAction = TimeSpan.FromSeconds(1);
            }
            ThreadMerger merger;
            lock (_locker)
            {
                merger = _threadMerger;
                _threadMerger = new ThreadMerger();
            }
            int numActions = merger._currentParticipants;
            merger._barrier.SignalAndWait();

            TimeSpan timeout = TimeSpan.FromTicks(timeoutPerAction.Ticks * numActions);
            // Don't use SpinWait.SpinUntil. https://github.com/dotnet/runtime/issues/115989#issuecomment-2920674169
            bool timedOut = true;
            var spinner = new System.Threading.SpinWait();
            var timestamp = TimeProvider.System.GetTimestamp();
            do
            {
                if (merger._currentParticipants <= 0 || merger._executionExceptionInfo != null)
                {
                    timedOut = false;
                    break;
                }
                spinner.SpinOnce(sleep1Threshold: -1);
            } while (timeout < TimeSpan.Zero || TimeProvider.System.GetElapsedTime(timestamp) < timeout);

            merger._executionExceptionInfo?.Throw();
            if (timedOut)
            {
                throw new TimeoutException(numActions + " Action(s) timed out after " + timeout + ", there may be a deadlock. Remaining Actions: " + merger._currentParticipants);
            }
        }

        /// <summary>
        /// Run each action in parallel.
        /// </summary>
        /// <param name="repeatCount">How many times to run the actions in parallel.</param>
        /// <param name="setup">The action to run before each parallel run.</param>
        /// <param name="teardown">The action to run after each parallel run.</param>
        /// <param name="actions">The actions to run in parallel.</param>
        public void ExecuteParallelActions(int repeatCount, Action setup, Action teardown, params Action[] actions)
        {
            setup += () => { };
            teardown += () => { };
            if (actions.Length <= 1)
            {
                setup.Invoke();
                if (actions.Length == 1)
                {
                    actions[0].Invoke();
                }
                teardown.Invoke();
                return;
            }

            int actionCount = actions.Length;
            for (int k = 0; k < repeatCount; ++k)
            {
                setup.Invoke();
                for (int i = 0; i < actionCount; ++i)
                {
                    AddParallelAction(actions[i]);
                }
                ExecutePendingParallelActions();
                teardown.Invoke();
            }
        }

        public void ExecuteParallelActionsMaybeWithOffsets(Action setup, Action teardown, List<Action> actions)
        {
            // If there are too many actions, don't use offsets because it takes too long to run tests.
            if (actions.Count > 8)
            {
                ExecuteParallelActions(multiExecutionCount, setup, teardown, actions.ToArray());
            }
            else
            {
                ExecuteParallelActionsWithOffsets(false, setup, teardown, actions.ToArray());
            }
        }

        /// <summary>
        /// Get how many times each parallel action will be invoked if parallel actions are expanded to processor count.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public static int GetExpandCount(int count)
        {
            return Math.Max(Environment.ProcessorCount / count, 1);
        }

        /// <summary>
        /// Run each action in parallel multiple times with differing offsets for each run.
        /// <para/>The number of runs is 3^actions.Length, so be careful if you don't want the test to run too long.
        /// </summary>
        /// <param name="expandToProcessorCount">If true, copies each action on additional threads up to the processor count. This can help test more without increasing the time it takes to complete.
        /// <para/>Example: 2 actions with 6 processors, runs each action 3 times in parallel.</param>
        /// <param name="setup">The action to run before each parallel run.</param>
        /// <param name="teardown">The action to run after each parallel run.</param>
        /// <param name="actions">The actions to run in parallel.</param>
        public void ExecuteParallelActionsWithOffsets(bool expandToProcessorCount, Action setup, Action teardown, params Action[] actions)
        {
            setup += () => { };
            teardown += () => { };
            if (actions.Length <= 1 && !expandToProcessorCount)
            {
                setup.Invoke();
                if (actions.Length == 1)
                {
                    actions[0].Invoke();
                }
                teardown.Invoke();
                return;
            }

            int actionCount = actions.Length;
            int expandCount = expandToProcessorCount ? GetExpandCount(actionCount) : 1;

            foreach (var combo in GenerateCombinations(offsets, actionCount))
            {
                setup.Invoke();
                for (int k = 0; k < expandCount; ++k)
                {
                    for (int i = 0; i < actionCount; ++i)
                    {
                        AddParallelAction(actions[i], combo[i]);
                    }
                }
                ExecutePendingParallelActions();
                teardown.Invoke();
            }
        }

        /// <summary>
        /// Add actions with parallel setup.
        /// Call the Action sent to the parallel action setup to wait for the next Barrier.
        /// </summary>
        public void ExecuteParallelActionsWithOffsetsAndSetup(Action setup, Action<Action>[] parallelActionsSetup, Action[] parallelActions, Action teardown)
        {
            setup += () => { };
            teardown += () => { };
            int actionCount = parallelActionsSetup.Length + parallelActions.Length;

            foreach (var combo in GenerateCombinations(offsets, actionCount))
            {
                setup.Invoke();
                for (int i = 0; i < actionCount; ++i)
                {
                    if (i < parallelActionsSetup.Length)
                    {
                        AddParallelActionSetup(parallelActionsSetup[i], combo[i]);
                    }
                    else
                    {
                        AddParallelAction(parallelActions[i - parallelActionsSetup.Length], combo[i]);
                    }
                }
                ExecutePendingParallelActions();
                teardown.Invoke();
            }
        }

        // Input: [1, 2, 3], 3
        // Ouput: [
        //          [1, 1, 1],
        //          [2, 1, 1],
        //          [3, 1, 1],
        //          [1, 2, 1],
        //          [2, 2, 1],
        //          [3, 2, 1],
        //          [1, 3, 1],
        //          [2, 3, 1],
        //          [3, 3, 1],
        //          [1, 1, 2],
        //          [2, 1, 2],
        //          [3, 1, 2],
        //          [1, 2, 2],
        //          [2, 2, 2],
        //          [3, 2, 2],
        //          [1, 3, 2],
        //          [2, 3, 2],
        //          [3, 3, 2],
        //          [1, 1, 3],
        //          [2, 1, 3],
        //          [3, 1, 3],
        //          [1, 2, 3],
        //          [2, 2, 3],
        //          [3, 2, 3],
        //          [1, 3, 3],
        //          [2, 3, 3],
        //          [3, 3, 3]
        //        ]
        private static IEnumerable<int[]> GenerateCombinations(int[] options, int count)
        {
            int[] indexTracker = new int[count];
            int[] combo = new int[count];
            for (int i = 0; i < count; ++i)
            {
                combo[i] = options[0];
            }
            // Same algorithm as picking a combination lock.
            int rollovers = 0;
            while (rollovers < count)
            {
                yield return combo; // No need to duplicate the array since we're just reading it.
                for (int i = 0; i < count; ++i)
                {
                    int index = ++indexTracker[i];
                    if (index == options.Length)
                    {
                        indexTracker[i] = 0;
                        combo[i] = options[0];
                        if (i == rollovers)
                        {
                            ++rollovers;
                        }
                    }
                    else
                    {
                        combo[i] = options[index];
                        break;
                    }
                }
            }
        }
    }
}
