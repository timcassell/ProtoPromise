#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using Proto.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        internal const MethodImplOptions InlineOption = MethodImplOptions.NoInlining;
#else
        internal const MethodImplOptions InlineOption = (MethodImplOptions) 256; // AggressiveInlining
#endif

#if !PROMISE_PROGRESS
        internal static void ThrowProgressException(int skipFrames)
        {
            throw new InvalidOperationException("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", GetFormattedStacktrace(skipFrames + 1));
        }
#endif

        // Calls to these get compiled away in RELEASE mode
        partial class PromiseRef
        {
            static partial void ValidateArgument(object arg, string argName, int skipFrames);
            partial void ValidateReturn(Promise other);
        }

#if PROMISE_DEBUG
        partial interface ITraceable
        {
            CausalityTrace Trace { get; set; }
        }

        partial interface IRejectValueContainer
        {
            void SetCreatedAndRejectedStacktrace(System.Diagnostics.StackTrace rejectedStacktrace, CausalityTrace createdStacktraces);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal class CausalityTrace
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
                List<StackTrace> stackTraces = new List<StackTrace>();
                for (CausalityTrace current = this; current != null; current = current._next)
                {
                    if (current._stackTrace == null)
                    {
                        break;
                    }
                    stackTraces.Add(current._stackTrace);
                }
                return FormatStackTrace(stackTraces);
            }
        }

        internal static void ValidateProgressValue(float value, int skipFrames)
        {
            const string argName = "progress";
            bool isBetween01 = value >= 0f && value <= 1f;
            if (!isBetween01)
            {
                throw new ArgumentOutOfRangeException(argName, "Must be between 0 and 1.", GetFormattedStacktrace(skipFrames + 1));
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

        partial class PromiseRef
        {
            static partial void ValidateArgument(object arg, string argName, int skipFrames)
            {
                Internal.ValidateArgument(arg, argName, skipFrames + 1);
            }

            partial void ValidateReturn(Promise other)
            {
                if (!other.IsValid)
                {
                    // Returning an invalid from the callback is not allowed.
                    throw new InvalidReturnException("An invalid promise was returned.", string.Empty);
                }

                // A promise cannot wait on itself.
                if (other._ref == this)
                {
                    throw new InvalidReturnException("A Promise cannot wait on itself.", string.Empty);
                }
                if (other._ref == null)
                {
                    return;
                }
                // This allows us to check All/Race/First Promises iteratively.
                Stack<PromisePassThrough> passThroughs = PassthroughsForIterativeAlgorithm;
                PromiseRef prev = other._ref._valueOrPrevious as PromiseRef;
            Repeat:
                for (; prev != null; prev = prev._valueOrPrevious as PromiseRef)
                {
                    if (prev == this)
                    {
                        other._ref.MarkAwaitedAndMaybeDispose(other._id, true);
                        while (passThroughs.Count > 0)
                        {
                            passThroughs.Pop().Release();
                        }
                        throw new InvalidReturnException("Circular Promise chain detected.", GetFormattedStacktrace(other._ref));
                    }
                    prev.BorrowPassthroughs(passThroughs);
                }

                if (passThroughs.Count > 0)
                {
                    var passThrough = passThroughs.Pop();
                    prev = passThrough.Owner;
                    passThrough.Release();
                    goto Repeat;
                }
            }

            [ThreadStatic]
            private static Stack<PromisePassThrough> _passthroughsForIterativeAlgorithm;
            private static Stack<PromisePassThrough> PassthroughsForIterativeAlgorithm
            {
                get
                {
                    if (_passthroughsForIterativeAlgorithm == null)
                    {
                        _passthroughsForIterativeAlgorithm = new Stack<PromisePassThrough>();
                    }
                    return _passthroughsForIterativeAlgorithm;
                }
            }

            protected virtual void BorrowPassthroughs(Stack<PromisePassThrough> borrower) { }

            private static void ExchangePassthroughs(ref ValueLinkedStack<PromisePassThrough> from, Stack<PromisePassThrough> to, object locker)
            {
                lock (locker)
                {
                    foreach (var passthrough in from)
                    {
                        passthrough.Retain();
                        to.Push(passthrough);
                    }
                }
            }

            partial class MergePromise
            {
                protected override void BorrowPassthroughs(Stack<PromisePassThrough> borrower)
                {
                    ExchangePassthroughs(ref _passThroughs, borrower, _locker);
                }
            }

            partial class RacePromise
            {
                protected override void BorrowPassthroughs(Stack<PromisePassThrough> borrower)
                {
                    ExchangePassthroughs(ref _passThroughs, borrower, _locker);
                }
            }

            partial class FirstPromise
            {
                protected override void BorrowPassthroughs(Stack<PromisePassThrough> borrower)
                {
                    ExchangePassthroughs(ref _passThroughs, borrower, _locker);
                }
            }
        }
#endif
    }

    partial struct Promise
    {
        // Calls to these get compiled away in RELEASE mode
        partial void ValidateOperation(int skipFrames);
        static partial void ValidateProgress(float progress, int skipFrames);
        static partial void ValidateArgument(object arg, string argName, int skipFrames);
        static partial void ValidateArgument(Promise arg, string argName, int skipFrames);
        static partial void ValidateElement(Promise promise, string argName, int skipFrames);

#if PROMISE_DEBUG
        partial void ValidateOperation(int skipFrames)
        {
            Internal.ValidateOperation(this, skipFrames + 1);
        }

        static partial void ValidateProgress(float progress, int skipFrames)
        {
            Internal.ValidateProgressValue(progress, skipFrames + 1);
        }

        static partial void ValidateArgument(object arg, string argName, int skipFrames)
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
        static partial void ValidateProgress(float progress, int skipFrames);
        static partial void ValidateArgument(object arg, string argName, int skipFrames);
        static partial void ValidateArgument(Promise<T> arg, string argName, int skipFrames);
        static partial void ValidateElement(Promise<T> promise, string argName, int skipFrames);
#if PROMISE_DEBUG
        partial void ValidateOperation(int skipFrames)
        {
            Internal.ValidateOperation(this, skipFrames + 1);
        }

        static partial void ValidateProgress(float progress, int skipFrames)
        {
            Internal.ValidateProgressValue(progress, skipFrames + 1);
        }

        static partial void ValidateArgument(object arg, string argName, int skipFrames)
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