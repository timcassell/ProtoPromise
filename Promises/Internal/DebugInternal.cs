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

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0041 // Use 'is null' check

using System;
using Proto.Utils;

#if PROMISE_DEBUG
using System.Diagnostics;
#endif

namespace Proto.Promises
{
    partial class Promise
    {
        // Calls to these get compiled away in RELEASE mode
        static partial void ValidateOperation(Promise promise, int skipFrames);
        static partial void ValidateProgress(float progress, int skipFrames);
        static partial void ValidateArgument(object arg, string argName, int skipFrames);
        partial void ValidateReturn(Promise other);
        static partial void ValidateYieldInstructionOperation(object valueContainer, int skipFrames);
        static partial void ValidateElementNotNull(Promise promise, string argName, string message, int skipFrames);

        static partial void SetCreatedStacktrace(Internal.ITraceable traceable, int skipFrames);
        partial void SetNotDisposed();
        static partial void SetCurrentInvoker(Internal.ITraceable current);
        static partial void ClearCurrentInvoker();
#if PROMISE_DEBUG
        // TODO: Check thread at all public access.
        //private static readonly System.Threading.Thread initialThread = System.Threading.Thread.CurrentThread;

        private static readonly object disposedObject = DisposedChecker.instance;

        private static int idCounter;
        protected readonly int _id;

        private ushort _userRetainCounter;
        Internal.CausalityTrace Internal.ITraceable.Trace { get; set; }

        static partial void SetCurrentInvoker(Internal.ITraceable current)
        {
            Internal.SetCurrentInvoker(current);
        }

        static partial void ClearCurrentInvoker()
        {
            Internal.ClearCurrentInvoker();
        }

        partial void SetNotDisposed()
        {
            _valueOrPrevious = null;
        }

        static partial void SetCreatedStacktrace(Internal.ITraceable traceable, int skipFrames)
        {
            Internal.SetCreatedStacktrace(traceable, skipFrames + 1);
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
                throw new InvalidReturnException("A disposed promise was returned.", Internal.GetFormattedStacktrace(other));
            }

            // A promise cannot wait on itself.

            // This allows us to check All/Race/First Promises iteratively.
            ValueLinkedStack<InternalProtected.PromisePassThrough> passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>();
            var prev = other;
        Repeat:
            for (; prev != null; prev = prev._valueOrPrevious as Promise)
            {
                if (prev == this)
                {
                    throw new InvalidReturnException("Circular Promise chain detected.", Internal.GetFormattedStacktrace(other));
                }
                prev.BorrowPassthroughs(ref passThroughs);
            }

            if (passThroughs.IsNotEmpty)
            {
                // passThroughs are removed from their targets before adding to passThroughs. Add them back here.
                var passThrough = passThroughs.Pop();
                prev = passThrough.Owner;
                passThrough.Target.ReAdd(passThrough);
                goto Repeat;
            }
        }

        protected static void ValidateProgressValue(float value, int skipFrames)
        {
            const string argName = "progress";
            if (value < 0f || value > 1f || float.IsNaN(value))
            {
                throw new ArgumentOutOfRangeException(argName, "Must be between 0 and 1.", Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }

        private static bool IsDisposed(object valueContainer)
        {
            return ReferenceEquals(valueContainer, DisposedChecker.instance);
        }

        static protected void ValidateNotDisposed(object valueContainer, int skipFrames)
        {
            if (IsDisposed(valueContainer))
            {
                throw new PromiseDisposedException("Always nullify your references when you are finished with them!" +
                    " Call Retain() if you want to perform operations after the object has finished. Remember to call Release() when you are finished with it!"
                    , Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }

        static partial void ValidateYieldInstructionOperation(object valueContainer, int skipFrames)
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

        static partial void ValidateArgument(object arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }

        static partial void ValidateElementNotNull(Promise promise, string argName, string message, int skipFrames)
        {
            if (promise == null)
            {
                throw new ElementNullException(argName, message, Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }

        public override string ToString()
        {
            return string.Format("Type: Promise, Id: {0}, State: {1}", _id, _state);
        }

        // This allows us to re-use a reference field without having to add another bool field.
        [DebuggerNonUserCode]
        private sealed class DisposedChecker
        {
            public static readonly DisposedChecker instance = new DisposedChecker();

            private DisposedChecker() { }
        }
#else
        private const object disposedObject = null;

        public override string ToString()
        {
            return string.Format("Type: Promise, State: {0}", _state);
        }
#endif
    }

    partial class Promise<T>
    {
        // Calls to these get compiled away in RELEASE mode
        static partial void ValidateYieldInstructionOperation(object valueContainer, int skipFrames);
        static partial void ValidateOperation(Promise<T> promise, int skipFrames);
        static partial void ValidateArgument(object arg, string argName, int skipFrames);
        static partial void ValidateProgress(float progress, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateProgress(float progress, int skipFrames)
        {
            ValidateProgressValue(progress, skipFrames + 1);
        }

        static partial void ValidateYieldInstructionOperation(object valueContainer, int skipFrames)
        {
            ValidateNotDisposed(valueContainer, skipFrames + 1);
        }

        static partial void ValidateOperation(Promise<T> promise, int skipFrames)
        {
            ValidateNotDisposed(promise._valueOrPrevious, skipFrames + 1);
        }

        static partial void ValidateArgument(object arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
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