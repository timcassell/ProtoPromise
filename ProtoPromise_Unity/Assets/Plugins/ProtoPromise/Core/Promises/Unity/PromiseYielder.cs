#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

using System.Collections;
using UnityEngine;

namespace Proto.Promises
{
    /// <summary>
    /// Yielder used to wait for a yield instruction to complete in the form of a Promise, using Unity's coroutines.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public sealed class PromiseYielder : MonoBehaviour
    {
        static PromiseYielder _instance;

        static PromiseYielder Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("Proto.Promises.PromiseYielder").AddComponent<PromiseYielder>();
                }
                return _instance;
            }
        }

        private PromiseYielder() { }

        private void Start()
        {
            if (_instance != this)
            {
                Debug.LogWarning("There can only be one instance of PromiseYielder. Destroying new instance.");
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
                if (_instance == this)
                {
                    Debug.LogWarning("PromiseYielder destroyed! Any pending PromiseYielder.WaitFor promises will not be resolved!");
                    _instance = null;
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        private class Routine : IEnumerator, Internal.ILinked<Routine>
        {
            private MonoBehaviour _currentRunner;
            private Promise.Deferred _deferred;
            private bool _continue;

            public object Current { get; private set; }
            Routine Internal.ILinked<Routine>.Next { get; set; }

            private Routine() { }

            public static void WaitForInstruction(Promise.Deferred deferred, object yieldInstruction, MonoBehaviour runner)
            {
                var routine = Internal.ObjectPool<Routine>.TryTake<Routine>()
                    ?? new Routine();
                bool sameRunner = routine._currentRunner == runner & runner != null;
                routine._currentRunner = runner != null ? runner : Instance;
                routine._deferred = deferred;
                routine.Current = yieldInstruction;
                if (routine._continue & sameRunner)
                {
                    // The routine is already running, so don't start a new one, just set the continue flag. This prevents extra GC allocations from Unity's Coroutine.
                    routine._continue = false;
                }
                else
                {
                    routine._currentRunner.StartCoroutine(routine);
                }
            }

            public bool MoveNext()
            {
                // As a coroutine, this will wait for the Current's yield, then execute this once, then stop.
                if (_continue)
                {
                    Complete();
                }
                return _continue = !_continue; // If the continue flag is flipped from the callback, this will continue to run.
            }

            void Complete()
            {
                var deferred = _deferred;
                _deferred = default(Promise.Deferred);
                Current = null;
                // Place this back in the pool before invoking in case the invocation will re-use this.
                Internal.ObjectPool<Routine>.MaybeRepool(this);
                try
                {
                    deferred.Resolve();
                }
                catch
                {
                    // Reset the flag if there was an error. This should never happen.
                    _continue = false;
                    throw;
                }
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
            var deferred = Promise.NewDeferred();
            Routine.WaitForInstruction(deferred, yieldInstruction, runner);
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve after 1 frame.
        /// If <paramref name="runner"/> is provided, the coroutine will be ran on it, otherwise it will be ran on the singleton PromiseYielder instance.
        /// </summary>
        public static Promise WaitOneFrame(MonoBehaviour runner = null)
        {
            return WaitFor(null, runner);
        }
    }
}