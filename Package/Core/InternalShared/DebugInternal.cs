#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0028 // Simplify collection initialization
#pragma warning disable IDE0062 // Local function can be made static
#pragma warning disable IDE0074 // Use compound assignment
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0290 // Use primary constructor
#pragma warning disable IDE0300 // Simplify collection initialization

using Proto.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
#if PROTO_PROMISE_DEVELOPER_MODE
        internal const MethodImplOptions InlineOption = MethodImplOptions.NoInlining;
#elif PROMISE_DEBUG
        internal const MethodImplOptions InlineOption = 0;
#else
        internal const MethodImplOptions InlineOption = MethodImplOptions.AggressiveInlining;
#endif

        internal static void ValidateProgressValue(double value, string argName, int skipFrames)
        {
            bool isBetween01 = value >= 0f && value <= 1f;
            if (!isBetween01)
            {
                throw new ArgumentOutOfRangeException(argName, "Must be between 0 and 1. Actual: " + value, GetFormattedStacktrace(skipFrames + 1));
            }
        }

        // Calls to these get compiled away in RELEASE mode
        partial class PromiseRefBase
        {
            partial void ValidateReturn(Promise other);
            partial void ValidateAwait(PromiseRefBase other, short promiseId);
        }

        internal static void RecordException(Exception e, ref List<Exception> exceptions)
        {
            if (exceptions == null)
            {
                exceptions = new List<Exception>();
            }
            exceptions.Add(e);
        }

#if PROMISE_DEBUG
        // In DEBUG mode, causality traces are stored in fields, so they're reliably available when leaked objects are reported from their finalizers.
        // (Some runtimes, like Unity's Mono and IL2CPP, clear ConditionalWeakTable entries before the key's finalizer runs.)
        internal interface ITraceable
        {
            CausalityTrace Trace { get; set; }
        }

        // Returns null if causality traces are disabled, the same as in RELEASE mode.
        [MethodImpl(InlineOption)]
        internal static CausalityTrace GetTrace(object obj)
            => CausalityTrace.IsEnabled ? (obj as ITraceable)?.Trace : null;

        internal static void SetTrace(object obj, CausalityTrace trace)
            // This throws if a traced type is missing the ITraceable implementation.
            => ((ITraceable) obj).Trace = trace;

        // Objects are never pooled in DEBUG mode, so their traces are released with them.
        internal static void ClearTraces() { }
#else
        // In RELEASE mode, causality traces are stored in a table instead of fields to keep objects small, since they are rarely enabled.
        // Object pooling is disabled while causality traces are enabled, so entries are collected with their objects.
        // Some runtimes (like Unity's Mono and IL2CPP) clear the entries before the key's finalizer runs,
        // so leaked objects that are reported from their finalizers may not have a trace. Older runtimes (like Unity 2018's Mono and IL2CPP)
        // never clear the entries, so traced objects leak while traces are enabled. DEBUG mode stores them in fields instead.
        // The table is replaced when causality traces are disabled, so that objects that are pooled again don't keep their traces alive.
        private static ConditionalWeakTable<object, CausalityTrace> s_causalityTraces = new ConditionalWeakTable<object, CausalityTrace>();

        internal static void ClearTraces()
            => s_causalityTraces = new ConditionalWeakTable<object, CausalityTrace>();

        // Returns null if causality traces are disabled, without accessing the table.
        [MethodImpl(InlineOption)]
        internal static CausalityTrace GetTrace(object obj)
            => CausalityTrace.IsEnabled ? GetTraceFromTable(obj) : null;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static CausalityTrace GetTraceFromTable(object obj)
            => obj != null && s_causalityTraces.TryGetValue(obj, out var trace) ? trace : null;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SetTrace(object obj, CausalityTrace trace)
        {
            // The object may already have an entry if it was taken from the pool after it was traced previously.
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
            s_causalityTraces.AddOrUpdate(obj, trace);
#else
            // This is only set from the thread that creates the object, so we don't need to lock.
            s_causalityTraces.Remove(obj);
            s_causalityTraces.Add(obj, trace);
#endif
        }
#endif // PROMISE_DEBUG

        // Gives target the same trace as source (e.g. a lock key uses the trace of the promise that acquired the lock).
        [MethodImpl(InlineOption)]
        internal static void CopyTrace(object source, object target)
        {
            var trace = GetTrace(source);
            if (trace != null)
            {
                SetTrace(target, trace);
            }
        }

        // We don't skip frames, because our own frames are hidden when the trace is formatted, and counting frames is unreliable when methods are inlined.
        // We don't clear the trace when it's disabled, because GetTrace returns null when it's disabled.
        [MethodImpl(InlineOption)]
        internal static void SetCreatedStacktrace(object obj)
        {
            if (CausalityTrace.IsEnabled)
            {
                SetTrace(obj, CausalityTrace.Capture());
            }
        }

        private static StackTrace GetStackTrace(int skipFrames)
#if PROTO_PROMISE_DEVELOPER_MODE // Don't skip frames in developer mode.
            => new StackTrace(1, true);
#elif NET6_0_OR_GREATER // Internal frames are already hidden in .Net 6+.
            => new StackTrace(true);
#else
            => new StackTrace(skipFrames + 1, true);
#endif

        static partial void SetPrevious(this PromiseRefBase promise, PromiseRefBase previous);

#if PROMISE_DEBUG
        // We only set _previous to support circular await detection in DEBUG mode.
        static partial void SetPrevious(this PromiseRefBase promise, PromiseRefBase previous)
            => promise._previous = previous;

        internal static string GetFormattedStacktrace(object traceable)
            => GetTrace(traceable)?.ToString();

        internal static string GetFormattedStacktrace(int skipFrames)
            => CausalityTrace.FormatStackTrace(GetStackTrace(skipFrames + 1));

        internal static void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(argName, null, GetFormattedStacktrace(skipFrames + 1));
            }
        }
        internal static void ValidateArgument(Promise arg, string argName, int skipFrames)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (!arg.IsValid)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                throw new InvalidArgumentException(argName,
                    "Promise is invalid. Call `GetRetainer()` if you intend to await multiple times.",
                    GetFormattedStacktrace(skipFrames + 1));
            }
        }

        internal static void ValidateOperation(Promise promise, int skipFrames)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (!promise.IsValid)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                throw new InvalidOperationException("Promise is invalid." +
                    " Call `GetRetainer()` if you intend to await multiple times.",
                    GetFormattedStacktrace(skipFrames + 1));
            }
        }

        partial class PromiseRefBase
        {
            partial void ValidateReturn(Promise other)
                => ValidateAwait(other._ref, other._id, false);

            partial void ValidateAwait(PromiseRefBase other, short promiseId)
                => ValidateAwait(other, promiseId, true);

            private void ValidateAwait(PromiseRefBase other, short promiseId, bool awaited)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (new Promise(other, promiseId).IsValid == false)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    // Awaiting or returning an invalid from the callback is not allowed.
                    if (awaited)
                    {
                        throw new InvalidOperationException("An invalid promise was awaited.", string.Empty);
                    }
                    throw new InvalidReturnException("An invalid promise was returned.", string.Empty);
                }

                // A promise cannot wait on itself.
                if (other == this)
                {
                    other.MaybeMarkAwaitedAndDispose(other.Id);
                    if (awaited)
                    {
                        throw new InvalidOperationException("A Promise cannot wait on itself.", string.Empty);
                    }
                    throw new InvalidReturnException("A Promise cannot wait on itself.", string.Empty);
                }
                if (other == null)
                {
                    return;
                }
                // This allows us to check Merge/All/Race/First Promises iteratively.
                Stack<PromiseRefBase> previouses = PreviousesForIterativeAlgorithm;
                PromiseRefBase prev = other._previous;
                while (true)
                {
                    for (; prev != null; prev = prev._previous)
                    {
                        if (prev == this)
                        {
                            other.MaybeMarkAwaitedAndDispose(other.Id);
                            previouses.Clear();
                            if (awaited)
                            {
                                throw new InvalidOperationException("Circular Promise chain detected.", GetFormattedStacktrace(other));
                            }
                            throw new InvalidReturnException("Circular Promise chain detected.", GetFormattedStacktrace(other));
                        }
                        prev.BorrowPreviousPromises(previouses);
                    }

                    if (previouses.Count <= 0)
                    {
                        break;
                    }
                    prev = previouses.Pop();
                }
            }

            [ThreadStatic]
            private static Stack<PromiseRefBase> ts_previousesForIterativeAlgorithm;
            private static Stack<PromiseRefBase> PreviousesForIterativeAlgorithm
            {
                get
                {
                    if (ts_previousesForIterativeAlgorithm == null)
                    {
                        ts_previousesForIterativeAlgorithm = new Stack<PromiseRefBase>();
                    }
                    return ts_previousesForIterativeAlgorithm;
                }
            }

            protected virtual void BorrowPreviousPromises(Stack<PromiseRefBase> borrower) { }

            partial class MultiHandleablePromiseBase<TResult>
            {
                private readonly HashSet<PromiseRefBase> _pendingPromises = new HashSet<PromiseRefBase>();

                protected override void BorrowPreviousPromises(Stack<PromiseRefBase> borrower)
                {
                    lock (_pendingPromises)
                    {
                        foreach (var promiseRef in _pendingPromises)
                        {
                            borrower.Push(promiseRef);
                        }
                    }
                }

                private void ValidateNoPending()
                {
                    lock (_pendingPromises)
                    {
                        if (_pendingPromises.Count != 0)
                        {
                            throw new System.InvalidOperationException("MultiHandleablePromiseBase disposed with pending promises.");
                        }
                    }
                }

                partial void AddPending(PromiseRefBase pendingPromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Add(pendingPromise);
                    }
                }

                partial void RemoveComplete(PromiseRefBase completePromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Remove(completePromise);
                    }
                }

                new protected void Dispose()
                {
                    ValidateNoPending();
                    base.Dispose();
                }
            }

            partial class PromiseParallelForEach<TEnumerator, TParallelBody, TSource>
            {
                private readonly HashSet<PromiseRefBase> _pendingPromises = new HashSet<PromiseRefBase>();

                protected override void BorrowPreviousPromises(Stack<PromiseRefBase> borrower)
                {
                    lock (_pendingPromises)
                    {
                        foreach (var promiseRef in _pendingPromises)
                        {
                            borrower.Push(promiseRef);
                        }
                    }
                }

                partial void ValidateNoPending()
                {
                    lock (_pendingPromises)
                    {
                        if (_pendingPromises.Count != 0)
                        {
                            throw new System.InvalidOperationException("PromiseParallelForEach disposed with pending promises.");
                        }
                    }
                }

                partial void AddPending(PromiseRefBase pendingPromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Add(pendingPromise);
                    }
                }

                partial void RemoveComplete(PromiseRefBase completePromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Remove(completePromise);
                    }
                }
            }

            partial class PromiseParallelForEachAsync<TParallelBody, TSource>
            {
                private readonly HashSet<PromiseRefBase> _pendingPromises = new HashSet<PromiseRefBase>();

                protected override void BorrowPreviousPromises(Stack<PromiseRefBase> borrower)
                {
                    lock (_pendingPromises)
                    {
                        foreach (var promiseRef in _pendingPromises)
                        {
                            borrower.Push(promiseRef);
                        }
                    }
                }

                partial void ValidateNoPending()
                {
                    lock (_pendingPromises)
                    {
                        if (_pendingPromises.Count != 0)
                        {
                            throw new System.InvalidOperationException("PromiseParallelForEachAsync disposed with pending promises.");
                        }
                    }
                }

                partial void AddPending(PromiseRefBase pendingPromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Add(pendingPromise);
                    }
                }

                partial void RemoveComplete(PromiseRefBase completePromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Remove(completePromise);
                    }
                }
            }
        }
#else // PROMISE_DEBUG
        internal static string GetFormattedStacktrace(int skipFrames) => null;

        internal static string GetFormattedStacktrace(object traceable) => null;
#endif // PROMISE_DEBUG
    } // class Internal

    partial struct Promise
    {
        // Calls to these get compiled away in RELEASE mode
        partial void ValidateOperation(int skipFrames);
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
        static partial void ValidateArgument(Promise arg, string argName, int skipFrames);
        static partial void ValidateElement(Promise promise, string argName, int skipFrames);

#if PROMISE_DEBUG
        partial void ValidateOperation(int skipFrames)
            => Internal.ValidateOperation(this, skipFrames + 1);

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            => Internal.ValidateArgument(arg, argName, skipFrames + 1);

        static partial void ValidateArgument(Promise arg, string argName, int skipFrames)
            => Internal.ValidateArgument(arg, argName, skipFrames + 1);

        static partial void ValidateElement(Promise promise, string argName, int skipFrames)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (!promise.IsValid)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                throw new InvalidElementException(argName,
                    $"A promise is invalid in {argName}." +
                    " Call `GetRetainer()` if you intend to await multiple times.",
                    Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }
#endif
    }

    partial struct Promise<T>
    {
        // Calls to these get compiled away in RELEASE mode
        partial void ValidateOperation(int skipFrames);
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
        static partial void ValidateArgument(Promise<T> arg, string argName, int skipFrames);
        static partial void ValidateElement(Promise<T> promise, string argName, int skipFrames);
#if PROMISE_DEBUG
        partial void ValidateOperation(int skipFrames)
            => Internal.ValidateOperation(this, skipFrames + 1);

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            => Internal.ValidateArgument(arg, argName, skipFrames + 1);

        static partial void ValidateArgument(Promise<T> arg, string argName, int skipFrames)
            => Internal.ValidateArgument(arg, argName, skipFrames + 1);

        static partial void ValidateElement(Promise<T> promise, string argName, int skipFrames)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (!promise.IsValid)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                throw new InvalidElementException(argName,
                    $"A promise is invalid in {argName}." +
                    " Call `GetRetainer()` if you intend to await multiple times.",
                    Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }
#endif
    }
}