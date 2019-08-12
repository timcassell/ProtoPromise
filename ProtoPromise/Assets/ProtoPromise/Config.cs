// undef CANCEL to disable, or define CANCEL to enable cancelations on promises.
// If CANCEL is defined, it breaks the Promises/A+ spec "2.1. Promise States", but allows breaking promise chains.
#define CANCEL
// undef PROGRESS to disable, or define PROGRESS to enable progress reports on promises.
// If PROGRESS is defined, promises use more memory. If PROGRESS is undefined, there is no limit to the depth of a promise chain.
#define PROGRESS

#pragma warning disable IDE0034 // Simplify 'default' expression
using System;

namespace ProtoPromise
{
    partial class Promise
    {
        public enum GeneratedStacktrace
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
            /// NOTE: This can be extremely expensive, so you should only enable this if you ran into an error and you are not sure where it came from.
            /// </summary>
            All
        }

        public static class Config
        {
            /// <summary>
            /// If you need to support more whole numbers (longer promise chains), decrease decimalBits. If you need higher precision, increase decimalBits.
            /// Max Whole Number: 2^(32-<see cref="ProgressDecimalBits"/>)
            /// Precision: 1/(2^<see cref="ProgressDecimalBits"/>)
            /// NOTE: promises that don't wait (.Then with an onResolved that simply returns a value or void) don't count towards the promise chain depth limit.
            /// </summary>
            public const int ProgressDecimalBits = 13;

            /// <summary>
            /// Highly recommend to leave this false in DEBUG mode, so that exceptions will fire if/when promises are used incorrectly after they have already completed.
            /// </summary>
            public static bool PoolObjects { get; set; }

            public static void ClearObjectPool()
            {
                Internal.OnClearPool.Invoke();
            }

#if DEBUG
            public static GeneratedStacktrace DebugStacktraceGenerator { get; set; }
#else
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter
			public static GeneratedStacktrace DebugStacktraceGenerator { get { return default(GeneratedStacktrace); } set { } }
#pragma warning restore RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter
#endif
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

#if DEBUG || PROGRESS
        private Promise _previous;
#endif

        // Calls to these get compiled away in RELEASE mode
        static partial void ValidateOperation(Promise promise);
        static partial void ValidateProgress(float progress);
        static partial void ValidateArgument(Delegate del, string argName);
        partial void ValidateReturn(Promise other);
        static partial void ValidateReturn(Delegate other);
        partial void SetCreatedStackTrace(int skipFrames);
        partial void SetStackTraceFromCreated(UnhandledException unhandledException);
        static partial void SetRejectStackTrace(UnhandledException unhandledException, int skipFrames);
        partial void SetDisposed(bool disposed);
#if DEBUG
        private bool _disposed;
        private string _createdStackTrace;
        private static int idCounter;
        protected readonly int _id;

        partial void SetDisposed(bool disposed)
        {
            _disposed = disposed;
        }

        partial void SetCreatedStackTrace(int skipFrames)
        {
            if (Config.DebugStacktraceGenerator == GeneratedStacktrace.All)
            {
                _createdStackTrace = GetStackTrace(skipFrames + 1);
            }
        }

        partial void SetStackTraceFromCreated(UnhandledException unhandledException)
        {
            unhandledException.SetStackTrace(FormatStackTrace(_createdStackTrace));
        }

        static partial void SetRejectStackTrace(UnhandledException unhandledException, int skipFrames)
        {
            if (Config.DebugStacktraceGenerator != GeneratedStacktrace.None)
            {
                unhandledException.SetStackTrace(FormatStackTrace(GetStackTrace(skipFrames + 1)));
            }
        }

        private static System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(128);

        private static string GetStackTrace(int skipFrames)
        {
            return new System.Diagnostics.StackTrace(skipFrames + 1, true).ToString();
        }

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
            try
            {
                ValidateOperation(other);
            }
            catch (ObjectDisposedException e)
            {
                throw new InvalidReturnException("A disposed promise was returned.", innerException: e);
            }

            // A promise cannot wait on itself.
            for (var prev = other; prev != null; prev = prev._previous)
            {
                if (prev == this)
                {
                    throw new InvalidReturnException("Circular Promise chain detected.", other._createdStackTrace);
                }
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

        static protected void ValidateProgressValue(float value)
        {
            const string argName = "progress";
            if (value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(argName, "Must be between 0 and 1.");
            }
        }

        protected void ValidateNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Call Retain() if you want to perform operations after the promise has finished. Remember to call Release() when you are finished with it!");
            }
        }

        static partial void ValidateOperation(Promise promise)
        {
            promise.ValidateNotDisposed();
        }

        static partial void ValidateProgress(float progress)
        {
            ValidateProgressValue(progress);
        }

        static protected void ValidateArg(Delegate del, string argName)
        {
            if (del == null)
            {
                throw new ArgumentNullException(argName);
            }
        }

        static partial void ValidateArgument(Delegate del, string argName)
        {
            ValidateArg(del, argName);
        }

        public override string ToString()
        {
            return string.Format("Type: Promise, Id: {0}, State: {1}", _id, _state);
        }
#else
        public override string ToString()
        {
            return string.Format("Type: Promise, State: {0}", _state);
        }
#endif


        // Calls to this get compiled away when CANCEL is defined.
        static partial void ValidateCancel();
#if !CANCEL
        static protected void ThrowCancelException()
        {
            throw new InvalidOperationException("Define CANCEL in ProtoPromise/Manager.cs to enable cancelations.");
        }

		static partial void ValidateCancel()
		{
            ThrowCancelException();
		}

        private void WaitFor(Promise other)
        {
            Validate(other);
            SubscribeProgress(other);
            other.AddWaiter(this);
        }
#else
        private void WaitFor(Promise other)
        {
            ValidateReturn(other);
            if (_state == DeferredState.Canceled)
            {
                // Do nothing if this promise was canceled during the callback, just place in the handle queue so it can be repooled.
                AddToHandleQueue(this);
            }
            else
            {
                SubscribeProgress(other);
                other.AddWaiter(this);
            }
        }
#endif


        partial class Internal
        {
#pragma warning disable RECS0001 // Class is declared partial but has only one part
            public abstract partial class PromiseWaitPromise<TPromise> : PoolablePromise<TPromise> where TPromise : PromiseWaitPromise<TPromise> { }

            public abstract partial class PromiseWaitPromise<T, TPromise> : PoolablePromise<T, TPromise> where TPromise : PromiseWaitPromise<T, TPromise> { }

            public abstract partial class PromiseWaitDeferred<TPromise> : PoolablePromise<TPromise> where TPromise : PromiseWaitDeferred<TPromise>
            {
                public readonly Deferred deferred;

                protected PromiseWaitDeferred()
                {
                    deferred = new DeferredInternal(this);
                }
            }

            public abstract partial class PromiseWaitDeferred<T, TPromise> : PoolablePromise<T, TPromise> where TPromise : PromiseWaitDeferred<T, TPromise>
            {
                public readonly Deferred deferred;

                protected PromiseWaitDeferred()
                {
                    deferred = new Internal.DeferredInternal(this);
                }
            }
#pragma warning restore RECS0001 // Class is declared partial but has only one part
        }

        protected virtual void ReportProgress(float progress) { }

        // Calls to these get compiled away when PROGRESS is undefined.
        partial void SetDepthAndPrevious(Promise next);
        partial void ClearPrevious();
        partial void ResetDepth();
        partial void SubscribeProgress(Promise other);
        partial void ProgressInternal(Action<float> onProgress);

        static partial void ValidateProgress();
#if !PROGRESS
        static protected void ThrowProgressException()
        {
            throw new InvalidOperationException("Define PROGRESS in ProtoPromise/Managers.cs to enable progress reports.");
        }

        static partial void ValidateProgress()
        {
            ThrowProgressException();
        }
#else
        private Internal.UnsignedFixed32 _waitDepthAndProgress;

        partial void SubscribeProgress(Promise other)
        {
            _SubscribeProgress(other);
        }

        protected virtual void _SubscribeProgress(Promise other) { }

        partial void ResetDepth()
        {
            _waitDepthAndProgress = default(Internal.UnsignedFixed32);
        }

        partial void SetDepthAndPrevious(Promise next)
        {
            next._previous = this;
            next.SetDepth(_waitDepthAndProgress);
        }

        partial void ClearPrevious()
        {
            _previous = null;
        }

        protected virtual void SetDepth(Internal.UnsignedFixed32 previousDepth)
        {
            _waitDepthAndProgress = previousDepth;
        }

        protected virtual void SubscribeProgress(Internal.ProgressDelegate progressDelegate) { }

        partial void ProgressInternal(Action<float> onProgress)
        {
            if (_state == DeferredState.Rejected || _state == DeferredState.Canceled)
            {
                // Don't report progress if the promise is canceled or rejected.
                return;
            }

            Internal.ProgressDelegate progressDelegate = Internal.ProgressDelegate.GetOrCreate(onProgress, _waitDepthAndProgress.WholePart + 1u);
            Promise promise = this;
            // If previous is not null, the promise is guaranteed to be still pending, and it's not the start of the promise chain, so we can simply add to the listeners.
            while (promise._previous != null)
            {
                promise.SubscribeProgress(progressDelegate);
                promise = promise._previous;
            }

            switch (promise._state)
            {
                case DeferredState.Resolved:
                    {
                        // Report if resolved.
                        progressDelegate.Increment(_waitDepthAndProgress.GetIncrementedWholeTruncated().ToUInt32());
                        break;
                    }
                case DeferredState.Pending:
                    {
                        // Subscribe and report if pending.
                        promise.SubscribeProgress(progressDelegate);
                        progressDelegate.Increment(_waitDepthAndProgress.ToUInt32());
                        break;
                    }
                    // Else do nothing. At this point, the progress delegate is guaranteed to be subscribed to at least 1 promise, and/or already reported.
            }
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

                public uint WholePart { get { return _value >> Config.ProgressDecimalBits; } }
                public float DecimalPart { get { return (float)DecimalPartAsUInt32 / (float)DecimalMax; } }
                private uint DecimalPartAsUInt32 { get { return _value & DecimalMask; } }

                public uint ToUInt32()
                {
                    return _value;
                }

                public uint AssignNewDecimalPartAndGetDifferenceAsUInt32(float decimalPart)
                {
                    uint oldDecimalPart = DecimalPartAsUInt32;
                    // Don't bother rounding, it's more expensive and we don't want to accidentally round to 1.0.
                    uint newDecimalPart = (uint)(decimalPart * DecimalMax);
                    _value = (_value & WholeMask) | newDecimalPart;
                    return newDecimalPart - oldDecimalPart;
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
            }

            public class ProgressDelegate : IDisposable
            {
                private Action<float> _onProgress;
                private UnsignedFixed32 _current;
                private uint _expected;

                private static ValueLinkedStackZeroGC<ProgressDelegate> _pool;

                static ProgressDelegate()
                {
                    OnClearPool += () => _pool.ClearAndDontRepool();
                }

                private ProgressDelegate() { }

                public static ProgressDelegate GetOrCreate(Action<float> onProgress, uint expected)
                {
                    var progress = _pool.IsNotEmpty ? _pool.Pop() : new ProgressDelegate();
                    progress._onProgress = onProgress;
                    progress._expected = expected;
                    progress._current = default(UnsignedFixed32);
                    return progress;
                }

                // TODO: Invoke this async
                public void Invoke()
                {
                    if (_current.WholePart == _expected)
                    {
                        var temp = _onProgress;
                        Dispose();
                        temp.Invoke(1f);
                    }
                    else
                    {
                        // Calculate the normalized progress for the depth that the listener was added.
                        // Divide twice is slower, but gives better precision than single divide.
                        _onProgress.Invoke(((float)_current.WholePart / _expected) + (_current.DecimalPart / _expected));
                    }
                }

                public void Increment(uint amount)
                {
                    _current.Increment(amount);
                    // TODO: add to async invoke list
                }

                public void Dispose()
                {
                    _onProgress = null;
                    _pool.Push(this);
                }
            }

            partial class PromiseWaitPromise<TPromise>
            {
                private ValueLinkedStackZeroGC<ProgressDelegate> _progressListeners;

                // This is used to prevent adding progress listeners to the entire chain of a promise when progress isn't even listened for on this promise.
                private Promise _cachedPromise;

                // So that a new delegate doesn't need to be created every time.
                private readonly Action<float> _reportProgress;

                protected PromiseWaitPromise()
                {
                    _reportProgress = ReportProgress;
                }

                protected override void SubscribeProgress(ProgressDelegate progressDelegate)
                {
                    _progressListeners.Push(progressDelegate);
                }

                protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
                {
                    _waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
                }

                protected override sealed void ReportProgress(float progress)
                {
                    if (progress >= 1f)
                    {
                        // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                        return;
                    }

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress);
                    foreach (var pl in _progressListeners)
                    {
                        pl.Increment(increment);
                    }
                }

                protected override void _SubscribeProgress(Promise other)
                {
                    if (_progressListeners.IsEmpty)
                    {
                        _cachedPromise = other;
                    }
                    else
                    {
                        other.Progress(_reportProgress);
                    }
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _cachedPromise = null;
                    _progressListeners.Clear();
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    _cachedPromise = null;
                    _progressListeners.Clear();
                }
            }

            partial class PromiseWaitPromise<T, TPromise>
            {
                private ValueLinkedStackZeroGC<ProgressDelegate> _progressListeners;

                // This is used to prevent adding progress listeners to the entire chain of a promise when progress isn't even listened for on this promise.
                private Promise _cachedPromise;

                // So that a new delegate doesn't need to be created every time.
                private readonly Action<float> _reportProgress;

                protected PromiseWaitPromise()
                {
                    _reportProgress = ReportProgress;
                }

                protected override void SubscribeProgress(ProgressDelegate progressDelegate)
                {
                    _progressListeners.Push(progressDelegate);
                }

                protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
                {
                    _waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
                }

                protected override sealed void ReportProgress(float progress)
                {
                    if (progress >= 1f)
                    {
                        // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                        return;
                    }

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress);
                    foreach (var pl in _progressListeners)
                    {
                        pl.Increment(increment);
                    }
                }

                protected override void _SubscribeProgress(Promise other)
                {
                    if (_progressListeners.IsEmpty)
                    {
                        _cachedPromise = other;
                    }
                    else
                    {
                        other.Progress(_reportProgress);
                    }
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _cachedPromise = null;
                    _progressListeners.Clear();
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    _cachedPromise = null;
                    _progressListeners.Clear();
                }
            }

            partial class PromiseWaitDeferred<TPromise>
            {
                private ValueLinkedStackZeroGC<ProgressDelegate> _progressListeners;

                protected override void SubscribeProgress(ProgressDelegate progressDelegate)
                {
                    _progressListeners.Push(progressDelegate);
                }

                protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
                {
                    _waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
                }

                protected override sealed void ReportProgress(float progress)
                {
                    if (progress >= 1f)
                    {
                        // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                        return;
                    }

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress);
                    foreach (var pl in _progressListeners)
                    {
                        pl.Increment(increment);
                    }
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _progressListeners.Clear();
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    _progressListeners.Clear();
                }
            }

            partial class PromiseWaitDeferred<T, TPromise>
            {
                private ValueLinkedStackZeroGC<ProgressDelegate> _progressListeners;

                protected override void SubscribeProgress(ProgressDelegate progressDelegate)
                {
                    _progressListeners.Push(progressDelegate);
                }

                protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
                {
                    _waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
                }

                protected override sealed void ReportProgress(float progress)
                {
                    if (progress >= 1f)
                    {
                        // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                        return;
                    }

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress);
                    foreach (var pl in _progressListeners)
                    {
                        pl.Increment(increment);
                    }
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _progressListeners.Clear();
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    _progressListeners.Clear();
                }
            }
        }
#endif
    }

    partial class Promise<T>
    {
        // Calls to these get compiled away in RELEASE mode
        static partial void ValidateOperation(Promise<T> promise);
        static partial void ValidateArgument(Delegate del, string argName);
        static partial void ValidateProgress(float progress);
#if DEBUG
        static partial void ValidateProgress(float progress)
        {
            ValidateProgressValue(progress);
        }

        static partial void ValidateOperation(Promise<T> promise)
        {
            promise.ValidateNotDisposed();
        }

        static partial void ValidateArgument(Delegate del, string argName)
        {
            ValidateArg(del, argName);
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

        // Calls to this get compiled away when PROGRESS is defined.
        static partial void ValidateProgress();
#if !PROGRESS
        static partial void ValidateProgress()
        {
            ThrowProgressException();
        }
#endif
    }
}
#pragma warning restore IDE0034 // Simplify 'default' expression