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
        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise All(Promise promise1, Promise promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            return All(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise All(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            return All(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="promise4">The fourth promise to combine.</param>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise All(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            return All(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise All(params Promise[] promises)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return All(promises.GetGenericEnumerator());
        }

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise All(ReadOnlySpan<Promise> promises)
            => All(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise All(IEnumerable<Promise> promises)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return All(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise All<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, nameof(promises), 1);

            using (promises)
            {
                Promise p;
                while (promises.MoveNext())
                {
                    p = promises.Current;
                    if (p._ref != null)
                    {
                        goto HookupMaybePending;
                    }
                }
                // No non-resolved promises.
                return Resolved();

            HookupMaybePending:
                ValidateElement(p, "promises", 1);
                var promise = Internal.PromiseRefBase.GetOrCreateAllPromiseVoid();
                uint pendingCount = 1;
                promise.AddWaiter(p._ref, p._id);
                while (promises.MoveNext())
                {
                    p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (p._ref != null)
                    {
                        checked { ++pendingCount; }
                        promise.AddWaiter(p._ref, p._id);
                    }
                }
                promise.MarkReady(pendingCount);
                return new Promise(promise, promise.Id);
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        /// <remarks>
        /// Consider using <see cref="PromiseAllGroup{T}"/> instead.
        /// </remarks>
        public static Promise<IList<T>> All<T>(Promise<T> promise1, Promise<T> promise2, IList<T> valueContainer = null)
            => Promise<T>.All(promise1, promise2, valueContainer);

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        /// <remarks>
        /// Consider using <see cref="PromiseAllGroup{T}"/> instead.
        /// </remarks>
        public static Promise<IList<T>> All<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, IList<T> valueContainer = null)
            => Promise<T>.All(promise1, promise2, promise3, valueContainer);

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="promise4">The fourth promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        /// <remarks>
        /// Consider using <see cref="PromiseAllGroup{T}"/> instead.
        /// </remarks>
        public static Promise<IList<T>> All<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4, IList<T> valueContainer = null)
            => Promise<T>.All(promise1, promise2, promise3, promise4, valueContainer);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <remarks>
        /// Consider using <see cref="PromiseAllGroup{T}"/> instead.
        /// </remarks>
        public static Promise<IList<T>> All<T>(params Promise<T>[] promises)
            => Promise<T>.All(promises);

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <remarks>
        /// Consider using <see cref="PromiseAllGroup{T}"/> instead.
        /// </remarks>
        public static Promise All<T>(ReadOnlySpan<Promise<T>> promises)
            => Promise<T>.All(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/>s when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        /// <remarks>
        /// Consider using <see cref="PromiseAllGroup{T}"/> instead.
        /// </remarks>
        public static Promise<IList<T>> All<T>(IEnumerable<Promise<T>> promises, IList<T> valueContainer = null)
            => Promise<T>.All(promises, valueContainer);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promises">The enumerator of promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        /// <remarks>
        /// Consider using <see cref="PromiseAllGroup{T}"/> instead.
        /// </remarks>
        public static Promise<IList<T>> All<T, TEnumerator>(TEnumerator promises, IList<T> valueContainer = null) where TEnumerator : IEnumerator<Promise<T>>
            => Promise<T>.All(promises, valueContainer);

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class One<T1>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref T1 result)
                {
                    // We don't need to check the index for this in RELEASE mode because this will not be called for the promise void.
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (index != 0)
                    {
                        throw new System.ArgumentOutOfRangeException(nameof(index), "index must be 0");
                    }
#endif
                    result = handler.GetResult<T1>();
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<T1> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<T1> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<T1> GetOne<T1>() => One<T1>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the value of <paramref name="promise1"/> when both promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise<T1> Merge<T1>(Promise<T1> promise1, Promise promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            if (promise2._ref == null)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                return promise1.Duplicate();
#pragma warning restore CS0618 // Type or member is obsolete
            }

            uint pendingCount = 1;
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(default, MergeResultFuncs.GetOne<T1>());
            promise.AddWaiter(promise2._ref, promise2._id);
            if (promise1._ref == null)
            {
                promise._result = promise1._result;
            }
            else
            {
                ++pendingCount;
                promise.AddWaiterWithIndex(promise1._ref, promise1._id, 0);
            }
            promise.MarkReady(pendingCount);
            return new Promise<T1>(promise, promise.Id);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Two<T1, T2>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (T1, T2) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            result.Item2 = handler.GetResult<T2>();
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(T1, T2)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(T1, T2)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(T1, T2)> GetTwo<T1, T2>() => Two<T1, T2>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise<(T1, T2)> Merge<T1, T2>(Promise<T1> promise1, Promise<T2> promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);

            (T1, T2) value = default;
            var merger = Internal.CreateMergePreparer(ref value, MergeResultFuncs.GetTwo<T1, T2>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            return merger.ToPromise(value);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise<(T1, T2)> Merge<T1, T2>(Promise<T1> promise1, Promise<T2> promise2, Promise promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);

            (T1, T2) value = default;
            var merger = Internal.CreateMergePreparer(ref value, MergeResultFuncs.GetTwo<T1, T2>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, value);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Three<T1, T2, T3>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (T1, T2, T3) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            result.Item2 = handler.GetResult<T2>();
                            break;
                        case 2:
                            result.Item3 = handler.GetResult<T3>();
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(T1, T2, T3)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(T1, T2, T3)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(T1, T2, T3)> GetThree<T1, T2, T3>() => Three<T1, T2, T3>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise<(T1, T2, T3)> Merge<T1, T2, T3>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);

            (T1, T2, T3) value = default;
            var merger = Internal.CreateMergePreparer(ref value, MergeResultFuncs.GetThree<T1, T2, T3>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            return merger.ToPromise(value);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise<(T1, T2, T3)> Merge<T1, T2, T3>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);

            (T1, T2, T3) value = default;
            var merger = Internal.CreateMergePreparer(ref value, MergeResultFuncs.GetThree<T1, T2, T3>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, value);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Four<T1, T2, T3, T4>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (T1, T2, T3, T4) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            result.Item2 = handler.GetResult<T2>();
                            break;
                        case 2:
                            result.Item3 = handler.GetResult<T3>();
                            break;
                        case 3:
                            result.Item4 = handler.GetResult<T4>();
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(T1, T2, T3, T4)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(T1, T2, T3, T4)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(T1, T2, T3, T4)> GetFour<T1, T2, T3, T4>() => Four<T1, T2, T3, T4>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise<(T1, T2, T3, T4)> Merge<T1, T2, T3, T4>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);

            (T1, T2, T3, T4) value = default;
            var merger = Internal.CreateMergePreparer(ref value, MergeResultFuncs.GetFour<T1, T2, T3, T4>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            return merger.ToPromise(value);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise<(T1, T2, T3, T4)> Merge<T1, T2, T3, T4>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise promise5)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);

            (T1, T2, T3, T4) value = default;
            var merger = Internal.CreateMergePreparer(ref value, MergeResultFuncs.GetFour<T1, T2, T3, T4>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, value);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Five<T1, T2, T3, T4, T5>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (T1, T2, T3, T4, T5) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            result.Item2 = handler.GetResult<T2>();
                            break;
                        case 2:
                            result.Item3 = handler.GetResult<T3>();
                            break;
                        case 3:
                            result.Item4 = handler.GetResult<T4>();
                            break;
                        case 4:
                            result.Item5 = handler.GetResult<T5>();
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(T1, T2, T3, T4, T5)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(T1, T2, T3, T4, T5)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(T1, T2, T3, T4, T5)> GetFive<T1, T2, T3, T4, T5>() => Five<T1, T2, T3, T4, T5>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise<(T1, T2, T3, T4, T5)> Merge<T1, T2, T3, T4, T5>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);

            (T1, T2, T3, T4, T5) value = default;
            var merger = Internal.CreateMergePreparer(ref value, MergeResultFuncs.GetFive<T1, T2, T3, T4, T5>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, ref merger.GetResultRef(ref value).Item5, value, 4);
            return merger.ToPromise(value);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise<(T1, T2, T3, T4, T5)> Merge<T1, T2, T3, T4, T5>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise promise6)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);

            (T1, T2, T3, T4, T5) value = default;
            var merger = Internal.CreateMergePreparer(ref value, MergeResultFuncs.GetFive<T1, T2, T3, T4, T5>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, ref merger.GetResultRef(ref value).Item5, value, 4);
            merger.Prepare(promise6, value);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Six<T1, T2, T3, T4, T5, T6>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (T1, T2, T3, T4, T5, T6) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            result.Item2 = handler.GetResult<T2>();
                            break;
                        case 2:
                            result.Item3 = handler.GetResult<T3>();
                            break;
                        case 3:
                            result.Item4 = handler.GetResult<T4>();
                            break;
                        case 4:
                            result.Item5 = handler.GetResult<T5>();
                            break;
                        case 5:
                            result.Item6 = handler.GetResult<T6>();
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(T1, T2, T3, T4, T5, T6)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(T1, T2, T3, T4, T5, T6)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(T1, T2, T3, T4, T5, T6)> GetSix<T1, T2, T3, T4, T5, T6>() => Six<T1, T2, T3, T4, T5, T6>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise<(T1, T2, T3, T4, T5, T6)> Merge<T1, T2, T3, T4, T5, T6>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);

            (T1, T2, T3, T4, T5, T6) value = default;
            var merger = Internal.CreateMergePreparer(ref value, MergeResultFuncs.GetSix<T1, T2, T3, T4, T5, T6>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, ref merger.GetResultRef(ref value).Item5, value, 4);
            merger.Prepare(promise6, ref merger.GetResultRef(ref value).Item6, value, 5);
            return merger.ToPromise(value);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise<(T1, T2, T3, T4, T5, T6)> Merge<T1, T2, T3, T4, T5, T6>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise promise7)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);
            ValidateArgument(promise7, nameof(promise7), 1);

            (T1, T2, T3, T4, T5, T6) value = default;
            var merger = Internal.CreateMergePreparer(ref value, MergeResultFuncs.GetSix<T1, T2, T3, T4, T5, T6>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, ref merger.GetResultRef(ref value).Item5, value, 4);
            merger.Prepare(promise6, ref merger.GetResultRef(ref value).Item6, value, 5);
            merger.Prepare(promise7, value);
            return merger.ToPromise(value);
        }

        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Seven<T1, T2, T3, T4, T5, T6, T7>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, int index, ref (T1, T2, T3, T4, T5, T6, T7) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            result.Item2 = handler.GetResult<T2>();
                            break;
                        case 2:
                            result.Item3 = handler.GetResult<T3>();
                            break;
                        case 3:
                            result.Item4 = handler.GetResult<T4>();
                            break;
                        case 4:
                            result.Item5 = handler.GetResult<T5>();
                            break;
                        case 5:
                            result.Item6 = handler.GetResult<T6>();
                            break;
                        case 6:
                            result.Item7 = handler.GetResult<T7>();
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultDelegate<(T1, T2, T3, T4, T5, T6, T7)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultDelegate<(T1, T2, T3, T4, T5, T6, T7)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(T1, T2, T3, T4, T5, T6, T7)> GetSeven<T1, T2, T3, T4, T5, T6, T7>() => Seven<T1, T2, T3, T4, T5, T6, T7>.Func;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise<(T1, T2, T3, T4, T5, T6, T7)> Merge<T1, T2, T3, T4, T5, T6, T7>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise<T7> promise7)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);
            ValidateArgument(promise7, nameof(promise7), 1);

            (T1, T2, T3, T4, T5, T6, T7) value = default;
            var merger = Internal.CreateMergePreparer(ref value, MergeResultFuncs.GetSeven<T1, T2, T3, T4, T5, T6, T7>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, ref merger.GetResultRef(ref value).Item5, value, 4);
            merger.Prepare(promise6, ref merger.GetResultRef(ref value).Item6, value, 5);
            merger.Prepare(promise7, ref merger.GetResultRef(ref value).Item7, value, 6);
            return merger.ToPromise(value);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseMergeGroup"/> instead.
        /// </remarks>
        public static Promise<(T1, T2, T3, T4, T5, T6, T7)> Merge<T1, T2, T3, T4, T5, T6, T7>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise<T7> promise7, Promise promise8)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);
            ValidateArgument(promise7, nameof(promise7), 1);
            ValidateArgument(promise8, nameof(promise8), 1);

            (T1, T2, T3, T4, T5, T6, T7) value = default;
            var merger = Internal.CreateMergePreparer(ref value, MergeResultFuncs.GetSeven<T1, T2, T3, T4, T5, T6, T7>());
            merger.Prepare(promise1, ref merger.GetResultRef(ref value).Item1, value, 0);
            merger.Prepare(promise2, ref merger.GetResultRef(ref value).Item2, value, 1);
            merger.Prepare(promise3, ref merger.GetResultRef(ref value).Item3, value, 2);
            merger.Prepare(promise4, ref merger.GetResultRef(ref value).Item4, value, 3);
            merger.Prepare(promise5, ref merger.GetResultRef(ref value).Item5, value, 4);
            merger.Prepare(promise6, ref merger.GetResultRef(ref value).Item6, value, 5);
            merger.Prepare(promise7, ref merger.GetResultRef(ref value).Item7, value, 6);
            merger.Prepare(promise8, value);
            return merger.ToPromise(value);
        }
    }
}