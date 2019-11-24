// undef CANCEL to disable, or define CANCEL to enable cancelations on promises.
// If CANCEL is defined, it breaks the Promises/A+ spec "2.1. Promise States", but allows breaking promise chains. Execution is also a little slower.
#define CANCEL
// undef PROGRESS to disable, or define PROGRESS to enable progress reports on promises.
// If PROGRESS is defined, promises use more memory. If PROGRESS is undefined, there is no limit to the depth of a promise chain.
#define PROGRESS
// define DEBUG to enable debugging options in RELEASE mode. undef DEBUG to disable debugging options in DEBUG mode.
//#define DEBUG
//#undef DEBUG

#pragma warning disable RECS0096 // Type parameter is never used
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter
using System;
using System.Collections.Generic;

namespace Proto.Promises
{
    partial class Promise
    {
        public enum State : byte
        {
            Pending,
            Resolved,
            Rejected,
#if !CANCEL
            [Obsolete("Define CANCEL in ProtoPromise/Config.cs to enable cancelations.", false)]
#endif
            Canceled // This violates Promises/A+ 2.1 when CANCEL is enabled.
        }

        public enum GeneratedStacktrace : byte
        {
            /// <summary>
            /// Don't generate any extra stack traces.
            /// </summary>
            None,
            /// <summary>
            /// Generate stack traces when Deferred.Reject is called.
            /// If Reject is called with an exception, the generated stack trace is appended to the exception's stacktrace.
            /// </summary>
            Rejections,
            /// <summary>
            /// Generate stack traces when Deferred.Reject is called.
            /// Also generate stack traces every time a promise is created (i.e. with .Then). This can help debug where an invalid object was returned from a .Then delegate.
            /// If a .Then/.Catch callback throws an exception, the generated stack trace is appended to the exception's stacktrace.
            /// <para/>
            /// NOTE: This can be extremely expensive, so you should only enable this if you ran into an error and you are not sure where it came from.
            /// </summary>
            All
        }

        public enum PoolType : byte
        {
            /// <summary>
            /// Don't pool any objects.
            /// </summary>
            None,
            /// <summary>
            /// Only pool internal objects.
            /// </summary>
            Internal,
            /// <summary>
            /// Pool all objects, internal and public.
            /// </summary>
            All
        }

        /// <summary>
        /// Promise configuration. Configuration settings affect the global behaviour of promises.
        /// </summary>
        public static class Config
        {
#if PROGRESS
            /// <summary>
            /// If you need to support longer promise chains, decrease decimalBits. If you need higher precision, increase decimalBits.
            /// <para/>
            /// Promise chain limit: 2^(32-<see cref="ProgressDecimalBits"/>),
            /// Precision: 1/(N*2^<see cref="ProgressDecimalBits"/>) where N is the number of wait promises in the chain where Progress is subscribed.
            /// <para/>
            /// NOTE: promises that don't wait (.Then with an onResolved that simply returns a value or void) don't count towards the promise chain limit.
            /// </summary>
            public const int ProgressDecimalBits = 13;
#endif

            private static PoolType _objectPooling = PoolType.Internal;
            /// <summary>
            /// Highly recommend to leave this None or Internal in DEBUG mode, so that exceptions will propagate if/when promises are used incorrectly after they have already completed.
            /// </summary>
            public static PoolType ObjectPooling { get { return _objectPooling; } set { _objectPooling = value; } }

#if DEBUG
            private static GeneratedStacktrace _debugStacktraceGenerator = GeneratedStacktrace.Rejections;
            public static GeneratedStacktrace DebugStacktraceGenerator { get { return _debugStacktraceGenerator; } set { _debugStacktraceGenerator = value; } }
#else
            public static GeneratedStacktrace DebugStacktraceGenerator { get { return default(GeneratedStacktrace); } set { } }
#endif

            private static IValueConverter _valueConverter = new DefaultValueConverter();
            /// <summary>
            /// Value converter used to determine if a reject or cancel reason is convertible for a catch delegate expecting a reason.
            /// <para/>The default implementation uses <see cref="Type.IsAssignableFrom(Type)"/>.
            /// </summary>
            public static IValueConverter ValueConverter { get { return _valueConverter; } set { _valueConverter = value; } }

            private static IPromiseYielder _yielder;
            /// <summary>
            /// Yielder used to wait for a yield instruction to complete. This is used for <see cref="Yield"/> and <see cref="Yield{TYieldInstruction}(TYieldInstruction)"/>.
            /// <para/>The default implementation uses Unity's coroutines.
            /// </summary>
            public static IPromiseYielder Yielder
            {
                get
                {
                    if (_yielder == null)
                    {
                        // Lazily create a new yielder if necessary. This allows users to assign their own yielder without the default ever being created.
                        _yielder = PromiseBehaviour.Instance.gameObject.AddComponent<DefaultPromiseYielder>();
                    }
                    return _yielder;
                }
                set
                {
                    _yielder = value;
                }
            }

            private sealed class DefaultValueConverter : IValueConverter
            {
                bool IValueConverter.TryConvert<TOriginal, TConvert>(IValueContainer<TOriginal> valueContainer, out TConvert converted)
                {
                    // This avoids boxing value types.
#if CSHARP_7_OR_LATER
                    if (valueContainer is IValueContainer<TConvert> casted)
#else
                    var casted = valueContainer as IValueContainer<TConvert>;
                    if (casted != null)
#endif
                    {
                        converted = casted.Value;
                        return true;
                    }
                    // Can it be up-casted or down-casted, null or not?
                    TOriginal value = valueContainer.Value;
                    if (typeof(TConvert).IsAssignableFrom(typeof(TOriginal)) || value is TConvert)
                    {
                        converted = (TConvert) (object) value;
                        return true;
                    }
                    converted = default(TConvert);
                    return false;
                }

                bool IValueConverter.CanConvert<TOriginal, TConvert>(IValueContainer<TOriginal> valueContainer)
                {
                    // Can it be up-casted or down-casted, null or not?
                    return typeof(TConvert).IsAssignableFrom(typeof(TOriginal)) || valueContainer.Value is TConvert;
                }
            }
        }

        partial class DeferredBase
        {
            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
#if !PROGRESS
            [Obsolete("Define PROGRESS in ProtoPromise/Config.cs to enable progress reports.", true)]
#endif
            public abstract void ReportProgress(float progress);

            /// <summary>
            /// Cancels the promise and all promises that have been chained from it without a reason.
            /// </summary>
#if !CANCEL
            [Obsolete("Define CANCEL in ProtoPromise/Config.cs to enable cancelations.", true)]
#endif
            public void Cancel()
            {
                ValidateCancel();
                var promise = Promise;
                ValidateOperation(promise, 1);

                if (State == State.Pending)
                {
                    State = State.Canceled;
                    promise.Cancel();
                    promise.Release();
                }
                else
                {
                    Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
                }
            }

            /// <summary>
            /// Cancels the promise and all promises that have been chained from it with the provided cancel reason.
            /// </summary>
#if !CANCEL
            [Obsolete("Define CANCEL in ProtoPromise/Config.cs to enable cancelations.", true)]
#endif
            public void Cancel<TCancel>(TCancel reason)
            {
                ValidateCancel();
                var promise = Promise;
                ValidateOperation(promise, 1);

                if (State == State.Pending)
                {
                    State = State.Canceled;
                    promise.Cancel(reason);
                    promise.Release();
                }
                else
                {
                    Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
                }
            }
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/> that will be canceled without a reason.
        /// </summary>
#if !CANCEL
        [Obsolete("Define CANCEL in ProtoPromise/Config.cs to enable cancelations.", true)]
#endif
        public static Promise Canceled()
        {
            ValidateCancel();

            var promise = Internal.LitePromise0.GetOrCreate(1);
            promise.Cancel();
            return promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/> that will be canceled with <paramref name="reason"/>.
        /// </summary>
#if !CANCEL
        [Obsolete("Define CANCEL in ProtoPromise/Config.cs to enable cancelations.", true)]
#endif
        public static Promise Canceled<TCancel>(TCancel reason)
        {
            ValidateCancel();

            var promise = Internal.LitePromise0.GetOrCreate(1);
            promise.Cancel(reason);
            return promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/> that will be canceled without a reason.
        /// </summary>
#if !CANCEL
        [Obsolete("Define CANCEL in ProtoPromise/Config.cs to enable cancelations.", true)]
#endif
        public static Promise<T> Canceled<T>()
        {
            ValidateCancel();

            var promise = Internal.LitePromise<T>.GetOrCreate(1);
            promise.Cancel();
            return promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/> that will be canceled with <paramref name="reason"/>.
        /// </summary>
#if !CANCEL
        [Obsolete("Define CANCEL in ProtoPromise/Config.cs to enable cancelations.", true)]
#endif
        public static Promise<T> Canceled<T, TCancel>(TCancel reason)
        {
            ValidateCancel();

            var promise = Internal.LitePromise<T>.GetOrCreate(1);
            promise.Cancel(reason);
            return promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/> that adopts the state of this. This is mostly useful for branches that you expect might be canceled, and you don't want all branches to be canceled.
        /// </summary>
#if !CANCEL
        [Obsolete("This is mostly useful for cancelations of branched promises. Define CANCEL in ProtoPromise/Config.cs to enable cancelations.", false)]
#endif
        public Promise ThenDuplicate()
        {
            ValidateOperation(this, 1);

            var promise = GetDuplicate();
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a progress listener. <paramref name="onProgress"/> will be invoked with progress that is normalized between 0 and 1 from this and all previous waiting promises in the chain.
        /// Returns this.
        /// </summary>
#if !PROGRESS
        [Obsolete("Define PROGRESS in ProtoPromise/Config.cs to enable progress reports.", true)]
#endif
        public Promise Progress(Action<float> onProgress)
        {
            ValidateProgress();
#if PROGRESS
            ProgressInternal(onProgress, 1);
#endif
            return this;
        }

        /// <summary>
        /// Add a cancel callback.
        /// <para/>If this instance is canceled for any or no reason, <paramref name="onCanceled"/> will be invoked.
        /// </summary>
#if !CANCEL
        [Obsolete("Define CANCEL in ProtoPromise/Config.cs to enable cancelations.", true)]
#endif
        public void CatchCancelation(Action onCanceled)
        {
            ValidateCancel();
            ValidateOperation(this, 1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            if (_state == State.Pending | _state == State.Canceled)
            {
                AddWaiter(Internal.CancelDelegateAny.GetOrCreate(onCanceled, 1));
                ReleaseWithoutDisposeCheck(); // No need to keep this retained.
            }
        }

        /// <summary>
        /// Add a cancel callback. Returns an <see cref="IPotentialCancelation"/> object.
        /// <para/>If this is canceled with any reason that is convertible to <typeparamref name="TCancel"/>, <paramref name="onCanceled"/> will be invoked with that reason.
        /// <para/>If this is canceled for any other reason or no reason, the returned <see cref="IPotentialCancelation"/> will be canceled with the same reason.
        /// </summary>
#if !CANCEL
        [Obsolete("Define CANCEL in ProtoPromise/Config.cs to enable cancelations.", true)]
#endif
        public IPotentialCancelation CatchCancelation<TCancel>(Action<TCancel> onCanceled)
        {
            ValidateCancel();
            ValidateOperation(this, 1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            if (_state == State.Pending | _state == State.Canceled)
            {
                var cancelation = Internal.CancelDelegate<TCancel>.GetOrCreate(onCanceled, this, 1);
                AddWaiter(cancelation);
                return cancelation;
            }
            return this;
        }

        /// <summary>
        /// Cancels this promise and all promises that have been chained from this without a reason.
        /// Does nothing if this promise isn't pending.
        /// </summary>
#if !CANCEL
        [Obsolete("Define CANCEL in ProtoPromise/Config.cs to enable cancelations.", true)]
#endif
        public void Cancel()
        {
            ValidateCancel();
            ValidateOperation(this, 1);

            if (_state != State.Pending)
            {
                return;
            }

            CancelInternal(Internal.CancelVoid.GetOrCreate());
        }

        /// <summary>
        /// Cancels this promise and all promises that have been chained from this with the provided cancel reason.
        /// Does nothing if this promise isn't pending.
        /// </summary>
#if !CANCEL
        [Obsolete("Define CANCEL in ProtoPromise/Config.cs to enable cancelations.", true)]
#endif
        public void Cancel<TCancel>(TCancel reason)
        {
            ValidateCancel();
            ValidateOperation(this, 1);

            if (_state != State.Pending)
            {
                return;
            }

            CancelInternal(Internal.CancelValue<TCancel>.GetOrCreate(reason));
        }



#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
        private
#endif
        protected Promise()
        {
#if DEBUG
            _id = idCounter++;
#endif
        }

        protected virtual void SetPreviousTo(Promise other)
        {
            _rejectedOrCanceledValueOrPrevious = other;
        }

        protected static void _SetStackTraceFromCreated(Internal.IStacktraceable stacktraceable, Internal.UnhandledExceptionInternal unhandledException)
        {
            SetStacktraceFromCreated(stacktraceable, unhandledException);
        }

        // Calls to these get compiled away in RELEASE mode
        static partial void ValidateOperation(Promise promise, int skipFrames);
        static partial void ValidateProgress(float progress, int skipFrames);
        static partial void ValidateArgument(Delegate del, string argName, int skipFrames);
        partial void ValidateReturn(Promise other);
        static partial void ValidateReturn(Delegate other);
        static partial void ValidatePotentialOperation(Internal.IValueContainerOrPrevious valueContainer, int skipFrames);
        static partial void ValidateElementNotNull(Promise promise, string argName, string message, int skipFrames);

        static partial void SetCreatedStacktrace(Internal.IStacktraceable stacktraceable, int skipFrames);
        static partial void SetStacktraceFromCreated(Internal.IStacktraceable stacktraceable, Internal.UnhandledExceptionInternal unhandledException);
        static partial void SetRejectStacktrace(Internal.UnhandledExceptionInternal unhandledException, int skipFrames);
        static partial void SetNotDisposed(ref Internal.IValueContainerOrPrevious valueContainer);
#if DEBUG
        private string _createdStackTrace;
        string Internal.IStacktraceable.Stacktrace { get { return _createdStackTrace; } set { _createdStackTrace = value; } }

        private static int idCounter;
        protected readonly int _id;

        private static void SetDisposed(ref Internal.IValueContainerOrPrevious valueContainer)
        {
            valueContainer = Internal.DisposedChecker.instance;
        }

        static partial void SetNotDisposed(ref Internal.IValueContainerOrPrevious valueContainer)
        {
            valueContainer = null;
        }

        partial class Internal
        {
            // This allows us to re-use the reference field without having to add another bool field.
            public sealed class DisposedChecker : IValueContainerOrPrevious
            {
                public static readonly DisposedChecker instance = new DisposedChecker();

                private DisposedChecker() { }

                bool IValueContainerOrPrevious.ContainsType<U>() { throw new InvalidOperationException(); }
                bool IValueContainerOrPrevious.TryGetValueAs<U>(out U value) { throw new InvalidOperationException(); }
                void IRetainable.Release() { throw new InvalidOperationException(); }
                void IRetainable.Retain() { throw new InvalidOperationException(); }
            }
        }

        static partial void SetCreatedStacktrace(Internal.IStacktraceable stacktraceable, int skipFrames)
        {
            if (Config.DebugStacktraceGenerator == GeneratedStacktrace.All)
            {
                stacktraceable.Stacktrace = GetStackTrace(skipFrames + 1);
            }
        }

        static partial void SetStacktraceFromCreated(Internal.IStacktraceable stacktraceable, Internal.UnhandledExceptionInternal unhandledException)
        {
            unhandledException.SetStackTrace(FormatStackTrace(stacktraceable.Stacktrace));
        }

        static partial void SetRejectStacktrace(Internal.UnhandledExceptionInternal unhandledException, int skipFrames)
        {
            if (Config.DebugStacktraceGenerator != GeneratedStacktrace.None)
            {
                unhandledException.SetStackTrace(GetFormattedStacktrace(skipFrames + 1));
            }
        }

        private static string GetStackTrace(int skipFrames)
        {
            return new System.Diagnostics.StackTrace(skipFrames + 1, true).ToString();
        }

        private static string GetFormattedStacktrace(int skipFrames)
        {
            return FormatStackTrace(GetStackTrace(skipFrames + 1));
        }

        private static System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(128);

        private static string FormatStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
            {
                return stackTrace;
            }

            stringBuilder.Length = 0;
            stringBuilder.Append(stackTrace);

            // Format stacktrace to match "throw exception" so that double-clicking log in Unity console will go to the proper line.
            return stringBuilder.Remove(0, 1)
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
            if (IsDisposed(other._rejectedOrCanceledValueOrPrevious))
            {
                throw new InvalidReturnException("A disposed promise was returned.");
            }

            // A promise cannot wait on itself.

            // This allows us to check AllPromises and RacePromises iteratively.
            ValueLinkedStack<Internal.PromisePassThrough> passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>();
            var prev = other;
        Repeat:
            for (; prev != null; prev = prev._rejectedOrCanceledValueOrPrevious as Promise)
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
                passThrough.target.ReAdd(passThrough);
                prev = passThrough.owner;
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

        private static bool IsDisposed(Internal.IValueContainerOrPrevious valueContainer)
        {
            return ReferenceEquals(valueContainer, Internal.DisposedChecker.instance);
        }

        static protected void ValidateNotDisposed(Internal.IValueContainerOrPrevious valueContainer, int skipFrames)
        {
            if (IsDisposed(valueContainer))
            {
                throw new PromiseDisposedException("Always nullify your references when you are finished with them!" +
                	" Call Retain() if you want to perform operations after the object has finished. Remember to call Release() when you are finished with it!"
                    , GetFormattedStacktrace(skipFrames + 1));
            }
        }

        static partial void ValidatePotentialOperation(Internal.IValueContainerOrPrevious valueContainer, int skipFrames)
        {
            ValidateNotDisposed(valueContainer, skipFrames + 1);
        }

        static partial void ValidateOperation(Promise promise, int skipFrames)
        {
            ValidateNotDisposed(promise._rejectedOrCanceledValueOrPrevious, skipFrames + 1);
        }

        static partial void ValidateProgress(float progress, int skipFrames)
        {
            ValidateProgressValue(progress, skipFrames + 1);
        }

        static protected void ValidateArg(Delegate del, string argName, int skipFrames)
        {
            if (del == null)
            {
                throw new ArgumentNullException(argName, null, GetFormattedStacktrace(skipFrames + 1));
            }
        }

        static partial void ValidateArgument(Delegate del, string argName, int skipFrames)
        {
            ValidateArg(del, argName, skipFrames + 1);
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

        private static void SetDisposed(ref Internal.IValueContainerOrPreviousPromise valueContainer)
        {
            // Allow GC to clean up the object if necessary.
            valueContainer = null;
        }

        public override string ToString()
        {
            return string.Format("Type: Promise, State: {0}", _state);
        }
#endif

#if DEBUG || PROGRESS
        protected virtual void BorrowPassthroughs(ref ValueLinkedStack<Internal.PromisePassThrough> passThroughs) { }

        partial class Internal
        {
            partial class AllPromise0
            {
                protected override void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    // Remove this.passThroughs before adding to passThroughs. They are re-added by the caller.
                    var tempPassThroughs = this.passThroughs;
                    this.passThroughs.Clear();
                    while (tempPassThroughs.IsNotEmpty)
                    {
                        var passThrough = tempPassThroughs.Pop();
                        if (passThrough.owner == null)
                        {
                            // The owner already completed.
                            this.passThroughs.Push(passThrough);
                        }
                        else
                        {
                            passThroughs.Push(passThrough);
                        }
                    }
                }
            }

            partial class AllPromise<T>
            {
                protected override void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    // Remove this.passThroughs before adding to passThroughs. They are re-added by the caller.
                    var tempPassThroughs = this.passThroughs;
                    this.passThroughs.Clear();
                    while (tempPassThroughs.IsNotEmpty)
                    {
                        var passThrough = tempPassThroughs.Pop();
                        if (passThrough.owner == null)
                        {
                            // The owner already completed.
                            this.passThroughs.Push(passThrough);
                        }
                        else
                        {
                            passThroughs.Push(passThrough);
                        }
                    }
                }
            }

            partial class RacePromise0
            {
                protected override void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    // Remove this.passThroughs before adding to passThroughs. They are re-added by the caller.
                    var tempPassThroughs = this.passThroughs;
                    this.passThroughs.Clear();
                    while (tempPassThroughs.IsNotEmpty)
                    {
                        var passThrough = tempPassThroughs.Pop();
                        if (passThrough.owner == null)
                        {
                            // The owner already completed.
                            this.passThroughs.Push(passThrough);
                        }
                        else
                        {
                            passThroughs.Push(passThrough);
                        }
                    }
                }
            }

            partial class RacePromise<T>
            {
                protected override void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    // Remove this.passThroughs before adding to passThroughs. They are re-added by the caller.
                    var tempPassThroughs = this.passThroughs;
                    this.passThroughs.Clear();
                    while (tempPassThroughs.IsNotEmpty)
                    {
                        var passThrough = tempPassThroughs.Pop();
                        if (passThrough.owner == null)
                        {
                            // The owner already completed.
                            this.passThroughs.Push(passThrough);
                        }
                        else
                        {
                            passThroughs.Push(passThrough);
                        }
                    }
                }
            }

            partial class FirstPromise0
            {
                protected override void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    // Remove this.passThroughs before adding to passThroughs. They are re-added by the caller.
                    var tempPassThroughs = this.passThroughs;
                    this.passThroughs.Clear();
                    while (tempPassThroughs.IsNotEmpty)
                    {
                        var passThrough = tempPassThroughs.Pop();
                        if (passThrough.owner == null)
                        {
                            // The owner already completed.
                            this.passThroughs.Push(passThrough);
                        }
                        else
                        {
                            passThroughs.Push(passThrough);
                        }
                    }
                }
            }

            partial class FirstPromise<T>
            {
                protected override void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    // Remove this.passThroughs before adding to passThroughs. They are re-added by the caller.
                    var tempPassThroughs = this.passThroughs;
                    this.passThroughs.Clear();
                    while (tempPassThroughs.IsNotEmpty)
                    {
                        var passThrough = tempPassThroughs.Pop();
                        if (passThrough.owner == null)
                        {
                            // The owner already completed.
                            this.passThroughs.Push(passThrough);
                        }
                        else
                        {
                            passThroughs.Push(passThrough);
                        }
                    }
                }
            }
        }
#endif

        // Calls to this get compiled away when CANCEL is defined.
        static partial void ValidateCancel();

        static partial void AddToCancelQueueBack(Internal.ITreeHandleable cancelation);
        static partial void AddToCancelQueueFront(ref ValueLinkedQueue<Internal.ITreeHandleable> cancelations);
        static partial void HandleCanceled();
#if CANCEL
        // Cancel promises in a depth-first manner.
        private static ValueLinkedQueue<Internal.ITreeHandleable> _cancelQueue;

        static partial void AddToCancelQueueBack(Internal.ITreeHandleable cancelation)
        {
            _cancelQueue.Enqueue(cancelation);
        }

        static partial void AddToCancelQueueFront(ref ValueLinkedQueue<Internal.ITreeHandleable> cancelations)
        {
            _cancelQueue.PushAndClear(ref cancelations);
        }

        static partial void HandleCanceled()
        {
            while (_cancelQueue.IsNotEmpty)
            {
                _cancelQueue.DequeueRisky().Cancel();
            }
            _cancelQueue.ClearLast();
        }
#else
        static protected void ThrowCancelException()
        {
            throw new InvalidOperationException("Define CANCEL in ProtoPromise/Config.cs to enable cancelations.");
        }

        static partial void ValidateCancel()
        {
            ThrowCancelException();
        }
#endif

        private void WaitFor(Promise other)
        {
            ValidateReturn(other);
#if CANCEL
            if (_state == State.Canceled)
            {
                Release();
            }
            else
#endif
            {
                SetPreviousTo(other);
                other.AddWaiter(this);
            }
        }


        partial class Internal
        {
            public interface IStacktraceable
            {
#if DEBUG
                string Stacktrace { get; set; }
#endif
            }

#if PROGRESS
            partial class SequencePromise0
            {
                // Only wrap the promise to normalize its progress. If we're not using progress, we can just use the promise as-is.
                static partial void GetFirstPromise(ref Promise promise, int skipFrames)
                {
                    var newPromise = _pool.IsNotEmpty ? (Promise) _pool.Pop() : new SequencePromise0();
                    newPromise.Reset(skipFrames + 1);
                    newPromise.ResetDepth();
                    newPromise.WaitFor(promise);
                    promise = newPromise;
                }
            }
#endif
        }

        // Calls to these get compiled away when PROGRESS is undefined.
        partial void SetDepth(Promise next);
        partial void ResetDepth();

        partial void ClearProgressListeners();
        partial void ResolveProgressListeners();
        partial void RejectProgressListeners();
        partial void CancelProgressListeners();

        static partial void ValidateProgress();
        static partial void HandleProgress();
        static partial void ClearPooledProgress();
#if !PROGRESS
        partial class Internal
        {
            public abstract class PromiseWaitPromise<TPromise> : PoolablePromise<TPromise> where TPromise : PromiseWaitPromise<TPromise>
            {
                protected void ClearCachedPromise() { }
            }

            public abstract class PromiseWaitPromise<T, TPromise> : PoolablePromise<T, TPromise> where TPromise : PromiseWaitPromise<T, TPromise>
            {
                protected void ClearCachedPromise() { }
            }
        }

        protected void ReportProgress(float progress) { }

        static protected void ThrowProgressException()
        {
            throw new InvalidOperationException("Define PROGRESS in ProtoPromise/Config.cs to enable progress reports.");
        }

        static partial void ValidateProgress()
        {
            ThrowProgressException();
        }
#else
        protected ValueLinkedStackZeroGC<Internal.IProgressListener> _progressListeners;
        private Internal.UnsignedFixed32 _waitDepthAndProgress;

        static partial void ClearPooledProgress()
        {
            ValueLinkedStackZeroGC<Internal.IProgressListener>.ClearPooledNodes();
            ValueLinkedStackZeroGC<Internal.IInvokable>.ClearPooledNodes();
        }

        partial void ClearProgressListeners()
        {
            _progressListeners.Clear();
        }

        protected virtual uint GetIncrementMultiplier()
        {
            return 1u;
        }

        partial void ResolveProgressListeners()
        {
            if (_progressListeners.IsEmpty)
            {
                return;
            }

            // Reverse the order while removing the progress listeners. Back to FIFO.
            var forwardListeners = new ValueLinkedStackZeroGC<Internal.IProgressListener>();
            do
            {
                forwardListeners.Push(_progressListeners.Pop());
            } while (_progressListeners.IsNotEmpty);

            // All and Race promises return a value depending on the promises they are waiting on. Other promises return 1.
            uint increment = _waitDepthAndProgress.GetDifferenceToNextWholeAsUInt32() * GetIncrementMultiplier();
            do
            {
                forwardListeners.Pop().ResolveProgress(this, increment);
            } while (forwardListeners.IsNotEmpty);
        }

        partial void RejectProgressListeners()
        {
            while (_progressListeners.IsNotEmpty)
            {
                _progressListeners.Pop().CancelProgressIfOwner(this);
            }
        }

        partial void CancelProgressListeners()
        {
            while (_progressListeners.IsNotEmpty)
            {
                _progressListeners.Pop().CancelProgress();
            }
        }

        protected void ReportProgress(float progress)
        {
            if (progress >= 1f | _state != State.Pending)
            {
                // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                return;
            }

            uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress);
            foreach (var progressListener in _progressListeners)
            {
                progressListener.IncrementProgress(this, increment);
            }
        }

        partial void ResetDepth()
        {
            _waitDepthAndProgress = default(Internal.UnsignedFixed32);
        }

        partial void SetDepth(Promise next)
        {
            next.SetDepth(_waitDepthAndProgress);
        }

        protected virtual void SetDepth(Internal.UnsignedFixed32 previousDepth)
        {
            _waitDepthAndProgress = previousDepth;
        }

        protected virtual bool SubscribeProgressAndContinueLoop(ref Internal.IProgressListener progressListener, out Promise previous)
        {
            _progressListeners.Push(progressListener);
            return (previous = _rejectedOrCanceledValueOrPrevious as Promise) != null;
        }

        protected virtual bool SubscribeProgressIfWaiterAndContinueLoop(ref Internal.IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<Internal.PromisePassThrough> passThroughs)
        {
            return (previous = _rejectedOrCanceledValueOrPrevious as Promise) != null;
        }

        protected void ProgressInternal(Action<float> onProgress, int skipFrames)
        {
            ValidateOperation(this, skipFrames + 1);
            ValidateArgument(onProgress, "onProgress", skipFrames + 1);

            if (_state == State.Pending)
            {
                var progressDelegate = Internal.ProgressDelegate.GetOrCreate(onProgress, this, skipFrames + 1);
                Internal.IProgressListener progressListener = progressDelegate;

                // Directly add to listeners for this promise.
                // Sets promise to the one this is waiting on. Returns false if not waiting on another promise.
                Promise promise;
                if (!SubscribeProgressAndContinueLoop(ref progressListener, out promise))
                {
                    // This is the root of the promise tree.
                    progressListener.SetInitialAmount(_waitDepthAndProgress);
                    return;
                }

                SubscribeProgressToBranchesAndRoots(promise, progressListener);
            }
            else if (_state == State.Resolved)
            {
                AddToHandleQueueBack(Internal.ResolvedProgressHandler.GetOrCreate(onProgress, skipFrames + 1));
            }

            // Don't report progress if the promise is canceled or rejected.
        }

        private static void SubscribeProgressToBranchesAndRoots(Promise promise, Internal.IProgressListener progressListener)
        {
            // This allows us to subscribe progress to AllPromises and RacePromises iteratively instead of recursively
            ValueLinkedStack<Internal.PromisePassThrough> passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>();

        Repeat:
            SubscribeProgressToChain(promise, progressListener, ref passThroughs);

            if (passThroughs.IsNotEmpty)
            {
                // passThroughs are removed from their targets before adding to passThroughs. Add them back here.
                var passThrough = passThroughs.Pop();
                promise = passThrough.owner;
                progressListener = passThrough;
                passThrough.target.ReAdd(passThrough);
                goto Repeat;
            }
        }

        private static void SubscribeProgressToChain(Promise promise, Internal.IProgressListener progressListener, ref ValueLinkedStack<Internal.PromisePassThrough> passThroughs)
        {
            Promise next;
            // If the promise is not waiting on another promise (is the root), it sets next to null, does not add the listener, and returns false.
            // If the promise is waiting on another promise that is not its previous, it adds the listener, transforms progresslistener, sets next to the one it's waiting on, and returns true.
            // Otherwise, it sets next to its previous, adds the listener only if it is a WaitPromise, and returns true.
            while (promise.SubscribeProgressIfWaiterAndContinueLoop(ref progressListener, out next, ref passThroughs))
            {
                promise = next;
            }

            // promise is the root of the promise tree.
            switch (promise._state)
            {
                case State.Pending:
                {
                    progressListener.SetInitialAmount(promise._waitDepthAndProgress);
                    break;
                }
                case State.Resolved:
                {
                    progressListener.SetInitialAmount(promise._waitDepthAndProgress.GetIncrementedWholeTruncated());
                    break;
                }
                case State.Rejected:
                {
                    progressListener.CancelProgressIfOwner(promise);
                    break;
                }
                default: // case State.Canceled:
                {
                    progressListener.CancelProgress();
                    break;
                }
            }
        }

        // Handle progress.
        private static ValueLinkedQueueZeroGC<Internal.IInvokable> _progressQueue;
        private static bool _runningProgress;

        private static void AddToFrontOfProgressQueue(Internal.IInvokable progressListener)
        {
            _progressQueue.Push(progressListener);
        }

        private static void AddToBackOfProgressQueue(Internal.IInvokable progressListener)
        {
            _progressQueue.Enqueue(progressListener);
        }

        static partial void HandleProgress()
        {
            if (_runningProgress)
            {
                // HandleProgress is running higher in the program stack, so just return.
                return;
            }

            _runningProgress = true;

            while (_progressQueue.IsNotEmpty)
            {
                _progressQueue.DequeueRisky().Invoke();
            }

            _progressQueue.ClearLast();
            _runningProgress = false;
        }

        partial class Internal
        {
            /// <summary>
            /// Max Whole Number: 2^(32-<see cref="Config.ProgressDecimalBits"/>)
            /// Precision: 1/(2^<see cref="Config.ProgressDecimalBits"/>)
            /// </summary>
            public struct UnsignedFixed32
            {
                private const uint DecimalMax = 1u << Config.ProgressDecimalBits;
                private const uint DecimalMask = DecimalMax - 1u;
                private const uint WholeMask = ~DecimalMask;

                private uint _value;

                public UnsignedFixed32(uint wholePart)
                {
                    _value = wholePart << Config.ProgressDecimalBits;
                }

                public UnsignedFixed32(float decimalPart)
                {
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    _value = (uint) (decimalPart * DecimalMax);
                }

                public uint WholePart { get { return _value >> Config.ProgressDecimalBits; } }
                private double DecimalPart { get { return (double) DecimalPartAsUInt32 / (double) DecimalMax; } }
                private uint DecimalPartAsUInt32 { get { return _value & DecimalMask; } }

                public uint ToUInt32()
                {
                    return _value;
                }

                public double ToDouble()
                {
                    return (double) WholePart + DecimalPart;
                }

                public uint AssignNewDecimalPartAndGetDifferenceAsUInt32(float decimalPart)
                {
                    uint oldDecimalPart = DecimalPartAsUInt32;
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    uint newDecimalPart = (uint) (decimalPart * DecimalMax);
                    _value = (_value & WholeMask) | newDecimalPart;
                    return newDecimalPart - oldDecimalPart;
                }

                public uint GetDifferenceToNextWholeAsUInt32()
                {
                    return DecimalMax - DecimalPartAsUInt32;
                }

                public UnsignedFixed32 GetIncrementedWholeTruncated()
                {
#if DEBUG
                    checked
#endif
                    {
                        return new UnsignedFixed32()
                        {
                            _value = (_value & WholeMask) + (1u << Config.ProgressDecimalBits)
                        };
                    }
                }

                public void Increment(uint increment)
                {
                    _value += increment;
                }

                public static bool operator >(UnsignedFixed32 a, UnsignedFixed32 b)
                {
                    return a._value > b._value;
                }

                public static bool operator <(UnsignedFixed32 a, UnsignedFixed32 b)
                {
                    return a._value < b._value;
                }
            }

            // For the special case of adding a progress listener to an already resolved promise.
            public sealed class ResolvedProgressHandler : ITreeHandleable, IStacktraceable
            {
                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }
#if DEBUG
                string IStacktraceable.Stacktrace { get; set; }
#endif
                private Action<float> _onProgress;

                private static ValueLinkedStack<ITreeHandleable> _pool;

                static ResolvedProgressHandler()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private ResolvedProgressHandler() { }

                public static ResolvedProgressHandler GetOrCreate(Action<float> onProgress, int skipFrames)
                {
                    var handler = _pool.IsNotEmpty ? (ResolvedProgressHandler) _pool.Pop() : new ResolvedProgressHandler();
                    handler._onProgress = onProgress;
                    SetCreatedStacktrace(handler, skipFrames + 1);
                    return handler;
                }

                void ITreeHandleable.Handle()
                {
                    var temp = _onProgress;
                    _onProgress = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                    try
                    {
                        temp.Invoke(1f);
                    }
                    catch (Exception e)
                    {
                        UnhandledExceptionException unhandledException = UnhandledExceptionException.GetOrCreate(e);
                        SetStacktraceFromCreated(this, unhandledException);
                        AddRejectionToUnhandledStack(unhandledException);
                    }
                }

                void ITreeHandleable.Cancel() { throw new InvalidOperationException(); }
            }

            public interface IInvokable
            {
                void Invoke();
            }

            public interface IProgressListener
            {
                void SetInitialAmount(UnsignedFixed32 amount);
                void IncrementProgress(Promise sender, uint amount);
                void ResolveProgress(Promise sender, uint increment);
                void CancelProgressIfOwner(Promise sender);
                void CancelProgress();
            }

            partial interface IMultiTreeHandleable
            {
                void IncrementProgress(uint increment, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount);
                void SetInitialAmount(uint increment, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount);
            }

            public sealed class ProgressDelegate : IProgressListener, IInvokable, IStacktraceable
            {
#if DEBUG
                string IStacktraceable.Stacktrace { get; set; }
#endif

                private Action<float> _onProgress;
                private Promise _owner;
                private UnsignedFixed32 _current;
                private bool _handling;
                private bool _done;

                private static ValueLinkedStackZeroGC<IProgressListener> _pool;

                static ProgressDelegate()
                {
                    OnClearPool += () => _pool.ClearAndDontRepool();
                }

                private ProgressDelegate() { }

                public static ProgressDelegate GetOrCreate(Action<float> onProgress, Promise owner, int skipFrames)
                {
                    var progress = _pool.IsNotEmpty ? (ProgressDelegate) _pool.Pop() : new ProgressDelegate();
                    progress._onProgress = onProgress;
                    progress._owner = owner;
                    progress._current = default(UnsignedFixed32);
                    SetCreatedStacktrace(progress, skipFrames + 1);
                    return progress;
                }

                private void InvokeAndCatch(Action<float> callback, float progress)
                {
                    try
                    {
                        callback.Invoke(progress);
                    }
                    catch (Exception e)
                    {
                        UnhandledExceptionException unhandledException = UnhandledExceptionException.GetOrCreate(e);
                        SetStacktraceFromCreated(this, unhandledException);
                        AddRejectionToUnhandledStack(unhandledException);
                    }
                }

                void IInvokable.Invoke()
                {
                    _handling = false;

                    if (_done)
                    {
                        Dispose();
                        return;
                    }

                    // Calculate the normalized progress for the depth that the listener was added.
                    // Use double for better precision.
                    double expected = _owner._waitDepthAndProgress.WholePart + 1u;
                    InvokeAndCatch(_onProgress, (float) (_current.ToDouble() / expected));
                }

                // This is called by the promise in forward order that listeners were added.
                void IProgressListener.ResolveProgress(Promise sender, uint increment)
                {
                    if (sender == _owner)
                    {
                        var temp = _onProgress;
                        CancelProgress();
                        InvokeAndCatch(temp, 1f);
                    }
                    else
                    {
                        _current.Increment(increment);
                        if (!_handling)
                        {
                            _handling = true;
                            AddToBackOfProgressQueue(this);
                        }
                    }
                }

                void IProgressListener.IncrementProgress(Promise sender, uint amount)
                {
                    _current.Increment(amount);
                    if (!_handling)
                    {
                        _handling = true;
                        // This is called by the promise in reverse order that listeners were added, adding to the front reverses that and puts them in proper order.
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IProgressListener.SetInitialAmount(UnsignedFixed32 amount)
                {
                    _current = amount;
                    _handling = true;
                    // Always add new listeners to the back.
                    AddToBackOfProgressQueue(this);
                }

                void IProgressListener.CancelProgressIfOwner(Promise sender)
                {
                    if (sender == _owner)
                    {
                        CancelProgress();
                    }
                }

                public void CancelProgress()
                {
                    if (_handling)
                    {
                        // Mark done so InvokeProgressListeners will dispose.
                        _done = true;
                    }
                    else
                    {
                        // Dispose only if it's not in the progress queue.
                        Dispose();
                    }
                }

                private void Dispose()
                {
                    _onProgress = null;
                    _done = false;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            public abstract class PromiseWaitPromise<TPromise> : PoolablePromise<TPromise>, IProgressListener, IInvokable where TPromise : PromiseWaitPromise<TPromise>
            {
                // This is used to avoid rounding errors when normalizing the progress.
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;
                private bool _secondPrevious;

                protected override void Reset(int skipFrames)
                {
                    base.Reset(skipFrames + 1);
                    _invokingProgress = false;
                    _secondPrevious = false;
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out Promise previous)
                {
                    // This is guaranteed to be pending.
                    _progressListeners.Push(progressListener);
                    previous = _rejectedOrCanceledValueOrPrevious as Promise;
                    if (_secondPrevious)
                    {
                        bool firstSubscribe = _progressListeners.IsEmpty;
                        if (firstSubscribe)
                        {
                            // Subscribe this to the returned promise.
                            progressListener = this;
                        }
                        return firstSubscribe;
                    }
                    return true;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    if (_state != State.Pending)
                    {
                        previous = null;
                        return false;
                    }
                    return SubscribeProgressAndContinueLoop(ref progressListener, out previous);
                }

                protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
                {
                    _waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
                }

                protected override void SetPreviousTo(Promise other)
                {
                    base.SetPreviousTo(other);
                    _secondPrevious = true;
                    if (_progressListeners.IsNotEmpty)
                    {
                        SubscribeProgressToBranchesAndRoots(other, this);
                    }
                }

                void IInvokable.Invoke()
                {
                    if (_state != State.Pending)
                    {
                        return;
                    }

                    _invokingProgress = false;

                    // Calculate the normalized progress for the depth of the returned promise.
                    // Use double for better precision.
                    double expected = ((Promise) _rejectedOrCanceledValueOrPrevious)._waitDepthAndProgress.WholePart + 1u;
                    float progress = (float) (_currentAmount.ToDouble() / expected);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress);

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.IncrementProgress(this, increment);
                    }
                }

                void IProgressListener.SetInitialAmount(UnsignedFixed32 amount)
                {
                    _currentAmount = amount;
                    _invokingProgress = true;
                    AddToFrontOfProgressQueue(this);
                }

                void IProgressListener.IncrementProgress(Promise sender, uint amount)
                {
                    _currentAmount.Increment(amount);
                    if (!_invokingProgress)
                    {
                        _invokingProgress = true;
                        AddToFrontOfProgressQueue(this);
                    }
                }

                // Not used. The promise handles resolve and cancel.
                void IProgressListener.ResolveProgress(Promise sender, uint increment) { }
                void IProgressListener.CancelProgressIfOwner(Promise sender) { }
                void IProgressListener.CancelProgress() { }
            }

            public abstract class PromiseWaitPromise<T, TPromise> : PoolablePromise<T, TPromise>, IProgressListener, IInvokable where TPromise : PromiseWaitPromise<T, TPromise>
            {
                // This is used to avoid rounding errors when normalizing the progress.
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;
                private bool _secondPrevious;

                protected override void Reset(int skipFrames)
                {
                    base.Reset(skipFrames + 1);
                    _invokingProgress = false;
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out Promise previous)
                {
                    // This is guaranteed to be pending.
                    _progressListeners.Push(progressListener);
                    previous = _rejectedOrCanceledValueOrPrevious as Promise;
                    if (_secondPrevious)
                    {
                        bool firstSubscribe = _progressListeners.IsEmpty;
                        if (firstSubscribe)
                        {
                            // Subscribe this to the returned promise.
                            progressListener = this;
                        }
                        return firstSubscribe;
                    }
                    return true;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    if (_state != State.Pending)
                    {
                        previous = null;
                        return false;
                    }
                    return SubscribeProgressAndContinueLoop(ref progressListener, out previous);
                }

                protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
                {
                    _waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
                }

                protected override void SetPreviousTo(Promise other)
                {
                    base.SetPreviousTo(other);
                    _secondPrevious = true;
                    if (_progressListeners.IsNotEmpty)
                    {
                        SubscribeProgressToBranchesAndRoots(other, this);
                    }
                }

                void IInvokable.Invoke()
                {
                    if (_state != State.Pending)
                    {
                        return;
                    }

                    _invokingProgress = false;

                    // Calculate the normalized progress for the depth of the cached promise.
                    // Use double for better precision.
                    double expected = ((Promise) _rejectedOrCanceledValueOrPrevious)._waitDepthAndProgress.WholePart + 1u;
                    float progress = (float) (_currentAmount.ToDouble() / expected);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress);

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.IncrementProgress(this, increment);
                    }
                }

                void IProgressListener.SetInitialAmount(UnsignedFixed32 amount)
                {
                    _currentAmount = amount;
                    _invokingProgress = true;
                    AddToFrontOfProgressQueue(this);
                }

                void IProgressListener.IncrementProgress(Promise sender, uint amount)
                {
                    _currentAmount.Increment(amount);
                    if (!_invokingProgress)
                    {
                        _invokingProgress = true;
                        AddToFrontOfProgressQueue(this);
                    }
                }

                // Not used. The promise handles resolve and cancel.
                void IProgressListener.ResolveProgress(Promise sender, uint increment) { }
                void IProgressListener.CancelProgressIfOwner(Promise sender) { }
                void IProgressListener.CancelProgress() { }
            }

            partial class PromiseWaitDeferred<TPromise>
            {
                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    if (_state != State.Pending)
                    {
                        previous = null;
                        return false;
                    }
                    return SubscribeProgressAndContinueLoop(ref progressListener, out previous);
                }

                protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
                {
                    _waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
                }
            }

            partial class PromiseWaitDeferred<T, TPromise>
            {
                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    if (_state != State.Pending)
                    {
                        previous = null;
                        return false;
                    }
                    return SubscribeProgressAndContinueLoop(ref progressListener, out previous);
                }

                protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
                {
                    _waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
                }
            }

            partial class PromisePassThrough : IProgressListener
            {
                void IProgressListener.IncrementProgress(Promise sender, uint amount)
                {
                    target.IncrementProgress(amount, sender._waitDepthAndProgress, owner._waitDepthAndProgress);
                }

                void IProgressListener.SetInitialAmount(UnsignedFixed32 amount)
                {
                    target.SetInitialAmount(amount.ToUInt32(), amount, owner._waitDepthAndProgress);
                }

                // Not used. The promise handles resolve and cancel.
                void IProgressListener.ResolveProgress(Promise sender, uint increment) { }
                void IProgressListener.CancelProgressIfOwner(Promise sender) { }
                void IProgressListener.CancelProgress() { }
            }

            partial class AllPromise0 : IInvokable
            {
                // These are used to avoid rounding errors when normalizing the progress.
                private float _expected;
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;

                protected override void Reset(int skipFrames)
                {
#if DEBUG
                    checked
#endif
                    {
                        base.Reset(skipFrames + 1);
                        _currentAmount = default(UnsignedFixed32);

                        uint expectedProgressCounter = 0;
                        uint maxWaitDepth = 0;
                        foreach (var passThrough in passThroughs)
                        {
                            uint waitDepth = passThrough.owner._waitDepthAndProgress.WholePart;
                            expectedProgressCounter += waitDepth;
                            maxWaitDepth = Math.Max(maxWaitDepth, waitDepth);
                        }
                        _expected = expectedProgressCounter + _waitCount;

                        // Use the longest chain as this depth.
                        _waitDepthAndProgress = new UnsignedFixed32(maxWaitDepth);
                    }
                }

                partial void IncrementProgress(Promise feed)
                {
                    bool subscribedProgress = _progressListeners.IsNotEmpty;
                    uint increment = subscribedProgress ? feed._waitDepthAndProgress.GetDifferenceToNextWholeAsUInt32() : feed._waitDepthAndProgress.GetIncrementedWholeTruncated().ToUInt32();
                    IncrementProgress(increment);
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out Promise previous)
                {
                    // This is guaranteed to be pending.
                    previous = this;
                    return true;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    _progressListeners.Push(progressListener);
                    if (firstSubscribe & _state == State.Pending)
                    {
                        BorrowPassthroughs(ref passThroughs);
                    }

                    previous = null;
                    return false;
                }

                void IMultiTreeHandleable.SetInitialAmount(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // This is guaranteed to be pending.
                    _currentAmount.Increment(amount);
                    if (!_invokingProgress)
                    {
                        _invokingProgress = true;
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    IncrementProgress(amount);
                }

                private void IncrementProgress(uint amount)
                {
                    _currentAmount.Increment(amount);
                    if (!_invokingProgress & _state == State.Pending)
                    {
                        _invokingProgress = true;
                        AddToFrontOfProgressQueue(this);
                    }
                }

                protected override uint GetIncrementMultiplier()
                {
                    return _waitDepthAndProgress.WholePart + 1u;
                }

                void IInvokable.Invoke()
                {
                    _invokingProgress = false;

                    if (_state != State.Pending)
                    {
                        return;
                    }

                    // Calculate the normalized progress for all the awaited promises.
                    // Use double for better precision.
                    float progress = (float) (_currentAmount.ToDouble() / _expected);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress) * GetIncrementMultiplier();

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.IncrementProgress(this, increment);
                    }
                }
            }

            partial class AllPromise<T> : IInvokable
            {
                // These are used to avoid rounding errors when normalizing the progress.
                private float _expected;
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;

                protected override void Reset(int skipFrames)
                {
#if DEBUG
                    checked
#endif
                    {
                        base.Reset(skipFrames + 1);
                        _currentAmount = default(UnsignedFixed32);

                        uint expectedProgressCounter = 0;
                        uint maxWaitDepth = 0;
                        foreach (var passThrough in passThroughs)
                        {
                            uint waitDepth = passThrough.owner._waitDepthAndProgress.WholePart;
                            expectedProgressCounter += waitDepth;
                            maxWaitDepth = Math.Max(maxWaitDepth, waitDepth);
                        }
                        _expected = expectedProgressCounter + _waitCount;

                        // Use the longest chain as this depth.
                        _waitDepthAndProgress = new UnsignedFixed32(maxWaitDepth);
                    }
                }

                partial void IncrementProgress(Promise feed)
                {
                    bool subscribedProgress = _progressListeners.IsNotEmpty;
                    uint increment = subscribedProgress ? feed._waitDepthAndProgress.GetDifferenceToNextWholeAsUInt32() : feed._waitDepthAndProgress.GetIncrementedWholeTruncated().ToUInt32();
                    IncrementProgress(increment);
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out Promise previous)
                {
                    // This is guaranteed to be pending.
                    previous = this;
                    return true;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    _progressListeners.Push(progressListener);
                    if (firstSubscribe & _state == State.Pending)
                    {
                        BorrowPassthroughs(ref passThroughs);
                    }

                    previous = null;
                    return false;
                }

                void IMultiTreeHandleable.SetInitialAmount(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // This is guaranteed to be pending.
                    _currentAmount.Increment(amount);
                    if (!_invokingProgress)
                    {
                        _invokingProgress = true;
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    IncrementProgress(amount);
                }

                private void IncrementProgress(uint amount)
                {
                    _currentAmount.Increment(amount);
                    if (!_invokingProgress & _state == State.Pending)
                    {
                        _invokingProgress = true;
                        AddToFrontOfProgressQueue(this);
                    }
                }

                protected override uint GetIncrementMultiplier()
                {
                    return _waitDepthAndProgress.WholePart + 1u;
                }

                void IInvokable.Invoke()
                {
                    _invokingProgress = false;

                    if (_state != State.Pending)
                    {
                        return;
                    }

                    // Calculate the normalized progress for all the awaited promises.
                    // Use double for better precision.
                    float progress = (float) (_currentAmount.ToDouble() / _expected);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress) * GetIncrementMultiplier();

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.IncrementProgress(this, increment);
                    }
                }
            }

            partial class RacePromise0 : IInvokable
            {
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;

                protected override void Reset(int skipFrames)
                {
#if DEBUG
                    checked
#endif
                    {
                        base.Reset(skipFrames + 1);
                        _currentAmount = default(UnsignedFixed32);

                        uint minWaitDepth = uint.MaxValue;
                        foreach (var passThrough in passThroughs)
                        {
                            minWaitDepth = Math.Min(minWaitDepth, passThrough.owner._waitDepthAndProgress.WholePart);
                        }

                        // Expect the shortest chain to finish first.
                        _waitDepthAndProgress = new UnsignedFixed32(minWaitDepth);
                    }
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out Promise previous)
                {
                    // This is guaranteed to be pending.
                    previous = this;
                    return true;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    _progressListeners.Push(progressListener);
                    if (firstSubscribe & _state == State.Pending)
                    {
                        BorrowPassthroughs(ref passThroughs);
                    }

                    previous = null;
                    return false;
                }

                private void SetAmount(UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // Use double for better precision.
                    float progress = (float) ((double) senderAmount.ToUInt32() * (double) GetIncrementMultiplier() / (double) ownerAmount.GetIncrementedWholeTruncated().ToUInt32());
                    var newAmount = new UnsignedFixed32(progress);
                    if (newAmount > _currentAmount)
                    {
                        _currentAmount = newAmount;
                        if (!_invokingProgress)
                        {
                            _invokingProgress = true;
                            AddToFrontOfProgressQueue(this);
                        }
                    }
                }

                void IMultiTreeHandleable.SetInitialAmount(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // This is guaranteed to be pending.
                    SetAmount(senderAmount, ownerAmount);
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // This is guaranteed to be pending.
                    SetAmount(senderAmount, ownerAmount);
                }

                protected override uint GetIncrementMultiplier()
                {
                    return _waitDepthAndProgress.WholePart + 1u;
                }

                void IInvokable.Invoke()
                {
                    _invokingProgress = false;

                    if (_state != State.Pending)
                    {
                        return;
                    }

                    uint multiplier = GetIncrementMultiplier();

                    // Calculate the normalized progress.
                    // Use double for better precision.
                    float progress = (float) (_currentAmount.ToDouble() / multiplier);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress) * multiplier;

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.IncrementProgress(this, increment);
                    }
                }
            }

            partial class RacePromise<T> : IInvokable
            {
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;

                protected override void Reset(int skipFrames)
                {
#if DEBUG
                    checked
#endif
                    {
                        base.Reset(skipFrames + 1);
                        _currentAmount = default(UnsignedFixed32);

                        uint minWaitDepth = uint.MaxValue;
                        foreach (var passThrough in passThroughs)
                        {
                            minWaitDepth = Math.Min(minWaitDepth, passThrough.owner._waitDepthAndProgress.WholePart);
                        }

                        // Expect the shortest chain to finish first.
                        _waitDepthAndProgress = new UnsignedFixed32(minWaitDepth);
                    }
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out Promise previous)
                {
                    // This is guaranteed to be pending.
                    previous = this;
                    return true;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    _progressListeners.Push(progressListener);
                    if (firstSubscribe & _state == State.Pending)
                    {
                        BorrowPassthroughs(ref passThroughs);
                    }

                    previous = null;
                    return false;
                }

                private void SetAmount(UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // Use double for better precision.
                    float progress = (float) ((double) senderAmount.ToUInt32() * (double) GetIncrementMultiplier() / (double) ownerAmount.GetIncrementedWholeTruncated().ToUInt32());
                    var newAmount = new UnsignedFixed32(progress);
                    if (newAmount > _currentAmount)
                    {
                        _currentAmount = newAmount;
                        if (!_invokingProgress)
                        {
                            _invokingProgress = true;
                            AddToFrontOfProgressQueue(this);
                        }
                    }
                }

                void IMultiTreeHandleable.SetInitialAmount(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // This is guaranteed to be pending.
                    SetAmount(senderAmount, ownerAmount);
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // This is guaranteed to be pending.
                    SetAmount(senderAmount, ownerAmount);
                }

                protected override uint GetIncrementMultiplier()
                {
                    return _waitDepthAndProgress.WholePart + 1u;
                }

                void IInvokable.Invoke()
                {
                    _invokingProgress = false;

                    if (_state != State.Pending)
                    {
                        return;
                    }

                    uint multiplier = GetIncrementMultiplier();

                    // Calculate the normalized progress.
                    // Use double for better precision.
                    float progress = (float) (_currentAmount.ToDouble() / multiplier);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress) * multiplier;

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.IncrementProgress(this, increment);
                    }
                }
            }

            partial class FirstPromise0 : IInvokable
            {
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;

                protected override void Reset(int skipFrames)
                {
#if DEBUG
                    checked
#endif
                    {
                        base.Reset(skipFrames + 1);
                        _currentAmount = default(UnsignedFixed32);

                        uint minWaitDepth = uint.MaxValue;
                        foreach (var passThrough in passThroughs)
                        {
                            minWaitDepth = Math.Min(minWaitDepth, passThrough.owner._waitDepthAndProgress.WholePart);
                        }

                        // Expect the shortest chain to finish first.
                        _waitDepthAndProgress = new UnsignedFixed32(minWaitDepth);
                    }
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out Promise previous)
                {
                    // This is guaranteed to be pending.
                    previous = this;
                    return true;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    _progressListeners.Push(progressListener);
                    if (firstSubscribe & _state == State.Pending)
                    {
                        BorrowPassthroughs(ref passThroughs);
                    }

                    previous = null;
                    return false;
                }

                private void SetAmount(UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // Use double for better precision.
                    float progress = (float) ((double) senderAmount.ToUInt32() * (double) GetIncrementMultiplier() / (double) ownerAmount.GetIncrementedWholeTruncated().ToUInt32());
                    var newAmount = new UnsignedFixed32(progress);
                    if (newAmount > _currentAmount)
                    {
                        _currentAmount = newAmount;
                        if (!_invokingProgress)
                        {
                            _invokingProgress = true;
                            AddToFrontOfProgressQueue(this);
                        }
                    }
                }

                void IMultiTreeHandleable.SetInitialAmount(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // This is guaranteed to be pending.
                    SetAmount(senderAmount, ownerAmount);
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // This is guaranteed to be pending.
                    SetAmount(senderAmount, ownerAmount);
                }

                protected override uint GetIncrementMultiplier()
                {
                    return _waitDepthAndProgress.WholePart + 1u;
                }

                void IInvokable.Invoke()
                {
                    _invokingProgress = false;

                    if (_state != State.Pending)
                    {
                        return;
                    }

                    uint multiplier = GetIncrementMultiplier();

                    // Calculate the normalized progress.
                    // Use double for better precision.
                    float progress = (float) (_currentAmount.ToDouble() / multiplier);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress) * multiplier;

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.IncrementProgress(this, increment);
                    }
                }
            }

            partial class FirstPromise<T> : IInvokable
            {
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;

                protected override void Reset(int skipFrames)
                {
#if DEBUG
                    checked
#endif
                    {
                        base.Reset(skipFrames + 1);
                        _currentAmount = default(UnsignedFixed32);

                        uint minWaitDepth = uint.MaxValue;
                        foreach (var passThrough in passThroughs)
                        {
                            minWaitDepth = Math.Min(minWaitDepth, passThrough.owner._waitDepthAndProgress.WholePart);
                        }

                        // Expect the shortest chain to finish first.
                        _waitDepthAndProgress = new UnsignedFixed32(minWaitDepth);
                    }
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out Promise previous)
                {
                    // This is guaranteed to be pending.
                    previous = this;
                    return true;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    _progressListeners.Push(progressListener);
                    if (firstSubscribe & _state == State.Pending)
                    {
                        BorrowPassthroughs(ref passThroughs);
                    }

                    previous = null;
                    return false;
                }

                private void SetAmount(UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // Use double for better precision.
                    float progress = (float) ((double) senderAmount.ToUInt32() * (double) GetIncrementMultiplier() / (double) ownerAmount.GetIncrementedWholeTruncated().ToUInt32());
                    var newAmount = new UnsignedFixed32(progress);
                    if (newAmount > _currentAmount)
                    {
                        _currentAmount = newAmount;
                        if (!_invokingProgress)
                        {
                            _invokingProgress = true;
                            AddToFrontOfProgressQueue(this);
                        }
                    }
                }

                void IMultiTreeHandleable.SetInitialAmount(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // This is guaranteed to be pending.
                    SetAmount(senderAmount, ownerAmount);
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // This is guaranteed to be pending.
                    SetAmount(senderAmount, ownerAmount);
                }

                protected override uint GetIncrementMultiplier()
                {
                    return _waitDepthAndProgress.WholePart + 1u;
                }

                void IInvokable.Invoke()
                {
                    _invokingProgress = false;

                    if (_state != State.Pending)
                    {
                        return;
                    }

                    uint multiplier = GetIncrementMultiplier();

                    // Calculate the normalized progress.
                    // Use double for better precision.
                    float progress = (float) (_currentAmount.ToDouble() / multiplier);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress) * multiplier;

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.IncrementProgress(this, increment);
                    }
                }
            }
        }
#endif

        private void ResolveDirect()
        {
#if CANCEL
            if (_state == State.Canceled)
            {
                Release();
            }
            else
#endif
            {
                AddToHandleQueueBack(this);
            }
        }

        protected void RejectDirect(Internal.IValueContainerOrPrevious rejectValue)
        {
#if CANCEL
            if (_state == State.Canceled)
            {
                AddRejectionToUnhandledStack((Internal.UnhandledExceptionInternal) rejectValue);
                Release();
            }
            else
#endif
            {
                _rejectedOrCanceledValueOrPrevious = rejectValue;
                _rejectedOrCanceledValueOrPrevious.Retain();
                AddToHandleQueueBack(this);
            }
        }

        protected void ResolveInternalIfNotCanceled()
        {
#if CANCEL
            if (_state == State.Canceled)
            {
                Release();
            }
            else
#endif
            {
                ResolveInternal();
            }
        }

        protected void RejectInternalIfNotCanceled(Internal.IValueContainerOrPrevious rejectValue)
        {
#if CANCEL
            if (_state == State.Canceled)
            {
                AddRejectionToUnhandledStack((Internal.UnhandledExceptionInternal) rejectValue);
                Release();
            }
            else
#endif
            {
                RejectInternal(rejectValue);
            }
        }

        protected void AddWaiter(Internal.ITreeHandleable waiter)
        {
            Retain();
            if (_state == State.Pending)
            {
                _nextBranches.Enqueue(waiter);
            }
#if CANCEL
            else if (_state == State.Canceled)
            {
                AddToCancelQueueBack(waiter);
            }
#endif
            else
            {
                AddToHandleQueueBack(waiter);
            }
        }

        void Internal.ITreeHandleable.Handle()
        {
#if CANCEL
            if (_state == State.Canceled)
            {
                Release();
            }
            else
#endif
            {
                Handle();
            }
        }
    }

    partial class Promise<T>
    {
        // Calls to these get compiled away in RELEASE mode
        static partial void ValidateOperation(Promise<T> promise, int skipFrames);
        static partial void ValidateArgument(Delegate del, string argName, int skipFrames);
        static partial void ValidateProgress(float progress, int skipFrames);
#if DEBUG
        static partial void ValidateProgress(float progress, int skipFrames)
        {
            ValidateProgressValue(progress, skipFrames + 1);
        }

        static partial void ValidateOperation(Promise<T> promise, int skipFrames)
        {
            ValidateNotDisposed(promise._rejectedOrCanceledValueOrPrevious, skipFrames + 1);
        }

        static partial void ValidateArgument(Delegate del, string argName, int skipFrames)
        {
            ValidateArg(del, argName, skipFrames + 1);
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

        // Calls to this get compiled away when CANCEL is defined.
        static partial void ValidateCancel();
#if !CANCEL
        static partial void ValidateCancel()
        {
            ThrowCancelException();
        }
#endif

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/> that adopts the state of this. This is mostly useful for branches that you expect might be canceled, and you don't want all branches to be canceled.
        /// </summary>
#if !CANCEL
        [Obsolete("This is mostly useful for cancelations of branched promises. Define CANCEL in ProtoPromise/Config.cs to enable cancelations.", false)]
#endif
        public new Promise<T> ThenDuplicate()
        {
            var promise = Promise.Internal.DuplicatePromise<T>.GetOrCreate(1);
            HookupNewPromise(promise);
            return promise;
        }

        // Calls to these get compiled away when PROGRESS is defined.
        static partial void ValidateProgress();
#if PROGRESS
        /// <summary>
        /// Add a progress listener. <paramref name="onProgress"/> will be invoked with progress that is normalized between 0 and 1 from this and all previous waiting promises in the chain.
        /// Returns this.
        /// </summary>
        public new Promise<T> Progress(Action<float> onProgress)
        {
            ProgressInternal(onProgress, 1);
            return this;
        }
#else
        static partial void ValidateProgress()
        {
            ThrowProgressException();
        }
#endif

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
        protected void ResolveDirect(in T value)
#else
        protected void ResolveDirect(T value)
#endif
        {
#if CANCEL
            if (_state == State.Canceled)
            {
                Release();
            }
            else
#endif
            {
                _value = value;
                AddToHandleQueueBack(this);
            }
        }
    }

    partial class Promise
    {
        partial class Internal
        {
            partial class FinallyDelegate : IStacktraceable
            {
#if DEBUG
                string IStacktraceable.Stacktrace { get; set; }
#endif
            }

            partial class PotentialCancelation : IStacktraceable
            {
#if DEBUG
                string IStacktraceable.Stacktrace { get; set; }
#endif
            }
        }
    }
}