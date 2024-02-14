#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0074 // Use compound assignment

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    public partial struct Promise
    {
        [MethodImpl(Internal.InlineOption)]
        private static void GetAllResultContainer(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref IList<ResultContainer> result)
        {
            result[index] = new ResultContainer(rejectContainer, state);
        }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
        private static unsafe Internal.GetResultContainerDelegate<IList<ResultContainer>> GetAllResultContainerFunc
        {
            [MethodImpl(Internal.InlineOption)]
            get { return new(&GetAllResultContainer); }
        }
#else
        private static readonly Internal.GetResultContainerDelegate<IList<ResultContainer>> GetAllResultContainerFunc = GetAllResultContainer;
#endif

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order when they have all completed.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new 1 will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(Promise promise1, Promise promise2, IList<ResultContainer> valueContainer = null)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);

            if (valueContainer == null)
            {
                valueContainer = new ResultContainer[2];
            }
            else
            {
                // Make sure list has the same count as promises.
                int listSize = valueContainer.Count;
                while (listSize > 2)
                {
                    valueContainer.RemoveAt(--listSize);
                }
                while (listSize < 2)
                {
                    valueContainer.Add(default(ResultContainer));
                    ++listSize;
                }
            }
            valueContainer[0] = ResultContainer.Resolved;
            valueContainer[1] = ResultContainer.Resolved;

            if (pendingCount == 0)
            {
                return Resolved(valueContainer);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, valueContainer, pendingCount, GetAllResultContainerFunc);
            return new Promise<IList<ResultContainer>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order when they have all completed.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new 1 will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(Promise promise1, Promise promise2, Promise promise3, IList<ResultContainer> valueContainer = null)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);

            if (valueContainer == null)
            {
                valueContainer = new ResultContainer[3];
            }
            else
            {
                // Make sure list has the same count as promises.
                int listSize = valueContainer.Count;
                while (listSize > 3)
                {
                    valueContainer.RemoveAt(--listSize);
                }
                while (listSize < 3)
                {
                    valueContainer.Add(default(ResultContainer));
                    ++listSize;
                }
            }
            valueContainer[0] = ResultContainer.Resolved;
            valueContainer[1] = ResultContainer.Resolved;
            valueContainer[2] = ResultContainer.Resolved;

            if (pendingCount == 0)
            {
                return Resolved(valueContainer);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, valueContainer, pendingCount, GetAllResultContainerFunc);
            return new Promise<IList<ResultContainer>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order when they have all completed.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="promise4">The 4th promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new 1 will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(Promise promise1, Promise promise2, Promise promise3, Promise promise4, IList<ResultContainer> valueContainer = null)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);

            if (valueContainer == null)
            {
                valueContainer = new ResultContainer[4];
            }
            else
            {
                // Make sure list has the same count as promises.
                int listSize = valueContainer.Count;
                while (listSize > 4)
                {
                    valueContainer.RemoveAt(--listSize);
                }
                while (listSize < 4)
                {
                    valueContainer.Add(default(ResultContainer));
                    ++listSize;
                }
            }
            valueContainer[0] = ResultContainer.Resolved;
            valueContainer[1] = ResultContainer.Resolved;
            valueContainer[2] = ResultContainer.Resolved;
            valueContainer[3] = ResultContainer.Resolved;

            if (pendingCount == 0)
            {
                return Resolved(valueContainer);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, valueContainer, pendingCount, GetAllResultContainerFunc);
            return new Promise<IList<ResultContainer>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        public static Promise<IList<ResultContainer>> AllSettled(params Promise[] promises)
        {
            return AllSettled(promises, new ResultContainer[promises.Length]);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new 1 will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(Promise[] promises, IList<ResultContainer> valueContainer = null)
        {
            return AllSettled(promises.GetGenericEnumerator(), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new 1 will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(IEnumerable<Promise> promises, IList<ResultContainer> valueContainer = null)
        {
            return AllSettled(promises.GetEnumerator(), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The enumerator of promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new 1 will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled<TEnumerator>(TEnumerator promises, IList<ResultContainer> valueContainer = null) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, "promises", 1);

            using (promises)
            {
                var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
                int pendingCount = 0;

                if (valueContainer == null)
                {
                    valueContainer = new List<ResultContainer>();
                }

                int i = 0;
                int listSize = valueContainer.Count;
                while (promises.MoveNext())
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    Internal.PrepareForMerge(p, ref passThroughs, i, ref pendingCount);
                    // Make sure list has the same count as promises.
                    if (listSize < (i + 1))
                    {
                        ++listSize;
                        valueContainer.Add(ResultContainer.Resolved);
                    }
                    else
                    {
                        valueContainer[i] = ResultContainer.Resolved;
                    }
                    ++i;
                }
                // Make sure list has the same count as promises.
                while (listSize > i)
                {
                    valueContainer.RemoveAt(--listSize);
                }

                if (pendingCount == 0)
                {
                    return Resolved(valueContainer);
                }

                var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, valueContainer, pendingCount, GetAllResultContainerFunc);
                return new Promise<IList<ResultContainer>>(promise, promise.Id);
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="Promise{T}.ResultContainer"/>s in the same order when they have all completed.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<Promise<T>.ResultContainer>> AllSettled<T>(Promise<T> promise1, Promise<T> promise2, IList<Promise<T>.ResultContainer> valueContainer = null)
        {
            return Promise<T>.AllSettled(promise1, promise2, valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="Promise{T}.ResultContainer"/>s in the same order when they have all completed.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<Promise<T>.ResultContainer>> AllSettled<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, IList<Promise<T>.ResultContainer> valueContainer = null)
        {
            return Promise<T>.AllSettled(promise1, promise2, promise3, valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="Promise{T}.ResultContainer"/>s in the same order when they have all completed.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="promise4">The fourth promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<Promise<T>.ResultContainer>> AllSettled<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4, IList<Promise<T>.ResultContainer> valueContainer = null)
        {
            return Promise<T>.AllSettled(promise1, promise2, promise3, promise4, valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="Promise{T}.ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        public static Promise<IList<Promise<T>.ResultContainer>> AllSettled<T>(params Promise<T>[] promises)
        {
            return Promise<T>.AllSettled(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="Promise{T}.ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<Promise<T>.ResultContainer>> AllSettled<T>(IEnumerable<Promise<T>> promises, IList<ResultContainer> valueContainer = null)
        {
            return Promise<T>.AllSettled(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="Promise{T}.ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The enumerator of promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<Promise<T>.ResultContainer>> AllSettled<T, TEnumerator>(TEnumerator promises, IList<ResultContainer> valueContainer = null) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Promise<T>.AllSettled(promises);
        }

        #region 2Args

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled2
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new ResultContainer(rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer>>
                GetSettled2()
            {
                return Settled2.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<ResultContainer, ResultContainer>> MergeSettled(Promise promise1, Promise promise2)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);

            var value = new ValueTuple<ResultContainer, ResultContainer>(ResultContainer.Resolved, ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled2());
            return new Promise<ValueTuple<ResultContainer, ResultContainer>>(promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled1<T1>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer>>
                GetSettled1<T1>()
            {
                return Settled1<T1>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, ResultContainer>> MergeSettled<T1>(Promise<T1> promise1, Promise promise2)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, ResultContainer>(v0, ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled1<T1>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, ResultContainer>>(promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled0<T1, T2>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer>>
                GetSettled0<T1, T2>()
            {
                return Settled0<T1, T2>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer>> MergeSettled<T1, T2>(Promise<T1> promise1, Promise<T2> promise2)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer>(v0, v1);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled0<T1, T2>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer>>(promise, promise.Id);
        }

        #endregion // 2Args

        #region 3Args

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled3
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new ResultContainer(rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer>>
                GetSettled3()
            {
                return Settled3.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<ResultContainer, ResultContainer, ResultContainer>> MergeSettled(
            Promise promise1, Promise promise2, Promise promise3)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);

            var value = new ValueTuple<ResultContainer, ResultContainer, ResultContainer>(
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled3());
            return new Promise<ValueTuple<ResultContainer, ResultContainer, ResultContainer>>(promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled2<T1>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer>>
                GetSettled2<T1>()
            {
                return Settled2<T1>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1>(
            Promise<T1> promise1, Promise promise2, Promise promise3)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer>(
                v0,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled2<T1>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled1<T1, T2>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer>>
                GetSettled1<T1, T2>()
            {
                return Settled1<T1, T2>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer>> MergeSettled<T1, T2>(
            Promise<T1> promise1, Promise<T2> promise2, Promise promise3)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer>(
                v0,
                v1,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled1<T1, T2>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled0<T1, T2, T3>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer>>
                GetSettled0<T1, T2, T3>()
            {
                return Settled0<T1, T2, T3>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer>> MergeSettled<T1, T2, T3>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer>(
                v0,
                v1,
                v2);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled0<T1, T2, T3>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer>>(
                promise, promise.Id);
        }

        #endregion // 3Args

        #region 4Args

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled4
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new ResultContainer(rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer>>
                GetSettled4()
            {
                return Settled4.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer>> MergeSettled(
            Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);

            var value = new ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer>(
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled4());
            return new Promise<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer>>(promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled3<T1>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>>
                GetSettled3<T1>()
            {
                return Settled3<T1>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1>(
            Promise<T1> promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>(
                v0,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled3<T1>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled2<T1, T2>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer>>
                GetSettled2<T1, T2>()
            {
                return Settled2<T1, T2>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1, T2>(
            Promise<T1> promise1, Promise<T2> promise2, Promise promise3, Promise promise4)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer>(
                v0,
                v1,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled2<T1, T2>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled1<T1, T2, T3>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer>>
                GetSettled1<T1, T2, T3>()
            {
                return Settled1<T1, T2, T3>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer>> MergeSettled<T1, T2, T3>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise promise4)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer>(
                v0,
                v1,
                v2,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled1<T1, T2, T3>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled0<T1, T2, T3, T4>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer>>
                GetSettled0<T1, T2, T3, T4>()
            {
                return Settled0<T1, T2, T3, T4>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer>> MergeSettled<T1, T2, T3, T4>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            var v3 = default(T4);
            Internal.PrepareForMerge(promise4, ref v3, ref passThroughs, 3, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer>(
                v0,
                v1,
                v2,
                v3);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled0<T1, T2, T3, T4>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer>>(
                promise, promise.Id);
        }

        #endregion // 4Args

        #region 5Args

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled5
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new ResultContainer(rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>
                GetSettled5()
            {
                return Settled5.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> MergeSettled(
            Promise promise1, Promise promise2, Promise promise3, Promise promise4, Promise promise5)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);

            var value = new ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>(
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled5());
            return new Promise<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>(promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled4<T1>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>
                GetSettled4<T1>()
            {
                return Settled4<T1>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1>(
            Promise<T1> promise1, Promise promise2, Promise promise3, Promise promise4, Promise promise5)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>(
                v0,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled4<T1>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled3<T1, T2>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>>
                GetSettled3<T1, T2>()
            {
                return Settled3<T1, T2>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1, T2>(
            Promise<T1> promise1, Promise<T2> promise2, Promise promise3, Promise promise4, Promise promise5)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>(
                v0,
                v1,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled3<T1, T2>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled2<T1, T2, T3>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer>>
                GetSettled2<T1, T2, T3>()
            {
                return Settled2<T1, T2, T3>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1, T2, T3>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise promise4, Promise promise5)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer>(
                v0,
                v1,
                v2,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled2<T1, T2, T3>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled1<T1, T2, T3, T4>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer>>
                GetSettled1<T1, T2, T3, T4>()
            {
                return Settled1<T1, T2, T3, T4>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer>> MergeSettled<T1, T2, T3, T4>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise promise5)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            var v3 = default(T4);
            Internal.PrepareForMerge(promise4, ref v3, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer>(
                v0,
                v1,
                v2,
                v3,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled1<T1, T2, T3, T4>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled0<T1, T2, T3, T4, T5>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer>>
                GetSettled0<T1, T2, T3, T4, T5>()
            {
                return Settled0<T1, T2, T3, T4, T5>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer>> MergeSettled<T1, T2, T3, T4, T5>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            var v3 = default(T4);
            Internal.PrepareForMerge(promise4, ref v3, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            var v4 = default(T5);
            Internal.PrepareForMerge(promise5, ref v4, ref passThroughs, 4, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer>(
                v0, v1, v2, v3, v4);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled0<T1, T2, T3, T4, T5>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer>>(
                promise, promise.Id);
        }

        #endregion // 5Args

        #region 6Args

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled6
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new ResultContainer(rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>
                GetSettled6()
            {
                return Settled6.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> MergeSettled(
            Promise promise1, Promise promise2, Promise promise3, Promise promise4, Promise promise5, Promise promise6)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref passThroughs, 5, ref pendingCount);

            var value = new ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>(
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled6());
            return new Promise<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>(promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled5<T1>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>
                GetSettled5<T1>()
            {
                return Settled5<T1>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1>(
            Promise<T1> promise1, Promise promise2, Promise promise3, Promise promise4, Promise promise5, Promise promise6)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref passThroughs, 5, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>(
                v0,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled5<T1>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled4<T1, T2>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>
                GetSettled4<T1, T2>()
            {
                return Settled4<T1, T2>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1, T2>(
            Promise<T1> promise1, Promise<T2> promise2, Promise promise3, Promise promise4, Promise promise5, Promise promise6)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref passThroughs, 5, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>(
                v0,
                v1,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled4<T1, T2>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled3<T1, T2, T3>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>>
                GetSettled3<T1, T2, T3>()
            {
                return Settled3<T1, T2, T3>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1, T2, T3>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise promise4, Promise promise5, Promise promise6)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref passThroughs, 5, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>(
                v0,
                v1,
                v2,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled3<T1, T2, T3>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled2<T1, T2, T3, T4>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer>>
                GetSettled2<T1, T2, T3, T4>()
            {
                return Settled2<T1, T2, T3, T4>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1, T2, T3, T4>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise promise5, Promise promise6)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            var v3 = default(T4);
            Internal.PrepareForMerge(promise4, ref v3, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref passThroughs, 5, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer>(
                v0,
                v1,
                v2,
                v3,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled2<T1, T2, T3, T4>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled1<T1, T2, T3, T4, T5>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer>>
                GetSettled1<T1, T2, T3, T4, T5>()
            {
                return Settled1<T1, T2, T3, T4, T5>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer>> MergeSettled<T1, T2, T3, T4, T5>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise promise6)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            var v3 = default(T4);
            Internal.PrepareForMerge(promise4, ref v3, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            var v4 = default(T5);
            Internal.PrepareForMerge(promise5, ref v4, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref passThroughs, 5, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer>(
                v0,
                v1,
                v2,
                v3,
                v4,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled1<T1, T2, T3, T4, T5>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled0<T1, T2, T3, T4, T5, T6>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new Promise<T6>.ResultContainer(handler.GetResult<T6>(), rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer>>
                GetSettled0<T1, T2, T3, T4, T5, T6>()
            {
                return Settled0<T1, T2, T3, T4, T5, T6>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer>> MergeSettled<T1, T2, T3, T4, T5, T6>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            var v3 = default(T4);
            Internal.PrepareForMerge(promise4, ref v3, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            var v4 = default(T5);
            Internal.PrepareForMerge(promise5, ref v4, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            var v5 = default(T6);
            Internal.PrepareForMerge(promise6, ref v5, ref passThroughs, 5, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer>(
                v0, v1, v2, v3, v4, v5);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled0<T1, T2, T3, T4, T5, T6>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer>>(
                promise, promise.Id);
        }

        #endregion // 6Args

        #region 7Args

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled7
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new ResultContainer(rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(rejectContainer, state);
                            break;
                        case 6:
                            result.Item7 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>
                GetSettled7()
            {
                return Settled7.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> MergeSettled(
            Promise promise1, Promise promise2, Promise promise3, Promise promise4, Promise promise5, Promise promise6, Promise promise7)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref passThroughs, 5, ref pendingCount);
            ValidateArgument(promise7, "promise7", 1);
            Internal.PrepareForMerge(promise7, ref passThroughs, 6, ref pendingCount);

            var value = new ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>(
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled7());
            return new Promise<ValueTuple<ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>(promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled6<T1>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(rejectContainer, state);
                            break;
                        case 6:
                            result.Item7 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>
                GetSettled6<T1>()
            {
                return Settled6<T1>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1>(
            Promise<T1> promise1, Promise promise2, Promise promise3, Promise promise4, Promise promise5, Promise promise6, Promise promise7)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref passThroughs, 5, ref pendingCount);
            ValidateArgument(promise7, "promise7", 1);
            Internal.PrepareForMerge(promise7, ref passThroughs, 6, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>(
                v0,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled6<T1>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled5<T1, T2>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(rejectContainer, state);
                            break;
                        case 6:
                            result.Item7 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>
                GetSettled5<T1, T2>()
            {
                return Settled5<T1, T2>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1, T2>(
            Promise<T1> promise1, Promise<T2> promise2, Promise promise3, Promise promise4, Promise promise5, Promise promise6, Promise promise7)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref passThroughs, 5, ref pendingCount);
            ValidateArgument(promise7, "promise7", 1);
            Internal.PrepareForMerge(promise7, ref passThroughs, 6, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>(
                v0,
                v1,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled5<T1, T2>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled4<T1, T2, T3>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(rejectContainer, state);
                            break;
                        case 6:
                            result.Item7 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>
                GetSettled4<T1, T2, T3>()
            {
                return Settled4<T1, T2, T3>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1, T2, T3>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise promise4, Promise promise5, Promise promise6, Promise promise7)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref passThroughs, 5, ref pendingCount);
            ValidateArgument(promise7, "promise7", 1);
            Internal.PrepareForMerge(promise7, ref passThroughs, 6, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>(
                v0,
                v1,
                v2,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled4<T1, T2, T3>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled3<T1, T2, T3, T4>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(rejectContainer, state);
                            break;
                        case 6:
                            result.Item7 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>>
                GetSettled3<T1, T2, T3, T4>()
            {
                return Settled3<T1, T2, T3, T4>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1, T2, T3, T4>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise promise5, Promise promise6, Promise promise7)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            var v3 = default(T4);
            Internal.PrepareForMerge(promise4, ref v3, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref passThroughs, 5, ref pendingCount);
            ValidateArgument(promise7, "promise7", 1);
            Internal.PrepareForMerge(promise7, ref passThroughs, 6, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>(
                v0,
                v1,
                v2,
                v3,
                ResultContainer.Resolved,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled3<T1, T2, T3, T4>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled2<T1, T2, T3, T4, T5>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(rejectContainer, state);
                            break;
                        case 6:
                            result.Item7 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer, ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer, ResultContainer>>
                GetSettled2<T1, T2, T3, T4, T5>()
            {
                return Settled2<T1, T2, T3, T4, T5>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer, ResultContainer>> MergeSettled<T1, T2, T3, T4, T5>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise promise6, Promise promise7)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            var v3 = default(T4);
            Internal.PrepareForMerge(promise4, ref v3, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            var v4 = default(T5);
            Internal.PrepareForMerge(promise5, ref v4, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref passThroughs, 5, ref pendingCount);
            ValidateArgument(promise7, "promise7", 1);
            Internal.PrepareForMerge(promise7, ref passThroughs, 6, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer, ResultContainer>(
                v0,
                v1,
                v2,
                v3,
                v4,
                ResultContainer.Resolved,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled2<T1, T2, T3, T4, T5>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled1<T1, T2, T3, T4, T5, T6>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new Promise<T6>.ResultContainer(handler.GetResult<T6>(), rejectContainer, state);
                            break;
                        case 6:
                            result.Item7 = new ResultContainer(rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, ResultContainer>>
                GetSettled1<T1, T2, T3, T4, T5, T6>()
            {
                return Settled1<T1, T2, T3, T4, T5, T6>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, ResultContainer>> MergeSettled<T1, T2, T3, T4, T5, T6>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise promise7)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            var v3 = default(T4);
            Internal.PrepareForMerge(promise4, ref v3, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            var v4 = default(T5);
            Internal.PrepareForMerge(promise5, ref v4, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            var v5 = default(T6);
            Internal.PrepareForMerge(promise6, ref v5, ref passThroughs, 5, ref pendingCount);
            ValidateArgument(promise7, "promise7", 1);
            Internal.PrepareForMerge(promise7, ref passThroughs, 6, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, ResultContainer>(
                v0,
                v1,
                v2,
                v3,
                v4,
                v5,
                ResultContainer.Resolved);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled1<T1, T2, T3, T4, T5, T6>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, ResultContainer>>(
                promise, promise.Id);
        }

        private static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled0<T1, T2, T3, T4, T5, T6, T7>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer> result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), rejectContainer, state);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new Promise<T6>.ResultContainer(handler.GetResult<T6>(), rejectContainer, state);
                            break;
                        case 6:
                            result.Item7 = new Promise<T7>.ResultContainer(handler.GetResult<T7>(), rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer>> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer>> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer>>
                GetSettled0<T1, T2, T3, T4, T5, T6, T7>()
            {
                return Settled0<T1, T2, T3, T4, T5, T6, T7>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer>> MergeSettled<T1, T2, T3, T4, T5, T6, T7>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise<T7> promise7)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;

            ValidateArgument(promise1, "promise1", 1);
            var v0 = default(T1);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount);
            ValidateArgument(promise2, "promise2", 1);
            var v1 = default(T2);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount);
            ValidateArgument(promise3, "promise3", 1);
            var v2 = default(T3);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount);
            ValidateArgument(promise4, "promise4", 1);
            var v3 = default(T4);
            Internal.PrepareForMerge(promise4, ref v3, ref passThroughs, 3, ref pendingCount);
            ValidateArgument(promise5, "promise5", 1);
            var v4 = default(T5);
            Internal.PrepareForMerge(promise5, ref v4, ref passThroughs, 4, ref pendingCount);
            ValidateArgument(promise6, "promise6", 1);
            var v5 = default(T6);
            Internal.PrepareForMerge(promise6, ref v5, ref passThroughs, 5, ref pendingCount);
            ValidateArgument(promise7, "promise7", 1);
            var v6 = default(T7);
            Internal.PrepareForMerge(promise7, ref v6, ref passThroughs, 6, ref pendingCount);

            var value = new ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer>(
                v0, v1, v2, v3, v4, v5, v6);
            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergeSettledPromise(passThroughs, value, pendingCount, MergeResultFuncs.GetSettled0<T1, T2, T3, T4, T5, T6, T7>());
            return new Promise<ValueTuple<Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer>>(
                promise, promise.Id);
        }

        #endregion // 7Args
    }
}