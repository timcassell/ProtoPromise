﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

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
        public static Promise All(params Promise[] promises)
        {
            return All(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise All(IEnumerable<Promise> promises)
        {
            return All(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise All<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, "promises", 1);

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
        public static Promise<IList<T>> All<T>(Promise<T> promise1, Promise<T> promise2, IList<T> valueContainer = null)
        {
            return Promise<T>.All(promise1, promise2, valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, IList<T> valueContainer = null)
        {
            return Promise<T>.All(promise1, promise2, promise3, valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="promise4">The fourth promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4, IList<T> valueContainer = null)
        {
            return Promise<T>.All(promise1, promise2, promise3, promise4, valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<IList<T>> All<T>(params Promise<T>[] promises)
        {
            return Promise<T>.All(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/>s when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All<T>(IEnumerable<Promise<T>> promises, IList<T> valueContainer = null)
        {
            return Promise<T>.All(promises, valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promises">The enumerator of promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All<T, TEnumerator>(TEnumerator promises, IList<T> valueContainer = null) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Promise<T>.All(promises, valueContainer);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private static partial class MergeResultFuncs
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
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultDelegate<T1> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<T1> GetOne<T1>()
            {
                return One<T1>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the value of <paramref name="promise1"/> when both promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T1> Merge<T1>(Promise<T1> promise1, Promise promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            if (promise2._ref == null)
            {
                return promise1.Duplicate();
            }

            uint pendingCount = 1;
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(default(T1), MergeResultFuncs.GetOne<T1>());
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

        private static partial class MergeResultFuncs
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
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultDelegate<(T1, T2)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(T1, T2)> GetTwo<T1, T2>()
            {
                return Two<T1, T2>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(T1, T2)> Merge<T1, T2>(Promise<T1> promise1, Promise<T2> promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);

            (T1, T2) value = default;
            ref (T1, T2) valueRef = ref value;
            uint pendingCount = 0;
            var mergeResultFunc = MergeResultFuncs.GetTwo<T1, T2>();
            Internal.PromiseRefBase.MergePromise<(T1, T2)> promise = null;

            Internal.PrepareForMerge(promise1, ref valueRef.Item1, valueRef, ref pendingCount, 0, ref promise, mergeResultFunc);
            // It would be nice to be able to ref-reassign inside the PrepareForMerge helper to avoid an extra branch,
            // but unfortunately C# doesn't support ref to ref parameters (or ref fields in ref structs yet).
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise2, ref valueRef.Item2, valueRef, ref pendingCount, 1, ref promise, mergeResultFunc);

            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            promise.MarkReady(pendingCount);
            return new Promise<(T1, T2)>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(T1, T2)> Merge<T1, T2>(Promise<T1> promise1, Promise<T2> promise2, Promise promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);

            (T1, T2) value = default;
            ref (T1, T2) valueRef = ref value;
            uint pendingCount = 0;
            var mergeResultFunc = MergeResultFuncs.GetTwo<T1, T2>();
            Internal.PromiseRefBase.MergePromise<(T1, T2)> promise = null;

            Internal.PrepareForMerge(promise1, ref valueRef.Item1, valueRef, ref pendingCount, 0, ref promise, mergeResultFunc);
            // It would be nice to be able to ref-reassign inside the PrepareForMerge helper to avoid an extra branch,
            // but unfortunately C# doesn't support ref to ref parameters (or ref fields in ref structs yet).
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise2, ref valueRef.Item2, valueRef, ref pendingCount, 1, ref promise, mergeResultFunc);
            Internal.PrepareForMerge(promise3, valueRef, ref pendingCount, ref promise, mergeResultFunc);

            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            promise.MarkReady(pendingCount);
            return new Promise<(T1, T2)>(promise, promise.Id);
        }

        private static partial class MergeResultFuncs
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
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultDelegate<(T1, T2, T3)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(T1, T2, T3)> GetThree<T1, T2, T3>()
            {
                return Three<T1, T2, T3>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(T1, T2, T3)> Merge<T1, T2, T3>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);

            (T1, T2, T3) value = default;
            ref (T1, T2, T3) valueRef = ref value;
            uint pendingCount = 0;
            var mergeResultFunc = MergeResultFuncs.GetThree<T1, T2, T3>();
            Internal.PromiseRefBase.MergePromise<(T1, T2, T3)> promise = null;

            Internal.PrepareForMerge(promise1, ref valueRef.Item1, valueRef, ref pendingCount, 0, ref promise, mergeResultFunc);
            // It would be nice to be able to ref-reassign inside the PrepareForMerge helper to avoid extra branches,
            // but unfortunately C# doesn't support ref to ref parameters (or ref fields in ref structs yet).
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise2, ref valueRef.Item2, valueRef, ref pendingCount, 1, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise3, ref valueRef.Item3, valueRef, ref pendingCount, 2, ref promise, mergeResultFunc);

            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            promise.MarkReady(pendingCount);
            return new Promise<(T1, T2, T3)>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(T1, T2, T3)> Merge<T1, T2, T3>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);

            (T1, T2, T3) value = default;
            ref (T1, T2, T3) valueRef = ref value;
            uint pendingCount = 0;
            var mergeResultFunc = MergeResultFuncs.GetThree<T1, T2, T3>();
            Internal.PromiseRefBase.MergePromise<(T1, T2, T3)> promise = null;

            Internal.PrepareForMerge(promise1, ref valueRef.Item1, valueRef, ref pendingCount, 0, ref promise, mergeResultFunc);
            // It would be nice to be able to ref-reassign inside the PrepareForMerge helper to avoid extra branches,
            // but unfortunately C# doesn't support ref to ref parameters (or ref fields in ref structs yet).
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise2, ref valueRef.Item2, valueRef, ref pendingCount, 1, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise3, ref valueRef.Item3, valueRef, ref pendingCount, 2, ref promise, mergeResultFunc);
            Internal.PrepareForMerge(promise4, valueRef, ref pendingCount, ref promise, mergeResultFunc);

            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            promise.MarkReady(pendingCount);
            return new Promise<(T1, T2, T3)>(promise, promise.Id);
        }

        private static partial class MergeResultFuncs
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
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultDelegate<(T1, T2, T3, T4)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(T1, T2, T3, T4)> GetFour<T1, T2, T3, T4>()
            {
                return Four<T1, T2, T3, T4>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(T1, T2, T3, T4)> Merge<T1, T2, T3, T4>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);

            (T1, T2, T3, T4) value = default;
            ref (T1, T2, T3, T4) valueRef = ref value;
            uint pendingCount = 0;
            var mergeResultFunc = MergeResultFuncs.GetFour<T1, T2, T3, T4>();
            Internal.PromiseRefBase.MergePromise<(T1, T2, T3, T4)> promise = null;

            Internal.PrepareForMerge(promise1, ref valueRef.Item1, valueRef, ref pendingCount, 0, ref promise, mergeResultFunc);
            // It would be nice to be able to ref-reassign inside the PrepareForMerge helper to avoid extra branches,
            // but unfortunately C# doesn't support ref to ref parameters (or ref fields in ref structs yet).
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise2, ref valueRef.Item2, valueRef, ref pendingCount, 1, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise3, ref valueRef.Item3, valueRef, ref pendingCount, 2, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise4, ref valueRef.Item4, valueRef, ref pendingCount, 3, ref promise, mergeResultFunc);

            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            promise.MarkReady(pendingCount);
            return new Promise<(T1, T2, T3, T4)>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(T1, T2, T3, T4)> Merge<T1, T2, T3, T4>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise promise5)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);

            (T1, T2, T3, T4) value = default;
            ref (T1, T2, T3, T4) valueRef = ref value;
            uint pendingCount = 0;
            var mergeResultFunc = MergeResultFuncs.GetFour<T1, T2, T3, T4>();
            Internal.PromiseRefBase.MergePromise<(T1, T2, T3, T4)> promise = null;

            Internal.PrepareForMerge(promise1, ref valueRef.Item1, valueRef, ref pendingCount, 0, ref promise, mergeResultFunc);
            // It would be nice to be able to ref-reassign inside the PrepareForMerge helper to avoid extra branches,
            // but unfortunately C# doesn't support ref to ref parameters (or ref fields in ref structs yet).
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise2, ref valueRef.Item2, valueRef, ref pendingCount, 1, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise3, ref valueRef.Item3, valueRef, ref pendingCount, 2, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise4, ref valueRef.Item4, valueRef, ref pendingCount, 3, ref promise, mergeResultFunc);
            Internal.PrepareForMerge(promise5, valueRef, ref pendingCount, ref promise, mergeResultFunc);

            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            promise.MarkReady(pendingCount);
            return new Promise<(T1, T2, T3, T4)>(promise, promise.Id);
        }

        private static partial class MergeResultFuncs
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
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultDelegate<(T1, T2, T3, T4, T5)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(T1, T2, T3, T4, T5)> GetFive<T1, T2, T3, T4, T5>()
            {
                return Five<T1, T2, T3, T4, T5>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(T1, T2, T3, T4, T5)> Merge<T1, T2, T3, T4, T5>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);

            (T1, T2, T3, T4, T5) value = default;
            ref (T1, T2, T3, T4, T5) valueRef = ref value;
            uint pendingCount = 0;
            var mergeResultFunc = MergeResultFuncs.GetFive<T1, T2, T3, T4, T5>();
            Internal.PromiseRefBase.MergePromise<(T1, T2, T3, T4, T5)> promise = null;

            Internal.PrepareForMerge(promise1, ref valueRef.Item1, valueRef, ref pendingCount, 0, ref promise, mergeResultFunc);
            // It would be nice to be able to ref-reassign inside the PrepareForMerge helper to avoid extra branches,
            // but unfortunately C# doesn't support ref to ref parameters (or ref fields in ref structs yet).
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise2, ref valueRef.Item2, valueRef, ref pendingCount, 1, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise3, ref valueRef.Item3, valueRef, ref pendingCount, 2, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise4, ref valueRef.Item4, valueRef, ref pendingCount, 3, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise5, ref valueRef.Item5, valueRef, ref pendingCount, 4, ref promise, mergeResultFunc);

            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            promise.MarkReady(pendingCount);
            return new Promise<(T1, T2, T3, T4, T5)>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(T1, T2, T3, T4, T5)> Merge<T1, T2, T3, T4, T5>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise promise6)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);

            (T1, T2, T3, T4, T5) value = default;
            ref (T1, T2, T3, T4, T5) valueRef = ref value;
            uint pendingCount = 0;
            var mergeResultFunc = MergeResultFuncs.GetFive<T1, T2, T3, T4, T5>();
            Internal.PromiseRefBase.MergePromise<(T1, T2, T3, T4, T5)> promise = null;

            Internal.PrepareForMerge(promise1, ref valueRef.Item1, valueRef, ref pendingCount, 0, ref promise, mergeResultFunc);
            // It would be nice to be able to ref-reassign inside the PrepareForMerge helper to avoid extra branches,
            // but unfortunately C# doesn't support ref to ref parameters (or ref fields in ref structs yet).
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise2, ref valueRef.Item2, valueRef, ref pendingCount, 1, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise3, ref valueRef.Item3, valueRef, ref pendingCount, 2, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise4, ref valueRef.Item4, valueRef, ref pendingCount, 3, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise5, ref valueRef.Item5, valueRef, ref pendingCount, 4, ref promise, mergeResultFunc);
            Internal.PrepareForMerge(promise6, valueRef, ref pendingCount, ref promise, mergeResultFunc);

            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            promise.MarkReady(pendingCount);
            return new Promise<(T1, T2, T3, T4, T5)>(promise, promise.Id);
        }

        private static partial class MergeResultFuncs
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
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultDelegate<(T1, T2, T3, T4, T5, T6)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(T1, T2, T3, T4, T5, T6)> GetSix<T1, T2, T3, T4, T5, T6>()
            {
                return Six<T1, T2, T3, T4, T5, T6>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(T1, T2, T3, T4, T5, T6)> Merge<T1, T2, T3, T4, T5, T6>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            ValidateArgument(promise5, nameof(promise5), 1);
            ValidateArgument(promise6, nameof(promise6), 1);

            (T1, T2, T3, T4, T5, T6) value = default;
            ref (T1, T2, T3, T4, T5, T6) valueRef = ref value;
            uint pendingCount = 0;
            var mergeResultFunc = MergeResultFuncs.GetSix<T1, T2, T3, T4, T5, T6>();
            Internal.PromiseRefBase.MergePromise<(T1, T2, T3, T4, T5, T6)> promise = null;

            Internal.PrepareForMerge(promise1, ref valueRef.Item1, valueRef, ref pendingCount, 0, ref promise, mergeResultFunc);
            // It would be nice to be able to ref-reassign inside the PrepareForMerge helper to avoid extra branches,
            // but unfortunately C# doesn't support ref to ref parameters (or ref fields in ref structs yet).
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise2, ref valueRef.Item2, valueRef, ref pendingCount, 1, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise3, ref valueRef.Item3, valueRef, ref pendingCount, 2, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise4, ref valueRef.Item4, valueRef, ref pendingCount, 3, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise5, ref valueRef.Item5, valueRef, ref pendingCount, 4, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise6, ref valueRef.Item6, valueRef, ref pendingCount, 5, ref promise, mergeResultFunc);

            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            promise.MarkReady(pendingCount);
            return new Promise<(T1, T2, T3, T4, T5, T6)>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
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
            ref (T1, T2, T3, T4, T5, T6) valueRef = ref value;
            uint pendingCount = 0;
            var mergeResultFunc = MergeResultFuncs.GetSix<T1, T2, T3, T4, T5, T6>();
            Internal.PromiseRefBase.MergePromise<(T1, T2, T3, T4, T5, T6)> promise = null;

            Internal.PrepareForMerge(promise1, ref valueRef.Item1, valueRef, ref pendingCount, 0, ref promise, mergeResultFunc);
            // It would be nice to be able to ref-reassign inside the PrepareForMerge helper to avoid extra branches,
            // but unfortunately C# doesn't support ref to ref parameters (or ref fields in ref structs yet).
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise2, ref valueRef.Item2, valueRef, ref pendingCount, 1, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise3, ref valueRef.Item3, valueRef, ref pendingCount, 2, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise4, ref valueRef.Item4, valueRef, ref pendingCount, 3, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise5, ref valueRef.Item5, valueRef, ref pendingCount, 4, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise6, ref valueRef.Item6, valueRef, ref pendingCount, 5, ref promise, mergeResultFunc);
            Internal.PrepareForMerge(promise7, valueRef, ref pendingCount, ref promise, mergeResultFunc);

            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            promise.MarkReady(pendingCount);
            return new Promise<(T1, T2, T3, T4, T5, T6)>(promise, promise.Id);
        }

        private static partial class MergeResultFuncs
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
                    get { return new(&GetMergeResult); }
                }
#else
                internal static readonly Internal.GetResultDelegate<(T1, T2, T3, T4, T5, T6, T7)> Func = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultDelegate<(T1, T2, T3, T4, T5, T6, T7)> GetSeven<T1, T2, T3, T4, T5, T6, T7>()
            {
                return Seven<T1, T2, T3, T4, T5, T6, T7>.Func;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
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
            ref (T1, T2, T3, T4, T5, T6, T7) valueRef = ref value;
            uint pendingCount = 0;
            var mergeResultFunc = MergeResultFuncs.GetSeven<T1, T2, T3, T4, T5, T6, T7>();
            Internal.PromiseRefBase.MergePromise<(T1, T2, T3, T4, T5, T6, T7)> promise = null;

            Internal.PrepareForMerge(promise1, ref valueRef.Item1, valueRef, ref pendingCount, 0, ref promise, mergeResultFunc);
            // It would be nice to be able to ref-reassign inside the PrepareForMerge helper to avoid extra branches,
            // but unfortunately C# doesn't support ref to ref parameters (or ref fields in ref structs yet).
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise2, ref valueRef.Item2, valueRef, ref pendingCount, 1, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise3, ref valueRef.Item3, valueRef, ref pendingCount, 2, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise4, ref valueRef.Item4, valueRef, ref pendingCount, 3, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise5, ref valueRef.Item5, valueRef, ref pendingCount, 4, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise6, ref valueRef.Item6, valueRef, ref pendingCount, 5, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise7, ref valueRef.Item7, valueRef, ref pendingCount, 6, ref promise, mergeResultFunc);

            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            promise.MarkReady(pendingCount);
            return new Promise<(T1, T2, T3, T4, T5, T6, T7)>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
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
            ref (T1, T2, T3, T4, T5, T6, T7) valueRef = ref value;
            uint pendingCount = 0;
            var mergeResultFunc = MergeResultFuncs.GetSeven<T1, T2, T3, T4, T5, T6, T7>();
            Internal.PromiseRefBase.MergePromise<(T1, T2, T3, T4, T5, T6, T7)> promise = null;

            Internal.PrepareForMerge(promise1, ref valueRef.Item1, valueRef, ref pendingCount, 0, ref promise, mergeResultFunc);
            // It would be nice to be able to ref-reassign inside the PrepareForMerge helper to avoid extra branches,
            // but unfortunately C# doesn't support ref to ref parameters (or ref fields in ref structs yet).
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise2, ref valueRef.Item2, valueRef, ref pendingCount, 1, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise3, ref valueRef.Item3, valueRef, ref pendingCount, 2, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise4, ref valueRef.Item4, valueRef, ref pendingCount, 3, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise5, ref valueRef.Item5, valueRef, ref pendingCount, 4, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise6, ref valueRef.Item6, valueRef, ref pendingCount, 5, ref promise, mergeResultFunc);
            valueRef = promise == null ? ref value : ref promise._result;
            Internal.PrepareForMerge(promise7, ref valueRef.Item7, valueRef, ref pendingCount, 6, ref promise, mergeResultFunc);
            Internal.PrepareForMerge(promise8, valueRef, ref pendingCount, ref promise, mergeResultFunc);

            if (pendingCount == 0)
            {
                return Resolved(value);
            }
            promise.MarkReady(pendingCount);
            return new Promise<(T1, T2, T3, T4, T5, T6, T7)>(promise, promise.Id);
        }
    }
}