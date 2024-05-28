#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0090 // Use 'new(...)'

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace Proto.Promises
{
    internal static partial class InternalHelper
    {
        internal interface IYieldInstruction
        {
            bool Evaluate();
            void MaybeRetainCancelationToken();
        }

        [MethodImpl(Internal.InlineOption)]
        internal static bool IsOnMainThread()
            => PromiseBehaviour.s_mainThread == Thread.CurrentThread;

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        internal static void ValidateIsOnMainThread(int skipFrames)
        {
            if (!IsOnMainThread())
            {
                throw new InvalidOperationException("Must be on main thread to use PromiseYielder awaits. Use `Promise.SwitchToForeground()` to switch to the main thread.", Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }
#else
        [MethodImpl(Internal.InlineOption)]
#pragma warning disable IDE0060 // Remove unused parameter
        internal static void ValidateIsOnMainThread(int skipFrames)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            // We read Time.frameCount in RELEASE mode since it's a faster thread check than accessing Thread.CurrentThread.
            _ = Time.frameCount;
        }
#endif

        internal sealed partial class PromiseBehaviour
        {
            internal static Thread s_mainThread;

            internal static int s_currentFrame = -1;
            internal static float s_deltaTime = 0f;

            // These must not be readonly.

            // Processor for WaitOneFrame. Larger initial capacity as this is expected to be frequently used.
            internal static WaitOneFrameProcessor s_waitOneFrameProcessor = new WaitOneFrameProcessor(64);
            internal static SingleInstructionProcessor s_updateProcessor = new SingleInstructionProcessor(16);
            internal static SingleInstructionProcessor s_lateUpdateProcessor = new SingleInstructionProcessor(16);
            internal static SingleInstructionProcessor s_fixedUpdateProcessor = new SingleInstructionProcessor(16);
            // Processor for WaitForEndOfFrame. The initial capacity is small because it is expected to be used rarely (and almost never multiple simultaneously).
            internal static SingleInstructionProcessor s_endOfFrameProcessor = new SingleInstructionProcessor(4);

            // Generic processor for instructions that need to be called every frame potentially multiple times.
            internal InstructionProcessorGroup _updateProcessor = new InstructionProcessorGroup(16);

            private static void SetTimeValues()
            {
                s_currentFrame = Time.frameCount;
                s_deltaTime = Time.deltaTime;
            }

            static private void StaticInit()
            {
                s_mainThread = Thread.CurrentThread;
                SetTimeValues();
            }

            private void Init()
            {
                StartCoroutine(UpdateRoutine());
                StartCoroutine(FixedUpdateRoutine());
                StartCoroutine(EndOfFrameRoutine());
            }

            private void ResetProcessors()
            {
                s_waitOneFrameProcessor.Clear();
                s_updateProcessor.Clear();
                s_lateUpdateProcessor.Clear();
                s_fixedUpdateProcessor.Clear();
                s_endOfFrameProcessor.Clear();
                _updateProcessor.ResetProcessors();
            }

            private IEnumerator UpdateRoutine()
            {
                while (true)
                {
                    yield return null;
                    SetTimeValues();
                    s_waitOneFrameProcessor.Process();
                    _updateProcessor.Process();
                }
            }

            // This is called from Update after the synchronization context is executed.
            private void ProcessUpdate()
                => s_updateProcessor.Process();

            private void LateUpdate()
                => s_lateUpdateProcessor.Process();

            private IEnumerator FixedUpdateRoutine()
            {
                var fixedUpdateInstruction = new WaitForFixedUpdate();
                while (true)
                {
                    yield return fixedUpdateInstruction;
                    s_fixedUpdateProcessor.Process();
                }
            }

            private IEnumerator EndOfFrameRoutine()
            {
                var endOfFrameInstruction = new WaitForEndOfFrame();
                while (true)
                {
                    yield return endOfFrameInstruction;
                    s_endOfFrameProcessor.Process();
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // InstructionProcessor optimized for instructions that never need to keep waiting, without caring about which frame it's executed in.
        internal struct SingleInstructionProcessor
        {
            // We use 2 queues, 1 for the currently executing, another for the next update.
            private Action[] _currentQueue;
            private Action[] _nextQueue;
            private int _nextCount;

            internal SingleInstructionProcessor(int initialCapacity)
            {
                _currentQueue = new Action[initialCapacity];
                _nextQueue = new Action[initialCapacity];
                _nextCount = 0;
            }

            [MethodImpl(Internal.InlineOption)]
            internal void WaitForNext(Action continuation)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                ValidateIsOnMainThread(2);
#endif
                int index = _nextCount;
                int capacity = _nextQueue.Length;
                if (index >= capacity)
                {
                    int newCapcity = capacity * 2;
                    Array.Resize(ref _currentQueue, newCapcity);
                    Array.Resize(ref _nextQueue, newCapcity);
                }

                _nextQueue[index] = continuation;
                _nextCount = index + 1;
            }

            [MethodImpl(Internal.InlineOption)]
            internal void Process()
            {
                // Store the next in a local for iteration, and swap queues.
#pragma warning disable IDE0180 // Use tuple to swap values
                var current = _nextQueue;
#pragma warning restore IDE0180 // Use tuple to swap values
                _nextQueue = _currentQueue;
                _currentQueue = current;

                int max = _nextCount;
                _nextCount = 0;
                for (int i = 0; i < max; ++i)
                {
                    current[i].Invoke();
                }
                Array.Clear(_currentQueue, 0, max);
            }

            internal void Clear()
            {
                Array.Clear(_currentQueue, 0, _currentQueue.Length);
                Array.Clear(_nextQueue, 0, _nextQueue.Length);
                _nextCount = 0;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // InstructionProcessor optimized for WaitOneFrame, makes sure the instruction completes in the following frame.
        internal struct WaitOneFrameProcessor
        {
            // We use 3 queues, 1 for the currently executing, another for the next update,
            // and a third for the following, because WaitOneFrame needs to be able to wait for 2 updates to not be completed later in the same frame.
            private Action[] _currentQueue;
            private Action[] _nextQueue;
            private Action[] _followingQueue;
            private int _nextCount;
            private int _followingCount;

            internal WaitOneFrameProcessor(int initialCapacity)
            {
                _currentQueue = new Action[initialCapacity];
                _nextQueue = new Action[initialCapacity];
                _followingQueue = new Action[initialCapacity];
                _nextCount = 0;
                _followingCount = 0;
            }

            [MethodImpl(Internal.InlineOption)]
            internal void WaitForNext(Action continuation)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                ValidateIsOnMainThread(2);
#endif
                if (Time.frameCount == PromiseBehaviour.s_currentFrame)
                {
                    // The update queue already ran this frame, wait for the next.
                    Next(continuation);
                }
                else
                {
                    // The update queue has not yet run this frame, so to force it to wait for the next frame
                    // (instead of resolving later in the same frame), we wait for 2 frame updates.
                    Following(continuation);
                }
            }

            [MethodImpl(Internal.InlineOption)]
            private void Next(Action continuation)
            {
                int index = _nextCount;
                int capacity = _nextQueue.Length;
                if (index >= capacity)
                {
                    int newCapcity = capacity * 2;
                    Array.Resize(ref _currentQueue, newCapcity);
                    Array.Resize(ref _nextQueue, newCapcity);
                    Array.Resize(ref _followingQueue, newCapcity);
                }

                _nextQueue[index] = continuation;
                _nextCount = index + 1;
            }

            [MethodImpl(Internal.InlineOption)]
            private void Following(Action continuation)
            {
                int index = _followingCount;
                int capacity = _followingQueue.Length;
                if (index >= capacity)
                {
                    int newCapcity = capacity * 2;
                    Array.Resize(ref _currentQueue, newCapcity);
                    Array.Resize(ref _nextQueue, newCapcity);
                    Array.Resize(ref _followingQueue, newCapcity);
                }

                _followingQueue[index] = continuation;
                _followingCount = index + 1;
            }

            [MethodImpl(Internal.InlineOption)]
            internal void Process()
            {
                // Store the next in a local for iteration, and rotate queues.
                var current = _nextQueue;
                _nextQueue = _followingQueue;
                _followingQueue = _currentQueue;
                _currentQueue = current;

                // Rotate counts.
                int max = _nextCount;
                _nextCount = _followingCount;
                _followingCount = 0;
                for (int i = 0; i < max; ++i)
                {
                    current[i].Invoke();
                }
                Array.Clear(_currentQueue, 0, max);
            }

            internal void Clear()
            {
                Array.Clear(_currentQueue, 0, _currentQueue.Length);
                Array.Clear(_nextQueue, 0, _nextQueue.Length);
                Array.Clear(_followingQueue, 0, _followingQueue.Length);
                _nextCount = 0;
                _followingCount = 0;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal struct InstructionProcessorGroup
        {
            // Start with a smaller initial capacity since there could be lots of different types of instructions due to generics and custom instructions.
            internal const int InitialCapacityPerType = 64;

            // This must not be readonly.
            private Internal.ValueList<InstructionProcessorBase> _processors;

            internal InstructionProcessorGroup(int initialProcessorCapacity)
            {
                _processors = new Internal.ValueList<InstructionProcessorBase>(initialProcessorCapacity);
            }

            [MethodImpl(Internal.InlineOption)]
            internal void WaitFor<TYieldInstruction>(in TYieldInstruction instruction) where TYieldInstruction : struct, IYieldInstruction
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                ValidateIsOnMainThread(2);
#endif
                var processor = InstructionProcessor<TYieldInstruction>.s_instance;

                if (processor == null)
                {
                    processor = new InstructionProcessor<TYieldInstruction>(InitialCapacityPerType);
                    InstructionProcessor<TYieldInstruction>.s_instance = processor;
                    _processors.Add(processor);
                }

                processor.WaitFor(instruction);
            }

            [MethodImpl(Internal.InlineOption)]
            public void Process()
            {
                for (int i = 0, max = _processors.Count; i < max; ++i)
                {
                    _processors[i].Process();
                }
            }

            internal void ResetProcessors()
            {
                // We reset the generic static fields through the abstract class.
                for (int i = 0, max = _processors.Count; i < max; ++i)
                {
                    _processors[i].Reset();
                }
            }

            // Using abstract class instead of interface, because virtual calls are 2x faster.
            private abstract class InstructionProcessorBase
            {
                internal abstract void Process();
                internal abstract void Reset();
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class InstructionProcessor<TYieldInstruction> : InstructionProcessorBase
                // struct constraint forces the compiler to generate specialized code for each type, which allows for inlining method calls.
                where TYieldInstruction : struct, IYieldInstruction
            {
                internal static InstructionProcessor<TYieldInstruction> s_instance;

                // We use 3 queues, 1 for the currently executing, another for the next update, and a third for the following,
                // so that yield instructions will not be evaluated twice in the same frame (they are evaluated immediately when they are awaited).
                private TYieldInstruction[] _currentQueue;
                private TYieldInstruction[] _nextQueue;
                private TYieldInstruction[] _followingQueue;
                private int _currentCount;
                private int _nextCount;
                private int _followingCount;

                internal InstructionProcessor(int initialCapacity)
                {
                    _currentQueue = new TYieldInstruction[initialCapacity];
                    _nextQueue = new TYieldInstruction[initialCapacity];
                    _followingQueue = new TYieldInstruction[initialCapacity];
                    _currentCount = 0;
                    _nextCount = 0;
                    _followingCount = 0;
                }

                [MethodImpl(Internal.InlineOption)]
                internal void WaitFor(in TYieldInstruction instruction)
                {
                    int capacity;
                    if (Time.frameCount != PromiseBehaviour.s_currentFrame)
                    {
                        // This has not yet been processed this frame, wait for the following update.
                        capacity = _followingQueue.Length;
                        if (_followingCount >= capacity)
                        {
                            int newCapcity = capacity * 2;
                            Array.Resize(ref _currentQueue, newCapcity);
                            Array.Resize(ref _nextQueue, newCapcity);
                            Array.Resize(ref _followingQueue, newCapcity);
                        }

                        _followingQueue[_followingCount] = instruction;
                        _followingQueue[_followingCount].MaybeRetainCancelationToken();
                        ++_followingCount;
                        return;
                    }

                    // This could be called while the queue is still processing.
                    // When incomplete yield instructions are re-added, we skip bounds checks as an optimisation,
                    // so we need to make sure the queue has enough space to re-add them all in case none of them have finished.
                    int potentialFutureCount = _nextCount + _currentCount;

                    capacity = _nextQueue.Length;
                    if (potentialFutureCount >= capacity)
                    {
                        int newCapcity = capacity * 2;
                        Array.Resize(ref _currentQueue, newCapcity);
                        Array.Resize(ref _nextQueue, newCapcity);
                        Array.Resize(ref _followingQueue, newCapcity);
                    }

                    _nextQueue[_nextCount] = instruction;
                    _nextQueue[_nextCount].MaybeRetainCancelationToken();
                    ++_nextCount;
                }

                internal override void Process()
                {
                    // Store the next in a local for iteration, and rotate queues.
                    var current = _nextQueue;
                    _nextQueue = _followingQueue;
                    _followingQueue = _currentQueue;
                    _currentQueue = current;

                    // Rotate counts.
                    int max = _nextCount;
                    _nextCount = _followingCount;
                    _followingCount = 0;
                    _currentCount = max;

                    for (int i = 0; i < max; ++i)
                    {
                        ref TYieldInstruction instruction = ref current[i];
                        // If any instruction throws, we still need to execute the remaining instructions.
                        try
                        {
                            if (!instruction.Evaluate())
                            {
                                // This is hottest path, so we don't do a bounds check here (see WaitFor).
                                _nextQueue[_nextCount++] = instruction;
                            }
                        }
                        catch (Exception e)
                        {
                            UnityEngine.Debug.LogException(e);
                        }
                        --_currentCount;
                    }
                    Array.Clear(_currentQueue, 0, max);
                }

                internal override void Reset()
                    // We reset the static field.
                    => s_instance = null;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class YieldInstructionRunner : Internal.HandleablePromiseBase, IEnumerator
        {
            private Promise.Deferred _deferred;
            private CancelationRegistration _cancelationRegistration;
            private int _id = 1;
            private bool _isYieldInstructionComplete;

            public object Current { get; private set; }

            private YieldInstructionRunner() { }

            [MethodImpl(Internal.InlineOption)]
            private static YieldInstructionRunner GetOrCreate()
            {
                var obj = Internal.ObjectPool.TryTakeOrInvalid<YieldInstructionRunner>();
                return obj == Internal.PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new YieldInstructionRunner()
                    : obj.UnsafeAs<YieldInstructionRunner>();
            }

            internal static Promise WaitForInstruction(object yieldInstruction, MonoBehaviour runner, CancelationToken cancelationToken)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                ValidateIsOnMainThread(2);
#endif
                // Quick check to see if the token is already canceled.
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise.Canceled();
                }

                var routine = GetOrCreate();
                routine._next = null;
                routine._deferred = Promise.NewDeferred();
                runner = runner ? runner : PromiseBehaviour.Instance;
                routine.Current = yieldInstruction;
                cancelationToken.TryRegister((routine, runner), cv => cv.routine.OnCancel(cv.runner), out routine._cancelationRegistration);

                runner.StartCoroutine(routine);
                return routine._deferred.Promise;
            }

            public bool MoveNext()
            {
                // As a coroutine, this will wait for the Current's yield, then execute this once, then stop.
                if (!_isYieldInstructionComplete)
                {
                    _isYieldInstructionComplete = true;
                    return true;
                }

                Complete();
                return false;
            }

            private void Complete()
            {
                _cancelationRegistration.Dispose();
                SetCompleteAndRepoolAndGetDeferred().Resolve();
            }

            private void OnCancel(MonoBehaviour runner)
            {
                if (IsOnMainThread())
                {
                    OnCancel(runner, _id);
                    return;
                }
                // The token could be canceled from any thread, so we have to dispatch the cancelation logic to the main thread.
                Promise.Run((this, runner, _id), (cv) => cv.Item1.OnCancel(cv.runner, cv._id),
                     PromiseBehaviour.Instance._syncContext) // Explicitly use the sync context in case the user overwrites the Config.ForegroundContext.
                    .Forget();
            }

            private void OnCancel(MonoBehaviour runner, int id)
            {
                if (id != _id)
                {
                    // The yield instruction already completed.
                    return;
                }

                // If the runner is valid, we stop the coroutine. Otherwise, the coroutine was already stopped by Unity.
                if (runner)
                {
                    runner.StopCoroutine(this);
                }

                SetCompleteAndRepoolAndGetDeferred().Cancel();
            }

            private Promise.Deferred SetCompleteAndRepoolAndGetDeferred()
            {
                unchecked
                {
                    ++_id;
                }
                var deferred = _deferred;
                _deferred = default;
                _cancelationRegistration = default;
                Current = null;
                _isYieldInstructionComplete = false;
                // Place this back in the pool before invoking the deferred, in case the invocation will re-use this.
                Internal.ObjectPool.MaybeRepool(this);
                return deferred;
            }

            void IEnumerator.Reset() { }
        } // class YieldInstructionRunner
    } // class InternalHelper
}