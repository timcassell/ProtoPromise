#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0251 // Make member 'readonly'

using Proto.Promises.CompilerServices;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Proto.Promises
{
    /// <summary>
    /// Helper class containing methods for awaitable common Unity yield instructions and events.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static partial class PromiseYielder
    {
        /// <summary>
        /// Initializes the components and configuration necessary for operations to execute on Unity's main thread.
        /// </summary>
        /// <remarks>
        /// This usually does not need to be called directly. Unity calls it automatically in a <see cref="RuntimeInitializeOnLoadMethodAttribute"/> function.
        /// However, the order of execution is not guaranteed, so if you have code that also runs in a <see cref="RuntimeInitializeOnLoadMethodAttribute"/> function
        /// that requires the initialization to be done, you may call this.
        /// <para/>
        /// This will be a no-op if called more than once.
        /// </remarks>
        public static void Initialize()
            => InternalHelper.PromiseBehaviour.Initialize();

        /// <summary>
        /// Runs a <see cref="Coroutine"/> that yields the <paramref name="yieldInstruction"/>, and
        /// returns a <see cref="Promise"/> that will resolve after the <paramref name="yieldInstruction"/> has completed.
        /// </summary>
        /// <param name="yieldInstruction">The yield instruction to wait for.</param>
        /// <param name="runner">The <see cref="MonoBehaviour"/> instance on which the <see cref="Coroutine"/> will be ran.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to stop the <see cref="Coroutine"/> and cancel the promise.</param>
        /// <remarks>
        /// If <paramref name="runner"/> is provided, the <see cref="Coroutine"/> will be ran on it, otherwise it will be ran on singleton instance.
        /// </remarks>
        public static Promise WaitFor(object yieldInstruction, MonoBehaviour runner = null, CancelationToken cancelationToken = default)
            => InternalHelper.YieldInstructionRunner.WaitForInstruction(yieldInstruction, runner, cancelationToken);

        /// <summary>
        /// Returns a <see cref="Instructions.WaitOneFrameAwaiter"/> that will complete after 1 frame.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitOneFrameAwaiter WaitOneFrame()
            => new Instructions.WaitOneFrameAwaiter();

        /// <summary>
        /// Returns a <see cref="Instructions.WaitFramesInstruction"/> that will complete after the specified number of frames have passed.
        /// </summary>
        /// <param name="frames">How many frames to wait for.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitFramesInstruction WaitForFrames(uint frames)
            => new Instructions.WaitFramesInstruction(frames);

        /// <summary>
        /// Returns a <see cref="Instructions.WaitFramesWithProgressInstruction"/> that will complete after the specified number of frames have passed, while reporting progress.
        /// </summary>
        /// <param name="frames">How many frames to wait for.</param>
        /// <param name="progressToken">The progress token that will have progress reported to it.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitFramesWithProgressInstruction WaitForFrames(uint frames, ProgressToken progressToken)
            => new Instructions.WaitFramesWithProgressInstruction(frames, progressToken);

        /// <summary>
        /// Returns a <see cref="Instructions.WaitTimeInstruction"/> that will complete after the specified timespan has passed, using scaled time.
        /// </summary>
        /// <param name="time">How much time to wait for.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitTimeInstruction WaitForTime(TimeSpan time)
            => new Instructions.WaitTimeInstruction(time);

        /// <summary>
        /// Returns a <see cref="Instructions.WaitTimeWithProgressInstruction"/> that will complete after the specified timespan has passed, using scaled time, while reporting progress.
        /// </summary>
        /// <param name="time">How much time to wait for.</param>
        /// <param name="progressToken">The progress token that will have progress reported to it.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitTimeWithProgressInstruction WaitForTime(TimeSpan time, ProgressToken progressToken)
            => new Instructions.WaitTimeWithProgressInstruction(time, progressToken);

        /// <summary>
        /// Returns a <see cref="Instructions.WaitRealTimeInstruction"/> that will complete after the specified timespan has passed, using unscaled, real time.
        /// </summary>
        /// <param name="time">How much time to wait for.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitRealTimeInstruction WaitForRealTime(TimeSpan time)
            => new Instructions.WaitRealTimeInstruction(time);

        /// <summary>
        /// Returns a <see cref="Instructions.WaitRealTimeWithProgressInstruction"/> that will complete after the specified timespan has passed, using unscaled, real time, while reporting progress.
        /// </summary>
        /// <param name="time">How much time to wait for.</param>
        /// <param name="progressToken">The progress token that will have progress reported to it.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitRealTimeWithProgressInstruction WaitForRealTime(TimeSpan time, ProgressToken progressToken)
            => new Instructions.WaitRealTimeWithProgressInstruction(time, progressToken);

        /// <summary>
        /// Returns a <see cref="Instructions.WaitUntilInstruction"/> that will complete when the supplied delegate returns true.
        /// </summary>
        /// <param name="predicate">The function that will be ran to determine if the wait should complete.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitUntilInstruction WaitUntil(Func<bool> predicate)
            => new Instructions.WaitUntilInstruction(predicate);

        /// <summary>
        /// Returns a <see cref="Instructions.WaitUntilInstruction{T}"/> that will complete when the supplied delegate returns true.
        /// </summary>
        /// <param name="captureValue">The value that will be passed to the delegate.</param>
        /// <param name="predicate">The function that will be ran to determine if the wait should complete.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitUntilInstruction<TCapture> WaitUntil<TCapture>(TCapture captureValue, Func<TCapture, bool> predicate)
            => new Instructions.WaitUntilInstruction<TCapture>(captureValue, predicate);

        /// <summary>
        /// Returns a <see cref="Instructions.WaitWhileInstruction"/> that will complete when the supplied delegate returns false.
        /// </summary>
        /// <param name="predicate">The function that will be ran to determine if the wait should complete.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitWhileInstruction WaitWhile(Func<bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);
            return new Instructions.WaitWhileInstruction(predicate);
        }

        /// <summary>
        /// Returns a <see cref="Instructions.WaitWhileInstruction{T}"/> that will complete when the supplied delegate returns false.
        /// </summary>
        /// <param name="captureValue">The value that will be passed to the delegate.</param>
        /// <param name="predicate">The function that will be ran to determine if the wait should complete.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitWhileInstruction<TCapture> WaitWhile<TCapture>(TCapture captureValue, Func<TCapture, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);
            return new Instructions.WaitWhileInstruction<TCapture>(captureValue, predicate);
        }

        /// <summary>
        /// Returns a <see cref="Instructions.WaitAsyncOperationInstruction"/> that will complete when the <paramref name="asyncOperation"/> is complete.
        /// </summary>
        /// <param name="asyncOperation">The async operation to wait for.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitAsyncOperationInstruction WaitForAsyncOperation(AsyncOperation asyncOperation)
            => new Instructions.WaitAsyncOperationInstruction(asyncOperation);

        /// <summary>
        /// Returns a <see cref="Instructions.WaitAsyncOperationWithProgressInstruction"/> that will complete when the <paramref name="asyncOperation"/> is complete, while reporting progress.
        /// </summary>
        /// <param name="asyncOperation">The async operation to wait for.</param>
        /// <param name="progressToken">The progress token that will have progress reported to it.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitAsyncOperationWithProgressInstruction WaitForAsyncOperation(AsyncOperation asyncOperation, ProgressToken progressToken)
            => new Instructions.WaitAsyncOperationWithProgressInstruction(asyncOperation, progressToken);

        /// <summary>
        /// Returns a <see cref="Instructions.WaitForUpdateAwaiter"/> that will complete at the next Update.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitForUpdateAwaiter WaitForUpdate()
            => new Instructions.WaitForUpdateAwaiter();

        /// <summary>
        /// Returns a <see cref="Instructions.WaitForLateUpdateAwaiter"/> that will complete at the next LateUpdate.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitForLateUpdateAwaiter WaitForLateUpdate()
            => new Instructions.WaitForLateUpdateAwaiter();

        /// <summary>
        /// Returns a <see cref="Instructions.WaitForFixedUpdateAwaiter"/> that will complete at the next FixedUpdate.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitForFixedUpdateAwaiter WaitForFixedUpdate()
            => new Instructions.WaitForFixedUpdateAwaiter();

        /// <summary>
        /// Returns a <see cref="Instructions.WaitForEndOfFrameAwaiter"/> that will complete at the next EndOfFrame.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Instructions.WaitForEndOfFrameAwaiter WaitForEndOfFrame()
            => new Instructions.WaitForEndOfFrameAwaiter();

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            => Internal.ValidateArgument(arg, argName, skipFrames + 1);
#endif

        /// <summary>
        /// Contains instructions returned by <see cref="PromiseYielder"/> functions.
        /// </summary>
        public static class Instructions
        {
            /// <summary>
            /// Awaiter used to wait for a single frame to pass.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public readonly struct WaitOneFrameAwaiter : PromiseYieldExtensions.IAwaiter<WaitOneFrameAwaiter>
            {
                /// <summary>Gets the awaiter for this.</summary>
                /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
                /// <returns>this</returns>
                [MethodImpl(Internal.InlineOption)]
                public WaitOneFrameAwaiter GetAwaiter() => this;

                /// <summary>Gets whether the operation is complete.</summary>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                /// <returns>false</returns>
                public bool IsCompleted
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => false;
                }

                /// <summary>Called after the operation has completed.</summary>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                // Do nothing.
                public void GetResult() { }

                /// <summary>Schedules the continuation.</summary>
                /// <param name="continuation">The action to invoke when the operation completes.</param>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                public void OnCompleted(Action continuation)
                {
                    ValidateArgument(continuation, nameof(continuation), 1);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE // We don't need to validate in RELEASE because WaitForNext calls Time.frameCount which already has validation.
                    InternalHelper.ValidateIsOnMainThread(1);
#endif
                    InternalHelper.PromiseBehaviour.s_waitOneFrameProcessor.WaitForNext(continuation);
                }

                /// <summary>Schedules the continuation onto the <see cref="Promise"/> associated with this <see cref="PromiseAwaiterVoid"/>.</summary>
                /// <param name="continuation">The action to invoke when the await operation completes.</param>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                public void UnsafeOnCompleted(Action continuation)
                    => OnCompleted(continuation);
            }

            /// <summary>
            /// Await instruction used to wait a number of frames.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public struct WaitFramesInstruction : IAwaitInstruction
            {
                private readonly uint _target;
                private uint _current;

                /// <summary>
                /// Gets a new <see cref="WaitFramesInstruction"/>.
                /// </summary>
                public WaitFramesInstruction(uint frames)
                {
                    _target = frames;
                    _current = uint.MaxValue; // This is evaluated immediately, so set it to max to wrap back around to zero on the first call.
                }

                [MethodImpl(Internal.InlineOption)]
                bool IAwaitInstruction.IsCompleted()
                {
                    unchecked
                    {
                        ++_current;
                        return _current == _target;
                    }
                }
            }

            /// <summary>
            /// Await instruction used to wait a number of frames, while reporting progress.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public struct WaitFramesWithProgressInstruction : IAwaitInstruction
            {
                private readonly ProgressToken _progressToken;
                private readonly uint _target;
                private uint _current;

                /// <summary>
                /// Gets a new <see cref="WaitFramesWithProgressInstruction"/>.
                /// </summary>
                public WaitFramesWithProgressInstruction(uint frames, ProgressToken progressToken)
                {
                    _progressToken = progressToken;
                    _target = frames;
                    _current = 0;
                }

                [MethodImpl(Internal.InlineOption)]
                bool IAwaitInstruction.IsCompleted()
                {
                    unchecked
                    {
                        // _target could be zero, which would result in NaN progress if we divided it, so we have to check for it first.
                        if (_current == _target)
                        {
                            _progressToken.Report(1d);
                            return true;
                        }
                        _progressToken.Report((double) _current / _target);
                        ++_current;
                        return false;
                    }
                }

                /// <summary>
                /// Converts this to a <see cref="Promise"/>.
                /// </summary>
                public Promise ToPromise(CancelationToken cancelationToken = default)
                    => _progressToken.HasListener
                        ? PromiseYieldExtensions.ToPromise(this, cancelationToken)
                        : PromiseYieldExtensions.ToPromise(new WaitFramesInstruction(_target), cancelationToken);
            }

            /// <summary>
            /// Await instruction used to wait an amount of time, scaled to the game clock.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public struct WaitTimeInstruction : IAwaitInstruction
            {
                private double _current;
                private byte _multiplier;

                /// <summary>
                /// Gets a new <see cref="WaitTimeInstruction"/>.
                /// </summary>
                public WaitTimeInstruction(TimeSpan time)
                {
                    _current = time.TotalSeconds;
                    // Set the initial multiplier to 0, so when this is invoked immediately, it won't increment the time.
                    _multiplier = 0;
                }

                [MethodImpl(Internal.InlineOption)]
                bool IAwaitInstruction.IsCompleted()
                {
                    unchecked
                    {
                        // Multiplier is 0 on the first call, 1 on all future calls.
                        _current -= InternalHelper.PromiseBehaviour.s_deltaTime * _multiplier;
                        _multiplier = 1;
                        return _current <= 0;
                    }
                }
            }

            /// <summary>
            /// Await instruction used to wait an amount of time, scaled to the game clock, while reporting progress.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public struct WaitTimeWithProgressInstruction : IAwaitInstruction
            {
                private readonly ProgressToken _progressToken;
                private readonly double _target;
                private double _current;
                private float _multiplier;

                /// <summary>
                /// Gets a new <see cref="WaitTimeWithProgressInstruction"/>.
                /// </summary>
                public WaitTimeWithProgressInstruction(TimeSpan time, ProgressToken progressToken)
                {
                    _progressToken = progressToken;
                    _target = time.TotalSeconds;
                    _current = 0d;
                    // Set the initial multiplier to 0, so when this is invoked immediately, it won't increment the time.
                    _multiplier = 0f;
                }

                [MethodImpl(Internal.InlineOption)]
                bool IAwaitInstruction.IsCompleted()
                {
                    unchecked
                    {
                        // Multiplier is 0 on the first call, 1 on all future calls.
                        _current += InternalHelper.PromiseBehaviour.s_deltaTime * _multiplier;
                        // _target could be <= zero, which would result in NaN or +/-Infinity progress if we divided it, so we have to check for it first.
                        if (_current >= _target)
                        {
                            _progressToken.Report(1d);
                            return true;
                        }
                        _multiplier = 1f;
                        _progressToken.Report(_current / _target);
                        return false;
                    }
                }

                /// <summary>
                /// Converts this to a <see cref="Promise"/>.
                /// </summary>
                [MethodImpl(Internal.InlineOption)]
                public Promise ToPromise(CancelationToken cancelationToken = default)
                    => _progressToken.HasListener
                        ? PromiseYieldExtensions.ToPromise(this, cancelationToken)
                        : PromiseYieldExtensions.ToPromise(new WaitTimeInstruction(TimeSpan.FromSeconds(_target)), cancelationToken);
            }

            /// <summary>
            /// Await instruction used to wait an amount of time, using unscaled, real time.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public readonly struct WaitRealTimeInstruction : IAwaitInstruction
            {
                private readonly TimeSpan _target;
                private readonly Internal.ValueStopwatch _stopwatch;

                /// <summary>
                /// Gets a new <see cref="WaitRealTimeInstruction"/>.
                /// </summary>
                public WaitRealTimeInstruction(TimeSpan time)
                {
                    _target = time;
                    _stopwatch = Internal.ValueStopwatch.StartNew();
                }

                [MethodImpl(Internal.InlineOption)]
                bool IAwaitInstruction.IsCompleted()
                {
                    unchecked
                    {
                        return _stopwatch.GetElapsedTime() >= _target;
                    }
                }
            }

            /// <summary>
            /// Await instruction used to wait an amount of time, using unscaled, real time.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public readonly struct WaitRealTimeWithProgressInstruction : IAwaitInstruction
            {
                private readonly ProgressToken _progressToken;
                private readonly TimeSpan _target;
                private readonly Internal.ValueStopwatch _stopwatch;

                /// <summary>
                /// Gets a new <see cref="WaitRealTimeInstruction"/>.
                /// </summary>
                public WaitRealTimeWithProgressInstruction(TimeSpan time, ProgressToken progressToken)
                {
                    _progressToken = progressToken;
                    _target = time;
                    _stopwatch = Internal.ValueStopwatch.StartNew();
                }

                [MethodImpl(Internal.InlineOption)]
                bool IAwaitInstruction.IsCompleted()
                {
                    unchecked
                    {
                        var current = _stopwatch.GetElapsedTime();
                        // _target could be <= zero, which would result in NaN or +/-Infinity progress if we divided it, so we have to check for it first.
                        if (current >= _target)
                        {
                            _progressToken.Report(1d);
                            return true;
                        }
                        _progressToken.Report((double) current.Ticks / _target.Ticks);
                        return false;
                    }
                }

                /// <summary>
                /// Converts this to a <see cref="Promise"/>.
                /// </summary>
                [MethodImpl(Internal.InlineOption)]
                public Promise ToPromise(CancelationToken cancelationToken = default)
                    => _progressToken.HasListener
                        ? PromiseYieldExtensions.ToPromise(this, cancelationToken)
                        : PromiseYieldExtensions.ToPromise(new WaitRealTimeInstruction(_target), cancelationToken);
            }

            /// <summary>
            /// Await instruction used to wait until a condition is true.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public readonly struct WaitUntilInstruction : IAwaitInstruction
            {
                private readonly Func<bool> _predicate;

                /// <summary>
                /// Gets a new <see cref="WaitUntilInstruction"/>.
                /// </summary>
                public WaitUntilInstruction(Func<bool> predicate)
                {
                    ValidateArgument(predicate, nameof(predicate), 1);
                    _predicate = predicate;
                }

                [MethodImpl(Internal.InlineOption)]
                bool IAwaitInstruction.IsCompleted()
                    => _predicate.Invoke();
            }

            /// <summary>
            /// Await instruction used to wait until a condition is true.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public readonly struct WaitUntilInstruction<TCapture> : IAwaitInstruction
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, bool> _predicate;

                /// <summary>
                /// Gets a new <see cref="WaitUntilInstruction{T}"/>.
                /// </summary>
                public WaitUntilInstruction(TCapture captureValue, Func<TCapture, bool> predicate)
                {
                    ValidateArgument(predicate, nameof(predicate), 1);
                    _capturedValue = captureValue;
                    _predicate = predicate;
                }

                [MethodImpl(Internal.InlineOption)]
                bool IAwaitInstruction.IsCompleted()
                    => _predicate.Invoke(_capturedValue);
            }

            /// <summary>
            /// Await instruction used to wait while a condition is true.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public readonly struct WaitWhileInstruction : IAwaitInstruction
            {
                private readonly Func<bool> _predicate;

                /// <summary>
                /// Gets a new <see cref="WaitWhileInstruction"/>.
                /// </summary>
                public WaitWhileInstruction(Func<bool> predicate)
                {
                    ValidateArgument(predicate, nameof(predicate), 1);
                    _predicate = predicate;
                }

                [MethodImpl(Internal.InlineOption)]
                bool IAwaitInstruction.IsCompleted()
                    => !_predicate.Invoke();
            }

            /// <summary>
            /// Await instruction used to wait while a condition is true.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public readonly struct WaitWhileInstruction<TCapture> : IAwaitInstruction
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, bool> _predicate;

                /// <summary>
                /// Gets a new <see cref="WaitWhileInstruction{T}"/>.
                /// </summary>
                public WaitWhileInstruction(TCapture captureValue, Func<TCapture, bool> predicate)
                {
                    ValidateArgument(predicate, nameof(predicate), 1);
                    _capturedValue = captureValue;
                    _predicate = predicate;
                }

                [MethodImpl(Internal.InlineOption)]
                bool IAwaitInstruction.IsCompleted()
                    => !_predicate.Invoke(_capturedValue);
            }

            /// <summary>
            /// Await instruction used to wait for an <see cref="AsyncOperation"/>.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public readonly struct WaitAsyncOperationInstruction : IAwaitInstruction
            {
                private readonly AsyncOperation _asyncOperation;

                /// <summary>
                /// Gets a new <see cref="WaitAsyncOperationInstruction"/>.
                /// </summary>
                public WaitAsyncOperationInstruction(AsyncOperation asyncOperation)
                {
                    _asyncOperation = asyncOperation;
                }

                [MethodImpl(Internal.InlineOption)]
                bool IAwaitInstruction.IsCompleted()
                    => _asyncOperation.isDone;
            }

            /// <summary>
            /// Await instruction used to wait for an <see cref="AsyncOperation"/>.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public readonly struct WaitAsyncOperationWithProgressInstruction : IAwaitInstruction
            {
                private readonly ProgressToken _progressToken;
                private readonly AsyncOperation _asyncOperation;

                /// <summary>
                /// Gets a new <see cref="WaitAsyncOperationInstruction"/>.
                /// </summary>
                public WaitAsyncOperationWithProgressInstruction(AsyncOperation asyncOperation, ProgressToken progressToken)
                {
                    _progressToken = progressToken;
                    _asyncOperation = asyncOperation;
                }

                [MethodImpl(Internal.InlineOption)]
                bool IAwaitInstruction.IsCompleted()
                {
                    _progressToken.Report(_asyncOperation.progress);
                    return _asyncOperation.isDone;
                }

                /// <summary>
                /// Converts this to a <see cref="Promise"/>.
                /// </summary>
                [MethodImpl(Internal.InlineOption)]
                public Promise ToPromise(CancelationToken cancelationToken = default)
                    => _progressToken.HasListener
                        ? PromiseYieldExtensions.ToPromise(this, cancelationToken)
                        : PromiseYieldExtensions.ToPromise(new WaitAsyncOperationInstruction(_asyncOperation), cancelationToken);
            }

            /// <summary>
            /// Awaiter used to wait for the next Update.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public readonly struct WaitForUpdateAwaiter : PromiseYieldExtensions.IAwaiter<WaitForUpdateAwaiter>
            {
                /// <summary>Gets the awaiter for this.</summary>
                /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
                /// <returns>this</returns>
                [MethodImpl(Internal.InlineOption)]
                public WaitForUpdateAwaiter GetAwaiter() => this;

                /// <summary>Gets whether the operation is complete.</summary>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                /// <returns>false</returns>
                public bool IsCompleted
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => false;
                }

                /// <summary>Called after the operation has completed.</summary>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                // Do nothing.
                public void GetResult() { }

                /// <summary>Schedules the continuation.</summary>
                /// <param name="continuation">The action to invoke when the operation completes.</param>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                public void OnCompleted(Action continuation)
                {
                    ValidateArgument(continuation, nameof(continuation), 1);
                    InternalHelper.ValidateIsOnMainThread(1);
                    InternalHelper.PromiseBehaviour.s_updateProcessor.WaitForNext(continuation);
                }

                /// <summary>Schedules the continuation onto the <see cref="Promise"/> associated with this <see cref="PromiseAwaiterVoid"/>.</summary>
                /// <param name="continuation">The action to invoke when the await operation completes.</param>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                public void UnsafeOnCompleted(Action continuation)
                    => OnCompleted(continuation);
            }

            /// <summary>
            /// Awaiter used to wait for the next LateUpdate.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public readonly struct WaitForLateUpdateAwaiter : PromiseYieldExtensions.IAwaiter<WaitForLateUpdateAwaiter>
            {
                /// <summary>Gets the awaiter for this.</summary>
                /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
                /// <returns>this</returns>
                [MethodImpl(Internal.InlineOption)]
                public WaitForLateUpdateAwaiter GetAwaiter() => this;

                /// <summary>Gets whether the operation is complete.</summary>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                /// <returns>false</returns>
                public bool IsCompleted
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => false;
                }

                /// <summary>Called after the operation has completed.</summary>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                // Do nothing.
                public void GetResult() { }

                /// <summary>Schedules the continuation.</summary>
                /// <param name="continuation">The action to invoke when the operation completes.</param>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                public void OnCompleted(Action continuation)
                {
                    ValidateArgument(continuation, nameof(continuation), 1);
                    InternalHelper.ValidateIsOnMainThread(1);
                    InternalHelper.PromiseBehaviour.s_lateUpdateProcessor.WaitForNext(continuation);
                }

                /// <summary>Schedules the continuation onto the <see cref="Promise"/> associated with this <see cref="PromiseAwaiterVoid"/>.</summary>
                /// <param name="continuation">The action to invoke when the await operation completes.</param>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                public void UnsafeOnCompleted(Action continuation)
                    => OnCompleted(continuation);
            }

            /// <summary>
            /// Awaiter used to wait for the next FixedUpdate.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public readonly struct WaitForFixedUpdateAwaiter : PromiseYieldExtensions.IAwaiter<WaitForFixedUpdateAwaiter>
            {
                /// <summary>Gets the awaiter for this.</summary>
                /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
                /// <returns>this</returns>
                [MethodImpl(Internal.InlineOption)]
                public WaitForFixedUpdateAwaiter GetAwaiter() => this;

                /// <summary>Gets whether the operation is complete.</summary>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                /// <returns>false</returns>
                public bool IsCompleted
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => false;
                }

                /// <summary>Called after the operation has completed.</summary>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                // Do nothing.
                public void GetResult() { }

                /// <summary>Schedules the continuation.</summary>
                /// <param name="continuation">The action to invoke when the operation completes.</param>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                public void OnCompleted(Action continuation)
                {
                    ValidateArgument(continuation, nameof(continuation), 1);
                    InternalHelper.ValidateIsOnMainThread(1);
                    InternalHelper.PromiseBehaviour.s_fixedUpdateProcessor.WaitForNext(continuation);
                }

                /// <summary>Schedules the continuation onto the <see cref="Promise"/> associated with this <see cref="PromiseAwaiterVoid"/>.</summary>
                /// <param name="continuation">The action to invoke when the await operation completes.</param>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                public void UnsafeOnCompleted(Action continuation)
                    => OnCompleted(continuation);
            }

            /// <summary>
            /// Awaiter used to wait for the next EndOfFrame.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public struct WaitForEndOfFrameAwaiter : PromiseYieldExtensions.IAwaiter<WaitForEndOfFrameAwaiter>
            {
                /// <summary>Gets the awaiter for this.</summary>
                /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
                /// <returns>this</returns>
                [MethodImpl(Internal.InlineOption)]
                public WaitForEndOfFrameAwaiter GetAwaiter() => this;

                /// <summary>Gets whether the operation is complete.</summary>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                /// <returns>false</returns>
                public bool IsCompleted
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => false;
                }

                /// <summary>Called after the operation has completed.</summary>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                // Do nothing.
                public void GetResult() { }

                /// <summary>Schedules the continuation.</summary>
                /// <param name="continuation">The action to invoke when the operation completes.</param>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                public void OnCompleted(Action continuation)
                {
                    ValidateArgument(continuation, nameof(continuation), 1);
                    InternalHelper.ValidateIsOnMainThread(1);
                    InternalHelper.PromiseBehaviour.s_endOfFrameProcessor.WaitForNext(continuation);
                }

                /// <summary>Schedules the continuation onto the <see cref="Promise"/> associated with this <see cref="PromiseAwaiterVoid"/>.</summary>
                /// <param name="continuation">The action to invoke when the await operation completes.</param>
                /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
                [MethodImpl(Internal.InlineOption)]
                public void UnsafeOnCompleted(Action continuation)
                    => OnCompleted(continuation);
            }
        }
    } // class PromiseYielder
}