#pragma warning disable IDE0034 // Simplify 'default' expression
using System.Collections;
using UnityEngine;

namespace Proto.Promises
{
    partial class Promise
    {
        static Promise()
        {
            Config.Yielder = new GameObject("ProtoPromise.PromiseBehaviour")
            {
                hideFlags = HideFlags.HideAndDontSave // Don't show in hierarchy and don't destroy.
            }
            .AddComponent<PromiseBehaviour>();
        }
    }

    public sealed class PromiseBehaviour : MonoBehaviour, IPromiseYielder
    {
        private static PromiseBehaviour _instance;

        private void Awake()
        {
            _instance = this;
            StartCoroutine(_Enumerator());
        }

        private void OnDestroy()
        {
            Logger.LogWarning("PromiseBehaviour destroyed! Promise callbacks will no longer be automatically invoked!");
        }

        private IEnumerator _Enumerator()
        {
            var waitForEndOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return null;
                // Invoke progress delegates during the normal coroutine cycle.
                Promise.Manager.HandleCompletesAndProgress();
            }
        }

        private void Update()
        {
            Promise.Manager.HandleCompletes();
        }

        // Optionally add extra calls for LateUpdate, FixedUpdate, etc.



        static System.Action _onClearObjects;

        public static void ClearPooledObjects()
        {
            if (_onClearObjects != null)
            {
                _onClearObjects.Invoke();
            }
        }

        private class Routine : IEnumerator, ILinked<Routine>
        {
            Routine ILinked<Routine>.Next { get; set; }

            private static ValueLinkedStack<Routine> _pool;

            public static Routine GetOrCreate()
            {
                return _pool.IsNotEmpty ? _pool.Pop() : new Routine();
            }

            static Routine()
            {
                _onClearObjects += () => _pool.Clear();
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
                onComplete = null;
                // Place this back in the pool before invoking in case the invocation will re-use this.
                _pool.Push(this);
                try
                {
                    deferred.Resolve();
                }
                catch
                {
                    // Reset the flag if there was an error.
                    _continue = false;
                    throw;
                }
            }

            void Cancel()
            {
                _continue = false;
                _instance.StopCoroutine(this);
                onComplete = null;
                _pool.Push(this);
            }

            void IEnumerator.Reset() { }
        }

        private class Routine<T> : IEnumerator, ILinked<Routine<T>>
        {
            Routine<T> ILinked<Routine<T>>.Next { get; set; }

#pragma warning disable RECS0108 // Warns about static fields in generic types
            private static ValueLinkedStack<Routine<T>> _pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

            public static Routine<T> GetOrCreate()
            {
                return _pool.IsNotEmpty ? _pool.Pop() : new Routine<T>();
            }

            static Routine()
            {
                _onClearObjects += () => _pool.Clear();
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
                onComplete = null;
                T tempObj = Current;
                Current = default(T);
                // Place this back in the pool before invoking in case the invocation will re-use this.
                _pool.Push(this);
                try
                {
                    deferred.Resolve(tempObj);
                    Promise.Manager.HandleCompletes(); // Handle callbacks in case the yield instruction was WaitForEndOfFrame or WaitForFixedUpdate or other unusual yield.
                }
                catch
                {
                    // Reset the flag if there was an error.
                    _continue = false;
                    throw;
                }
            }

            public void Cancel()
            {
                _continue = false;
                _instance.StopCoroutine(this);
                onComplete = null;
                Current = default(T);
                _pool.Push(this);
            }

            void IEnumerator.Reset() { }
        }


        Promise<TYieldInstruction> IPromiseYielder.Yield<TYieldInstruction>(TYieldInstruction yieldInstruction)
        {
            Routine<TYieldInstruction> routine = Routine<TYieldInstruction>.GetOrCreate();
            routine.Current = yieldInstruction;
            routine.onComplete = Promise.NewDeferred<TYieldInstruction>();

            if (routine._continue)
            {
                // The routine is already running, so don't start a new one, just set the continue flag. This prevents extra GC allocations from Unity's Coroutine.
                routine._continue = false;
            }
            else
            {
                StartCoroutine(routine);
            }

            return routine.onComplete.Promise;
        }

        Promise IPromiseYielder.Yield()
        {
            Routine routine = Routine.GetOrCreate();
            routine.onComplete = Promise.NewDeferred();

            if (routine._continue)
            {
                // The routine is already running, so don't start a new one, just set the continue flag. This prevents extra GC allocations from Unity's Coroutine.
                routine._continue = false;
            }
            else
            {
                StartCoroutine(routine);
            }

            return routine.onComplete.Promise;
        }
    }
}