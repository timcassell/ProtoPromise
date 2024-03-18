#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0270 // Use coalesce expression

namespace Proto.Promises
{
    /// <summary>
    /// Provider for progress updates, normalized between 0 and 1 inclusive.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly partial struct Progress
    {
        private readonly Internal.ProgressListener _impl;
        private readonly int _id;

        [MethodImpl(Internal.InlineOption)]
        private Progress(Internal.ProgressListener impl)
        {
            _impl = impl;
            _id = impl.Id;
        }

        private static SynchronizationContext GetInvokeContext(SynchronizationOption invokeOption)
        {
            switch (invokeOption)
            {
                case SynchronizationOption.Synchronous:
                {
                    return null;
                }
                case SynchronizationOption.Foreground:
                {
                    var invokeContext = Promise.Config.ForegroundContext;
                    if (invokeContext == null)
                    {
                        throw new InvalidOperationException(
                            "SynchronizationOption.Foreground was provided, but Promise.Config.ForegroundContext was null. " +
                            "You should set Promise.Config.ForegroundContext at the start of your application (which may be as simple as 'Promise.Config.ForegroundContext = SynchronizationContext.Current;').",
                            Internal.GetFormattedStacktrace(2));
                    }
                    return invokeContext;
                }
                case SynchronizationOption.Background:
                {
                    return Promise.Config.BackgroundContext
                        ?? Internal.BackgroundSynchronizationContextSentinel.s_instance;
                }
            }
            throw new ArgumentException("Unexpected invokeOption: " + invokeOption, nameof(invokeOption));
        }

        /// <summary>
        /// Create a new progress object using the specified handler and invoke options.
        /// </summary>
        /// <param name="handler">A handler to invoke for each reported progress value.</param>
        /// <param name="invokeOption">Indicates on which context <paramref name="handler"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces progress invoke to happen asynchronously. If <paramref name="invokeOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
        /// <param name="cancelationToken">Cancelation token used to stop progress from being reported.</param>
        /// <returns>A new progress object.</returns>
        /// <remarks>Depending on the invoke option, the <paramref name="handler"/> may be invoked concurrently with itself.</remarks>
        public static Progress New(Action<double> handler,
            SynchronizationOption invokeOption = SynchronizationOption.Foreground,
            bool forceAsync = false,
            CancelationToken cancelationToken = default)
        {
            // Quick check to see if the token is already canceled.
            return cancelationToken.IsCancelationRequested
                ? default
                : new Progress(Internal.GetOrCreateProgress(
                    new Internal.DelegateProgress(handler),
                    GetInvokeContext(invokeOption),
                    forceAsync,
                    cancelationToken)
                );
        }

        /// <summary>
        /// Create a new progress object using the specified handler and invoke options, with a capture value.
        /// </summary>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="captureValue">The captured value that will be passed to the handler along with the reported progress.</param>
        /// <param name="handler">A handler to invoke for each reported progress value.</param>
        /// <param name="invokeOption">Indicates on which context <paramref name="handler"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces progress invoke to happen asynchronously. If <paramref name="invokeOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
        /// <param name="cancelationToken">Cancelation token used to stop progress from being reported.</param>
        /// <returns>A new progress object.</returns>
        /// <remarks>Depending on the invoke option, the <paramref name="handler"/> may be invoked concurrently with itself.</remarks>
        public static Progress New<TCapture>(TCapture captureValue, Action<TCapture, double> handler,
            SynchronizationOption invokeOption = SynchronizationOption.Foreground,
            bool forceAsync = false,
            CancelationToken cancelationToken = default)
        {
            // Quick check to see if the token is already canceled.
            return cancelationToken.IsCancelationRequested
                ? default
                : new Progress(Internal.GetOrCreateProgress(
                    new Internal.DelegateCaptureProgress<TCapture>(captureValue, handler),
                    GetInvokeContext(invokeOption),
                    forceAsync,
                    cancelationToken)
                );
        }

        /// <summary>
        /// Create a new progress object using the specified handler and invoke options.
        /// </summary>
        /// <typeparam name="TProgress">The type of the progress handler.</typeparam>
        /// <param name="handler">A handler to invoke for each reported progress value.</param>
        /// <param name="invokeOption">Indicates on which context <paramref name="handler"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces progress invoke to happen asynchronously. If <paramref name="invokeOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
        /// <param name="cancelationToken">Cancelation token used to stop progress from being reported.</param>
        /// <returns>A new progress object.</returns>
        /// <remarks>Depending on the invoke option, the <paramref name="handler"/> may be invoked concurrently with itself.</remarks>

        public static Progress New<TProgress>(TProgress handler,
            SynchronizationOption invokeOption = SynchronizationOption.Foreground,
            bool forceAsync = false,
            CancelationToken cancelationToken = default)
            where TProgress : IProgress<double>
        {
            // Quick check to see if the token is already canceled.
            return cancelationToken.IsCancelationRequested
                ? default
                : new Progress(Internal.GetOrCreateProgress(
                    handler,
                    GetInvokeContext(invokeOption),
                    forceAsync,
                    cancelationToken)
                );
        }

        /// <summary>
        /// Create a new progress object using the specified handler and invoke options.
        /// </summary>
        /// <param name="handler">A handler to invoke for each reported progress value.</param>
        /// <param name="invokeContext">The context on which the handler will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces progress invoke to happen asynchronously.</param>
        /// <param name="cancelationToken">Cancelation token used to stop progress from being reported.</param>
        /// <returns>A new progress object.</returns>
        /// <remarks>Depending on the invoke context, the <paramref name="handler"/> may be invoked concurrently with itself.</remarks>
        public static Progress New(Action<double> handler,
            SynchronizationContext invokeContext,
            bool forceAsync = false,
            CancelationToken cancelationToken = default)
        {
            // Quick check to see if the token is already canceled.
            return cancelationToken.IsCancelationRequested
                ? default
                : new Progress(Internal.GetOrCreateProgress(
                    new Internal.DelegateProgress(handler),
                    invokeContext ?? Internal.BackgroundSynchronizationContextSentinel.s_instance,
                    forceAsync,
                    cancelationToken)
                );
        }

        /// <summary>
        /// Create a new progress object using the specified handler and invoke options, with a capture value.
        /// </summary>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="captureValue">The captured value that will be passed to the handler along with the reported progress.</param>
        /// <param name="handler">A handler to invoke for each reported progress value.</param>
        /// <param name="invokeContext">The context on which the handler will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces progress invoke to happen asynchronously.</param>
        /// <param name="cancelationToken">Cancelation token used to stop progress from being reported.</param>
        /// <returns>A new progress object.</returns>
        /// <remarks>Depending on the invoke context, the <paramref name="handler"/> may be invoked concurrently with itself.</remarks>
        public static Progress New<TCapture>(TCapture captureValue, Action<TCapture, double> handler,
            SynchronizationContext invokeContext,
            bool forceAsync = false,
            CancelationToken cancelationToken = default)
        {
            // Quick check to see if the token is already canceled.
            return cancelationToken.IsCancelationRequested
                ? default
                : new Progress(Internal.GetOrCreateProgress(
                    new Internal.DelegateCaptureProgress<TCapture>(captureValue, handler),
                    invokeContext ?? Internal.BackgroundSynchronizationContextSentinel.s_instance,
                    forceAsync,
                    cancelationToken)
                );
        }

        /// <summary>
        /// Create a new progress object using the specified handler and invoke options.
        /// </summary>
        /// <typeparam name="TProgress">The type of the progress handler.</typeparam>
        /// <param name="handler">A handler to invoke for each reported progress value.</param>
        /// <param name="invokeContext">The context on which the handler will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces progress invoke to happen asynchronously.</param>
        /// <param name="cancelationToken">Cancelation token used to stop progress from being reported.</param>
        /// <returns>A new progress object.</returns>
        /// <remarks>Depending on the invoke context, the <paramref name="handler"/> may be invoked concurrently with itself.</remarks>
        public static Progress New<TProgress>(TProgress handler,
            SynchronizationContext invokeContext,
            bool forceAsync = false,
            CancelationToken cancelationToken = default)
            where TProgress : IProgress<double>
        {
            // Quick check to see if the token is already canceled.
            return cancelationToken.IsCancelationRequested
                ? default
                : new Progress(Internal.GetOrCreateProgress(
                    handler,
                    invokeContext ?? Internal.BackgroundSynchronizationContextSentinel.s_instance,
                    forceAsync,
                    cancelationToken)
                );
        }

        /// <summary>
        /// Get the <see cref="ProgressToken"/> that is used to report progress to the progress handler.
        /// </summary>
        public ProgressToken Token
        {
            [MethodImpl(Internal.InlineOption)]
            get { return new ProgressToken(_impl, _id, 0d, 1d); }
        }

        /// <summary>
        /// Dispose this, and wait for the handler to complete invoking.
        /// </summary>
        /// <returns>A promise that will be resolved when the handler has completed invoking.</returns>
        public Promise DisposeAsync()
        {
            var impl = _impl;
            return impl != null ? impl.DisposeAsync(_id) : Promise.Resolved();
        }

        /// <summary>
        /// Create a new progress race builder that produces progress tokens that will race to report their progress
        /// to the target progress token, with only the largest progress being reported.
        /// </summary>
        /// <param name="target">The progress token that the raced progress will be reported to.</param>
        /// <returns>A new progress race builder.</returns>
        public static RaceBuilder NewRaceBuilder(ProgressToken target)
        {
            return RaceBuilder.New(target);
        }

        /// <summary>
        /// Create a new progress merge builder that produces progress tokens, combining and normalizing their total progress, reported to the target progress token.
        /// </summary>
        /// <param name="target">The progress token that the merged progress will be reported to.</param>
        /// <returns>A new progress merge builder.</returns>
        public static MergeBuilder NewMergeBuilder(ProgressToken target)
        {
            return MergeBuilder.New(target);
        }

        /// <summary>
        /// Create a new progress handler that can report to multiple progress tokens.
        /// </summary>
        /// <remarks>
        /// This can be useful when combined with <see cref="Promise.Preserve"/> for reporting progress to multiple consumers from a single async operation.
        /// </remarks>
        /// <returns>A new progress multi handler.</returns>
        public static MultiHandler NewMultiHandler()
        {
            return MultiHandler.New();
        }

        /// <summary>
        /// Progress builder that produces progress tokens that will race to report their progress, with only the largest progress being reported.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly struct RaceBuilder : IDisposable
        {
            private readonly Internal.ProgressRacer _impl;
            private readonly int _id;

            [MethodImpl(Internal.InlineOption)]
            private RaceBuilder(Internal.ProgressRacer impl)
            {
                _impl = impl;
                _id = impl.Id;
            }

            /// <summary>
            /// Create a new progress race builder that produces progress tokens that will race to report their progress
            /// to the target progress token, with only the largest progress being reported.
            /// </summary>
            /// <param name="target">The progress token that the raced progress will be reported to.</param>
            /// <returns>A new progress race builder.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static RaceBuilder New(ProgressToken target)
            {
                var impl = target._impl;
                return impl == null || impl.Id != target._id
                    ? default
                    : new RaceBuilder(Internal.ProgressRacer.GetOrCreate(target));
            }

            /// <summary>
            /// Get a new progress token that will have its progress reports raced against other tokens generated from this builder.
            /// </summary>
            /// <returns>A new progress token.</returns>
            /// <exception cref="ObjectDisposedException">This instance was already disposed.</exception>
            public ProgressToken NewToken()
                => _impl?.NewToken(_id) ?? default;

            /// <summary>
            /// Stop generating new progress tokens, and release this instance.
            /// </summary>
            /// <exception cref="ObjectDisposedException">This instance was already disposed.</exception>
            public void Dispose()
                => _impl?.Dispose(_id);
        }

        /// <summary>
        /// Progress merge builder that produces progress tokens, combining and normalizing their total progress.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly struct MergeBuilder : IDisposable
        {
            private readonly Internal.ProgressMerger _impl;
            private readonly int _id;

            [MethodImpl(Internal.InlineOption)]
            private MergeBuilder(Internal.ProgressMerger impl)
            {
                _impl = impl;
                _id = impl.Id;
            }

            /// <summary>
            /// Create a new progress merge builder that produces progress tokens, combining and normalizing their total progress, reported to the target progress token.
            /// </summary>
            /// <param name="target">The progress token that the merged progress will be reported to.</param>
            /// <returns>A new progress merge builder.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static MergeBuilder New(ProgressToken target)
            {
                var impl = target._impl;
                return impl == null || impl.Id != target._id
                    ? default
                    : new MergeBuilder(Internal.ProgressMerger.GetOrCreate(target));
            }

            /// <summary>
            /// Get a new progress token that will have its progress reports merged and normalized with other tokens generated from this builder.
            /// </summary>
            /// <param name="weight">How much weight this token uses for the merged progress.</param>
            /// <returns>A new progress token.</returns>
            /// <exception cref="ObjectDisposedException">This instance was already disposed.</exception>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="weight"/> is not greater than 0.</exception>
            /// <exception cref="ArithmeticException">The addition of <paramref name="weight"/> is too small or too large to calculate.</exception>
            public ProgressToken NewToken(double weight = 1d)
            {
                // This catches nan, which `weight <= 0` does not.
                if (!(weight > 0))
                {
                    throw new ArgumentOutOfRangeException(nameof(weight), "weight must be greater than 0", Internal.GetFormattedStacktrace(1));
                }
                var impl = _impl;
                return impl != null ? impl.NewToken(weight, _id) : default;
            }

            /// <summary>
            /// Stop generating new progress tokens, and release this instance.
            /// </summary>
            /// <exception cref="ObjectDisposedException">This instance was already disposed.</exception>
            public void Dispose()
                => _impl?.Dispose(_id);
        }

        /// <summary>
        /// Progress handler that can report to multiple progress tokens.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly struct MultiHandler : IDisposable
        {
            private readonly Internal.ProgressMultiHandler _impl;
            private readonly int _id;

            [MethodImpl(Internal.InlineOption)]
            private MultiHandler(Internal.ProgressMultiHandler impl)
            {
                _impl = impl;
                _id = impl.Id;
            }

            /// <summary>
            /// Create a new progress handler that can report to multiple progress tokens.
            /// </summary>
            /// <remarks>
            /// This can be useful when combined with <see cref="Promise.Preserve"/> for reporting progress to multiple consumers from a single async operation.
            /// </remarks>
            /// <returns>A new progress multi handler.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static MultiHandler New()
                => new MultiHandler(Internal.ProgressMultiHandler.GetOrCreate());

            /// <summary>
            /// Get the <see cref="ProgressToken"/> that is used to report progress to the attached progress tokens.
            /// </summary>
            public ProgressToken Token
            {
                [MethodImpl(Internal.InlineOption)]
                get => new ProgressToken(_impl, _id, 0d, 1d);
            }

            /// <summary>
            /// Add a progress token that will have its progress reported when this instance's <see cref="Token"/> is reported.
            /// </summary>
            /// <exception cref="ObjectDisposedException">This instance was already disposed.</exception>
            public void Add(ProgressToken progressToken)
                => _impl?.Add(progressToken, _id);

            /// <summary>
            /// Release this instance.
            /// </summary>
            /// <exception cref="ObjectDisposedException">This instance was already disposed.</exception>
            public void Dispose()
                => _impl?.Dispose(_id);
        }
    } // struct Progress

#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
    partial struct Progress : IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask IAsyncDisposable.DisposeAsync()
            => DisposeAsync();
    }
#endif // UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER

    /// <summary>
    /// Propagates progress updates.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct ProgressToken : IProgress<double>
    {
        internal readonly Internal.ProgressBase _impl;
        internal readonly int _id;
        internal readonly double _minValue;
        internal readonly double _maxValue;

        /// <summary>
        /// Returns an empty <see cref="ProgressToken"/> with no listener.
        /// </summary>
        public static ProgressToken None
        {
            [MethodImpl(Internal.InlineOption)]
            get => default;
        }

        internal ProgressToken(Internal.ProgressBase impl, int id, double minValue, double maxValue)
        {
            _impl = impl;
            _id = id;
            _minValue = minValue;
            _maxValue = maxValue;
        }

        /// <summary>
        /// Gets whether this progress token has an associated progress listener.
        /// </summary>
        /// <remarks>
        /// If this does not have a listener, calls to <see cref="Report(double)"/> do nothing.
        /// </remarks>
        public bool HasListener => _impl?.Id == _id;

        /// <summary>
        /// Report a progress update between 0 and 1 inclusive.
        /// </summary>
        /// <param name="value">The value of the updated progress.</param>
        /// <remarks>
        /// If this does not have a listener, calling this does nothing.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="value"/> is not between 0 and 1 inclusive.</exception>
        public void Report(double value)
        {
            Internal.ValidateProgressValue(value, nameof(value), 1);

            var impl = _impl;
            if (impl == null)
            {
                return;
            }
            impl.Report(Internal.Lerp(_minValue, _maxValue, value), _id);
        }

        /// <summary>
        /// Get a new progress token that reports progress to this token, lerped between <paramref name="minValue"/> and <paramref name="maxValue"/>.
        /// Both values must be between 0 and 1 inclusive.
        /// </summary>
        /// <remarks>
        /// <paramref name="minValue"/> is not verified to be smaller than <paramref name="maxValue"/>.
        /// If <paramref name="minValue"/> is larger than <paramref name="maxValue"/>, progress will be reported in reverse order.
        /// </remarks>
        /// <param name="minValue">The minimum value of the lerp.</param>
        /// <param name="maxValue">The maximum value of the lerp.</param>
        /// <returns>A new progress token using the lerp values.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> or <paramref name="maxValue"/> are not between 0 and 1 inclusive.</exception>
        public ProgressToken Slice(double minValue, double maxValue)
        {
            Internal.ValidateProgressValue(minValue, nameof(minValue), 1);
            Internal.ValidateProgressValue(maxValue, nameof(maxValue), 1);

            return new ProgressToken(
                _impl,
                _id,
                Internal.Lerp(_minValue, _maxValue, minValue),
                Internal.Lerp(_minValue, _maxValue, maxValue)
            );
        }
    } // struct ProgressToken
}