#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable RECS0108 // Warns about static fields in generic types

using System;
using System.Collections;
using Proto.Utils;
using UnityEngine;

namespace Proto.Promises
{
    partial class Promise
    {
        /// <summary>
        /// Yield instruction that can be yielded in a coroutine to wait until the <see cref="Promise"/> it came from has settled.
        /// An instance of this should be disposed when you are finished with it.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough]
        public abstract class YieldInstruction : CustomYieldInstruction, IDisposable, Internal.ITreeHandleable
        {
            Internal.ITreeHandleable ILinked<Internal.ITreeHandleable>.Next { get; set; }

            protected object _value;
            protected State _state;

            internal YieldInstruction() { }

            /// <summary>
            /// The state of the <see cref="Promise"/> this came from.
            /// </summary>
            /// <value>The state.</value>
            public State State
            {
                get
                {
                    ValidateYieldInstructionOperation(_value, 1);

                    return _state;
                }
            }

            /// <summary>
            /// Is the Promise still pending?
            /// </summary>
            public override bool keepWaiting
            {
                get
                {
                    ValidateYieldInstructionOperation(_value, 1);

                    return State == State.Pending;
                }
            }

            /// <summary>
            /// Get the result. If the Promise resolved successfully, this will return without error.
            /// If the Promise was rejected, this will throw an <see cref="UnhandledException"/>.
            /// If the Promise was canceled, this will throw a <see cref="CanceledException"/>.
            /// </summary>
            public void GetResult()
            {
                ValidateYieldInstructionOperation(_value, 1);

                if (_state == State.Pending)
                {
                    throw new InvalidOperationException("Promise is still pending. You must wait for the promse to settle before calling GetResult.", GetFormattedStacktrace(1));
                }

                if (_state == State.Resolved)
                {
                    return;
                }
                // Throw unhandled exception or canceled exception.
                throw ((Internal.IThrowable) _value).GetException();
            }

            /// <summary>
            /// Adds this object back to the pool.
            /// Don't try to access it after disposing! Results are undefined.
            /// </summary>
            /// <remarks>Call <see cref="Dispose"/> when you are finished using the
            /// <see cref="T:ProtoPromise.Promise.YieldInstruction"/>. The <see cref="Dispose"/> method leaves the
            /// <see cref="T:ProtoPromise.Promise.YieldInstruction"/> in an unusable state. After calling
            /// <see cref="Dispose"/>, you must release all references to the
            /// <see cref="T:ProtoPromise.Promise.YieldInstruction"/> so the garbage collector can reclaim the memory
            /// that the <see cref="T:ProtoPromise.Promise.YieldInstruction"/> was occupying.</remarks>
            public virtual void Dispose()
            {
                ValidateYieldInstructionOperation(_value, 1);

                ((IRetainable) _value).Release();
                _value = DisposedObject;
            }

            void Internal.ITreeHandleable.MakeReady(Internal.IValueContainer valueContainer,
                ref ValueLinkedQueue<Internal.ITreeHandleable> handleQueue,
                ref ValueLinkedQueue<Internal.ITreeHandleable> cancelQueue)
            {
                valueContainer.Retain();
                _value = valueContainer;
                _state = valueContainer.GetState();
            }

            void Internal.ITreeHandleable.MakeReadyFromSettled(Internal.IValueContainer valueContainer)
            {
                valueContainer.Retain();
                _value = valueContainer;
                _state = valueContainer.GetState();
            }

            void Internal.ITreeHandleable.Handle() { throw new System.InvalidOperationException(); }
            void Internal.ITreeHandleable.Cancel() { throw new System.InvalidOperationException(); }
        }
    }

    partial class Promise<T>
    {
        /// <summary>
        /// Yield instruction that can be yielded in a coroutine to wait until the <see cref="Promise{T}"/> it came from has settled.
        /// An instance of this should be disposed when you are finished with it.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough]
        public abstract new class YieldInstruction : Promise.YieldInstruction
        {
            internal YieldInstruction() { }

            /// <summary>
            /// Get the result. If the Promise resolved successfully, this will return the result of the operation.
            /// If the Promise was rejected, this will throw an <see cref="UnhandledException"/>.
            /// If the Promise was canceled, this will throw a <see cref="CanceledException"/>.
            /// </summary>
            public new T GetResult()
            {
                ValidateYieldInstructionOperation(_value, 1);

                if (_state == State.Pending)
                {
                    throw new InvalidOperationException("Promise is still pending. You must wait for the promse to settle before calling GetResult.", GetFormattedStacktrace(1));
                }

                if (_state == State.Resolved)
                {
                    return ((Internal.ResolveContainer<T>) _value).value;
                }
                // Throw unhandled exception or canceled exception.
                throw ((Internal.IThrowable) _value).GetException();
            }
        }
    }

    partial class Promise
    {
        partial class Internal
        {
            [System.Diagnostics.DebuggerStepThrough]
            public sealed class YieldInstructionVoid : YieldInstruction
            {
                private static ValueLinkedStack<ITreeHandleable> _pool;

                static YieldInstructionVoid()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private YieldInstructionVoid() { }

                public static YieldInstructionVoid GetOrCreate(Promise owner)
                {
                    var yieldInstruction = _pool.IsNotEmpty ? (YieldInstructionVoid) _pool.Pop() : new YieldInstructionVoid();
                    yieldInstruction._state = owner._state;
                    return yieldInstruction;
                }

                public override void Dispose()
                {
                    base.Dispose();
                    if (Config.ObjectPooling == PoolType.All)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class YieldInstruction<T> : Promise<T>.YieldInstruction, ITreeHandleable
            {
                private static ValueLinkedStack<ITreeHandleable> _pool;

                static YieldInstruction()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private YieldInstruction() { }

                public static YieldInstruction<T> GetOrCreate(Promise owner)
                {
                    var yieldInstruction = _pool.IsNotEmpty ? (YieldInstruction<T>) _pool.Pop() : new YieldInstruction<T>();
                    yieldInstruction._state = owner._state;
                    return yieldInstruction;
                }

                public override void Dispose()
                {
                    base.Dispose();
                    if (Config.ObjectPooling == PoolType.All)
                    {
                        _pool.Push(this);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Yielder used to wait for a yield instruction to complete in the form of a Promise, using Unity's coroutines.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public sealed class PromiseYielder : MonoBehaviour
    {
        static Action _onClearObjects;
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
                Logger.LogWarning("There can only be one instance of PromiseYielder. Destroying new instance.");
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
                    Logger.LogWarning("PromiseYielder destroyed! Any pending yield promises will not be resolved!");
                    _instance = null;
                }
            }
        }

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

            void IEnumerator.Reset() { }
        }

        /// <summary>
        /// Returns a <see cref="Promise{TYieldInstruction}"/> that resolves with the <paramref name="yieldInstruction"/> after the <paramref name="yieldInstruction"/> has completed.
        /// </summary>
        /// <param name="yieldInstruction">Yield instruction.</param>
        /// <typeparam name="TYieldInstruction">The type of yieldInstruction.</typeparam>
        public static Promise<TYieldInstruction> WaitFor<TYieldInstruction>(TYieldInstruction yieldInstruction) where TYieldInstruction : class // Class constraint to prevent boxing.
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
            routine.onComplete = Promise.NewDeferred();

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
