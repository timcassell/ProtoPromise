#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0618 // Type or member is obsolete

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

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
                    ExceptionDispatchInfo.Capture(e).Throw();
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
        public static async Promise ToPromise<TAwaiter>(this TAwaiter awaiter)
            where TAwaiter : IAwaiter<TAwaiter>
        {
            await awaiter;
        }

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }
#endif
    } // PromiseYieldExtensions
}