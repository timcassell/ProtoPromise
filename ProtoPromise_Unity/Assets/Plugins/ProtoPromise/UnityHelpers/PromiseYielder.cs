#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable 0618 // Type or member is obsolete

using Proto.Promises.Async.CompilerServices;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Proto.Promises
{
    /// <summary>
    /// Yielder used to wait for a yield instruction to complete in the form of a Promise, using Unity's coroutines.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static partial class PromiseYielder
    {
        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve after the <paramref name="yieldInstruction"/> has completed.
        /// </summary>
        /// <param name="yieldInstruction">The yield instruction to wait for.</param>
        /// <param name="runner">The <see cref="MonoBehaviour"/> instance on which the <paramref name="yieldInstruction"/> will be ran.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to stop the internal wait and cancel the promise.</param>
        /// <remarks>
        /// If <paramref name="runner"/> is provided, the coroutine will be ran on it, otherwise it will be ran on the singleton PromiseYielder instance.
        /// </remarks>
        public static Promise WaitFor(object yieldInstruction, MonoBehaviour runner = null, CancelationToken cancelationToken = default(CancelationToken))
        {
            return InternalHelper.YieldInstructionRunner.WaitForInstruction(yieldInstruction, runner, cancelationToken);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve after 1 frame.
        /// </summary>
        /// <param name="runner">The <see cref="MonoBehaviour"/> instance on which the wait will be ran.</param>
        /// <remarks>
        /// If <paramref name="runner"/> is provided, the coroutine will be ran on it, otherwise it will be ran on the singleton PromiseYielder instance.
        /// </remarks>
        [Obsolete("Prefer to use `await PromiseYielder.WaitOneFrame()`, or use `PromiseYielder.WaitFor(null)` to get the old behaviour.", false), EditorBrowsable(EditorBrowsableState.Never)]
        public static Promise WaitOneFrame(MonoBehaviour runner)
        {
            return runner == null
                ? WaitOneFrame().ToPromise()
                : InternalHelper.YieldInstructionRunner.WaitForInstruction(null, runner, default(CancelationToken));
        }

        /// <summary>
        /// Awaiter used to wait for a single frame to pass.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public struct WaitOneFrameAwaiter : PromiseYieldExtensions.IAwaiter<WaitOneFrameAwaiter>
        {
            /// <summary>Gets the awaiter for this.</summary>
            /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
            /// <returns>this</returns>
            [MethodImpl(Internal.InlineOption)]
            public WaitOneFrameAwaiter GetAwaiter()
            {
                return this;
            }

            /// <summary>Gets whether the operation is complete.</summary>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <returns>false</returns>
            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get { return false; }
            }

            /// <summary>Called after the operation has completed.</summary>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            [MethodImpl(Internal.InlineOption)]
            public void GetResult()
            {
                // Do nothing.
            }

            /// <summary>Schedules the continuation.</summary>
            /// <param name="continuation">The action to invoke when the operation completes.</param>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, "continuation", 1);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                InternalHelper.ValidateIsOnMainThread(1);
#endif
                var behaviour = InternalHelper.PromiseBehaviour.Instance;
                if (Time.frameCount == behaviour._currentFrame)
                {
                    // The update queue already ran this frame, wait for the next.
                    behaviour._oneFrameProcessor.WaitForNext(continuation);
                    return;
                }

                // The update queue has not yet run this frame, so to force it to wait for the next frame
                // (instead of resolving later in the same frame), we wait for 2 frame updates.
                behaviour._oneFrameProcessor.WaitForFollowing(continuation);
            }

            /// <summary>Schedules the continuation onto the <see cref="Promise"/> associated with this <see cref="PromiseAwaiterVoid"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }
        }

        /// <summary>
        /// Returns a <see cref="WaitOneFrameAwaiter"/> that will complete after 1 frame.
        /// </summary>
        public static WaitOneFrameAwaiter WaitOneFrame()
        {
            return new WaitOneFrameAwaiter();
        }

        /// <summary>
        /// Await instruction used to wait a number of frames.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public struct WaitFramesInstruction : IAwaitWithProgressInstruction
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
            bool IAwaitWithProgressInstruction.IsCompleted(out float progress)
            {
                unchecked
                {
                    ++_current;
                    // If the target is 0, progress will be NaN. But it's fine because it won't be reported.
                    progress = (float) ((double) _current / _target);
                    return _current == _target;
                }
            }

#if !CSHARP_7_3_OR_NEWER
            /// <summary>
            /// Converts this to a <see cref="Promise"/>.
            /// </summary>
            // Old C# compiler thinks the .ToPromise() extension methods are ambiguous, so we add it here explicitly.
            [MethodImpl(Internal.InlineOption)]
            public Promise ToPromise(CancelationToken cancelationToken = default(CancelationToken))
            {
                return PromiseYieldWithProgressExtensions.ToPromise(this, cancelationToken);
            }
#endif
        }

        /// <summary>
        /// Returns a <see cref="WaitFramesInstruction"/> that will complete after the specified number of frames have passed.
        /// </summary>
        /// <param name="frames">How many frames to wait for.</param>
        public static WaitFramesInstruction WaitForFrames(uint frames)
        {
            return new WaitFramesInstruction(frames);
        }

        /// <summary>
        /// Await instruction used to wait an amount of time, scaled to the game clock.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public struct WaitTimeInstruction : IAwaitWithProgressInstruction
        {
            private readonly double _target;
            private double _current;
            private float _multiplier;

            /// <summary>
            /// Gets a new <see cref="WaitTimeInstruction"/>.
            /// </summary>
            public WaitTimeInstruction(TimeSpan time)
            {
                _target = time.TotalSeconds;
                _current = 0d;
                // Set the initial multiplier to 0, so when this is invoked immediately, it won't increment the time.
                _multiplier = 0f;
            }

            [MethodImpl(Internal.InlineOption)]
            bool IAwaitWithProgressInstruction.IsCompleted(out float progress)
            {
                unchecked
                {
                    // Multiplier is 0 on the first call, 1 on all future calls.
                    _current += InternalHelper.PromiseBehaviour.Instance._deltaTime * _multiplier;
                    _multiplier = 1f;
                    // If the target is <= 0, progress will be NaN or +/-Infinity. But it's fine because it won't be reported.
                    progress = (float) (_current / _target);
                    return _current >= _target;
                }
            }

#if !CSHARP_7_3_OR_NEWER
            /// <summary>
            /// Converts this to a <see cref="Promise"/>.
            /// </summary>
            // Old C# compiler thinks the .ToPromise() extension methods are ambiguous, so we add it here explicitly.
            [MethodImpl(Internal.InlineOption)]
            public Promise ToPromise(CancelationToken cancelationToken = default(CancelationToken))
            {
                return PromiseYieldWithProgressExtensions.ToPromise(this, cancelationToken);
            }
#endif
        }

        /// <summary>
        /// Returns a <see cref="WaitTimeInstruction"/> that will complete after the specified timespan has passed, using scaled time.
        /// </summary>
        /// <param name="time">How much time to wait for.</param>
        public static WaitTimeInstruction WaitForTime(TimeSpan time)
        {
            return new WaitTimeInstruction(time);
        }

        /// <summary>
        /// Await instruction used to wait an amount of time, using unscaled, real time.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public struct WaitRealTimeInstruction : IAwaitWithProgressInstruction
        {
            private readonly TimeSpan _target;
            private InternalHelper.ValueStopwatch _stopwatch;

            /// <summary>
            /// Gets a new <see cref="WaitRealTimeInstruction"/>.
            /// </summary>
            public WaitRealTimeInstruction(TimeSpan time)
            {
                _target = time;
                _stopwatch = InternalHelper.ValueStopwatch.StartNew();
            }

            [MethodImpl(Internal.InlineOption)]
            bool IAwaitWithProgressInstruction.IsCompleted(out float progress)
            {
                unchecked
                {
                    var current = _stopwatch.GetElapsedTime();
                    // If the target is <= 0, progress will be NaN or negative. But it's fine because it won't be reported.
                    progress = (float) ((double) current.Ticks / _target.Ticks);
                    return current >= _target;
                }
            }

#if !CSHARP_7_3_OR_NEWER
            /// <summary>
            /// Converts this to a <see cref="Promise"/>.
            /// </summary>
            // Old C# compiler thinks the .ToPromise() extension methods are ambiguous, so we add it here explicitly.
            [MethodImpl(Internal.InlineOption)]
            public Promise ToPromise(CancelationToken cancelationToken = default(CancelationToken))
            {
                return PromiseYieldWithProgressExtensions.ToPromise(this, cancelationToken);
            }
#endif
        }

        /// <summary>
        /// Returns a <see cref="WaitRealTimeInstruction"/> that will complete after the specified timespan has passed, using unscaled, real time.
        /// </summary>
        /// <param name="time">How much time to wait for.</param>
        public static WaitRealTimeInstruction WaitForRealTime(TimeSpan time)
        {
            return new WaitRealTimeInstruction(time);
        }

        /// <summary>
        /// Await instruction used to wait until a condition is true.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public struct WaitUntilInstruction : IAwaitInstruction
        {
            private readonly Func<bool> _predicate;

            /// <summary>
            /// Gets a new <see cref="WaitUntilInstruction"/>.
            /// </summary>
            public WaitUntilInstruction(Func<bool> predicate)
            {
                ValidateArgument(predicate, "predicate", 1);
                _predicate = predicate;
            }

            [MethodImpl(Internal.InlineOption)]
            bool IAwaitInstruction.IsCompleted()
            {
                return _predicate.Invoke();
            }

#if !CSHARP_7_3_OR_NEWER
            /// <summary>
            /// Converts this to a <see cref="Promise"/>.
            /// </summary>
            // Old C# compiler thinks the .ToPromise() extension methods are ambiguous, so we add it here explicitly.
            [MethodImpl(Internal.InlineOption)]
            public Promise ToPromise(CancelationToken cancelationToken = default(CancelationToken))
            {
                return PromiseYieldExtensions.ToPromise(this, cancelationToken);
            }
#endif
        }

        /// <summary>
        /// Returns a <see cref="WaitUntilInstruction"/> that will complete when the supplied delegate returns true.
        /// </summary>
        /// <param name="predicate">The function that will be ran to determine if the wait should complete.</param>
        public static WaitUntilInstruction WaitUntil(Func<bool> predicate)
        {
            return new WaitUntilInstruction(predicate);
        }

        /// <summary>
        /// Await instruction used to wait until a condition is true.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public struct WaitUntilInstruction<TCapture> : IAwaitInstruction
        {
            private readonly TCapture _capturedValue;
            private readonly Func<TCapture, bool> _predicate;

            /// <summary>
            /// Gets a new <see cref="WaitUntilInstruction{T}"/>.
            /// </summary>
            public WaitUntilInstruction(TCapture captureValue, Func<TCapture, bool> predicate)
            {
                ValidateArgument(predicate, "predicate", 1);
                _capturedValue = captureValue;
                _predicate = predicate;
            }

            [MethodImpl(Internal.InlineOption)]
            bool IAwaitInstruction.IsCompleted()
            {
                return _predicate.Invoke(_capturedValue);
            }

#if !CSHARP_7_3_OR_NEWER
            /// <summary>
            /// Converts this to a <see cref="Promise"/>.
            /// </summary>
            // Old C# compiler thinks the .ToPromise() extension methods are ambiguous, so we add it here explicitly.
            [MethodImpl(Internal.InlineOption)]
            public Promise ToPromise(CancelationToken cancelationToken = default(CancelationToken))
            {
                return PromiseYieldExtensions.ToPromise(this, cancelationToken);
            }
#endif
        }

        /// <summary>
        /// Returns a <see cref="WaitUntilInstruction{T}"/> that will complete when the supplied delegate returns true.
        /// </summary>
        /// <param name="captureValue">The value that will be passed to the delegate.</param>
        /// <param name="predicate">The function that will be ran to determine if the wait should complete.</param>
        public static WaitUntilInstruction<TCapture> WaitUntil<TCapture>(TCapture captureValue, Func<TCapture, bool> predicate)
        {
            return new WaitUntilInstruction<TCapture>(captureValue, predicate);
        }

        /// <summary>
        /// Await instruction used to wait while a condition is true.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public struct WaitWhileInstruction : IAwaitInstruction
        {
            private readonly Func<bool> _predicate;

            /// <summary>
            /// Gets a new <see cref="WaitWhileInstruction"/>.
            /// </summary>
            public WaitWhileInstruction(Func<bool> predicate)
            {
                _predicate = predicate;
            }

            [MethodImpl(Internal.InlineOption)]
            bool IAwaitInstruction.IsCompleted()
            {
                return !_predicate.Invoke();
            }

#if !CSHARP_7_3_OR_NEWER
            /// <summary>
            /// Converts this to a <see cref="Promise"/>.
            /// </summary>
            // Old C# compiler thinks the .ToPromise() extension methods are ambiguous, so we add it here explicitly.
            [MethodImpl(Internal.InlineOption)]
            public Promise ToPromise(CancelationToken cancelationToken = default(CancelationToken))
            {
                return PromiseYieldExtensions.ToPromise(this, cancelationToken);
            }
#endif
        }

        /// <summary>
        /// Returns a <see cref="WaitWhileInstruction"/> that will complete when the supplied delegate returns false.
        /// </summary>
        /// <param name="predicate">The function that will be ran to determine if the wait should complete.</param>
        public static WaitWhileInstruction WaitWhile(Func<bool> predicate)
        {
            ValidateArgument(predicate, "predicate", 1);
            return new WaitWhileInstruction(predicate);
        }

        /// <summary>
        /// Await instruction used to wait while a condition is true.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public struct WaitWhileInstruction<TCapture> : IAwaitInstruction
        {
            private readonly TCapture _capturedValue;
            private readonly Func<TCapture, bool> _predicate;

            /// <summary>
            /// Gets a new <see cref="WaitWhileInstruction{T}"/>.
            /// </summary>
            public WaitWhileInstruction(TCapture captureValue, Func<TCapture, bool> predicate)
            {
                _capturedValue = captureValue;
                _predicate = predicate;
            }

            [MethodImpl(Internal.InlineOption)]
            bool IAwaitInstruction.IsCompleted()
            {
                return !_predicate.Invoke(_capturedValue);
            }

#if !CSHARP_7_3_OR_NEWER
            /// <summary>
            /// Converts this to a <see cref="Promise"/>.
            /// </summary>
            // Old C# compiler thinks the .ToPromise() extension methods are ambiguous, so we add it here explicitly.
            [MethodImpl(Internal.InlineOption)]
            public Promise ToPromise(CancelationToken cancelationToken = default(CancelationToken))
            {
                return PromiseYieldExtensions.ToPromise(this, cancelationToken);
            }
#endif
        }

        /// <summary>
        /// Returns a <see cref="WaitWhileInstruction{T}"/> that will complete when the supplied delegate returns false.
        /// </summary>
        /// <param name="captureValue">The value that will be passed to the delegate.</param>
        /// <param name="predicate">The function that will be ran to determine if the wait should complete.</param>
        public static WaitWhileInstruction<TCapture> WaitWhile<TCapture>(TCapture captureValue, Func<TCapture, bool> predicate)
        {
            ValidateArgument(predicate, "predicate", 1);
            return new WaitWhileInstruction<TCapture>(captureValue, predicate);
        }

        /// <summary>
        /// Await instruction used to wait for an <see cref="UnityEngine.AsyncOperation"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public struct WaitAsyncOperationInstruction : IAwaitWithProgressInstruction
        {
            private readonly UnityEngine.AsyncOperation _asyncOperation;

            /// <summary>
            /// Gets a new <see cref="WaitAsyncOperationInstruction"/>.
            /// </summary>
            public WaitAsyncOperationInstruction(UnityEngine.AsyncOperation asyncOperation)
            {
                _asyncOperation = asyncOperation;
            }

            [MethodImpl(Internal.InlineOption)]
            bool IAwaitWithProgressInstruction.IsCompleted(out float progress)
            {
                progress = _asyncOperation.progress;
                return _asyncOperation.isDone;
            }

#if !CSHARP_7_3_OR_NEWER
            /// <summary>
            /// Converts this to a <see cref="Promise"/>.
            /// </summary>
            // Old C# compiler thinks the .ToPromise() extension methods are ambiguous, so we add it here explicitly.
            [MethodImpl(Internal.InlineOption)]
            public Promise ToPromise(CancelationToken cancelationToken = default(CancelationToken))
            {
                return PromiseYieldWithProgressExtensions.ToPromise(this, cancelationToken);
            }
#endif
        }

        /// <summary>
        /// Returns a <see cref="WaitAsyncOperationInstruction"/> that will complete when the <paramref name="asyncOperation"/> is complete.
        /// </summary>
        public static WaitAsyncOperationInstruction WaitForAsyncOperation(UnityEngine.AsyncOperation asyncOperation)
        {
            return new WaitAsyncOperationInstruction(asyncOperation);
        }

        /// <summary>
        /// Awaiter used to wait for a context (FixedUpdate, EndOfFrame).
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public struct WaitOnceAwaiter : PromiseYieldExtensions.IAwaiter<WaitOnceAwaiter>
        {
            private readonly InternalHelper.SingleInstructionProcessor _processor;

            internal WaitOnceAwaiter(InternalHelper.SingleInstructionProcessor processor)
            {
                _processor = processor;
            }

            /// <summary>Gets the awaiter for this.</summary>
            /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
            /// <returns>this</returns>
            [MethodImpl(Internal.InlineOption)]
            public WaitOnceAwaiter GetAwaiter()
            {
                return this;
            }

            /// <summary>Gets whether the operation is complete.</summary>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <returns>false</returns>
            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get { return false; }
            }

            /// <summary>Called after the operation has completed.</summary>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            [MethodImpl(Internal.InlineOption)]
            public void GetResult()
            {
                // Do nothing.
            }

            /// <summary>Schedules the continuation.</summary>
            /// <param name="continuation">The action to invoke when the operation completes.</param>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, "continuation", 1);
                InternalHelper.ValidateIsOnMainThread(1);
                _processor.WaitForNext(continuation);
            }

            /// <summary>Schedules the continuation onto the <see cref="Promise"/> associated with this <see cref="PromiseAwaiterVoid"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }
        }

        /// <summary>
        /// Returns a <see cref="WaitOnceAwaiter"/> that will complete at the next end of frame.
        /// </summary>
        public static WaitOnceAwaiter WaitForEndOfFrame()
        {
            return new WaitOnceAwaiter(InternalHelper.PromiseBehaviour.Instance._endOfFrameProcessor);
        }

        /// <summary>
        /// Returns a <see cref="WaitOnceAwaiter"/> that will complete at the next fixed update.
        /// </summary>
        public static WaitOnceAwaiter WaitForFixedUpdate()
        {
            return new WaitOnceAwaiter(InternalHelper.PromiseBehaviour.Instance._fixedUpdateProcessor);
        }

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }
#endif
    } // class PromiseYielder
}