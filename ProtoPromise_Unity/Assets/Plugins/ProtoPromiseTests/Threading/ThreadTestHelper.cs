#if !UNITY_WEBGL

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Collections;
using System.Collections.Generic;

// These help test all method for threaded concurrency.
namespace ProtoPromiseTests.Threading
{
    public class MaybeValuesAttribute : ValuesAttribute
    {
        // If progress is enabled, only use a single value instead of all values (otherwise takes a long time to run tests).
        public MaybeValuesAttribute(object arg1)
#if PROMISE_PROGRESS
            : base(arg1)
#else
            : base()
#endif
        { }
    }

    public enum CombineType
    {
        InSetup_NoProgress,
        Parallel_NoProgress,
#if PROMISE_PROGRESS
        InSetup_WithProgress,
        Parallel_WithProgress,
        InSetup_ProgressParallel,
#endif
    }

    public enum ActionPlace
    {
        InSetup,
        InTeardown,
        Parallel,
    }

    public abstract class ParallelCombineTestHelper
    {
        internal CombineType _combineType;
        public bool Success { get; protected set; }

        public abstract void MaybeAddParallelAction(List<Action> parallelActions);
        public abstract void Setup();
        public abstract void Teardown();

        public static ParallelCombineTestHelper Create(CombineType combineType, Func<Promise> combiner)
        {
            return new ParallelCombineTestHelperVoid()
            {
                _combineType = combineType,
                _combiner = combiner
            };
        }

        public static ParallelCombineTestHelper Create<T>(CombineType combineType, Func<Promise<T>> combiner, T expectedResolveValue)
        {
            return new ParallelCombineTestHelperT<T>()
            {
                _combineType = combineType,
                _combiner = combiner,
                _expectedResolveValue = expectedResolveValue
            };
        }

        private class ParallelCombineTestHelperVoid : ParallelCombineTestHelper
        {
            internal Promise _combinedPromise;
            internal Func<Promise> _combiner;

            public override void MaybeAddParallelAction(List<Action> parallelActions)
            {
                switch (_combineType)
                {
                    case CombineType.Parallel_NoProgress:
                        parallelActions.Add(() => _combinedPromise = _combiner());
                        break;
#if PROMISE_PROGRESS
                    case CombineType.Parallel_WithProgress:
                        parallelActions.Add(() => _combinedPromise = _combiner().Progress(v => { }));
                        break;
                    case CombineType.InSetup_ProgressParallel:
                        parallelActions.Add(() => _combinedPromise = _combinedPromise.Progress(v => { }));
                        break;
#endif
                }
            }

            public override void Setup()
            {
                Success = false;
                if (
#if PROMISE_PROGRESS
                    _combineType == CombineType.InSetup_WithProgress)
                {
                    _combinedPromise = _combiner().Progress(v => { });
                }
                else if (_combineType == CombineType.InSetup_ProgressParallel ||
#endif
                    _combineType == CombineType.InSetup_NoProgress)
                {
                    _combinedPromise = _combiner();
                }
            }

            public override void Teardown()
            {
                _combinedPromise.ContinueWith(r => Success = true).Forget();
                _combinedPromise = default(Promise);
            }
        }

        private class ParallelCombineTestHelperT<T> : ParallelCombineTestHelper
        {
            internal Promise<T> _combinedPromise;
            internal Func<Promise<T>> _combiner;
            internal T _expectedResolveValue;

            public override void MaybeAddParallelAction(List<Action> parallelActions)
            {
                switch (_combineType)
                {
                    case CombineType.Parallel_NoProgress:
                        parallelActions.Add(() => _combinedPromise = _combiner());
                        break;
#if PROMISE_PROGRESS
                    case CombineType.Parallel_WithProgress:
                        parallelActions.Add(() => _combinedPromise = _combiner().Progress(v => { }));
                        break;
                    case CombineType.InSetup_ProgressParallel:
                        parallelActions.Add(() => _combinedPromise = _combinedPromise.Progress(v => { }));
                        break;
#endif
                }
            }

            public override void Setup()
            {
                Success = false;
                if (
#if PROMISE_PROGRESS
                    _combineType == CombineType.InSetup_WithProgress)
                {
                    _combinedPromise = _combiner().Progress(v => { });
                }
                else if (_combineType == CombineType.InSetup_ProgressParallel ||
#endif
                    _combineType == CombineType.InSetup_NoProgress)
                {
                    _combinedPromise = _combiner();
                }
            }

            public override void Teardown()
            {
                _combinedPromise
                    .ContinueWith(r =>
                    {
                        if (r.State == Promise.State.Resolved)
                        {
                            if (_expectedResolveValue is IEnumerable)
                            {
                                CollectionAssert.AreEqual((IEnumerable) _expectedResolveValue, (IEnumerable) r.Result);
                            }
                            else
                            {
                                Assert.AreEqual(_expectedResolveValue, r.Result);
                            }
                        }
                        Success = true;
                    })
                    .Forget();
                _combinedPromise = default(Promise<T>);
            }
        }
    }

    public class ParallelActionTestHelper
    {
        private ActionPlace _actionPlace;
        private Action _action;
        private int _repeatCount;

        private ParallelActionTestHelper() { }

        public void MaybeAddParallelAction(List<Action> parallelActions)
        {
            if (_actionPlace == ActionPlace.Parallel)
            {
                for (int i = 0; i < _repeatCount; ++i)
                {
                    parallelActions.Add(_action);
                }
            }
        }
        
        public void Setup()
        {
            if (_actionPlace == ActionPlace.InSetup)
            {
                for (int i = 0; i < _repeatCount; ++i)
                {
                    _action();
                }
            }
        }

        public void Teardown()
        {
            if (_actionPlace == ActionPlace.InTeardown)
            {
                for (int i = 0; i < _repeatCount; ++i)
                {
                    _action();
                }
            }
        }

        public static ParallelActionTestHelper Create(ActionPlace actionPlace, int repeatCount, Action action)
        {
            return new ParallelActionTestHelper()
            {
                _actionPlace = actionPlace,
                _action = action,
                _repeatCount = repeatCount
            };
        }
    }
}

#endif // !UNITY_WEBGL