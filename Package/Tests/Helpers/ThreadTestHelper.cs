#if !UNITY_WEBGL

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable 0618 // Type or member is obsolete

// These help test all method for threaded concurrency.
namespace ProtoPromiseTests.Concurrency
{
    public enum CombineType
    {
        InSetup,
        Parallel,
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
                if (_combineType == CombineType.Parallel)
                {
                    parallelActions.Add(() => _combinedPromise = _combiner());
                }
            }

            public override void Setup()
            {
                Success = false;
                if (_combineType == CombineType.InSetup)
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
                if (_combineType == CombineType.Parallel)
                {
                    parallelActions.Add(() => _combinedPromise = _combiner());
                }
            }

            public override void Setup()
            {
                Success = false;
                if (_combineType == CombineType.InSetup)
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
                                CollectionAssert.AreEqual((IEnumerable) _expectedResolveValue, (IEnumerable) r.Value);
                            }
                            else
                            {
                                Assert.AreEqual(_expectedResolveValue, r.Value);
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