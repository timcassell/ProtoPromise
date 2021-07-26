#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#if CSHARP_7_OR_LATER

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

// These help test all method for threaded concurrency.
namespace Proto.Promises.Tests
{
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

    public static partial class TestHelper
    {
        public static Action<Promise>[] ResolveActionsVoid(Action onResolved)
        {
            return new Action<Promise>[8]
            {
                promise => promise.Then(() => onResolved()).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(1, cv => onResolved()).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }).Forget()
            };
        }

        public static Action<Promise<T>>[] ResolveActions<T>(Action<T> onResolved)
        {
            return new Action<Promise<T>>[8]
            {
                promise => promise.Then(v => onResolved(v)).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(1, (cv, v) => onResolved(v)).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }).Forget(),
            };
        }

        public static Action<Promise>[] ThenActionsVoid(Action onResolved, Action onRejected)
        {
            return new Action<Promise>[64]
            {
                promise => promise.Then(() => onResolved(), () => onRejected()).Forget(),
                promise => promise.Then(() => onResolved(), (object o) => onRejected()).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, () => onRejected()).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, (object o) => onRejected()).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, (object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(() => onResolved(), () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => onResolved(), (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(1, cv => onResolved(), () => onRejected()).Forget(),
                promise => promise.Then(1, cv => onResolved(), (object o) => onRejected()).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, () => onRejected()).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, (object o) => onRejected()).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, (object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(1, cv => onResolved(), () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => onResolved(), (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(() => onResolved(), 1, cv => onRejected()).Forget(),
                promise => promise.Then(() => onResolved(), 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, cv => onRejected()).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(() => onResolved(), 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => onResolved(), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(1, cv => onResolved(), 1, cv => onRejected()).Forget(),
                promise => promise.Then(1, cv => onResolved(), 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, cv => onRejected()).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(1, cv => onResolved(), 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => onResolved(), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
            };
        }

        public static Action<Promise<T>>[] ThenActions<T>(Action<T> onResolved, Action onRejected)
        {
            return new Action<Promise<T>>[64]
            {
                promise => promise.Then(v => onResolved(v), () => onRejected()).Forget(),
                promise => promise.Then(v => onResolved(v), (object o) => onRejected()).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, () => onRejected()).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, (object o) => onRejected()).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, (object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(v => onResolved(v), () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => onResolved(v), (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(1, (cv, v) => onResolved(v), () => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => onResolved(v), (object o) => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, () => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, (object o) => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, (object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(1, (cv, v) => onResolved(v), () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => onResolved(v), (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(v => onResolved(v), 1, cv => onRejected()).Forget(),
                promise => promise.Then(v => onResolved(v), 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, cv => onRejected()).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(v => onResolved(v), 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => onResolved(v), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(1, (cv, v) => onResolved(v), 1, cv => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => onResolved(v), 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, cv => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(1, (cv, v) => onResolved(v), 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => onResolved(v), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget()
            };
        }

        public static Action<Promise>[] CatchActionsVoid(Action onRejected)
        {
            return new Action<Promise>[8]
            {
                promise => promise.Catch(() => onRejected()).Forget(),
                promise => promise.Catch(() => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Catch((object o) => onRejected()).Forget(),
                promise => promise.Catch((object o) => { onRejected(); return Promise.Resolved(); }).Forget(),

                promise => promise.Catch(1, cv => onRejected()).Forget(),
                promise => promise.Catch(1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Catch(1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Catch(1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget()
            };
        }

        public static Action<Promise<T>>[] CatchActions<T>(Action onRejected)
        {
            return new Action<Promise<T>>[8]
            {
                promise => promise.Catch(() => { onRejected(); return default(T); }).Forget(),
                promise => promise.Catch(() => { onRejected(); return Promise.Resolved(default(T)); }).Forget(),
                promise => promise.Catch((object o) => { onRejected(); return default(T); }).Forget(),
                promise => promise.Catch((object o) => { onRejected(); return Promise.Resolved(default(T)); }).Forget(),

                promise => promise.Catch(1, cv => { onRejected(); return default(T); }).Forget(),
                promise => promise.Catch(1, cv => { onRejected(); return Promise.Resolved(default(T)); }).Forget(),
                promise => promise.Catch(1, (int cv, object o) => { onRejected(); return default(T); }).Forget(),
                promise => promise.Catch(1, (int cv, object o) => { onRejected(); return Promise.Resolved(default(T)); }).Forget(),
            };
        }

        public static Action<Promise>[] ContinueWithActionsVoid(Action onContinue)
        {
            return new Action<Promise>[8]
            {
                promise => promise.ContinueWith(_ => onContinue()).Forget(),
                promise => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(); }).Forget(),
                promise => promise.ContinueWith(_ => { onContinue(); return 1; }).Forget(),
                promise => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(1); }).Forget(),

                promise => promise.ContinueWith(1, (int cv, Promise.ResultContainer _) => onContinue()).Forget(),
                promise => promise.ContinueWith(1, (int cv, Promise.ResultContainer _) => { onContinue(); return Promise.Resolved(); }).Forget(),
                promise => promise.ContinueWith(1, (int cv, Promise.ResultContainer _) => { onContinue(); return 1; }).Forget(),
                promise => promise.ContinueWith(1, (int cv, Promise.ResultContainer _) => { onContinue(); return Promise.Resolved(1); }).Forget()
            };
        }

        public static Action<Promise<T>>[] ContinueWithActions<T>(Action onContinue)
        {
            return new Action<Promise<T>>[8]
            {
                promise => promise.ContinueWith(_ => onContinue()).Forget(),
                promise => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(); }).Forget(),
                promise => promise.ContinueWith(_ => { onContinue(); return 1; }).Forget(),
                promise => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(1); }).Forget(),

                promise => promise.ContinueWith(1, (int cv, Promise<T>.ResultContainer _) => onContinue()).Forget(),
                promise => promise.ContinueWith(1, (int cv, Promise<T>.ResultContainer _) => { onContinue(); return Promise.Resolved(); }).Forget(),
                promise => promise.ContinueWith(1, (int cv, Promise<T>.ResultContainer _) => { onContinue(); return 1; }).Forget(),
                promise => promise.ContinueWith(1, (int cv, Promise<T>.ResultContainer _) => { onContinue(); return Promise.Resolved(1); }).Forget()
            };
        }
        public static Func<Promise, CancelationToken, Promise>[] ResolveActionsVoidWithCancelation(Action onResolved)
        {
            return new Func<Promise, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.Then(() => onResolved(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, cancelationToken)
            };
        }

        public static Func<Promise<T>, CancelationToken, Promise>[] ResolveActionsWithCancelation<T>(Action<T> onResolved)
        {
            return new Func<Promise<T>, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.Then(v => onResolved(v), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, cancelationToken),
            };
        }

        public static Func<Promise, CancelationToken, Promise>[] ThenActionsVoidWithCancelation(Action onResolved, Action onRejected)
        {
            return new Func<Promise, CancelationToken, Promise>[64]
            {
                (promise, cancelationToken) => promise.Then(() => onResolved(), () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => onResolved(), (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, (object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(() => onResolved(), () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => onResolved(), (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, (object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(() => onResolved(), 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => onResolved(), 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(() => onResolved(), 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => onResolved(), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
            };
        }

        public static Func<Promise<T>, CancelationToken, Promise>[] ThenActionsWithCancelation<T>(Action<T> onResolved, Action onRejected)
        {
            return new Func<Promise<T>, CancelationToken, Promise>[64]
            {
                (promise, cancelationToken) => promise.Then(v => onResolved(v), () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => onResolved(v), (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, (object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(v => onResolved(v), () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => onResolved(v), (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, (object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(v => onResolved(v), 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => onResolved(v), 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(v => onResolved(v), 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => onResolved(v), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken)
            };
        }

        public static Func<Promise, CancelationToken, Promise>[] CatchActionsVoidWithCancelation(Action onRejected)
        {
            return new Func<Promise, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.Catch(() => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Catch(() => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch((object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Catch((object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),

                (promise, cancelationToken) => promise.Catch(1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Catch(1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch(1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Catch(1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken)
            };
        }

        public static Func<Promise<T>, CancelationToken, Promise>[] CatchActionsWithCancelation<T>(Action onRejected)
        {
            return new Func<Promise<T>, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.Catch(() => { onRejected(); return default(T); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch(() => { onRejected(); return Promise.Resolved(default(T)); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch((object o) => { onRejected(); return default(T); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch((object o) => { onRejected(); return Promise.Resolved(default(T)); }, cancelationToken),

                (promise, cancelationToken) => promise.Catch(1, cv => { onRejected(); return default(T); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch(1, cv => { onRejected(); return Promise.Resolved(default(T)); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch(1, (int cv, object o) => { onRejected(); return default(T); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch(1, (int cv, object o) => { onRejected(); return Promise.Resolved(default(T)); }, cancelationToken),
            };
        }

        public static Func<Promise, CancelationToken, Promise>[] ContinueWithActionsVoidWithCancelation(Action onContinue)
        {
            return new Func<Promise, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.ContinueWith(_ => onContinue(), cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(_ => { onContinue(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.ContinueWith(1, (int cv, Promise.ResultContainer _) => onContinue(), cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(1, (int cv, Promise.ResultContainer _) => { onContinue(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(1, (int cv, Promise.ResultContainer _) => { onContinue(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(1, (int cv, Promise.ResultContainer _) => { onContinue(); return Promise.Resolved(1); }, cancelationToken)
            };
        }

        public static Func<Promise<T>, CancelationToken, Promise>[] ContinueWithActionsWithCancelation<T>(Action onContinue)
        {
            return new Func<Promise<T>, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.ContinueWith(_ => onContinue(), cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(_ => { onContinue(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.ContinueWith(1, (int cv, Promise<T>.ResultContainer _) => onContinue(), cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(1, (int cv, Promise<T>.ResultContainer _) => { onContinue(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(1, (int cv, Promise<T>.ResultContainer _) => { onContinue(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(1, (int cv, Promise<T>.ResultContainer _) => { onContinue(); return Promise.Resolved(1); }, cancelationToken)
            };
        }
    }
}

#endif