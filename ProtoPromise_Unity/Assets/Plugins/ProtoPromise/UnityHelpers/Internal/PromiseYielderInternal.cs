#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
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
        {
            return PromiseBehaviour.s_mainThread == Thread.CurrentThread;
        }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        internal static void ValidateIsOnMainThread(int skipFrames)
        {
            if (PromiseBehaviour.s_mainThread != Thread.CurrentThread)
            {
                throw new InvalidOperationException("Must be on main thread to use PromiseYielder awaits. Use `Promise.SwitchToForeground()` to switch to the main thread.", Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }
#else
        internal static int s_frameHolder;
        [MethodImpl(Internal.InlineOption)]
        internal static void ValidateIsOnMainThread(int skipFrames)
        {
            // We read Time.frameCount in RELEASE mode since it's a faster thread check than accessing Thread.CurrentThread.
            s_frameHolder = Time.frameCount;
        }
#endif

        internal sealed partial class PromiseBehaviour
        {
            internal static Thread s_mainThread;

            // These must not be readonly.

            // Generic processor for instructions that need to be called every frame potentially multiple times.
            internal InstructionProcessorGroup _updateProcessor = new InstructionProcessorGroup(16);

            // Processor optimized for WaitOneFrame instructions. Larger initial capacity as this is expected to be the most used instruction.
            internal SingleInstructionProcessor _oneFrameProcessor = new SingleInstructionProcessor(1024);

            // Processor optimized for fixed update instructions. The initial capacity is lower than the frame processor, because it is expected to be used less, but still large enough in case it is used a lot.
            internal SingleInstructionProcessor _fixedUpdateProcessor = new SingleInstructionProcessor(256);

            // Processor optimized for end of frame instructions. The initial capacity is small because it is expected to be used rarely (and almost never multiple simultaneously).
            internal SingleInstructionProcessor _endOfFrameProcessor = new SingleInstructionProcessor(16);

            internal int _currentFrame = -1;
            internal float _deltaTime = 0f;

            private void SetTimeValues()
            {
                _currentFrame = Time.frameCount;
                _deltaTime = Time.deltaTime;
            }

            partial void Init()
            {
                s_mainThread = Thread.CurrentThread;
                SetTimeValues();
                StartCoroutine(UpdateRoutine());
                StartCoroutine(FixedUpdateRoutine());
                StartCoroutine(EndOfFrameRoutine());
            }

            partial void ResetProcessors()
            {
                _updateProcessor.ResetProcessors();
            }

            private IEnumerator UpdateRoutine()
            {
                while (true)
                {
                    yield return null;
                    SetTimeValues();
                    _oneFrameProcessor.Process();
                    _updateProcessor.Process();
                }
            }

            private IEnumerator FixedUpdateRoutine()
            {
                var fixedUpdateInstruction = new WaitForFixedUpdate();
                while (true)
                {
                    yield return fixedUpdateInstruction;
                    _fixedUpdateProcessor.Process();
                }
            }

            private IEnumerator EndOfFrameRoutine()
            {
                var endOfFrameInstruction = new WaitForEndOfFrame();
                while (true)
                {
                    yield return endOfFrameInstruction;
                    _endOfFrameProcessor.Process();
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // InstructionProcessor optimized for instructions that never need to keep waiting.
        internal sealed class SingleInstructionProcessor
        {
            // We use 3 queues, 1 for the currently executing, another for the next update,
            // and a third for the following, because WaitOneFrame needs to be able to wait for 2 updates to not be completed later in the same frame.
            private Action[] _currentQueue;
            private Action[] _nextQueue;
            private Action[] _followingQueue;
            private int _nextCount;
            private int _followingCount;

            internal SingleInstructionProcessor(int initialCapacity)
            {
                _currentQueue = new Action[initialCapacity];
                _nextQueue = new Action[initialCapacity];
                _followingQueue = new Action[initialCapacity];
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
                    // We only resize the array that is currently being added to. We don't resize the other arrays until they need it.
                    Array.Resize(ref _nextQueue, capacity * 2);
                }

                _nextQueue[index] = continuation;
                _nextCount = index + 1;
            }

            [MethodImpl(Internal.InlineOption)]
            internal void WaitForFollowing(Action continuation)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                ValidateIsOnMainThread(2);
#endif
                int index = _followingCount;
                int capacity = _followingQueue.Length;
                if (index >= capacity)
                {
                    // We only resize the array that is currently being added to. We don't resize the other arrays until they need it.
                    Array.Resize(ref _followingQueue, capacity * 2);
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
                Array.Clear(current, 0, max);
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
            private Internal.ValueList<IInstructionProcessor> _processors;

            internal InstructionProcessorGroup(int initialProcessorCapacity)
            {
                _processors = new Internal.ValueList<IInstructionProcessor>(initialProcessorCapacity);
            }

            [MethodImpl(Internal.InlineOption)]
            internal void WaitFor<TYieldInstruction>(ref TYieldInstruction instruction) where TYieldInstruction : struct, IYieldInstruction
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

                processor.WaitFor(ref instruction);
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
                // We reset the generic static fields through the interface.
                for (int i = 0, max = _processors.Count; i < max; ++i)
                {
                    _processors[i].Reset();
                }
            }

            private interface IInstructionProcessor
            {
                void Process();
                void Reset();
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class InstructionProcessor<TYieldInstruction> : IInstructionProcessor
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
                internal void WaitFor(ref TYieldInstruction instruction)
                {
                    int capacity;
                    if (Time.frameCount != PromiseBehaviour.Instance._currentFrame)
                    {
                        // This has not yet been processed this frame, wait for the following update.
                        capacity = _followingQueue.Length;
                        if (_followingCount >= capacity)
                        {
                            // We only resize the array that is currently being added to. We don't resize the other one until it's needed.
                            Array.Resize(ref _nextQueue, capacity * 2);
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
                        // We only resize the array that is currently being added to. We don't resize the other one until it's needed.
                        Array.Resize(ref _nextQueue, capacity * 2);
                    }

                    _nextQueue[_nextCount] = instruction;
                    _nextQueue[_nextCount].MaybeRetainCancelationToken();
                    ++_nextCount;
                }

                public void Process()
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
                        // ref locals are not available in older C# versions, so we pass it to a function with aggressive inlining instead.
                        Evaluate(ref current[i]);
                        --_currentCount;
                    }
                    Array.Clear(current, 0, max);
                }

                [MethodImpl(Internal.InlineOption)]
                private void Evaluate(ref TYieldInstruction instruction)
                {
                    if (!instruction.Evaluate())
                    {
                        // This is hottest path, so we don't do a bounds check here (see Add).
                        _nextQueue[_nextCount] = instruction;
                        ++_nextCount;
                    }
                }

                void IInstructionProcessor.Reset()
                {
                    // We reset the static field.
                    s_instance = null;
                }
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
                runner = runner != null ? runner : PromiseBehaviour.Instance;
                routine.Current = yieldInstruction;
                cancelationToken.TryRegister(ValueTuple.Create(routine, runner), cv => cv.Item1.OnCancel(cv.Item2), out routine._cancelationRegistration);

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
                Promise.Run(ValueTuple.Create(this, runner, _id), (cv) => cv.Item1.OnCancel(cv.Item2, cv.Item3),
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

                // If it's null, the monobehaviour was destroyed, so the coroutine will have already been stopped.
                if (runner != null)
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
                _deferred = default(Promise.Deferred);
                _cancelationRegistration = default(CancelationRegistration);
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