#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable RECS0096 // Type parameter is never used
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter

using System;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
        // Calls to these get compiled away in RELEASE mode
        static partial void ValidateOperation(Promise promise, int skipFrames);
        static partial void ValidateProgress(float progress, int skipFrames);
        static partial void ValidateArgument(object arg, string argName, int skipFrames);
        partial void ValidateReturn(Promise other);
        static partial void ValidateReturn(Delegate other);
        static partial void ValidatePotentialOperation(object valueContainer, int skipFrames);
        static partial void ValidateElementNotNull(Promise promise, string argName, string message, int skipFrames);

        static partial void SetCreatedStacktrace(Internal.IStacktraceable stacktraceable, int skipFrames);
        partial void SetOwnerAndRejectStacktrace(Internal.IRejectionContainer unhandledException, bool generateStacktrace);
        partial void SetNotDisposed();
#if PROMISE_DEBUG
        private ushort _userRetainCounter;
        private string _createdStackTrace;
        string Internal.IStacktraceable.Stacktrace { get { return _createdStackTrace; } set { _createdStackTrace = value; } }

        private static int idCounter;
        protected readonly int _id;

        private static object DisposedObject
        {
            get
            {
                return Internal.DisposedChecker.instance;
            }
        }

        private static string GetFormattedStacktrace(Internal.IStacktraceable traceable)
        {
            return FormatStackTrace(traceable.Stacktrace);
        }

        partial void SetNotDisposed()
        {
            _valueOrPrevious = null;
        }

        partial class Internal
        {
            // This allows us to re-use the reference field without having to add another bool field.
            public sealed class DisposedChecker
            {
                public static readonly DisposedChecker instance = new DisposedChecker();

                private DisposedChecker() { }
            }
        }

        static partial void SetCreatedStacktrace(Internal.IStacktraceable stacktraceable, int skipFrames)
        {
            if (Config.DebugStacktraceGenerator == GeneratedStacktrace.All)
            {
                stacktraceable.Stacktrace = GetStackTrace(skipFrames + 1);
            }
        }

        partial void SetOwnerAndRejectStacktrace(Internal.IRejectionContainer unhandledException, bool generateStacktrace)
        {
            string stacktrace = generateStacktrace & Config.DebugStacktraceGenerator != GeneratedStacktrace.None
                ? new System.Diagnostics.StackTrace(1, true).ToString()
                : null;
            unhandledException.SetOwnerAndRejectedStacktrace(this, stacktrace);
        }

        private static string GetStackTrace(int skipFrames)
        {
            return new System.Diagnostics.StackTrace(skipFrames + 1, true).ToString();
        }

        private static string GetFormattedStacktrace(int skipFrames)
        {
            return FormatStackTrace(GetStackTrace(skipFrames + 1));
        }

        private static readonly System.Text.StringBuilder _stringBuilder = new System.Text.StringBuilder(128);

        private static string FormatStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
            {
                return stackTrace;
            }

            _stringBuilder.Length = 0;
            _stringBuilder.Append(stackTrace);

            // Format stacktrace to match "throw exception" so that double-clicking log in Unity console will go to the proper line.
            return _stringBuilder.Remove(0, 1)
                .Replace(":line ", ":")
                .Replace("\n ", " \n")
                .Replace("(", " (")
                .Replace(") in", ") [0x00000] in") // Not sure what "[0x00000]" is, but it's necessary for Unity's parsing.
                .Append(" ")
                .ToString();
        }

        partial void ValidateReturn(Promise other)
        {
            if (other == null)
            {
                // Returning a null from the callback is not allowed.
                throw new InvalidReturnException("A null promise was returned.");
            }

            // Validate returned promise as not disposed.
            if (IsDisposed(other._valueOrPrevious))
            {
                throw new InvalidReturnException("A disposed promise was returned.");
            }

            // A promise cannot wait on itself.

            // This allows us to check AllPromises and RacePromises iteratively.
            ValueLinkedStack<Internal.PromisePassThrough> passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>();
            var prev = other;
        Repeat:
            for (; prev != null; prev = prev._valueOrPrevious as Promise)
            {
                if (prev == this)
                {
                    throw new InvalidReturnException("Circular Promise chain detected.", other._createdStackTrace);
                }
                prev.BorrowPassthroughs(ref passThroughs);
            }

            if (passThroughs.IsNotEmpty)
            {
                // passThroughs are removed from their targets before adding to passThroughs. Add them back here.
                var passThrough = passThroughs.Pop();
                prev = passThrough.Owner;
                passThrough.target.ReAdd(passThrough);
                goto Repeat;
            }
        }

        static partial void ValidateReturn(Delegate other)
        {
            if (other == null)
            {
                // Returning a null from the callback is not allowed.
                throw new InvalidReturnException("A null delegate was returned.");
            }
        }

        protected static void ValidateProgressValue(float value, int skipFrames)
        {
            const string argName = "progress";
            if (value < 0f || value > 1f || float.IsNaN(value))
            {
                throw new ArgumentOutOfRangeException(argName, "Must be between 0 and 1.");
            }
        }

        private static bool IsDisposed(object valueContainer)
        {
            return ReferenceEquals(valueContainer, Internal.DisposedChecker.instance);
        }

        static protected void ValidateNotDisposed(object valueContainer, int skipFrames)
        {
            if (IsDisposed(valueContainer))
            {
                throw new PromiseDisposedException("Always nullify your references when you are finished with them!" +
                    " Call Retain() if you want to perform operations after the object has finished. Remember to call Release() when you are finished with it!"
                    , GetFormattedStacktrace(skipFrames + 1));
            }
        }

        static partial void ValidatePotentialOperation(object valueContainer, int skipFrames)
        {
            ValidateNotDisposed(valueContainer, skipFrames + 1);
        }

        static partial void ValidateOperation(Promise promise, int skipFrames)
        {
            ValidateNotDisposed(promise._valueOrPrevious, skipFrames + 1);
        }

        static partial void ValidateProgress(float progress, int skipFrames)
        {
            ValidateProgressValue(progress, skipFrames + 1);
        }

        static protected void ValidateArg(object del, string argName, int skipFrames)
        {
            if (del == null)
            {
                throw new ArgumentNullException(argName, null, GetFormattedStacktrace(skipFrames + 1));
            }
        }

        static partial void ValidateArgument(object arg, string argName, int skipFrames)
        {
            ValidateArg(arg, argName, skipFrames + 1);
        }

        static partial void ValidateElementNotNull(Promise promise, string argName, string message, int skipFrames)
        {
            if (promise == null)
            {
                throw new ElementNullException(argName, message, GetFormattedStacktrace(skipFrames + 1));
            }
        }

        public override string ToString()
        {
            return string.Format("Type: Promise, Id: {0}, State: {1}", _id, _state);
        }
#else
        private static string GetFormattedStacktrace(int skipFrames)
        {
            return null;
        }

        private static string GetFormattedStacktrace(Internal.IStacktraceable traceable)
        {
            return null;
        }

        private static object DisposedObject
        {
            get
            {
                return null;
            }
        }

        public override string ToString()
        {
            return string.Format("Type: Promise, State: {0}", _state);
        }
#endif

        partial class Internal
        {
            public interface IStacktraceable
            {
#if PROMISE_DEBUG
                string Stacktrace { get; set; }
#endif
            }

            partial class FinallyDelegate : IStacktraceable
            {
#if PROMISE_DEBUG
                string IStacktraceable.Stacktrace { get; set; }
#endif
            }

            partial class FinallyDelegateCapture<TCapture> : IStacktraceable
            {
#if PROMISE_DEBUG
                string IStacktraceable.Stacktrace { get; set; }
#endif
            }

            partial class PotentialCancelation : IStacktraceable
            {
#if PROMISE_DEBUG
                string IStacktraceable.Stacktrace { get; set; }
#endif
            }
        }
    }

    partial class Promise<T>
    {
        // Calls to these get compiled away in RELEASE mode
        static partial void ValidateOperation(Promise<T> promise, int skipFrames);
        static partial void ValidateArgument(object arg, string argName, int skipFrames);
        static partial void ValidateProgress(float progress, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateProgress(float progress, int skipFrames)
        {
            ValidateProgressValue(progress, skipFrames + 1);
        }

        static partial void ValidateOperation(Promise<T> promise, int skipFrames)
        {
            ValidateNotDisposed(promise._valueOrPrevious, skipFrames + 1);
        }

        static partial void ValidateArgument(object arg, string argName, int skipFrames)
        {
            ValidateArg(arg, argName, skipFrames + 1);
        }

        public override string ToString()
        {
            return string.Format("Type: Promise<{0}>, Id: {1}, State: {2}", typeof(T), _id, _state);
        }
#else
        public override string ToString()
        {
            return string.Format("Type: Promise<{0}>, State: {1}", typeof(T), _state);
        }
#endif
    }
}