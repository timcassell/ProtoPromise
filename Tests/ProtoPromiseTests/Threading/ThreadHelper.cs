#if CSHARP_7_OR_LATER

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proto.Promises.Tests.Threading
{
    public class ThreadHelper
    {
        public static readonly int multiExecutionCount = Math.Min(Environment.ProcessorCount * 100, 32766); // Maximum participants Barrier allows is 32767 (15 bits), subtract 1 for main/test thread.
        private static readonly int[] offsets = new int[] { 0, 10, 100, 1000 };

        private readonly Stack<Task> _executingTasks = new Stack<Task>(multiExecutionCount);
        private readonly Barrier _barrier = new Barrier(1);
        private int _currentParticipants = 0;
        private readonly TimeSpan _timeout;

        public ThreadHelper() : this(TimeSpan.FromSeconds(10)) { } // 10 second timeout should be enough for most cases.

        public ThreadHelper(TimeSpan timeout)
        {
            _timeout = timeout;
        }

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
            ExecutePendingParallelActions();
        }

        /// <summary>
        /// Add an action to be run in parallel.
        /// </summary>
        public void AddParallelAction(Action action)
        {
            var taskSource = new TaskCompletionSource<bool>();
            lock (_executingTasks)
            {
                ++_currentParticipants;
                _barrier.AddParticipant();
                _executingTasks.Push(taskSource.Task);
            }
            new Thread(() =>
            {
                try
                {
                    _barrier.SignalAndWait(); // Try to make actions run in lock-step to increase likelihood of breaking race conditions.
                    action.Invoke();
                    taskSource.SetResult(true);
                }
                catch (Exception e)
                {
                    taskSource.SetException(e);
                }
            }).Start();
        }

        /// <summary>
        /// Runs the pending actions in parallel, attempting to run them in lock-step.
        /// </summary>
        public void ExecutePendingParallelActions()
        {
            Task[] tasks;
            lock (_executingTasks)
            {
                _barrier.SignalAndWait();
                _barrier.RemoveParticipants(_currentParticipants);
                _currentParticipants = 0;
                tasks = _executingTasks.ToArray();
                _executingTasks.Clear();
            }
            try
            {
                if (!Task.WaitAll(tasks, _timeout))
                {
                    throw new TimeoutException($"Action(s) timed out after {_timeout}, there may be a deadlock.");
                }
            }
            catch (AggregateException e)
            {
                // Only throw one exception instead of aggregate to try to avoid overloading the test error output.
                throw new Exception(null, e.Flatten().InnerException);
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

        /// <summary>
        /// Run each action in parallel multiple times with differing offsets for each run.
        /// <para/>The number of runs is 4^actions.Length, so be careful if you don't want the test to run too long.
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
            int actionCount = actions.Length;
            int expandCount = expandToProcessorCount ? Math.Max(Environment.ProcessorCount / actionCount, 1) : 1;
            foreach (var combo in GenerateCombinations(offsets, actionCount))
            {
                setup.Invoke();
                for (int k = 0; k < expandCount; ++k)
                {
                    for (int i = 0; i < actionCount; ++i)
                    {
                        int offset = combo[i];
                        Action action = actions[i];
                        AddParallelAction(() =>
                        {
                            for (int j = offset; j > 0; --j) { } // Just spin in a loop for the offset.
                            action.Invoke();
                        });
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

#endif