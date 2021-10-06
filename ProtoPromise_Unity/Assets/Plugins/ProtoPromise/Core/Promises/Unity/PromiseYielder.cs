#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable RECS0108 // Warns about static fields in generic types

using System;
using System.Collections;
using UnityEngine;

namespace Proto.Promises
{
    partial struct Promise
    {
        /// <summary>
        /// Yield instruction that can be yielded in a coroutine to wait until the <see cref="Promise"/> it came from has settled.
        /// An instance of this should be disposed when you are finished with it.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public abstract class YieldInstruction : CustomYieldInstruction, IDisposable
        {
            volatile protected object _value;
            volatile protected State _state;
            volatile protected bool _isActive;

            internal YieldInstruction() { }

            /// <summary>
            /// The state of the <see cref="Promise"/> this came from.
            /// </summary>
            /// <value>The state.</value>
            public State State
            {
                get
                {
                    ValidateOperation();
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
                    ValidateOperation();
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
                ValidateOperation();

                if (_state == State.Pending)
                {
                    throw new InvalidOperationException("Promise is still pending. You must wait for the promse to settle before calling GetResult.", Internal.GetFormattedStacktrace(1));
                }

                if (_state == State.Resolved)
                {
                    return;
                }
                // Throw unhandled exception or canceled exception.
                throw ((Internal.IThrowable) _value).GetException();
            }

            /// <summary>
            /// Adds this object back to the pool if object pooling is enabled.
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
                ValidateOperation();

                // Not bothering to remove from owner's branches, just mark for when the promise completes.
                _isActive = false;
                if (_state != State.Pending)
                {
                    ((IRetainable) _value).Release();
                    _value = null;
                }
            }

#if CSHARP_7_3_OR_NEWER // private protected not available in language versions.
            private protected void Settle(Internal.IValueContainer valueContainer)
            {
#else
            protected void Settle(object container)
            {
                Internal.IValueContainer valueContainer = (Internal.IValueContainer) container;
#endif
                // TODO: thread safety
                if (_isActive)
                {
                    valueContainer.Retain();
                    _value = valueContainer;
                    _state = valueContainer.GetState();
                }
            }

            protected void ValidateOperation()
            {
                if (!_isActive)
                {
                    throw new InvalidOperationException("Promise yield instruction is not valid. You can get a validate yield instruction by calling promise.ToYieldInstruction(). After you have disposed ", Internal.GetFormattedStacktrace(1));
                }
            }
        }

        /// <summary>
        /// Returns a new <see cref="YieldInstruction"/> that can be yielded in a coroutine to wait until this is settled.
        /// </summary>
        public YieldInstruction ToYieldInstruction()
        {
            ValidateOperation(1);

            Internal.YieldInstructionVoid yieldInstruction;
            if (_target._ref != null)
            {
                yieldInstruction = Internal.YieldInstructionVoid.GetOrCreate(null, State.Pending);
                var executionStack = new Internal.ValueLinkedStack<Internal.ITreeHandleable>();
                _target._ref.AddWaiter(yieldInstruction, ref executionStack);
            }
            else
            {
                yieldInstruction = Internal.YieldInstructionVoid.GetOrCreate(Internal.ResolveContainerVoid.GetOrCreate(), State.Resolved);
            }
            return yieldInstruction;
        }
    }

    partial struct Promise<T>
    {
        /// <summary>
        /// Yield instruction that can be yielded in a coroutine to wait until the <see cref="Promise{T}"/> it came from has settled.
        /// An instance of this should be disposed when you are finished with it.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public abstract class YieldInstruction : Promise.YieldInstruction
        {
            internal YieldInstruction() { }

            /// <summary>
            /// Get the result. If the Promise resolved successfully, this will return the result of the operation.
            /// If the Promise was rejected, this will throw an <see cref="UnhandledException"/>.
            /// If the Promise was canceled, this will throw a <see cref="CanceledException"/>.
            /// </summary>
            public new T GetResult()
            {
                ValidateOperation();

                if (_state == Promise.State.Pending)
                {
                    throw new InvalidOperationException("Promise is still pending. You must wait for the promse to settle before calling GetResult.", Internal.GetFormattedStacktrace(1));
                }

                if (_state == Promise.State.Resolved)
                {
                    return ((Internal.ResolveContainer<T>) _value).value;
                }
                // Throw unhandled exception or canceled exception.
                throw ((Internal.IThrowable) _value).GetException();
            }
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}.YieldInstruction"/> that can be yielded in a coroutine to wait until this is settled.
        /// </summary>
        public YieldInstruction ToYieldInstruction()
        {
            ValidateOperation(1);

            Internal.YieldInstruction<T> yieldInstruction;
            if (_ref != null)
            {
                yieldInstruction = Internal.YieldInstruction<T>.GetOrCreate(null, Promise.State.Pending);
                var executionStack = new Internal.ValueLinkedStack<Internal.ITreeHandleable>();
                _ref.AddWaiter(yieldInstruction, ref executionStack);
            }
            else
            {
                yieldInstruction = Internal.YieldInstruction<T>.GetOrCreate(Internal.ResolveContainerVoid.GetOrCreate(), Promise.State.Resolved);
            }
            return yieldInstruction;
        }
    }

    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal sealed class YieldInstructionVoid : Promise.YieldInstruction, ITreeHandleable // Annoying old runtime can't compile generic ObjectPool with interface only in the base.
        {
            private YieldInstructionVoid() { }

            public static YieldInstructionVoid GetOrCreate(object valueContainer, Promise.State state)
            {
                var yieldInstruction = ObjectPool<ITreeHandleable>.TryTake<YieldInstructionVoid>()
                    ?? new YieldInstructionVoid();
                yieldInstruction._value = valueContainer;
                yieldInstruction._state = state;
                yieldInstruction._isActive = true;
                return yieldInstruction;
            }

            public override void Dispose()
            {
                base.Dispose();
                ObjectPool<ITreeHandleable>.MaybeRepool(this);
            }

            ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

            void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
            {
                Settle(valueContainer);
            }

            void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
            {
                Settle(valueContainer);
            }

            void ITreeHandleable.Handle(ref ValueLinkedStack<ITreeHandleable> executionStack) { throw new System.InvalidOperationException(); }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal sealed class YieldInstruction<T> : Promise<T>.YieldInstruction, ITreeHandleable // Annoying old runtime can't compile generic ObjectPool with interface only in the base.
        {
            private YieldInstruction() { }

            public static YieldInstruction<T> GetOrCreate(object valueContainer, Promise.State state)
            {
                var yieldInstruction = ObjectPool<ITreeHandleable>.TryTake<YieldInstruction<T>>()
                    ?? new YieldInstruction<T>();
                yieldInstruction._value = valueContainer;
                yieldInstruction._state = state;
                yieldInstruction._isActive = true;
                return yieldInstruction;
            }

            public override void Dispose()
            {
                base.Dispose();
                ObjectPool<ITreeHandleable>.MaybeRepool(this);
            }

            ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

            void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
            {
                Settle(valueContainer);
            }

            void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
            {
                Settle(valueContainer);
            }

            void ITreeHandleable.Handle(ref ValueLinkedStack<ITreeHandleable> executionStack) { throw new System.InvalidOperationException(); }
        }
    }

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

namespace UnityEngine
{
#if !UNITY_5_3_OR_NEWER
    /// <summary>
    /// Custom yield instruction. Use yield return StartCoroutine(customYieldInstruction)
    /// </summary>
    public abstract class CustomYieldInstruction : IEnumerator
    {
        public abstract bool keepWaiting { get; }

        public object Current { get { return null; } }

        public bool MoveNext()
        {
            return keepWaiting;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
#endif
}