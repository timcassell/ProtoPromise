#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    public partial struct Promise
    {
        [MethodImpl(Internal.InlineOption)]
        private static void GetAllResultContainer(Internal.PromiseRefBase handler, int index, ref IList<ResultContainer> result)
            => result[index] = new ResultContainer(handler.RejectContainer, handler.State);

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
        private static unsafe Internal.GetResultDelegate<IList<ResultContainer>> GetAllResultContainerFunc
        {
            [MethodImpl(Internal.InlineOption)]
            get => new(&GetAllResultContainer);
        }
#else
        private static readonly Internal.GetResultDelegate<IList<ResultContainer>> GetAllResultContainerFunc = GetAllResultContainer;
#endif

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order when they have all completed.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(Promise promise1, Promise promise2, IList<ResultContainer> valueContainer = null)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            return AllSettled(Internal.GetEnumerator(promise1, promise2), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order when they have all completed.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(Promise promise1, Promise promise2, Promise promise3, IList<ResultContainer> valueContainer = null)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            return AllSettled(Internal.GetEnumerator(promise1, promise2, promise3), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order when they have all completed.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="promise4">The 4th promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(Promise promise1, Promise promise2, Promise promise3, Promise promise4, IList<ResultContainer> valueContainer = null)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            return AllSettled(Internal.GetEnumerator(promise1, promise2, promise3, promise4), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        public static Promise<IList<ResultContainer>> AllSettled(params Promise[] promises)
            => AllSettled(promises, new ResultContainer[promises.Length]);

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(ReadOnlySpan<Promise> promises, IList<ResultContainer> valueContainer = null)
            => AllSettled(promises.GetPersistedEnumerator(), valueContainer);
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(Promise[] promises, IList<ResultContainer> valueContainer = null)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return AllSettled(promises.GetGenericEnumerator(), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(IEnumerable<Promise> promises, IList<ResultContainer> valueContainer = null)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return AllSettled(promises.GetEnumerator(), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The enumerator of promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled<TEnumerator>(TEnumerator promises, IList<ResultContainer> valueContainer = null) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, nameof(promises), 1);

            using (promises)
            {
                if (valueContainer == null)
                {
                    valueContainer = new List<ResultContainer>();
                }

                int i = 0;
                int index;
                int listSize = valueContainer.Count;
                Promise p;
                while (promises.MoveNext())
                {
                    index = i;
                    ++i;
                    // Make sure list has the same count as promises.
                    if (listSize < i)
                    {
                        ++listSize;
                        valueContainer.Add(default);
                    }
                    p = promises.Current;
                    if (p._ref == null)
                    {
                        valueContainer[index] = ResultContainer.Resolved;
                    }
                    else
                    {
                        goto HookupMaybePending;
                    }
                }
                // No non-resolved promises.
                // Make sure list has the same count as promises.
                while (listSize > i)
                {
                    valueContainer.RemoveAt(--listSize);
                }
                return Resolved(valueContainer);

            HookupMaybePending:
                ValidateElement(p, "promises", 1);

                // In order to prevent a race condition with the list being expanded and results being assigned concurrently,
                // we create the passthroughs and link them together in a queue before creating the return promise
                // so that we can make sure the list's size is correct before hooking up any promises.
                var passthroughs = new Internal.ValueLinkedQueue<Internal.PromiseRefBase.PromisePassThroughForAll>(
                    Internal.PromiseRefBase.PromisePassThroughForAll.GetOrCreate(p._ref, p._id, index));
                uint waitCount = 1;
                while (promises.MoveNext())
                {
                    index = i;
                    ++i;
                    // Make sure list has the same count as promises.
                    if (listSize < i)
                    {
                        ++listSize;
                        valueContainer.Add(default);
                    }
                    p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (p._ref == null)
                    {
                        valueContainer[index] = ResultContainer.Resolved;
                    }
                    else
                    {
                        checked { ++waitCount; }
                        passthroughs.EnqueueUnsafe(
                            Internal.PromiseRefBase.PromisePassThroughForAll.GetOrCreate(p._ref, p._id, index));
                    }
                }
                // Make sure list has the same count as promises.
                while (listSize > i)
                {
                    valueContainer.RemoveAt(--listSize);
                }
                var promise = Internal.PromiseRefBase.GetOrCreateAllSettledPromise(valueContainer, GetAllResultContainerFunc, passthroughs.MoveElementsToStack(), waitCount);
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
            => Promise<T>.AllSettled(promise1, promise2, valueContainer);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="Promise{T}.ResultContainer"/>s in the same order when they have all completed.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<Promise<T>.ResultContainer>> AllSettled<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, IList<Promise<T>.ResultContainer> valueContainer = null)
            => Promise<T>.AllSettled(promise1, promise2, promise3, valueContainer);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="Promise{T}.ResultContainer"/>s in the same order when they have all completed.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="promise4">The fourth promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<Promise<T>.ResultContainer>> AllSettled<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4, IList<Promise<T>.ResultContainer> valueContainer = null)
            => Promise<T>.AllSettled(promise1, promise2, promise3, promise4, valueContainer);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="Promise{T}.ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        public static Promise<IList<Promise<T>.ResultContainer>> AllSettled<T>(params Promise<T>[] promises)
            => Promise<T>.AllSettled(promises);

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="Promise{T}.ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<Promise<T>.ResultContainer>> AllSettled<T>(ReadOnlySpan<Promise<T>> promises, IList<Promise<T>.ResultContainer> valueContainer = null)
            => Promise<T>.AllSettled(promises.GetPersistedEnumerator(), valueContainer);
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="Promise{T}.ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<Promise<T>.ResultContainer>> AllSettled<T>(IEnumerable<Promise<T>> promises, IList<Promise<T>.ResultContainer> valueContainer = null)
            => Promise<T>.AllSettled(promises, valueContainer);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="Promise{T}.ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The enumerator of promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<Promise<T>.ResultContainer>> AllSettled<T, TEnumerator>(TEnumerator promises, IList<Promise<T>.ResultContainer> valueContainer = null) where TEnumerator : IEnumerator<Promise<T>>
            => Promise<T>.AllSettled(promises, valueContainer);

        #region 2Args

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled2
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(ResultContainer, ResultContainer)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(ResultContainer, ResultContainer)>
                GetSettled2() => Settled2.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(ResultContainer, ResultContainer)> MergeSettled(Promise promise1, Promise promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);

            (ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled2());
            merger.Prepare(promise1, value, 0);
            merger.Prepare(promise2, value, 1);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled1<T1>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer)>
                GetSettled1<T1>() => Settled1<T1>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, ResultContainer)> MergeSettled<T1>(Promise<T1> promise1, Promise promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);

            (Promise<T1>.ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled1<T1>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, value, 1);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled0<T1, T2>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer)>
                GetSettled0<T1, T2>() => Settled0<T1, T2>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer)> MergeSettled<T1, T2>(Promise<T1> promise1, Promise<T2> promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled0<T1, T2>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            return merger.ToPromise(value);
        }

        #endregion // 2Args

        #region 3Args

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled3
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer)>
                GetSettled3() => Settled3.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(ResultContainer, ResultContainer, ResultContainer)> MergeSettled(
            Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);

            (ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled3());
            merger.Prepare(promise1, value, 0);
            merger.Prepare(promise2, value, 1);
            merger.Prepare(promise3, value, 2);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled2<T1>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer)>
                GetSettled2<T1>() => Settled2<T1>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1>(
            Promise<T1> promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);

            (Promise<T1>.ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled2<T1>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, value, 1);
            merger.Prepare(promise3, value, 2);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled1<T1, T2>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer)>
                GetSettled1<T1, T2>() => Settled1<T1, T2>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer)> MergeSettled<T1, T2>(
            Promise<T1> promise1, Promise<T2> promise2, Promise promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled1<T1, T2>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, value, 2);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled0<T1, T2, T3>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)>
                GetSettled0<T1, T2, T3>() => Settled0<T1, T2, T3>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)> MergeSettled<T1, T2, T3>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled0<T1, T2, T3>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            return merger.ToPromise(value);
        }

        #endregion // 3Args

        #region 4Args

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled4
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (ResultContainer, ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer, ResultContainer)>
                GetSettled4() => Settled4.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(ResultContainer, ResultContainer, ResultContainer, ResultContainer)> MergeSettled(
            Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);

            (ResultContainer, ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled4());
            merger.Prepare(promise1, value, 0);
            merger.Prepare(promise2, value, 1);
            merger.Prepare(promise3, value, 2);
            merger.Prepare(promise4, value, 3);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled3<T1>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)>
                GetSettled3<T1>() => Settled3<T1>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1>(
            Promise<T1> promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);

            (Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled3<T1>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, value, 1);
            merger.Prepare(promise3, value, 2);
            merger.Prepare(promise4, value, 3);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled2<T1, T2>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer)>
                GetSettled2<T1, T2>() => Settled2<T1, T2>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1, T2>(
            Promise<T1> promise1, Promise<T2> promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled2<T1, T2>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, value, 2);
            merger.Prepare(promise4, value, 3);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled1<T1, T2, T3>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer)>
                GetSettled1<T1, T2, T3>() => Settled1<T1, T2, T3>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer)> MergeSettled<T1, T2, T3>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled1<T1, T2, T3>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, value, 3);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled0<T1, T2, T3, T4>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)>
                GetSettled0<T1, T2, T3, T4>() => Settled0<T1, T2, T3, T4>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)> MergeSettled<T1, T2, T3, T4>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled0<T1, T2, T3, T4>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            return merger.ToPromise(value);
        }

        #endregion // 4Args

        #region 5Args

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled5
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)>
                GetSettled5() => Settled5.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> MergeSettled(
            Promise promise1, Promise promise2, Promise promise3, Promise promise4, Promise promise5)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);

            (ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled5());
            merger.Prepare(promise1, value, 0);
            merger.Prepare(promise2, value, 1);
            merger.Prepare(promise3, value, 2);
            merger.Prepare(promise4, value, 3);
            merger.Prepare(promise5, value, 4);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled4<T1>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)>
                GetSettled4<T1>() => Settled4<T1>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1>(
            Promise<T1> promise1, Promise promise2, Promise promise3, Promise promise4, Promise promise5)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);

            (Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled4<T1>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, value, 1);
            merger.Prepare(promise3, value, 2);
            merger.Prepare(promise4, value, 3);
            merger.Prepare(promise5, value, 4);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled3<T1, T2>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)>
                GetSettled3<T1, T2>() => Settled3<T1, T2>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1, T2>(
            Promise<T1> promise1, Promise<T2> promise2, Promise promise3, Promise promise4, Promise promise5)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled3<T1, T2>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, value, 2);
            merger.Prepare(promise4, value, 3);
            merger.Prepare(promise5, value, 4);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled2<T1, T2, T3>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer)>
                GetSettled2<T1, T2, T3>() => Settled2<T1, T2, T3>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1, T2, T3>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise promise4, Promise promise5)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled2<T1, T2, T3>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, value, 3);
            merger.Prepare(promise5, value, 4);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled1<T1, T2, T3, T4>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer)>
                GetSettled1<T1, T2, T3, T4>() => Settled1<T1, T2, T3, T4>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer)> MergeSettled<T1, T2, T3, T4>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise promise5)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled1<T1, T2, T3, T4>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, value, 4);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled0<T1, T2, T3, T4, T5>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)>
                GetSettled0<T1, T2, T3, T4, T5>() => Settled0<T1, T2, T3, T4, T5>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)> MergeSettled<T1, T2, T3, T4, T5>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled0<T1, T2, T3, T4, T5>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, ref merger.GetResultRef(ref value).Item5, value, 4);
            return merger.ToPromise(value);
        }

        #endregion // 5Args

        #region 6Args

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled6
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)>
                GetSettled6() => Settled6.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> MergeSettled(
            Promise promise1, Promise promise2, Promise promise3, Promise promise4, Promise promise5, Promise promise6)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);

            (ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled6());
            merger.Prepare(promise1, value, 0);
            merger.Prepare(promise2, value, 1);
            merger.Prepare(promise3, value, 2);
            merger.Prepare(promise4, value, 3);
            merger.Prepare(promise5, value, 4);
            merger.Prepare(promise6, value, 5);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled5<T1>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)>
                GetSettled5<T1>() => Settled5<T1>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1>(
            Promise<T1> promise1, Promise promise2, Promise promise3, Promise promise4, Promise promise5, Promise promise6)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);

            (Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled5<T1>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, value, 1);
            merger.Prepare(promise3, value, 2);
            merger.Prepare(promise4, value, 3);
            merger.Prepare(promise5, value, 4);
            merger.Prepare(promise6, value, 5);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled4<T1, T2>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)>
                GetSettled4<T1, T2>() => Settled4<T1, T2>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1, T2>(
            Promise<T1> promise1, Promise<T2> promise2, Promise promise3, Promise promise4, Promise promise5, Promise promise6)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled4<T1, T2>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, value, 2);
            merger.Prepare(promise4, value, 3);
            merger.Prepare(promise5, value, 4);
            merger.Prepare(promise6, value, 5);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled3<T1, T2, T3>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)>
                GetSettled3<T1, T2, T3>() => Settled3<T1, T2, T3>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1, T2, T3>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise promise4, Promise promise5, Promise promise6)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled3<T1, T2, T3>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, value, 3);
            merger.Prepare(promise5, value, 4);
            merger.Prepare(promise6, value, 5);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled2<T1, T2, T3, T4>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer)>
                GetSettled2<T1, T2, T3, T4>() => Settled2<T1, T2, T3, T4>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1, T2, T3, T4>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise promise5, Promise promise6)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled2<T1, T2, T3, T4>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, value, 4);
            merger.Prepare(promise6, value, 5);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled1<T1, T2, T3, T4, T5>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer)>
                GetSettled1<T1, T2, T3, T4, T5>() => Settled1<T1, T2, T3, T4, T5>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer)> MergeSettled<T1, T2, T3, T4, T5>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise promise6)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled1<T1, T2, T3, T4, T5>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, ref merger.GetResultRef(ref value).Item5, value, 4);
            merger.Prepare(promise6, value, 5);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled0<T1, T2, T3, T4, T5, T6>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new Promise<T6>.ResultContainer(handler.GetResult<T6>(), handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)>
                GetSettled0<T1, T2, T3, T4, T5, T6>() => Settled0<T1, T2, T3, T4, T5, T6>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)> MergeSettled<T1, T2, T3, T4, T5, T6>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled0<T1, T2, T3, T4, T5, T6>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, ref merger.GetResultRef(ref value).Item5, value, 4);
            merger.Prepare(promise6, ref merger.GetResultRef(ref value).Item6, value, 5);
            return merger.ToPromise(value);
        }

        #endregion // 6Args

        #region 7Args

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled7
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 6:
                            result.Item7 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)>
                GetSettled7() => Settled7.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> MergeSettled(
            Promise promise1, Promise promise2, Promise promise3, Promise promise4, Promise promise5, Promise promise6, Promise promise7)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);
            ValidateArgument(promise7, nameof(promise7), 1);

            (ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled7());
            merger.Prepare(promise1, value, 0);
            merger.Prepare(promise2, value, 1);
            merger.Prepare(promise3, value, 2);
            merger.Prepare(promise4, value, 3);
            merger.Prepare(promise5, value, 4);
            merger.Prepare(promise6, value, 5);
            merger.Prepare(promise7, value, 6);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled6<T1>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 6:
                            result.Item7 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)>
                GetSettled6<T1>() => Settled6<T1>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1>(
            Promise<T1> promise1, Promise promise2, Promise promise3, Promise promise4, Promise promise5, Promise promise6, Promise promise7)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);
            ValidateArgument(promise7, nameof(promise7), 1);

            (Promise<T1>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled6<T1>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, value, 1);
            merger.Prepare(promise3, value, 2);
            merger.Prepare(promise4, value, 3);
            merger.Prepare(promise5, value, 4);
            merger.Prepare(promise6, value, 5);
            merger.Prepare(promise7, value, 6);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled5<T1, T2>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 6:
                            result.Item7 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)>
                GetSettled5<T1, T2>() => Settled5<T1, T2>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1, T2>(
            Promise<T1> promise1, Promise<T2> promise2, Promise promise3, Promise promise4, Promise promise5, Promise promise6, Promise promise7)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);
            ValidateArgument(promise7, nameof(promise7), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled5<T1, T2>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, value, 2);
            merger.Prepare(promise4, value, 3);
            merger.Prepare(promise5, value, 4);
            merger.Prepare(promise6, value, 5);
            merger.Prepare(promise7, value, 6);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled4<T1, T2, T3>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 6:
                            result.Item7 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)>
                GetSettled4<T1, T2, T3>() => Settled4<T1, T2, T3>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1, T2, T3>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise promise4, Promise promise5, Promise promise6, Promise promise7)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);
            ValidateArgument(promise7, nameof(promise7), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, ResultContainer, ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled4<T1, T2, T3>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, value, 3);
            merger.Prepare(promise5, value, 4);
            merger.Prepare(promise6, value, 5);
            merger.Prepare(promise7, value, 6);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled3<T1, T2, T3, T4>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 6:
                            result.Item7 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)>
                GetSettled3<T1, T2, T3, T4>() => Settled3<T1, T2, T3, T4>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1, T2, T3, T4>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise promise5, Promise promise6, Promise promise7)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);
            ValidateArgument(promise7, nameof(promise7), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled3<T1, T2, T3, T4>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, value, 4);
            merger.Prepare(promise6, value, 5);
            merger.Prepare(promise7, value, 6);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled2<T1, T2, T3, T4, T5>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                        case 6:
                            result.Item7 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer, ResultContainer)>
                GetSettled2<T1, T2, T3, T4, T5>() => Settled2<T1, T2, T3, T4, T5>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer, ResultContainer)> MergeSettled<T1, T2, T3, T4, T5>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise promise6, Promise promise7)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);
            ValidateArgument(promise7, nameof(promise7), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled2<T1, T2, T3, T4, T5>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, ref merger.GetResultRef(ref value).Item5, value, 4);
            merger.Prepare(promise6, value, 5);
            merger.Prepare(promise7, value, 6);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled1<T1, T2, T3, T4, T5, T6>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new Promise<T6>.ResultContainer(handler.GetResult<T6>(), handler.RejectContainer, handler.State);
                            break;
                        case 6:
                            result.Item7 = new ResultContainer(handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, ResultContainer)>
                GetSettled1<T1, T2, T3, T4, T5, T6>() => Settled1<T1, T2, T3, T4, T5, T6>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, ResultContainer)> MergeSettled<T1, T2, T3, T4, T5, T6>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise promise7)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);
            ValidateArgument(promise7, nameof(promise7), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled1<T1, T2, T3, T4, T5, T6>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, ref merger.GetResultRef(ref value).Item5, value, 4);
            merger.Prepare(promise6, ref merger.GetResultRef(ref value).Item6, value, 5);
            merger.Prepare(promise7, value, 6);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Settled0<T1, T2, T3, T4, T5, T6, T7>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = new Promise<T1>.ResultContainer(handler.GetResult<T1>(), handler.RejectContainer, handler.State);
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), handler.RejectContainer, handler.State);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), handler.RejectContainer, handler.State);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), handler.RejectContainer, handler.State);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), handler.RejectContainer, handler.State);
                            break;
                        case 5:
                            result.Item6 = new Promise<T6>.ResultContainer(handler.GetResult<T6>(), handler.RejectContainer, handler.State);
                            break;
                        case 6:
                            result.Item7 = new Promise<T7>.ResultContainer(handler.GetResult<T7>(), handler.RejectContainer, handler.State);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)>
                GetSettled0<T1, T2, T3, T4, T5, T6, T7>() => Settled0<T1, T2, T3, T4, T5, T6, T7>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the result container of each promise when they have all completed.
        /// </summary>
        public static Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)> MergeSettled<T1, T2, T3, T4, T5, T6, T7>(
            Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise<T7> promise7)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);
            ValidateArgument(promise7, nameof(promise7), 1);

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)
                value = default;
            var merger = Internal.CreateMergeSettledPreparer(ref value, MergeResultFuncs.GetSettled0<T1, T2, T3, T4, T5, T6, T7>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, ref merger.GetResultRef(ref value).Item5, value, 4);
            merger.Prepare(promise6, ref merger.GetResultRef(ref value).Item6, value, 5);
            merger.Prepare(promise7, ref merger.GetResultRef(ref value).Item7, value, 6);
            return merger.ToPromise(value);
        }

        #endregion // 7Args
    }
}