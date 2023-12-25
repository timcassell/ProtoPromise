#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0618 // Type or member is obsolete

using Proto.Promises.Async.CompilerServices;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Proto.Promises
{
    /// <summary>
    /// Interface used to await a condition.
    /// </summary>
    public interface IAwaitInstruction
    {
        /// <summary>
        /// Continues the async function when it returns true.
        /// </summary>
        bool IsCompleted();
    }

    /// <summary>
    /// Interface used to await a condition with progress.
    /// </summary>
    [Obsolete(Internal.ProgressObsoleteMessage, false), EditorBrowsable(EditorBrowsableState.Never)]
    public interface IAwaitWithProgressInstruction
    {
        /// <summary>
        /// Continues the async function when it returns true. Progress may be reported.
        /// </summary>
        bool IsCompleted(out float progress);
    }

    /// <summary>
    /// Awaiter extensions facilitating the `await` keyword on await instructions.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static partial class PromiseYieldExtensions
    {
        /// <summary>
        /// Helper interface intended for internal use.
        /// </summary>
        public interface IAwaiter<TAwaiter> : ICriticalNotifyCompletion
            where TAwaiter : IAwaiter<TAwaiter>
        {
            /// <summary>
            /// Returns self.
            /// </summary>
            TAwaiter GetAwaiter();
            /// <summary>
            /// Gets whether this is complete.
            /// </summary>
            bool IsCompleted { get; }
            /// <summary>
            /// Completes the await.
            /// </summary>
            void GetResult();
        }

        /// <summary>
        /// Awaiter facilitating the `await` keyword on await instructions.
        /// </summary>
        public struct AwaitInstructionAwaiter<TAwaitInstruction> : InternalHelper.IYieldInstruction, IAwaiter<AwaitInstructionAwaiter<TAwaitInstruction>>
            where TAwaitInstruction : IAwaitInstruction
        {
            private static Exception s_exception;
            private static bool s_isCanceled;

            // This must not be readonly.
            private TAwaitInstruction _awaitInstruction;
            private Action _continuation;
            private CancelationToken _cancelationToken;
            private bool _isTokenRetained;

            /// <summary>
            /// Creates a new awaiter wrapping the instruction.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public AwaitInstructionAwaiter(TAwaitInstruction awaitInstruction)
            {
                _awaitInstruction = awaitInstruction;
                _cancelationToken = default(CancelationToken);
                _continuation = null;
                _isTokenRetained = false;
            }

            /// <summary>
            /// Creates a new awaiter wrapping the <paramref name="awaitInstruction"/>, with cancelation.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public AwaitInstructionAwaiter(TAwaitInstruction awaitInstruction, CancelationToken cancelationToken)
            {
                _awaitInstruction = awaitInstruction;
                _cancelationToken = cancelationToken;
                _continuation = null;
                _isTokenRetained = false;
            }

            /// <summary>
            /// Returns a duplicate awaiter with cancelation.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public AwaitInstructionAwaiter<TAwaitInstruction> WithCancelation(CancelationToken cancelationToken)
            {
                return new AwaitInstructionAwaiter<TAwaitInstruction>(_awaitInstruction, cancelationToken);
            }

            /// <summary>
            /// Gets whether this is complete.
            /// </summary>
            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    // Quick check to see if the token is already canceled.
                    bool isCanceled = _cancelationToken.IsCancelationRequested;
                    if (isCanceled)
                    {
                        s_isCanceled = isCanceled;
                        return true;
                    }
                    // If the token is not already canceled, we evaluate the instruction to see if it should complete immediately.
                    return _awaitInstruction.IsCompleted();
                }
            }

            /// <summary>
            /// Returns self.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public AwaitInstructionAwaiter<TAwaitInstruction> GetAwaiter()
            {
                return this;
            }

            /// <summary>
            /// Schedules the continuation.
            /// </summary>
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, "continuation", 1);
                InternalHelper.ValidateIsOnMainThread(1);
                var copy = this;
                copy._continuation = continuation;
                InternalHelper.PromiseBehaviour.Instance._updateProcessor.WaitFor(ref copy);
            }

            /// <summary>
            /// Schedules the continuation.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }

            /// <summary>
            /// Completes the await.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void GetResult()
            {
                if (s_isCanceled)
                {
                    s_isCanceled = false;
                    throw Promise.CancelException();
                }

                var e = s_exception;
                if (e != null)
                {
                    s_exception = null;
#if NET_LEGACY
                    throw Promise.RejectException(e); // Wrap the exception and throw to preserve the stacktrace.
#elif UNITY_2021_2_OR_NEWER
                    ExceptionDispatchInfo.Throw(e);
#else
                    ExceptionDispatchInfo.Capture(e).Throw();
#endif
                }
            }

            [MethodImpl(Internal.InlineOption)]
            bool InternalHelper.IYieldInstruction.Evaluate()
            {
                try
                {
                    if (_cancelationToken.IsCancelationRequested)
                    {
                        // Store canceled state in static field, it will be read to throw in GetResult when the continuation callback is invoked.
                        s_isCanceled = true;
                        if (_isTokenRetained)
                        {
                            _cancelationToken.Release();
                        }
                        _continuation.Invoke();
                        return true;
                    }

                    if (!_awaitInstruction.IsCompleted())
                    {
                        return false;
                    }

                    if (_isTokenRetained)
                    {
                        _cancelationToken.Release();
                    }
                    _continuation.Invoke();
                    return true;
                }
                catch (Exception e)
                {
                    // store exception in static field, it will be thrown in GetResult when the continuation callback is invoked.
                    s_exception = e;
                    if (_isTokenRetained)
                    {
                        _cancelationToken.Release();
                    }
                    _continuation.Invoke();
                    return true;
                }
            }

            [MethodImpl(Internal.InlineOption)]
            void InternalHelper.IYieldInstruction.MaybeRetainCancelationToken()
            {
                // This is called after it's placed in the array, so the field will stick.
                _isTokenRetained = _cancelationToken.TryRetain();
            }
        }

        /// <summary>
        /// Gets an awaiter wrapping the <paramref name="awaitInstruction"/>.
        /// </summary>
        public static AwaitInstructionAwaiter<TAwaitInstruction> GetAwaiter<TAwaitInstruction>(this TAwaitInstruction awaitInstruction)
            where TAwaitInstruction : IAwaitInstruction
        {
            return new AwaitInstructionAwaiter<TAwaitInstruction>(awaitInstruction);
        }

        /// <summary>
        /// Gets an awaiter wrapping the <paramref name="awaitInstruction"/>, with cancelation.
        /// </summary>
        public static AwaitInstructionAwaiter<TAwaitInstruction> WithCancelation<TAwaitInstruction>(this TAwaitInstruction awaitInstruction, CancelationToken cancelationToken)
            where TAwaitInstruction : IAwaitInstruction
        {
            return new AwaitInstructionAwaiter<TAwaitInstruction>(awaitInstruction, cancelationToken);
        }

        /// <summary>
        /// Converts the <paramref name="awaitInstruction"/> to a <see cref="Promise"/>.
        /// </summary>
        public static Promise ToPromise<TAwaitInstruction>(this TAwaitInstruction awaitInstruction, CancelationToken cancelationToken = default(CancelationToken))
            where TAwaitInstruction : IAwaitInstruction
        {
            return new AwaitInstructionAwaiter<TAwaitInstruction>(awaitInstruction, cancelationToken).ToPromise();
        }

        /// <summary>
        /// Converts the <paramref name="awaiter"/> to a <see cref="Promise"/>.
        /// </summary>
#if CSHARP_7_3_OR_NEWER
        public static async Promise ToPromise<TAwaiter>(this TAwaiter awaiter)
            where TAwaiter : IAwaiter<TAwaiter>
        {
            await awaiter;
        }
#else // CSHARP_7_3_OR_NEWER
        public static Promise ToPromise<TAwaiter>(this TAwaiter awaiter)
            where TAwaiter : IAwaiter<TAwaiter>
        {
            __d__0<TAwaiter> stateMachine = default(__d__0<TAwaiter>);
            stateMachine.__t__builder = PromiseMethodBuilder.Create();
            stateMachine.awaiter = awaiter;
            stateMachine.__1__state = -1;
            stateMachine.__t__builder.Start(ref stateMachine);
            return stateMachine.__t__builder.Task;
        }

        // Manually creating the state machine that `await awaiter` would generate, because old C# versions don't support async/await.

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        [StructLayout(LayoutKind.Auto)]
        [CompilerGenerated]
        private struct __d__0<TAwaiter> : IAsyncStateMachine where TAwaiter : IAwaiter<TAwaiter>
        {
            public int __1__state;

            public PromiseMethodBuilder __t__builder;

            public TAwaiter awaiter;

            private TAwaiter __u__1;

            private void MoveNext()
            {
                int num = __1__state;
                try
                {
                    TAwaiter val;
                    if (num != 0)
                    {
                        val = awaiter.GetAwaiter();
                        if (!val.IsCompleted)
                        {
                            num = (__1__state = 0);
                            __u__1 = val;
                            __t__builder.AwaitUnsafeOnCompleted(ref val, ref this);
                            return;
                        }
                    }
                    else
                    {
                        val = __u__1;
                        __u__1 = default(TAwaiter);
                        num = (__1__state = -1);
                    }
                    val.GetResult();
                }
                catch (Exception exception)
                {
                    __1__state = -2;
                    __t__builder.SetException(exception);
                    return;
                }
                __1__state = -2;
                __t__builder.SetResult();
            }

            void IAsyncStateMachine.MoveNext()
            {
                //ILSpy generated this explicit interface implementation from .override directive in MoveNext
                this.MoveNext();
            }

            [DebuggerHidden]
            private void SetStateMachine(IAsyncStateMachine stateMachine)
            {
                __t__builder.SetStateMachine(stateMachine);
            }

            void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
            {
                //ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
                this.SetStateMachine(stateMachine);
            }
        }
#endif // !CSHARP_7_3_OR_NEWER

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }
#endif
    } // PromiseYieldExtensions

    /// <summary>
    /// Awaiter extensions facilitating the `await` keyword on await instructions.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static partial class PromiseYieldWithProgressExtensions
    {
        private struct AwaitWithProgressInstruction<TAwaitInstruction> : InternalHelper.IYieldInstruction
            where TAwaitInstruction : IAwaitWithProgressInstruction
        {
            // This must not be readonly.
            private TAwaitInstruction _awaitInstruction;
            private Promise.Deferred _deferred;
            private CancelationToken.Retainer _cancelationTokenRetainer;

            [MethodImpl(Internal.InlineOption)]
            internal static Promise Process(TAwaitInstruction awaitInstruction, CancelationToken cancelationToken)
            {
                InternalHelper.ValidateIsOnMainThread(1);

                // Quick check to see if the token is already canceled.
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise.Canceled();
                }

                // If the token is not already canceled, we evaluate the instruction to see if it should complete immediately.
                float _;
                if (awaitInstruction.IsCompleted(out _))
                {
                    return Promise.Resolved();
                }

                var instruction = new AwaitWithProgressInstruction<TAwaitInstruction>(awaitInstruction, cancelationToken);
                InternalHelper.PromiseBehaviour.Instance._updateProcessor.WaitFor(ref instruction);
                return instruction._deferred.Promise;
            }

            [MethodImpl(Internal.InlineOption)]
            internal AwaitWithProgressInstruction(TAwaitInstruction awaitInstruction, CancelationToken cancelationToken)
            {
                _awaitInstruction = awaitInstruction;
                _cancelationTokenRetainer = cancelationToken.GetRetainer();
                _deferred = Promise.NewDeferred();
            }

            [MethodImpl(Internal.InlineOption)]
            bool InternalHelper.IYieldInstruction.Evaluate()
            {
                try
                {
                    if (_cancelationTokenRetainer.token.IsCancelationRequested)
                    {
                        _cancelationTokenRetainer.Dispose();
                        _deferred.Cancel();
                        return true;
                    }

                    float progress;
                    if (!_awaitInstruction.IsCompleted(out progress))
                    {
                        _deferred.TryReportProgress(progress);
                        return false;
                    }

                    _cancelationTokenRetainer.Dispose();
                    _deferred.Resolve();
                    return true;
                }
                catch (OperationCanceledException)
                {
                    _cancelationTokenRetainer.Dispose();
                    _deferred.Cancel();
                    return true;
                }
                catch (Exception e)
                {
                    _cancelationTokenRetainer.Dispose();
                    _deferred.Reject(e);
                    return true;
                }
            }

            [MethodImpl(Internal.InlineOption)]
            void InternalHelper.IYieldInstruction.MaybeRetainCancelationToken()
            {
                // Do nothing, we already retained the token.
            }
        }

        /// <summary>
        /// Awaiter facilitating the `await` keyword on await instructions.
        /// </summary>
        // This is used to optimize awaits that ignore the progress.
        public struct AwaitInstructionAwaiter<TAwaitInstruction> : InternalHelper.IYieldInstruction, PromiseYieldExtensions.IAwaiter<AwaitInstructionAwaiter<TAwaitInstruction>>
            where TAwaitInstruction : IAwaitWithProgressInstruction
        {
            private static Exception s_exception;
            private static bool s_isCanceled;

            // This must not be readonly.
            private TAwaitInstruction _awaitInstruction;
            private Action _continuation;
            private CancelationToken _cancelationToken;
            private bool _isTokenRetained;

            /// <summary>
            /// Creates a new awaiter wrapping the <paramref name="awaitInstruction"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public AwaitInstructionAwaiter(TAwaitInstruction awaitInstruction)
            {
                _awaitInstruction = awaitInstruction;
                _cancelationToken = default(CancelationToken);
                _continuation = null;
                _isTokenRetained = false;
            }

            /// <summary>
            /// Creates a new awaiter wrapping the <paramref name="awaitInstruction"/>, with cancelation.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public AwaitInstructionAwaiter(TAwaitInstruction awaitInstruction, CancelationToken cancelationToken)
            {
                _awaitInstruction = awaitInstruction;
                _cancelationToken = cancelationToken;
                _continuation = null;
                _isTokenRetained = false;
            }

            /// <summary>
            /// Returns a duplicate awaiter with cancelation.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public AwaitInstructionAwaiter<TAwaitInstruction> WithCancelation(CancelationToken cancelationToken)
            {
                return new AwaitInstructionAwaiter<TAwaitInstruction>(_awaitInstruction, cancelationToken);
            }

            /// <summary>
            /// Gets whether this is complete.
            /// </summary>
            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    // Quick check to see if the token is already canceled.
                    bool isCanceled = _cancelationToken.IsCancelationRequested;
                    if (isCanceled)
                    {
                        s_isCanceled = isCanceled;
                        return true;
                    }
                    // If the token is not already canceled, we evaluate the instruction to see if it should complete immediately.
                    float _;
                    return _awaitInstruction.IsCompleted(out _);
                }
            }

            /// <summary>
            /// Returns self.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public AwaitInstructionAwaiter<TAwaitInstruction> GetAwaiter()
            {
                return this;
            }

            /// <summary>
            /// Schedules the continuation.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, "continuation", 1);
                InternalHelper.ValidateIsOnMainThread(1);
                var copy = this;
                copy._continuation = continuation;
                InternalHelper.PromiseBehaviour.Instance._updateProcessor.WaitFor(ref copy);
            }

            /// <summary>
            /// Schedules the continuation.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }

            /// <summary>
            /// Completes the await.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void GetResult()
            {
                if (s_isCanceled)
                {
                    s_isCanceled = false;
                    throw Promise.CancelException();
                }

                var e = s_exception;
                if (e != null)
                {
                    s_exception = null;
#if NET_LEGACY
                    throw Promise.RejectException(e); // Wrap the exception and throw to preserve the stacktrace.
#elif UNITY_2021_2_OR_NEWER
                    ExceptionDispatchInfo.Throw(e);
#else
                    ExceptionDispatchInfo.Capture(e).Throw();
#endif
                }
            }

            [MethodImpl(Internal.InlineOption)]
            bool InternalHelper.IYieldInstruction.Evaluate()
            {
                try
                {
                    if (_cancelationToken.IsCancelationRequested)
                    {
                        // Store canceled state in static field, it will be read to throw in GetResult when the continuation callback is invoked.
                        s_isCanceled = true;
                        if (_isTokenRetained)
                        {
                            _cancelationToken.Release();
                        }
                        _continuation.Invoke();
                        return true;
                    }

                    float _;
                    if (!_awaitInstruction.IsCompleted(out _))
                    {
                        return false;
                    }

                    if (_isTokenRetained)
                    {
                        _cancelationToken.Release();
                    }
                    _continuation.Invoke();
                    return true;
                }
                catch (Exception e)
                {
                    // store exception in static field, it will be thrown in GetResult when the continuation callback is invoked.
                    s_exception = e;
                    if (_isTokenRetained)
                    {
                        _cancelationToken.Release();
                    }
                    _continuation.Invoke();
                    return true;
                }
            }

            [MethodImpl(Internal.InlineOption)]
            void InternalHelper.IYieldInstruction.MaybeRetainCancelationToken()
            {
                // This is called after it's placed in the array, so the field will stick.
                _isTokenRetained = _cancelationToken.TryRetain();
            }
        }

        /// <summary>
        /// Gets an awaiter wrapping the <paramref name="awaitInstruction"/>.
        /// </summary>
        public static AwaitInstructionAwaiter<TAwaitInstruction> GetAwaiter<TAwaitInstruction>(this TAwaitInstruction awaitInstruction)
            where TAwaitInstruction : IAwaitWithProgressInstruction
        {
            return new AwaitInstructionAwaiter<TAwaitInstruction>(awaitInstruction);
        }

        /// <summary>
        /// Gets an awaiter wrapping the <paramref name="awaitInstruction"/>, with cancelation.
        /// </summary>
        public static AwaitInstructionAwaiter<TAwaitInstruction> WithCancelation<TAwaitInstruction>(this TAwaitInstruction awaitInstruction, CancelationToken cancelationToken)
            where TAwaitInstruction : IAwaitWithProgressInstruction
        {
            return new AwaitInstructionAwaiter<TAwaitInstruction>(awaitInstruction, cancelationToken);
        }

        /// <summary>
        /// Gets an awaiter wrapping the <paramref name="awaitInstruction"/>, with progress reported to the `async Promise` function, optionally with cancelation.
        /// </summary>
        [Obsolete(Internal.ProgressObsoleteMessage, false), EditorBrowsable(EditorBrowsableState.Never)]
        public static PromiseProgressAwaiterVoid AwaitWithProgress<TAwaitInstruction>(this TAwaitInstruction awaitInstruction, float minProgress, float maxprogress, CancelationToken cancelationToken = default(CancelationToken))
            where TAwaitInstruction : IAwaitWithProgressInstruction
        {
            return ToPromise(awaitInstruction, cancelationToken).AwaitWithProgress(minProgress, maxprogress);
        }

        /// <summary>
        /// Gets an awaiter wrapping the <paramref name="awaitInstruction"/>, with progress reported to the `async Promise` function, optionally with cancelation.
        /// </summary>
        [Obsolete(Internal.ProgressObsoleteMessage, false), EditorBrowsable(EditorBrowsableState.Never)]
        public static PromiseProgressAwaiterVoid AwaitWithProgress<TAwaitInstruction>(this TAwaitInstruction awaitInstruction, float maxprogress, CancelationToken cancelationToken = default(CancelationToken))
            where TAwaitInstruction : IAwaitWithProgressInstruction
        {
            return ToPromise(awaitInstruction, cancelationToken).AwaitWithProgress(maxprogress);
        }

        /// <summary>
        /// Converts the <paramref name="awaitInstruction"/> to a <see cref="Promise"/>.
        /// </summary>
        public static Promise ToPromise<TAwaitInstruction>(this TAwaitInstruction awaitInstruction, CancelationToken cancelationToken = default(CancelationToken))
            where TAwaitInstruction : IAwaitWithProgressInstruction
        {
            return AwaitWithProgressInstruction<TAwaitInstruction>.Process(awaitInstruction, cancelationToken);
        }

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }
#endif
    } // PromiseYieldWithProgressExtensions
}