#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable 0618 // Type or member is obsolete

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace Proto.Promises
{
    /// <summary>
    /// Yielder used to wait for a yield instruction to complete in the form of a Promise, using Unity's coroutines.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static class PromiseYielder
    {
        /// <summary>
        /// The interface used to wait for an asynchronous instruction to complete.
        /// </summary>
        internal interface IWaitInstruction
        {
            /// <summary>
            /// Gets whether this instruction is complete.
            /// </summary>
            /// <param name="deferred">The <see cref="Promise.Deferred"/> optionally used to report progress.</param>
            /// <remarks>
            /// The <paramref name="deferred"/> may be optionally used to report progress. The implementer should call <see cref="Promise.Deferred.TryReportProgress(float)"/>,
            /// because the first time it is called will be with an invalid <see cref="Promise.Deferred"/>.
            /// </remarks>
            bool GetIsComplete(Promise.Deferred deferred); // TODO: should we implement a `Promise.ProgressSource` type for this to be safe to make public?
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private abstract class WaitInstructionBase : Internal.HandleablePromiseBase, Internal.ILinked<WaitInstructionBase>
        {
            internal Promise.Deferred _deferred;
            protected CancelationRegistration _cancelationRegistration;
            protected int _id = 1;

            WaitInstructionBase Internal.ILinked<WaitInstructionBase>.Next
            {
                get { return _next.UnsafeAs<WaitInstructionBase>(); }
                set { _next = value; }
            }

            public abstract bool GetIsComplete();

            internal void MaybeHookupCancelation(CancelationToken cancelationToken, PromiseYielderBehaviour promiseYielder)
            {
                cancelationToken.TryRegister(ValueTuple.Create(this, promiseYielder), cv =>
                {
                    // The token could be canceled from any thread, so we have to dispatch the cancelation logic to the main thread.
                    Promise.Run(ValueTuple.Create(cv.Item1, cv.Item2, cv.Item1._id), (cv3) => cv3.Item1.OnCancel(cv3.Item2, cv3.Item3),
                        PromiseYielderBehaviour.s_creatorThread == Thread.CurrentThread ? SynchronizationOption.Synchronous : SynchronizationOption.Foreground, forceAsync: false)
                        .Forget();
                }, out _cancelationRegistration);
            }

            protected abstract void OnCancel(PromiseYielderBehaviour promiseYielder, int id);

            internal abstract void Reject(Exception e);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class WaitInstruction<TWaitInstruction> : WaitInstructionBase
            where TWaitInstruction : struct, IWaitInstruction
        {
            private TWaitInstruction _waitInstruction;

            private WaitInstruction() { }

            [MethodImpl(Internal.InlineOption)]
            private static WaitInstruction<TWaitInstruction> GetOrCreate()
            {
                var obj = Internal.ObjectPool.TryTakeOrInvalid<WaitInstruction<TWaitInstruction>>();
                return obj == Internal.PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new WaitInstruction<TWaitInstruction>()
                    : obj.UnsafeAs<WaitInstruction<TWaitInstruction>>();
            }

            internal static WaitInstruction<TWaitInstruction> GetOrCreate(TWaitInstruction waitInstruction)
            {
                var wi = GetOrCreate();
                wi._next = null;
                wi._waitInstruction = waitInstruction;
                wi._deferred = Promise.NewDeferred();
                ++wi._id;
                return wi;
            }

            public override bool GetIsComplete()
            {
                var isComplete = _waitInstruction.GetIsComplete(_deferred);
                if (isComplete)
                {
                    Resolve();
                }
                return isComplete;
            }

            private void Resolve()
            {
                _cancelationRegistration.Dispose();
                SetCompleteAndRepoolAndGetDeferred().Resolve();
            }

            protected override void OnCancel(PromiseYielderBehaviour promiseYielder, int id)
            {
                if (id != _id)
                {
                    // The wait instruction already completed.
                    return;
                }

                promiseYielder.RemoveWaitInstruction(this);
                SetCompleteAndRepoolAndGetDeferred().Cancel();
            }

            internal override void Reject(Exception e)
            {
                if (!_deferred.IsValidAndPending)
                {
                    // This should never happen.
                    Internal.ReportRejection(e, null);
                    return;
                }
                _cancelationRegistration.Dispose();
                SetCompleteAndRepoolAndGetDeferred().Reject(e);
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
                _waitInstruction = default(TWaitInstruction);
                // Place this back in the pool before invoking the deferred, in case the invocation will re-use this.
                Internal.ObjectPool.MaybeRepool(this);
                return deferred;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        [AddComponentMenu("")] // Hide this in the add component menu.
        private sealed class PromiseYielderBehaviour : MonoBehaviour
        {
            private static PromiseYielderBehaviour s_instance;
            internal static Thread s_creatorThread;

            internal static MonoBehaviour Instance
            {
                [MethodImpl(Internal.InlineOption)]
                get { return s_instance; }
            }

            // This must not be readonly.
            // We don't synchronize this, because everything is ran on the main thread.
            private Internal.ValueLinkedQueue<WaitInstructionBase> _waitInstructions = new Internal.ValueLinkedQueue<WaitInstructionBase>();
            private int _currentFrame = -1;
            private int _totalWaitInstructionCount;
            private int _nextWaitInstructionCount;
            private int _currentWaitInstructionCount;

            private PromiseYielderBehaviour() { }

            private void Awake()
            {
                _currentFrame = Time.frameCount;
            }

            private void Start()
            {
                if (s_instance != this)
                {
                    UnityEngine.Debug.LogWarning("There can only be one instance of PromiseYielder. Destroying new instance.");
                    Destroy(this);
                    return;
                }
                DontDestroyOnLoad(gameObject);
                gameObject.hideFlags = HideFlags.HideAndDontSave; // Don't show in hierarchy and don't destroy.
                StartCoroutine(UpdateRoutine());
            }

            private void OnDestroy()
            {
                if (InternalHelper.PromiseBehaviour.s_isApplicationQuitting)
                {
                    return;
                }
                if (s_instance == this)
                {
                    UnityEngine.Debug.LogWarning("PromiseYielderBehaviour destroyed! Any pending PromiseYielder.WaitFor promises running on the default MonoBehaviour will not be resolved!");
                    s_instance = null;
                }
            }

            internal static Promise WaitForInstruction<TWaitInstruction>(TWaitInstruction waitInstruction, CancelationToken cancelationToken)
                where TWaitInstruction : struct, IWaitInstruction
            {
                VerifyMainThread();

                // Quick check to see if the token is already canceled.
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise.Canceled();
                }

                // Always run the instruction immediately. Only create and enqueue if it's not already complete.
                if (waitInstruction.GetIsComplete(default(Promise.Deferred)))
                {
                    return Promise.Resolved();
                }

                return s_instance.EnqueueWaitInstruction(WaitInstruction<TWaitInstruction>.GetOrCreate(waitInstruction), cancelationToken);
            }

            private Promise EnqueueWaitInstruction(WaitInstructionBase waitInstruction, CancelationToken cancelationToken)
            {
                var promise = waitInstruction._deferred.Promise;
                checked
                {
                    ++_totalWaitInstructionCount;
                }
                // If the instructions were already ran this frame, we increment the counter so it will be ran next frame.
                // Otherwise, we don't increment it so it won't be ran twice in the same frame.
                if (Time.frameCount == _currentFrame)
                {
                    unchecked
                    {
                        ++_nextWaitInstructionCount;
                    }
                }
                _waitInstructions.Enqueue(waitInstruction);
                waitInstruction.MaybeHookupCancelation(cancelationToken, this);
                return promise;
            }

            internal void RemoveWaitInstruction(WaitInstructionBase waitInstruction)
            {
                var index = _waitInstructions.RemoveAndGetIndexOf(waitInstruction);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (index < 0)
                {
                    throw new System.InvalidOperationException("Tried to remove wait instruction, but wasn't found.");
                }
#endif
                unchecked
                {
                    --_totalWaitInstructionCount;
                    // Decrement the current counter in case this is called from a wait instruction.
                    --_currentWaitInstructionCount;
                    // If the instruction was going to be invoked at the next update cycle, we decrement the next counter.
                    if (index < _nextWaitInstructionCount)
                    {
                        --_nextWaitInstructionCount;
                    }
                }
            }

            // Run wait instructions in a Coroutine rather than in Update.
            private IEnumerator UpdateRoutine()
            {
                // We end up missing the first frame here, but that's fine because wait instructions are always ran immediately.
                while (true)
                {
                    yield return null;
                    _currentFrame = Time.frameCount;

                    // We place incomplete instruction in this new queue, then replace the entire wait queue.
                    var incompleteInstructions = new Internal.ValueLinkedQueue<WaitInstructionBase>();

                    // We don't iterate over the entire queue, only the count that should be ran this frame.
                    _currentWaitInstructionCount = _nextWaitInstructionCount;
                    if (_currentWaitInstructionCount == 0)
                    {
                        _nextWaitInstructionCount = _totalWaitInstructionCount;
                        continue;
                    }

                    // _currentWaitInstructionCount can be decremented while the wait instruction is invoked due to cancelations, so we need to be careful.
                    while (_currentWaitInstructionCount > 1)
                    {
                        --_currentWaitInstructionCount;
                        RunWaitInstruction(_waitInstructions.DequeueUnsafe(), ref incompleteInstructions);
                    }
                    if (_currentWaitInstructionCount > 0)
                    {
                        RunWaitInstruction(_waitInstructions.Dequeue(), ref incompleteInstructions);
                    }

                    _nextWaitInstructionCount = _totalWaitInstructionCount;
                    incompleteInstructions.TakeAndEnqueueElements(ref _waitInstructions);
                    _waitInstructions = incompleteInstructions;
                }
            }

            private void RunWaitInstruction(WaitInstructionBase instruction, ref Internal.ValueLinkedQueue<WaitInstructionBase> incompleteInstructions)
            {
                bool isComplete = true;
                try
                {
                    isComplete = instruction.GetIsComplete();
                }
                catch (Exception e)
                {
                    // Currently this should never happen, but if we expose IWaitInstruction, this will already be ready to handle it.
                    instruction.Reject(e);
                }

                if (isComplete)
                {
                    --_totalWaitInstructionCount;
                }
                else
                {
                    incompleteInstructions.Enqueue(instruction);
                }
            }

            internal static void VerifyMainThread()
            {
                if (s_instance == null)
                {
                    // Unity will throw if this is not ran on the main thread.
                    s_instance = new GameObject("Proto.Promises.PromiseYielderBehaviour").AddComponent<PromiseYielderBehaviour>();
                    s_creatorThread = Thread.CurrentThread;
                    return;
                }
                if (s_creatorThread != Thread.CurrentThread)
                {
                    ThrowNotOnMainThread();
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowNotOnMainThread()
            {
                throw new InvalidOperationException("PromiseYielder functions can only be called from the main thread. Use Promise.SwitchToForeground() to switch to the main thread.", Internal.GetFormattedStacktrace(3));
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private class Routine : Internal.HandleablePromiseBase, IEnumerator, ICancelable
        {
            // WeakReference so we aren't creating a memory leak of MonoBehaviours while this is in the pool.
            private readonly WeakReference _currentRunnerRef = new WeakReference(null, false);
            private Promise.Deferred _deferred;
            private CancelationRegistration _cancelationRegistration;
            private int _id = 1;
            private bool _isInvokingComplete;
            private bool _isYieldInstructionComplete;
            private bool _shouldContinueCoroutine;

            public object Current { get; private set; }

            private Routine() { }

            [MethodImpl(Internal.InlineOption)]
            private static Routine GetOrCreate()
            {
                var obj = Internal.ObjectPool.TryTakeOrInvalid<Routine>();
                return obj == Internal.PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new Routine()
                    : obj.UnsafeAs<Routine>();
            }

            internal static Promise WaitForInstruction(object yieldInstruction, MonoBehaviour runner, CancelationToken cancelationToken)
            {
                PromiseYielderBehaviour.VerifyMainThread();

                // Quick check to see if the token is already canceled.
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise.Canceled();
                }

                var routine = GetOrCreate();
                routine._next = null;
                routine._deferred = Promise.NewDeferred();
                bool validRunner = runner != null;
                runner = validRunner ? runner : PromiseYielderBehaviour.Instance;
                bool sameRunner = ReferenceEquals(runner, routine._currentRunnerRef.Target);
                routine._currentRunnerRef.Target = runner;
                routine.Current = yieldInstruction;
                cancelationToken.TryRegister(routine, out routine._cancelationRegistration);

                if (routine._isInvokingComplete & sameRunner)
                {
                    // The routine is already running, so don't start a new one, just set the continue flag. This prevents extra GC allocations from Unity's StartCoroutine.
                    routine._shouldContinueCoroutine = true;
                }
                else
                {
                    runner.StartCoroutine(routine);
                }
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
                return _shouldContinueCoroutine; // This is usually false, it only gets set to true when this is re-used from the continuation.
            }

            private void Complete()
            {
                _cancelationRegistration.Dispose();
                SetCompleteAndRepoolAndGetDeferred().Resolve();
                _isInvokingComplete = false;
            }

            void ICancelable.Cancel()
            {
                // The token could be canceled from any thread, so we have to dispatch the cancelation logic to the main thread.
                Promise.Run(ValueTuple.Create(this, _id), (cv) => cv.Item1.OnCancel(cv.Item2),
                    PromiseYielderBehaviour.s_creatorThread == Thread.CurrentThread ? SynchronizationOption.Synchronous : SynchronizationOption.Foreground, forceAsync: false)
                    .Forget();
            }

            private void OnCancel(int id)
            {
                if (id != _id)
                {
                    // The yield instruction already completed.
                    return;
                }

                var runner = _currentRunnerRef.Target as MonoBehaviour;
                // If it's null, the monobehaviour was destroyed, so the coroutine will have already been stopped.
                if (runner != null)
                {
                    runner.StopCoroutine(this);
                }

                SetCompleteAndRepoolAndGetDeferred().Cancel();
                _isInvokingComplete = false;
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
                _shouldContinueCoroutine = false;
                _isYieldInstructionComplete = false;
                _isInvokingComplete = true;
                // Place this back in the pool before invoking the deferred, in case the invocation will re-use this.
                Internal.ObjectPool.MaybeRepool(this);
                return deferred;
            }

            void IEnumerator.Reset() { }
        }

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
            return Routine.WaitForInstruction(yieldInstruction, runner, cancelationToken);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve after 1 frame.
        /// </summary>
        /// <param name="runner">The <see cref="MonoBehaviour"/> instance on which the wait will be ran.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to stop the internal wait and cancel the promise.</param>
        /// <remarks>
        /// If <paramref name="runner"/> is provided, the coroutine will be ran on it, otherwise it will be ran on the singleton PromiseYielder instance.
        /// </remarks>
        public static Promise WaitOneFrame(MonoBehaviour runner, CancelationToken cancelationToken = default(CancelationToken))
        {
            return runner == null
                ? WaitOneFrame(cancelationToken)
                : Routine.WaitForInstruction(null, runner, cancelationToken);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve after 1 frame.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to stop the internal wait and cancel the promise.</param>
        public static Promise WaitOneFrame(CancelationToken cancelationToken = default(CancelationToken))
        {
            return PromiseYielderBehaviour.WaitForInstruction(new WaitOneFrameInstruction(), cancelationToken);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private struct WaitOneFrameInstruction : IWaitInstruction
        {
            private bool _isComplete;

            [MethodImpl(Internal.InlineOption)]
            bool IWaitInstruction.GetIsComplete(Promise.Deferred deferred)
            {
                var temp = _isComplete;
                _isComplete = true;
                return temp;
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve after the specified number of frames have passed.
        /// </summary>
        /// <param name="frames">How many frames to wait for.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to stop the internal wait and cancel the promise.</param>
        public static Promise WaitForFrames(uint frames, CancelationToken cancelationToken = default(CancelationToken))
        {
            return PromiseYielderBehaviour.WaitForInstruction(new WaitFramesInstruction(frames), cancelationToken);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private struct WaitFramesInstruction : IWaitInstruction
        {
            private readonly uint _max;
            private uint _counter;

            internal WaitFramesInstruction(uint frames)
            {
                _max = frames;
                _counter = 0;
            }

            [MethodImpl(Internal.InlineOption)]
            bool IWaitInstruction.GetIsComplete(Promise.Deferred deferred)
            {
                var counter = _counter;
                unchecked
                {
                    ++_counter;
                }
                if (counter >= _max)
                {
                    return true;
                }
                deferred.TryReportProgress((float) counter / _max);
                return false;
            }
        }
    }
}