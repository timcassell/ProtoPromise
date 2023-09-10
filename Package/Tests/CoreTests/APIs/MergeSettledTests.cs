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
using System.Linq;
using System.Runtime.CompilerServices;

#if UNITY_5_5_OR_NEWER && !NET_LEGACY && !UNITY_2021_2_OR_NEWER
// ITuple interface was added in net471, which Unity 2018 and 2019 don't support.
namespace System.Runtime.CompilerServices
{
    public interface ITuple
    {
        int Length { get; }

        object this[int index] { get; }
    }
}
#endif

namespace ProtoPromiseTests.APIs
{
    public class MergeSettledTests
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

        private class TupleWrapper : ITuple
        {
            private readonly ArrayList list = new ArrayList(7);

            public object this[int index] { get { return list[index]; } }

            public int Length { get { return list.Count; } }

            internal TupleWrapper(object tuple)
            {
                var type = tuple.GetType();
                for (int i = 1; i <= 7; ++i)
                {
                    var fieldInfo = type.GetField("Item" + i);
                    if (fieldInfo == null) break;

                    list.Add(fieldInfo.GetValue(tuple));
                }
            }
        }

        // There are a lot of MergeSettled methods, it's easier to use reflection to test them all.
        public class MergeSettledArg
        {
            public CompleteType completeType;
            public object completeValue;
            public bool alreadyComplete;

            public Type Type
            {
                get
                {
                    return completeValue == null ? null : completeValue.GetType();
                }
            }

            public override string ToString()
            {
                var strings = new string[3]
                {
                    completeType.ToString(),
                    completeValue == null ? "void" : completeValue.ToString(),
                    alreadyComplete ? "complete" : "pending"
                };
                return "{" + string.Join(",", strings) + "}";
            }
        }

        private static readonly object[] possibleCompleteValues = new object[7]
        {
            "a",
            'b',
            (byte) 1,
            -2,
            1.5f,
            true,
            double.PositiveInfinity
        };

        private static readonly CompleteType[] completeTypes = new CompleteType[]
        {
            CompleteType.Resolve,
            CompleteType.Reject,
            CompleteType.Cancel
        };

        private static IEnumerable<TestCaseData> GetArgs()
        {
            // MergeSettled functions accept between 2 and 7 args
            for (int i = 2; i <= 7; ++i)
            {
                foreach (var genericArgs in GetArgumentsAreGeneric(i))
                {
                    List<MergeSettledArg>[] potentialArgs = new List<MergeSettledArg>[i];
                    for (int j = 0; j < genericArgs.Length; ++j)
                    {
                        potentialArgs[j] = GetMergeSettledArgs(genericArgs[j], j).ToList();
                    }
                    foreach (var args in GenerateCombinations(potentialArgs))
                    {
                        yield return new TestCaseData((object) args);

                        // Number of test cases explodes exponentially if we run every combination of alreadyComplete,
                        // so to keep test times reasonable, we only set 1 arg to alreadyComplete.
                        var argsWithAlreadyComplete = args.Select(arg => new MergeSettledArg()
                            {
                                completeType = arg.completeType,
                                completeValue = arg.completeValue,
                                alreadyComplete = arg.alreadyComplete
                            })
                            .ToArray();
                        argsWithAlreadyComplete[0].alreadyComplete = true;
                        yield return new TestCaseData((object) argsWithAlreadyComplete);
                    }
                }
            }
        }

        private static IEnumerable<MergeSettledArg> GetMergeSettledArgs(bool isGeneric, int index)
        {
            // In order to keep the number of test cases down, we only do all combinations for the first 3 args.
            if (index >= 3)
            {
                yield return new MergeSettledArg()
                {
                    completeType = CompleteType.Resolve,
                    completeValue = isGeneric ? possibleCompleteValues[index] : null,
                    alreadyComplete = false
                };
                yield break;
            }

            foreach (var completeType in completeTypes)
            {
                yield return new MergeSettledArg()
                {
                    completeType = completeType,
                    completeValue = isGeneric ? possibleCompleteValues[index] : null,
                    alreadyComplete = false
                };
            }
        }

        // Input: [ [1,2,3], [1,2] ]
        // Output:
        //      [1, 1],
        //      [2, 1],
        //      [3, 1],
        //      [1, 2],
        //      [2, 2],
        //      [3, 2]
        //
        // Input: [ [1,2], [1,2], [1,2] ]
        // Output:
        //      [1, 1, 1],
        //      [2, 1, 1],
        //      [1, 2, 1],
        //      [2, 2, 1],
        //      [1, 1, 2],
        //      [2, 1, 2],
        //      [1, 2, 2],
        //      [2, 2, 2],
        private static IEnumerable<T[]> GenerateCombinations<T>(List<T>[] options)
        {
            int[] indexTracker = new int[options.Length];
            T[] combo = new T[options.Length];
            for (int i = 0; i < options.Length; ++i)
            {
                combo[i] = options[i][0];
            }
            // Same algorithm as picking a combination lock, but with different numbers on each wheel.
            int rollovers = 0;
            while (rollovers < combo.Length)
            {
                yield return combo.ToArray();
                for (int i = 0; i < combo.Length; ++i)
                {
                    int index = ++indexTracker[i];
                    if (index == options[i].Count)
                    {
                        indexTracker[i] = 0;
                        combo[i] = options[i][0];
                        if (i == rollovers)
                        {
                            ++rollovers;
                        }
                    }
                    else
                    {
                        combo[i] = options[i][index];
                        break;
                    }
                }
            }
        }

        private static IEnumerable<bool[]> GetArgumentsAreGeneric(int argCount)
        {
            bool[] args = new bool[argCount];
            yield return args;
            for (int i = 0; i < argCount; ++i)
            {
                args[i] = true;
                yield return args;
            }
        }

        private static void Complete(object deferred, MergeSettledArg opts)
        {
            if (opts.completeType == CompleteType.Resolve)
            {
                deferred.GetType()
                    .GetMethod("Resolve", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .Invoke(deferred, opts.Type == null ? null : new object[] { opts.completeValue });
            }
            else if (opts.completeType == CompleteType.Cancel)
            {
                deferred.GetType()
                    .GetMethod("Cancel", new Type[0])
                    .Invoke(deferred, null);
            }
            else
            {
                deferred.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .First(method => method.Name == "Reject")
                    .MakeGenericMethod(typeof(string))
                    .Invoke(deferred, new object[] { rejectValue });
            }
        }

        private static Promise<ITuple> ConvertPromise<T>(Promise<T> promise)
        {
            return promise.Then(v => v as ITuple ?? new TupleWrapper(v));
        }

        private const string rejectValue = "reject";

        [Test, TestCaseSource("GetArgs")]
        public void MergeSettledWorksProperly(MergeSettledArg[] args)
        {
            Type[] genericArgTypes = args.Select(arg => arg.Type)
                .Where(type => type != null)
                .ToArray();
            Type[] paramTypes = args.Select(arg => arg.Type == null ? typeof(Promise) : typeof(Promise<>))
                .ToArray();
            var mergeSettledMethod = typeof(Promise).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .First(method =>
                {
                    if (method.Name != "MergeSettled") return false;
                    var parameters = method.GetParameters();
                    if (parameters.Length != args.Length) return false;
                    return Enumerable.SequenceEqual(paramTypes, parameters.Select(p =>
                        p.ParameterType.IsGenericType ? p.ParameterType.GetGenericTypeDefinition() : p.ParameterType
                    ));
                });
            if (genericArgTypes.Length > 0)
            {
                mergeSettledMethod = mergeSettledMethod.MakeGenericMethod(genericArgTypes);
            }

            object[] deferreds = args.Select(arg => arg.Type == null
                ? (object) Promise.NewDeferred()
                : typeof(Promise<>.Deferred)
                    .MakeGenericType(arg.Type)
                    .GetMethod("New", new Type[0])
                    .Invoke(null, null))
                .ToArray();

            object[] promises = deferreds.Select(deferred => deferred.GetType()
                    .GetProperty("Promise")
                    .GetGetMethod()
                    .Invoke(deferred, null))
                .ToArray();

            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i].alreadyComplete)
                {
                    Complete(deferreds[i], args[i]);
                }
            }

            object mergePromise = mergeSettledMethod.Invoke(null, promises);

            bool invoked = false;

            ((Promise<ITuple>) typeof(MergeSettledTests).GetMethod("ConvertPromise", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .MakeGenericMethod(mergePromise.GetType().GetGenericArguments()[0])
                .Invoke(null, new object[] { mergePromise }))
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(args.Length, results.Length);
                    for (int i = 0; i <  results.Length; ++i)
                    {
                        var expectedState = (Promise.State) args[i].completeType;
                        if (results[i] is Promise.ResultContainer)
                        {
                            var resultContainer = (Promise.ResultContainer) results[i];
                            Assert.AreEqual(expectedState, resultContainer.State);
                            if (expectedState == Promise.State.Rejected)
                            {
                                Assert.AreEqual(rejectValue, resultContainer.Reason);
                            }
                        }
                        else
                        {
                            var type = results[i].GetType();
                            Assert.AreEqual(expectedState, type.GetProperty("State").GetGetMethod().Invoke(results[i], null));
                            if (expectedState == Promise.State.Resolved)
                            {
                                Assert.AreEqual(args[i].completeValue, type.GetProperty("Value").GetGetMethod().Invoke(results[i], null));
                            }
                            else if (expectedState == Promise.State.Rejected)
                            {
                                Assert.AreEqual(rejectValue, type.GetProperty("Reason").GetGetMethod().Invoke(results[i], null));
                            }
                        }
                    }
                })
                .Forget();

            Assert.IsFalse(invoked);

            for (int i = 0; i < args.Length; ++i)
            {
                if (!args[i].alreadyComplete)
                {
                    Assert.IsFalse(invoked);

                    Complete(deferreds[i], args[i]);
                }
            }

            Assert.IsTrue(invoked);
        }

#if PROMISE_PROGRESS
        [Test]
        public void MergeSettledProgressIsNormalized0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<string>();
            var deferred3 = Promise.NewDeferred<float>();
            var deferred4 = Promise.NewDeferred<bool>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.MergeSettled(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f / 4f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred2, "Success", 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred3, 2f, 3f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 3.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred4, true, 4f / 4f);
        }

        [Test]
        public void MergeSettledProgressIsNormalized1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<float>();
            var deferred4 = Promise.NewDeferred<bool>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.MergeSettled(deferred1.Promise, Promise.Resolved("Success"), deferred3.Promise, deferred4.Promise)
                .SubscribeProgressAndAssert(progressHelper, 1f / 4f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 1.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred3, 2f, 3f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 3.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred4, true, 4f / 4f);
        }

        [Test]
        public void MergeSettledProgressIsNormalized2(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<bool>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.MergeSettled(deferred1.Promise, Promise.Resolved(1f), deferred3.Promise)
                .SubscribeProgressAndAssert(progressHelper, 1f / 3f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 1.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred3, true, 3f / 3f);
        }

        [Test]
        public void MergeSettledProgressIsNormalized3(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.MergeSettled
            (
                deferred1.Promise.ThenDuplicate(),
                deferred2.Promise.ThenDuplicate()
            )
                .SubscribeProgressAndAssert(progressHelper, 0f / 2f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred2, 1f, 2f / 2f);
        }

        [Test]
        public void MergeSettledProgressIsNormalized4(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<float>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.MergeSettled
            (
                deferred1.Promise
                    .Then(() => deferred3.Promise),
                deferred2.Promise
                    .Then(() => deferred4.Promise)
            )
                .SubscribeProgressAndAssert(progressHelper, 0f / 4f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 1.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred3, 1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred2, 1f, 3f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 3.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred4, 1f, 4f / 4f);
        }

        [Test]
        public void MergeSettledProgressIsNormalized5(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.MergeSettled
            (
                deferred1.Promise
                    .Then(x => Promise.Resolved(x)),
                deferred2.Promise
                    .Then(x => Promise.Resolved(x))
            )
                .SubscribeProgressAndAssert(progressHelper, 0f / 4f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred2, 1, 4f / 4f);
        }

        [Test]
        public void MergeSettledProgressIsReportedFromRejected(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();
            var deferred3 = Promise.NewDeferred<bool>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.MergeSettled(deferred1.Promise, deferred2.Promise, deferred3.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f / 3f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 3f);
            progressHelper.RejectAndAssertResult(deferred2, "Reject", 2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred3, true, 3f / 3f);
        }

        [Test]
        public void MergeSettledProgressIsReportedFromCanceled(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<float>();
            cancelationSource.Token.Register(deferred2);
            var deferred3 = Promise.NewDeferred<bool>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.MergeSettled(deferred1.Promise, deferred2.Promise, deferred3.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f / 3f)
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 3f);
            progressHelper.CancelAndAssertResult(deferred2, 2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred3, true, 3f / 3f);

            cancelationSource.Dispose();
        }

        [Test]
        public void MergeSettledProgressWillBeInvokedProperlyFromARecoveredPromise(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();
            var deferred3 = Promise.NewDeferred<string>();
            var deferred4 = Promise.NewDeferred<bool>();
            var cancelationSource = CancelationSource.New();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.MergeSettled
            (
                // Make first and second promise chains the same length
                deferred1.Promise
                    .Then(x => Promise.Resolved(x))
                    .Then(x => Promise.Resolved(x)),
                deferred2.Promise
                    .Then(() => deferred3.Promise, cancelationSource.Token)
                    .ContinueWith(_ => deferred4.Promise)
            )
                .SubscribeProgressAndAssert(progressHelper, 0f / 6f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 6f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 3f / 6f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.25f, 3.25f / 6f);
            progressHelper.CancelAndAssertResult(cancelationSource, 5f / 6f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 5f / 6f, false);
            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 5f / 6f, false);
            progressHelper.ResolveAndAssertResult(deferred3, "Success", 5f / 6f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 5f / 6f, false);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 5.5f / 6f);
            progressHelper.ResolveAndAssertResult(deferred4, true, 6f / 6f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 6f / 6f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 1f, 6f / 6f, false);

            cancelationSource.Dispose();
            deferred3.Promise.Forget(); // Need to forget this promise because it was never awaited due to the cancelation.
        }

        [Test]
        public void MergeSettledProgressWillBeInvokedProperlyFromChainedPromise_FlatDepth_void([Values] bool isPending)
        {
            // Testing an implementation detail, not guaranteed by the API - Promise.MergeSettled's depth is set to the longest promise chain's depth.
            // We test if all promises are already resolved to make sure progress reports remain consistent.
            var maybePendingDeferred = isPending
                ? Promise.NewDeferred()
                : default(Promise.Deferred);

            // .Then waiting on another promise increases the depth of the promise chain from 0 to 1.
            var promise1 = (isPending ? maybePendingDeferred.Promise : Promise.Resolved())
                .Then(() => Promise.Resolved(1));
            var promise2 = Promise.Resolved()
                .Then(() => Promise.Resolved(2f));
            var promise3 = Promise.Resolved()
                .Then(() => Promise.Resolved("Success"));
            var promise4 = Promise.Resolved()
                .Then(() => Promise.Resolved(true));

            const float initialCompletedProgress = 3f / 4f;
            const float expectedCompletedProgress = initialCompletedProgress * 2f / 3f;

            var deferredForProgress = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            Promise.MergeSettled(promise1, promise2, promise3, promise4)
                .Then(v => deferredForProgress.Promise) // Increases the depth to 2.
                .SubscribeProgressAndAssert(progressHelper, isPending ? expectedCompletedProgress : 2f / 3f)
                .Forget();

            maybePendingDeferred.TryResolve();

            progressHelper.AssertCurrentProgress(2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferredForProgress, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferredForProgress, 3f / 3f);
        }

        [Test]
        public void MergeSettledProgressWillBeInvokedProperlyFromChainedPromise_StaggeredDepth_void([Values] bool isPending)
        {
            // Testing an implementation detail, not guaranteed by the API - Promise.MergeSettled's depth is set to the longest promise chain's depth.
            // We test if all promises are already resolved to make sure progress reports remain consistent.
            var maybePendingDeferred = isPending
                ? Promise.NewDeferred()
                : default(Promise.Deferred);

            // .Then waiting on another promise increases the depth of the promise chain from 0 to 1.
            var promise1 = (isPending ? maybePendingDeferred.Promise : Promise.Resolved())
                .Then(() => Promise.Resolved(1));
            var promise2 = Promise.Resolved(2f);
            var promise3 = Promise.Resolved("Success");
            var promise4 = Promise.Resolved(true);

            // Implementation detail - progress isn't divided evenly for each promise, their weights are based on their depth.
            const float initialCompletedProgress = 3f / 5f;
            const float expectedCompletedProgress = initialCompletedProgress * 2f / 3f;

            var deferredForProgress = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            Promise.MergeSettled(promise1, promise2, promise3, promise4)
                .Then(v => deferredForProgress.Promise) // Increases the depth to 2.
                .SubscribeProgressAndAssert(progressHelper, isPending ? expectedCompletedProgress : 2f / 3f)
                .Forget();

            maybePendingDeferred.TryResolve();

            progressHelper.AssertCurrentProgress(2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferredForProgress, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferredForProgress, 3f / 3f);
        }
#endif // PROMISE_PROGRESS
    }
}