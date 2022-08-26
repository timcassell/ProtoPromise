#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace Proto.Promises
{
    /// <summary>
    /// Yielder used to wait for a yield instruction to complete in the form of a Promise, using Unity's coroutines.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    [AddComponentMenu("")] // Hide this in the add component menu.
    public sealed class PromiseYielder : MonoBehaviour
    {
        private static PromiseYielder s_instance;

        static PromiseYielder Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new GameObject("Proto.Promises.PromiseYielder").AddComponent<PromiseYielder>();
                }
                return s_instance;
            }
        }

        private PromiseYielder() { }

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
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
#endif
            {
                if (s_instance == this)
                {
                    UnityEngine.Debug.LogWarning("PromiseYielder destroyed! Any pending PromiseYielder.WaitFor promises will not be resolved!");
                    s_instance = null;
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private class Routine : Internal.HandleablePromiseBase, IEnumerator
        {
            // WeakReference so we aren't creating a memory leak of MonoBehaviours while this is in the pool.
            private readonly WeakReference _currentRunnerRef = new WeakReference(null, false);
            private Promise.Deferred _deferred;
            private bool _isInvokingComplete;
            private bool _isYieldInstructionComplete;
            private bool _shouldContinueCoroutine;

            public object Current { get; private set; }

            private Routine() { }

            internal static Promise WaitForInstruction(object yieldInstruction, MonoBehaviour runner)
            {
                var routine = Internal.ObjectPool.TryTake<Routine>()
                    ?? new Routine();
                routine._deferred = Promise.NewDeferred();
                bool validRunner = runner != null;
                runner = validRunner ? runner : Instance;
                bool sameRunner = ReferenceEquals(runner, routine._currentRunnerRef.Target);
                routine._currentRunnerRef.Target = runner;
                routine.Current = yieldInstruction;

                if (routine._isInvokingComplete & sameRunner)
                {
                    // The routine is already running, so don't start a new one, just set the continue flag. This prevents extra GC allocations from Unity's Coroutine.
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
                var deferred = _deferred;
                _deferred = default(Promise.Deferred);
                Current = null;
                _shouldContinueCoroutine = false;
                _isYieldInstructionComplete = false;
                // Place this back in the pool before invoking in case the invocation will re-use this.
                Internal.ObjectPool.MaybeRepool(this);

                _isInvokingComplete = true;
                deferred.Resolve();
                _isInvokingComplete = false;
            }

            void IEnumerator.Reset() { }
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve after the <paramref name="yieldInstruction"/> has completed.
        /// If <paramref name="runner"/> is provided, the coroutine will be ran on it, otherwise it will be ran on the singleton PromiseYielder instance.
        /// </summary>
        /// <param name="yieldInstruction">The yield instruction to wait for.</param>
        public static Promise WaitFor(object yieldInstruction, MonoBehaviour runner = null)
        {
            return Routine.WaitForInstruction(yieldInstruction, runner);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve after 1 frame.
        /// If <paramref name="runner"/> is provided, the coroutine will be ran on it, otherwise it will be ran on the singleton PromiseYielder instance.
        /// </summary>
        public static Promise WaitOneFrame(MonoBehaviour runner = null)
        {
            return Routine.WaitForInstruction(null, runner);
        }
    }
}