﻿#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0031 // Use null propagation
#pragma warning disable IDE0074 // Use compound assignment
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#if PROMISE_DEBUG && !NET_LEGACY
using System.Linq;
#endif

namespace Proto.Promises
{
    partial class Internal
    {
        internal static string CausalityTraceMessage
        {
            get
            {
#if PROMISE_DEBUG
                return Promise.Config.DebugCausalityTracer == Promise.TraceLevel.All
                    ? " -- This exception's Stacktrace contains the causality trace of all async callbacks that ran."
                    : " -- Set Promise.Config.DebugCausalityTracer = Promise.TraceLevel.All to get a causality trace.";
#else
                return " -- Enable DEBUG mode and set Promise.Config.DebugCausalityTracer = Promise.TraceLevel.All to get a causality trace.";
#endif
            }
        }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        internal const MethodImplOptions InlineOption = MethodImplOptions.NoInlining;
#else
        internal const MethodImplOptions InlineOption = (MethodImplOptions) 256; // AggressiveInlining
#endif

        internal static void ValidateProgressValue(float value, string argName, int skipFrames)
        {
            bool isBetween01 = value >= 0f && value <= 1f;
            if (!isBetween01)
            {
                throw new ArgumentOutOfRangeException(argName, "Must be between 0 and 1.", GetFormattedStacktrace(skipFrames + 1));
            }
        }

        // Calls to these get compiled away in RELEASE mode
        partial class PromiseRefBase
        {
            partial void ValidateReturn(Promise other);
            partial void ValidateAwait(PromiseRefBase other, short promiseId);
        }

        [MethodImpl(InlineOption)]
        internal static void SetCreatedStacktraceInternal(ITraceable traceable, int skipFrames)
        {
            SetCreatedStacktrace(traceable, skipFrames);
        }

#if PROMISE_DEBUG
        private sealed class SyncTrace : ITraceable
        {
            CausalityTrace ITraceable.Trace { get; set; }

            private SyncTrace() { }

            internal static ITraceable GetCurrent(int skipFrames)
            {
                if (Promise.Config.DebugCausalityTracer != Promise.TraceLevel.All)
                {
                    return null;
                }

                var syncTrace = new SyncTrace();
                SetCreatedStacktrace(syncTrace, skipFrames + 1);
                return syncTrace;
            }
        }

        private static ITraceable SynchronousTraceable
        {
            get { return SyncTrace.GetCurrent(2); }
        }
#else
        private const ITraceable SynchronousTraceable = null;
#endif

        static partial void SetCreatedStacktrace(ITraceable traceable, int skipFrames);
        static partial void SetCurrentInvoker(ITraceable current);
        static partial void ClearCurrentInvoker();
#if PROMISE_DEBUG
        static partial void SetCreatedStacktrace(ITraceable traceable, int skipFrames)
        {
            StackTrace stackTrace = Promise.Config.DebugCausalityTracer == Promise.TraceLevel.All
                ? GetStackTrace(skipFrames + 1)
                : null;
            traceable.Trace = new CausalityTrace(stackTrace, ts_currentTrace);
        }

        [ThreadStatic]
        private static CausalityTrace ts_currentTrace;
        [ThreadStatic]
        private static Stack<CausalityTrace> ts_traces;

        static partial void SetCurrentInvoker(ITraceable current)
        {
            if (ts_traces == null)
            {
                ts_traces = new Stack<CausalityTrace>();
            }
            ts_traces.Push(ts_currentTrace);
            if (current != null)
            {
                ts_currentTrace = current.Trace;
            }
        }

        static partial void ClearCurrentInvoker()
        {
            ts_currentTrace = ts_traces.Pop();
        }

        private static StackTrace GetStackTrace(int skipFrames)
        {
            return new StackTrace(skipFrames + 1, true);
        }

        internal static string GetFormattedStacktrace(ITraceable traceable)
        {
            return traceable != null ? traceable.Trace.ToString() : null;
        }

        internal static string GetFormattedStacktrace(int skipFrames)
        {
            return FormatStackTrace(new StackTrace[1] { GetStackTrace(skipFrames + 1) });
        }

        internal static void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(argName, null, GetFormattedStacktrace(skipFrames + 1));
            }
        }

        internal static string FormatStackTrace(IEnumerable<StackTrace> stackTraces)
        {
            const string CausalitySplitMessage = "--- End of stack trace from the previous location where the exception was thrown ---";

#if NET_LEGACY
            // Format stack trace to match "throw exception" so that double-clicking log in Unity console will go to the proper line.
            var _stackTraces = new List<string>();
            var separator = new string[1] { Environment.NewLine + " " };
            var sb = new System.Text.StringBuilder();
            foreach (StackTrace st in stackTraces)
            {
                string stackTrace = st.ToString();
                if (string.IsNullOrEmpty(stackTrace))
                {
                    continue;
                }
                foreach (var trace in stackTrace.Substring(1).Split(separator, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!trace.Contains("Proto.Promises"))
                    {
                        sb.AppendLine(trace);
                    }
                }
                sb.Replace(":line ", ":")
                    .Replace("(", " (")
                    .Replace(") in", ") [0x00000] in"); // Not sure what "[0x00000]" is, but it's necessary for Unity's parsing.
                _stackTraces.Add(sb.ToString());
                sb.Length = 0;
            }
            if (_stackTraces.Count == 0)
            {
                return " ";
            }
            if (_stackTraces.Count == 1)
            {
                return _stackTraces[0] + " ";
            }
            for (int i = 0, max = _stackTraces.Count - 1; i < max ; ++i)
            {
                sb.Append(_stackTraces[i]).Append(" ")
                    .AppendLine()
                    .Append(CausalitySplitMessage).Append(" ")
                    .AppendLine();

            }
            sb.Append(_stackTraces[_stackTraces.Count - 1]).Append(" ");
            return sb.ToString();
#else // NET_LEGACY
            // StackTrace.ToString() format issue was fixed in the new runtime.
            var causalityTrace = stackTraces
                .Select(stackTrace => stackTrace.GetFrames()
                    .Where(frame =>
                    {
                        // Ignore DebuggerNonUserCode and DebuggerHidden.
                        var methodType = frame?.GetMethod();
                        return methodType != null
                            && !methodType.IsDefined(typeof(DebuggerHiddenAttribute), false)
                            && !IsNonUserCode(methodType.DeclaringType);
                    })
                    // Create a new StackTrace to get proper formatting.
                    .Select(frame => new StackTrace(frame).ToString())
                )
                .Select(filteredStackTrace => string.Join(
                    Environment.NewLine,
                    filteredStackTrace
                    )
                );

            return string.Join(
                Environment.NewLine + CausalitySplitMessage + Environment.NewLine,
                causalityTrace);
#endif // NET_LEGACY
        }

#if !NET_LEGACY
        private static bool IsNonUserCode(System.Reflection.MemberInfo memberInfo)
        {
            if (memberInfo == null)
            {
                return false;
            }
            return memberInfo.IsDefined(typeof(DebuggerNonUserCodeAttribute), false)
                || IsNonUserCode(memberInfo.DeclaringType);
        }
#endif // !NET_LEGACY

        partial interface ITraceable
        {
            CausalityTrace Trace { get; set; }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class CausalityTrace
        {
            private readonly StackTrace _stackTrace;
            private readonly CausalityTrace _next;

            public CausalityTrace(StackTrace stackTrace, CausalityTrace higherStacktrace)
            {
                _stackTrace = stackTrace;
                _next = higherStacktrace;
            }

            public override string ToString()
            {
                if (_stackTrace == null)
                {
                    return null;
                }
                return FormatStackTrace(GetStackTraces());
            }

            private IEnumerable<StackTrace> GetStackTraces()
            {
                for (CausalityTrace current = this; current != null; current = current._next)
                {
                    if (current._stackTrace == null)
                    {
                        yield break;
                    }
                    yield return current._stackTrace;
                }
            }
        }

        internal static void ValidateOperation(Promise promise, int skipFrames)
        {
            if (!promise.IsValid)
            {
                throw new InvalidOperationException("Promise is invalid." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!",
                    GetFormattedStacktrace(skipFrames + 1));
            }
        }

        partial class PromiseRefBase
        {
            partial void ValidateReturn(Promise other)
            {
                ValidateAwait(other._ref, other._id, false);
            }

            partial void ValidateAwait(PromiseRefBase other, short promiseId)
            {
                ValidateAwait(other, promiseId, true);
            }

            private void ValidateAwait(PromiseRefBase other, short promiseId, bool awaited)
            {
                if (new Promise(other, promiseId, 0).IsValid == false)
                {
                    // Awaiting or returning an invalid from the callback is not allowed.
                    if (awaited)
                        throw new InvalidOperationException("An invalid promise was awaited.", string.Empty);
                    throw new InvalidReturnException("An invalid promise was returned.", string.Empty);
                }

                // A promise cannot wait on itself.
                if (other == this)
                {
                    other.MaybeMarkAwaitedAndDispose(other.Id);
                    if (awaited)
                        throw new InvalidOperationException("A Promise cannot wait on itself.", string.Empty);
                    throw new InvalidReturnException("A Promise cannot wait on itself.", string.Empty);
                }
                if (other == null)
                {
                    return;
                }
                // This allows us to check Merge/All/Race/First Promises iteratively.
                Stack<PromiseRefBase> previouses = PreviousesForIterativeAlgorithm;
                PromiseRefBase prev = other._previous;
            Repeat:
                for (; prev != null; prev = prev._previous)
                {
                    if (prev == this)
                    {
                        other.MaybeMarkAwaitedAndDispose(other.Id);
                        previouses.Clear();
                        if (awaited)
                            throw new InvalidOperationException("Circular Promise chain detected.", GetFormattedStacktrace(other));
                        throw new InvalidReturnException("Circular Promise chain detected.", GetFormattedStacktrace(other));
                    }
                    prev.BorrowPreviousPromises(previouses);
                }

                if (previouses.Count > 0)
                {
                    prev = previouses.Pop();
                    goto Repeat;
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
                private readonly Stack<PromiseRefBase> _previousPromises = new Stack<PromiseRefBase>();

                protected override void BorrowPreviousPromises(Stack<PromiseRefBase> borrower)
                {
                    lock (_previousPromises)
                    {
                        foreach (var promiseRef in _previousPromises)
                        {
                            borrower.Push(promiseRef);
                        }
                    }
                }

                partial void AddPending(PromiseRefBase pendingPromise)
                {
                    lock (_previousPromises)
                    {
                        _previousPromises.Push(pendingPromise);
                    }
                }

                partial void ClearPending()
                {
                    lock (_previousPromises)
                    {
                        _previousPromises.Clear();
                    }
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

                partial void AddPending(PromiseRefBase pendingPromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Add(pendingPromise);
                    }
                }

                partial void RemovePending(PromiseRefBase completePromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Remove(completePromise);
                    }
                }
            }

#if CSHARP_7_3_OR_NEWER
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

                partial void AddPending(PromiseRefBase pendingPromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Add(pendingPromise);
                    }
                }

                partial void RemovePending(PromiseRefBase completePromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Remove(completePromise);
                    }
                }
            }
#endif // CSHARP_7_3_OR_NEWER
        }
#else // PROMISE_DEBUG
        internal static string GetFormattedStacktrace(int skipFrames)
        {
            return null;
        }

        internal static string GetFormattedStacktrace(ITraceable traceable)
        {
            return null;
        }
#endif // PROMISE_DEBUG

        static partial void ThrowIfInPool(object obj);
        static partial void MaybeThrowIfInPool(object obj, bool shouldCheck);
#if PROTO_PROMISE_DEVELOPER_MODE
        internal static bool s_trackObjectsForRelease = false;
        private static readonly HashSet<object> s_pooledObjects = new HashSet<object>();
        private static readonly HashSet<object> s_inUseObjects = new HashSet<object>();

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
        static unsafe Internal() { AddClearPoolListener(&ClearObjectTracking); }
#else
        static Internal() { AddClearPoolListener(ClearObjectTracking); }
#endif

        private static void ClearObjectTracking()
        {
            lock (s_pooledObjects)
            {
                s_pooledObjects.Clear();
            }
        }

        internal static void MarkInPool(object obj)
        {
            lock (s_pooledObjects)
            {
                if (Promise.Config.ObjectPoolingEnabled && !s_pooledObjects.Add(obj))
                {
                    throw new Exception("Same object was added to the pool twice: " + obj);
                }
                s_inUseObjects.Remove(obj);
            }
        }

        internal static void MarkNotInPool(object obj)
        {
            if (obj == null || obj == PromiseRefBase.InvalidAwaitSentinel.s_instance)
            {
                return;
            }
            lock (s_pooledObjects)
            {
                s_pooledObjects.Remove(obj);
                if (s_trackObjectsForRelease && !s_inUseObjects.Add(obj))
                {
                    throw new Exception("Same object was taken from the pool twice: " + obj);
                }
            }
        }

        static partial void ThrowIfInPool(object obj)
        {
            lock (s_pooledObjects)
            {
                if (s_pooledObjects.Contains(obj))
                {
                    throw new Exception("Object is in pool: " + obj);
                }
            }
        }

        static partial void MaybeThrowIfInPool(object obj, bool shouldCheck)
        {
            if (shouldCheck)
            {
                ThrowIfInPool(obj);
            }
        }

        // This is used in unit testing, because finalizers are not guaranteed to run, even when calling `GC.WaitForPendingFinalizers()`.
        internal static void TrackObjectsForRelease()
        {
            s_trackObjectsForRelease = true;
        }

        internal static void AssertAllObjectsReleased()
        {
            lock (s_pooledObjects)
            {
                if (s_inUseObjects.Count > 0)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.AppendLine(s_inUseObjects.Count + " objects not released:");
                    sb.AppendLine();
                    ITraceable traceable = null;
                    int counter = 0;
                    foreach (var obj in s_inUseObjects)
                    {
                        // Only capture up to 100 objects to prevent overloading the test error output.
                        if (++counter <= 100)
                        {
                            traceable = traceable ?? obj as ITraceable;
                            sb.AppendLine(obj.ToString());
                        }
                        GC.SuppressFinalize(obj); // SuppressFinalize to not spoil the results of subsequent unit tests.
                    }
                    s_inUseObjects.Clear();
                    throw new UnreleasedObjectException(sb.ToString(), GetFormattedStacktrace(traceable));
                }
            }
        }
#endif // PROTO_PROMISE_DEVELOPER_MODE

        [MethodImpl(InlineOption)]
        internal static void TrackFinalizableInternal(IFinalizable finalizable)
        {
            TrackFinalizable(finalizable);
        }

        // Calls to these will be compiled away if the mode is not DEBUG or DEVELOPER.
        static partial void TrackFinalizable(IFinalizable finalizable);
        static partial void Discard(IFinalizable waste);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        partial interface IFinalizable
        {
            WeakNode Tracker { get; set; }
        }

        // Linked-list of weak references.
        // Using sentinel object for branchless algorithm.
        private static readonly WeakNode s_trackers = WeakNode.CreateSentinel();
        private static SpinLocker s_trackersLock;

        static partial void Discard(IFinalizable waste)
        {
            SuppressAndUntrackFinalizable(waste);
#if PROTO_PROMISE_DEVELOPER_MODE
            lock (s_pooledObjects)
            {
                s_inUseObjects.Remove(waste);
            }
#endif
        }

        static partial void TrackFinalizable(IFinalizable finalizable)
        {
            if (finalizable.Tracker != null)
            {
                throw new System.InvalidOperationException("Cannot track same object more than once. " + finalizable);
            }

            var newNode = WeakNode.GetOrCreate(finalizable);
            finalizable.Tracker = newNode;
            s_trackersLock.Enter();
            newNode.AddToList(s_trackers);
            s_trackersLock.Exit();
        }

        internal static void SuppressAndUntrackFinalizable(IFinalizable finalizable)
        {
            GC.SuppressFinalize(finalizable);
            var node = UntrackFinalizable(finalizable);
            finalizable.Tracker = null;
            node.Target = null;
            WeakNode.Repool(node);
        }

        internal static WeakNode UntrackFinalizable(IFinalizable finalizable)
        {
            // This is called from finalizers, so we don't touch the WeakReference, as it can cause a crash. (See comments in https://github.com/timcassell/ProtoPromise/pull/303)
            var node = finalizable.Tracker;
            s_trackersLock.Enter();
            node.RemoveFromList();
            s_trackersLock.Exit();
            return node;
        }

        internal static void SuppressAllFinalizables()
        {
            s_trackersLock.Enter();
            var first = s_trackers._next;
            var last = s_trackers._previous;
            s_trackers.PointToSelf();
            s_trackersLock.Exit();

            if (first == s_trackers)
            {
                return;
            }

            // Make the chain circular so we can pick out already-GC'd items.
            first._previous = last;
            last._next = first;

            var node = first;
            do
            {
                var thisNode = node;
                node = node._next;
                var target = thisNode.Target;
                if (target == null)
                {
                    thisNode.RemoveFromList();
                }
                else
                {
                    thisNode.Target = null;
                    GC.SuppressFinalize(target);
                }
            } while (node != first);

            WeakNode.Repool(first, last);

#if PROTO_PROMISE_DEVELOPER_MODE
            lock (s_pooledObjects)
            {
                s_inUseObjects.Clear();
            }
#endif
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class WeakNode : WeakReference
        {
            // Creating WeakReferences is expensive, so we pool them.
            private static readonly WeakNode s_pooledNodes = CreateSentinel();
            private static SpinLocker s_pooledNodesLock;

            internal static WeakNode CreateSentinel()
            {
                var node = new WeakNode(null);
                node.PointToSelf();
                return node;
            }

            internal WeakNode _previous;
            internal WeakNode _next;

            private WeakNode(object target) : base(target) { }

            internal static WeakNode GetOrCreate(IFinalizable target)
            {
                s_pooledNodesLock.Enter();
                var newNode = s_pooledNodes._next;
                if (newNode == s_pooledNodes)
                {
                    s_pooledNodesLock.Exit();
                    return new WeakNode(target);
                }

                newNode.RemoveFromList();
                s_pooledNodesLock.Exit();
                newNode.Target = target;
                return newNode;
            }

            internal static void Repool(WeakNode node)
            {
                s_pooledNodesLock.Enter();
                node.AddToList(s_pooledNodes);
                s_pooledNodesLock.Exit();
            }

            internal static void Repool(WeakNode first, WeakNode last)
            {
                s_pooledNodesLock.Enter();
                last._next = s_pooledNodes;
                first._previous = s_pooledNodes._previous;
                s_pooledNodes._previous._next = first;
                s_pooledNodes._previous = last;
                s_pooledNodesLock.Exit();
            }

            internal void PointToSelf()
            {
                _next = this;
                _previous = this;
            }

            internal void RemoveFromList()
            {
                _previous._next = _next;
                _next._previous = _previous;
            }

            internal void AddToList(WeakNode head)
            {
                _next = head;
                _previous = head._previous;
                _previous._next = this;
                head._previous = this;
            }
        }
#endif // PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
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
        {
            Internal.ValidateOperation(this, skipFrames + 1);
        }

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }

        static partial void ValidateArgument(Promise arg, string argName, int skipFrames)
        {
            if (!arg.IsValid)
            {
                throw new InvalidArgumentException(argName,
                    "Promise is invalid." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!",
                    Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }

        static partial void ValidateElement(Promise promise, string argName, int skipFrames)
        {
            if (!promise.IsValid)
            {
                throw new InvalidElementException(argName,
                    string.Format("A promise is invalid in {0}." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!", argName),
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
        {
            Internal.ValidateOperation(this, skipFrames + 1);
        }

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }

        static partial void ValidateArgument(Promise<T> arg, string argName, int skipFrames)
        {
            if (!arg.IsValid)
            {
                throw new InvalidArgumentException(argName,
                    "Promise is invalid." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!",
                    Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }

        static partial void ValidateElement(Promise<T> promise, string argName, int skipFrames)
        {
            if (!promise.IsValid)
            {
                throw new InvalidElementException(argName,
                    string.Format("A promise is invalid in {0}." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!", argName),
                    Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }
#endif
    }
}