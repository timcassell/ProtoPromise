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
using System.Linq;
using System.Runtime.CompilerServices;

#if UNITY_2018_3_OR_NEWER && !UNITY_2021_2_OR_NEWER
// ITuple interface was added in net471, which Unity 2018 and 2019 don't support with netstandard2.0 API.
namespace System.Runtime.CompilerServices
{
    public interface ITuple
    {
        int Length { get; }

        object this[int index] { get; }
    }
}
#endif

namespace ProtoPromise.Tests.APIs
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

#if !ENABLE_IL2CPP
        // IL2CPP does not support MakeGenericMethod and MakeGenericType, so remove these tests from the IL2CPP build.

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
            // In order to keep the number of test cases down, we only do all combinations for the first 2 args.
            if (index >= 2)
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

        [Test, TestCaseSource(nameof(GetArgs))]
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

#endif // !ENABLE_IL2CPP
    }
}