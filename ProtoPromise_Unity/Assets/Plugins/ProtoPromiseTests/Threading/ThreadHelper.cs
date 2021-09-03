#if CSHARP_7_3_OR_NEWER && !UNITY_WEBGL

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proto.Promises.Tests.Threading
{
    public class ThreadHelper
    {
        private sealed class ThreadRunner
        {
            private static readonly Stack<ThreadRunner> _pool = new Stack<ThreadRunner>();

            private readonly object _locker = new object();
            private Action _autoAction;
            private Action<Action> _manualAction;
            bool didWait = false;
            private int _offset;
            private Barrier _barrier;
            private TaskCompletionSource<bool> _taskCompletionSource;

            private readonly Action WaitAction;

            private ThreadRunner()
            {
                WaitAction = WaitForBarrier;
            }

            private void WaitForBarrier()
            {
                didWait = true;
                _barrier.SignalAndWait(); // Try to make actions run in lock-step to increase likelihood of breaking race conditions.
                for (int j = _offset; j > 0; --j) { } // Just spin in a loop for the offset.
            }

            public static Task Run(Action action, int offset, Barrier barrier)
            {
                return Run(action, null, offset, barrier);
            }

            public static Task Run(Action<Action> action, int offset, Barrier barrier)
            {
                return Run(null, action, offset, barrier);
            }

            private static Task Run(Action autoAction, Action<Action> manualAction, int offset, Barrier barrier)
            {
                bool reused = false;
                ThreadRunner threadRunner = null;
                lock (_pool)
                {
                    if (_pool.Count > 0)
                    {
                        reused = true;
                        threadRunner = _pool.Pop();
                    }
                }
                if (!reused)
                {
                    threadRunner = new ThreadRunner();
                }
                lock (threadRunner._locker)
                {
                    threadRunner._autoAction = autoAction;
                    threadRunner._manualAction = manualAction;
                    threadRunner._offset = offset;
                    threadRunner._barrier = barrier;
                    var taskSource = threadRunner._taskCompletionSource = new TaskCompletionSource<bool>();
                    if (reused)
                    {
                        Monitor.Pulse(threadRunner._locker);
                    }
                    else
                    {
                        // Thread will never be garbage collected until the application terminates.
                        new Thread(threadRunner.ThreadAction) { IsBackground = true }.Start();
                    }
                    return taskSource.Task;
                }
            }

            private void ThreadAction()
            {
                while (true)
                {
                    Exception exception = null;
                    try
                    {
                        Action autoAction = _autoAction;
                        Action<Action> manualAction = _manualAction;
                        if (autoAction != null)
                        {
                            WaitForBarrier();
                            autoAction.Invoke();
                        }
                        else
                        {
                            didWait = false;
                            manualAction.Invoke(WaitAction);
                            if (!didWait)
                            {
                                throw new InvalidOperationException("Wait action was not invoked.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                    // Allow GC to reclaim memory.
                    TaskCompletionSource<bool> taskSource = _taskCompletionSource;
                    _autoAction = null;
                    _barrier = null;
                    _taskCompletionSource = null;
                    if (exception == null)
                    {
                        taskSource.SetResult(true);
                    }
                    else
                    {
                        taskSource.SetException(exception);
                    }
                    lock (_locker)
                    {
                        lock (_pool)
                        {
                            _pool.Push(this);
                        }
                        Monitor.Wait(_locker);
                    }
                }
            }
        }

        public static readonly int multiExecutionCount = Math.Min(Environment.ProcessorCount * 100, 32766); // Maximum participants Barrier allows is 32767 (15 bits), subtract 1 for main/test thread.
        private static readonly int[] offsets = new int[] { 0, 10, 100 }; // Only using 3 values to not explode test times.

        private readonly Stack<Task> _executingTasks = new Stack<Task>(multiExecutionCount);
        private readonly Barrier _barrier = new Barrier(1);
        private int _currentParticipants = 0;
        private readonly TimeSpan _timeout;

        public ThreadHelper() : this(TimeSpan.FromSeconds(1000)) { } // 1000 second timeout should be enough for most cases (10 seconds for each thread).

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
        public void AddParallelAction(Action action, int offset = 0)
        {
            lock (_executingTasks)
            {
                ++_currentParticipants;
                _barrier.AddParticipant();
                _executingTasks.Push(ThreadRunner.Run(action, offset, _barrier));
            }
        }

        /// <summary>
        /// Add an action and immediately run it in parallel. Call the Action provided to wait for the next Barrier.
        /// </summary>
        public void AddParallelActionSetup(Action<Action> action, int offset = 0)
        {
            lock (_executingTasks)
            {
                ++_currentParticipants;
                _barrier.AddParticipant();
                _executingTasks.Push(ThreadRunner.Run(action, offset, _barrier));
            }
        }

        /// <summary>
        /// Runs the pending actions in parallel, attempting to run them in lock-step.
        /// </summary>
        public void ExecutePendingParallelActions()
        {
            Task[] tasks;
            lock (_executingTasks)
            {
                tasks = _executingTasks.ToArray();
                _executingTasks.Clear();
                _barrier.SignalAndWait();
                _barrier.RemoveParticipants(_currentParticipants);
                _currentParticipants = 0;
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
        /// Run each action in parallel multiple times with differing offsets for each run.
        /// <para/>The number of runs is 3^actions.Length, so be careful if you don't want the test to run too long.
        /// </summary>
        /// <param name="expandToProcessorCount">If true, copies each action on additional threads up to the processor count. This can help test more without increasing the time it takes to complete.
        /// <para/>Example: 2 actions with 6 processors, runs each action 3 times in parallel.</param>
        /// <param name="setup">The action to run before each parallel run.</param>
        /// <param name="teardown">The action to run after each parallel run.</param>
        /// <param name="parallelActions">The actions to run in parallel.</param>
        public void ExecuteParallelActionsWithOffsets(bool expandToProcessorCount, Action setup, Action teardown, params Action[] parallelActions)
        {
            setup += () => { };
            teardown += () => { };
            int actionCount = parallelActions.Length;
            int expandCount = expandToProcessorCount ? Math.Max(Environment.ProcessorCount / actionCount, 1) : 1;

            foreach (var combo in GenerateCombinations(offsets, actionCount))
            {
                setup.Invoke();
                for (int k = 0; k < expandCount; ++k)
                {
                    for (int i = 0; i < actionCount; ++i)
                    {
                        AddParallelAction(parallelActions[i], combo[i]);
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

#endif