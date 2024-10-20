#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0270 // Use coalesce expression

namespace Proto.Promises
{
    /// <summary>
    /// Specifies the context on which promise continuations should be executed.
    /// </summary>
    public enum SynchronizationOption : byte
    {
        /// <summary>
        /// The continuation will be executed synchronously.
        /// </summary>
        Synchronous,
        /// <summary>
        /// The continuation will be executed on the <see cref="Promise.Config.ForegroundContext"/>.
        /// </summary>
        Foreground,
        /// <summary>
        /// The continuation will be executed on the <see cref="Promise.Config.BackgroundContext"/>.
        /// </summary>
        Background,
        /// <summary>
        /// The continuation will be executed on the captured context.
        /// </summary>
        CapturedContext
    }

    /// <summary>
    /// Specifies the continuation execution behavior to use when a promise is already complete.
    /// </summary>
    public enum CompletedContinuationBehavior
    {
        /// <summary>
        /// The continuation will be executed synchronously if the provided context is the same as the current context. Otherwise, the continuation will be executed asynchronously.
        /// </summary>
        SynchronousIfSameContext,
        /// <summary>
        /// The continuation will be executed synchronously.
        /// </summary>
        Synchronous,
        /// <summary>
        /// The continuation will be executed asynchronously.
        /// </summary>
        Asynchronous
    }

    /// <summary>
    /// Provides options that control the behavior of promise continuation execution.
    /// </summary>
    public readonly struct ContinuationOptions
    {
        // Matches SynchronizationOption, plus an extra Explicit option.
        private enum Option : byte
        {
            Synchronous,
            Foreground,
            Background,
            CapturedContext,
            Explicit
        }

        private readonly SynchronizationContext _continuationContext;
        private readonly Option _option;
        private readonly CompletedContinuationBehavior _completedBehavior;

        /// <summary>
        /// Returns an instance of <see cref="ContinuationOptions"/> that configures continuations to execute synchronously.
        /// </summary>
        public static ContinuationOptions Synchronous
        {
            [MethodImpl(Internal.InlineOption)]
            get => new ContinuationOptions(SynchronizationOption.Synchronous, CompletedContinuationBehavior.Synchronous);
        }

        /// <summary>
        /// Returns an instance of <see cref="ContinuationOptions"/> that configures continuations to execute on the <see cref="Promise.Config.ForegroundContext"/>.
        /// </summary>
        public static ContinuationOptions Foreground
        {
            [MethodImpl(Internal.InlineOption)]
            get => new ContinuationOptions(SynchronizationOption.Foreground, CompletedContinuationBehavior.SynchronousIfSameContext);
        }

        /// <summary>
        /// Returns an instance of <see cref="ContinuationOptions"/> that configures continuations to execute on the <see cref="Promise.Config.BackgroundContext"/>.
        /// </summary>
        public static ContinuationOptions Background
        {
            [MethodImpl(Internal.InlineOption)]
            get => new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.SynchronousIfSameContext);
        }

        /// <summary>
        /// Returns an instance of <see cref="ContinuationOptions"/> that configures continuations to execute on the captured context, or synchronously if the promise is already complete.
        /// </summary>
        public static ContinuationOptions CapturedContext
        {
            [MethodImpl(Internal.InlineOption)]
            get => new ContinuationOptions(SynchronizationOption.CapturedContext, CompletedContinuationBehavior.Synchronous);
        }

        internal bool IsSynchronous
        {
            [MethodImpl(Internal.InlineOption)]
            get => _option == Option.Synchronous;
        }

        internal CompletedContinuationBehavior CompletedBehavior
        {
            [MethodImpl(Internal.InlineOption)]
            get => _completedBehavior;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ContinuationOptions"/> using the provided <paramref name="continuationOption"/>.
        /// </summary>
        /// <param name="continuationOption">The option used to configure the context that the continuation will be executed on.</param>
        [MethodImpl(Internal.InlineOption)]
        public ContinuationOptions(SynchronizationOption continuationOption)
        {
            _continuationContext = null;
            _option = (Option) continuationOption;
            _completedBehavior = continuationOption == SynchronizationOption.Synchronous || continuationOption == SynchronizationOption.CapturedContext
                ? CompletedContinuationBehavior.Synchronous
                : CompletedContinuationBehavior.SynchronousIfSameContext;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ContinuationOptions"/> using the provided <paramref name="continuationContext"/>.
        /// </summary>
        /// <param name="continuationContext">The context that the continuation will be executed on. If <see langword="null"/>, it will be queued to the <see cref="ThreadPool"/>.</param>
        [MethodImpl(Internal.InlineOption)]
        public ContinuationOptions(SynchronizationContext continuationContext)
        {
            _continuationContext = continuationContext;
            _option = Option.Explicit;
            _completedBehavior = CompletedContinuationBehavior.SynchronousIfSameContext;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ContinuationOptions"/> using the provided <paramref name="continuationOption"/> and <paramref name="completedContinuationBehavior"/>.
        /// </summary>
        /// <param name="continuationOption">The option used to configure the context that the continuation will be executed on.</param>
        /// <param name="completedContinuationBehavior">
        /// The option used to configure the continuation behavior when the promise is already complete.
        /// If <paramref name="continuationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.
        /// </param>
        [MethodImpl(Internal.InlineOption)]
        public ContinuationOptions(SynchronizationOption continuationOption, CompletedContinuationBehavior completedContinuationBehavior)
        {
            _continuationContext = null;
            _option = (Option) continuationOption;
            _completedBehavior = completedContinuationBehavior;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ContinuationOptions"/> using the provided <paramref name="continuationContext"/> and <paramref name="completedContinuationBehavior"/>.
        /// </summary>
        /// <param name="continuationContext">The context that the continuation will be executed on. If <see langword="null"/>, it will be queued to the <see cref="ThreadPool"/>.</param>
        /// <param name="completedContinuationBehavior">The option used to configure the continuation behavior when the promise is already complete.</param>
        [MethodImpl(Internal.InlineOption)]
        public ContinuationOptions(SynchronizationContext continuationContext, CompletedContinuationBehavior completedContinuationBehavior)
        {
            _continuationContext = continuationContext;
            _option = Option.Explicit;
            _completedBehavior = completedContinuationBehavior;
        }

        // Constructors to bridge old configure continuation methods.
        [MethodImpl(Internal.InlineOption)]
        internal ContinuationOptions(SynchronizationOption continuationOption, bool forceAsync)
        {
            _continuationContext = null;
            _option = (Option) continuationOption;
            _completedBehavior = forceAsync ? CompletedContinuationBehavior.Asynchronous : CompletedContinuationBehavior.SynchronousIfSameContext;
        }

        [MethodImpl(Internal.InlineOption)]
        internal ContinuationOptions(SynchronizationContext continuationContext, bool forceAsync)
        {
            _continuationContext = continuationContext;
            _option = Option.Explicit;
            _completedBehavior = forceAsync ? CompletedContinuationBehavior.Asynchronous : CompletedContinuationBehavior.SynchronousIfSameContext;
        }

        internal SynchronizationContext GetContinuationContext()
        {
            switch (_option)
            {
                case Option.Foreground:
                {
                    return GetForegroundContext();
                }
                case Option.Background:
                {
                    return Promise.Config.BackgroundContext ?? Internal.BackgroundSynchronizationContextSentinel.s_instance;
                }
                case Option.CapturedContext:
                {
                    return CaptureContext();
                }
                case Option.Explicit:
                {
                    return _continuationContext ?? Internal.BackgroundSynchronizationContextSentinel.s_instance;
                }
            }
            return null;
        }

        internal bool GetShouldContinueImmediately()
        {
            if (_option == Option.Synchronous | _completedBehavior == CompletedContinuationBehavior.Synchronous)
            {
                return true;
            }
            if (_completedBehavior == CompletedContinuationBehavior.Asynchronous)
            {
                return false;
            }
            if (_option == Option.CapturedContext)
            {
                return true;
            }

            var context = _option == Option.Foreground ? GetForegroundContext()
                : _option == Option.Background ? Promise.Config.BackgroundContext ?? Internal.BackgroundSynchronizationContextSentinel.s_instance
                // Explicit
                : _continuationContext ?? Internal.BackgroundSynchronizationContextSentinel.s_instance;
            return context == Promise.Manager.ThreadStaticSynchronizationContext;
        }

        internal bool GetShouldContinueImmediately(out SynchronizationContext context)
        {
            if (_option == Option.Synchronous | _completedBehavior == CompletedContinuationBehavior.Synchronous)
            {
                context = null;
                return true;
            }
            if (_completedBehavior == CompletedContinuationBehavior.Asynchronous)
            {
                context = GetContinuationContext();
                return false;
            }
            if (_option == Option.CapturedContext)
            {
                context = null;
                return true;
            }

            context = _option == Option.Foreground ? GetForegroundContext()
                : _option == Option.Background ? Promise.Config.BackgroundContext ?? Internal.BackgroundSynchronizationContextSentinel.s_instance
                // Explicit
                : _continuationContext ?? Internal.BackgroundSynchronizationContextSentinel.s_instance;
            return context == Promise.Manager.ThreadStaticSynchronizationContext;
        }

        private static SynchronizationContext GetForegroundContext()
        {
            var context = Promise.Config.ForegroundContext;
            if (context == null)
            {
                throw new InvalidOperationException(
                    "SynchronizationOption.Foreground was provided, but Promise.Config.ForegroundContext was null. " +
                    "You should set Promise.Config.ForegroundContext at the start of your application (which may be as simple as 'Promise.Config.ForegroundContext = SynchronizationContext.Current;').",
                    Internal.GetFormattedStacktrace(3));
            }
            return context;
        }

        internal static SynchronizationContext CaptureContext()
        {
            // Capture the current context. If it's null, use the background context.
            return Promise.Manager.ThreadStaticSynchronizationContext
                // TODO: update compilation symbol when Unity adopts .Net Core.
#if !NETCOREAPP
                // Old .Net Framework/Mono includes `SynchronizationContext.Current` in the `ExecutionContext`, so it may not be null on a background thread.
                // We check for that case to not unnecessarily invoke continuations on a foreground thread when they can continue on a background thread.
                ?? (Thread.CurrentThread.IsBackground ? null : SynchronizationContext.Current)
#endif
                ?? Promise.Config.BackgroundContext
                ?? Internal.BackgroundSynchronizationContextSentinel.s_instance;
        }
    }
}