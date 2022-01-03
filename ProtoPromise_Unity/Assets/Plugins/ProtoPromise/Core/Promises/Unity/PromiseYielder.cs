#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

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
            Routine Internal.ILinked<Routine>.Next { get; set; }

            public static Routine GetOrCreate()
            {
                return Internal.ObjectPool<Routine>.TryTake<Routine>()
                    ?? new Routine();
            }

            private Routine() { }

            public Promise.Deferred onComplete;
            public bool _continue;

            public object Current { get { return null; } }

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
                var deferred = onComplete;
                onComplete = default(Promise.Deferred);
                // Place this back in the pool before invoking in case the invocation will re-use this.
                Internal.ObjectPool<Routine>.MaybeRepool(this);
                try
                {
                    deferred.Resolve();
                    // Don't need to handle completes here since they will be handled in PromiseBehaviour.
                }
                catch
                {
                    // Reset the flag if there was an error.
                    _continue = false;
                    throw;
                }
            }

            void IEnumerator.Reset() { }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        private class Routine<T> : IEnumerator, Internal.ILinked<Routine<T>>
        {
            Routine<T> Internal.ILinked<Routine<T>>.Next { get; set; }

            public static Routine<T> GetOrCreate()
            {
                return Internal.ObjectPool<Routine<T>>.TryTake<Routine<T>>()
                    ?? new Routine<T>();
            }

            private Routine() { }

            public Promise<T>.Deferred onComplete;
            public bool _continue;

            public T Current { get; set; }
            object IEnumerator.Current { get { return Current; } }

            public bool MoveNext()
            {
                // As a coroutine, this will wait for the Current's yield, then execute this once, then stop.
                if (_continue)
                {
                    Complete();
                }
                return _continue = !_continue;
            }

            public void Complete()
            {
                var deferred = onComplete;
                onComplete = default(Promise<T>.Deferred);
                T tempObj = Current;
                Current = default(T);
                // Place this back in the pool before invoking in case the invocation will re-use this.
                Internal.ObjectPool<Routine<T>>.MaybeRepool(this);
                try
                {
                    deferred.Resolve(tempObj);
                }
                catch
                {
                    // Reset the flag if there was an error.
                    _continue = false;
                    throw;
                }
            }

            void IEnumerator.Reset() { }
        }

        /// <summary>
        /// Returns a <see cref="Promise{TYieldInstruction}"/> that resolves with the <paramref name="yieldInstruction"/> after the <paramref name="yieldInstruction"/> has completed.
        /// </summary>
        /// <param name="yieldInstruction">Yield instruction.</param>
        /// <typeparam name="TYieldInstruction">The type of yieldInstruction.</typeparam>
        public static Promise<TYieldInstruction> WaitFor<TYieldInstruction>(TYieldInstruction yieldInstruction)
        {
            Routine<TYieldInstruction> routine = Routine<TYieldInstruction>.GetOrCreate();
            routine.Current = yieldInstruction;
            routine.onComplete = Promise<TYieldInstruction>.Deferred.New();

            if (routine._continue)
            {
                // The routine is already running, so don't start a new one, just set the continue flag. This prevents extra GC allocations from Unity's Coroutine.
                routine._continue = false;
            }
            else
            {
                Instance.StartCoroutine(routine);
            }

            return routine.onComplete.Promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that resolves after 1 frame.
        /// </summary>
        public static Promise WaitOneFrame()
        {
            Routine routine = Routine.GetOrCreate();
            routine.onComplete = Promise.Deferred.New();

            if (routine._continue)
            {
                // The routine is already running, so don't start a new one, just set the continue flag. This prevents extra GC allocations from Unity's Coroutine.
                routine._continue = false;
            }
            else
            {
                Instance.StartCoroutine(routine);
            }

            return routine.onComplete.Promise;
        }
    }
}