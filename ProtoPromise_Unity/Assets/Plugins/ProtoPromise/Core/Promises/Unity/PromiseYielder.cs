#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
            partial class CallbackHelper
            {
                // TODO: refactor to change Promise(<T>).ToYieldInstruction() to an extension instead of part of the type.
                internal static Promise<T>.YieldInstruction AddYieldInstruction<T>(Promise<T> _this)
                {
                    YieldInstruction<T> yieldInstruction;
                    if (_this._ref == null)
                    {
                        yieldInstruction = YieldInstruction<T>.GetOrCreate(CreateResolveContainer(_this.Result, 1), Promise.State.Resolved);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this.Id, PromiseFlags.WasAwaitedOrForgotten | PromiseFlags.SuppressRejection);
                        yieldInstruction = YieldInstruction<T>.GetOrCreate(null, Promise.State.Pending);
                        _this._ref.HookupNewWaiter(yieldInstruction);
                    }
                    return yieldInstruction;
                }
            }
        }
    }

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
            public abstract void Dispose();

            protected void ValidateOperation()
            {
                if (!_isActive)
                {
                    throw new InvalidOperationException("Promise yield instruction is not valid after you have disposed. You can get a validate yield instruction by calling promise.ToYieldInstruction().", Internal.GetFormattedStacktrace(1));
                }
            }
        }

        /// <summary>
        /// Returns a new <see cref="YieldInstruction"/> that can be yielded in a coroutine to wait until this is settled.
        /// </summary>
        public YieldInstruction ToYieldInstruction()
        {
            return _target.ToYieldInstruction();
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

            return Internal.PromiseRef.CallbackHelper.AddYieldInstruction(this);
        }
    }

    partial class Internal
    {

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal sealed class YieldInstruction<T> : Promise<T>.YieldInstruction, ITreeHandleable // Annoying old runtime can't compile generic ObjectPool with interface only in the base.
        {
            ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

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
                ValidateOperation();

                // Not bothering to remove from owner's branches, just mark for when the promise completes.
                _isActive = false;
                Thread.MemoryBarrier();
                object container = Interlocked.Exchange(ref _value, null);
                if (container != null)
                {
                    ((IRetainable) container).Release();
                }
#if !PROMISE_DEBUG // Don't repool in DEBUG mode.
                ObjectPool<ITreeHandleable>.MaybeRepool(this);
#endif
            }

            private void Settle(IValueContainer valueContainer)
            {
                valueContainer.Retain();
                _value = valueContainer;
                _state = valueContainer.GetState();
                Thread.MemoryBarrier();
                if (!_isActive) // Was disposed?
                {
                    // Handle race condition with Dispose. Make sure we're removing the same container.
                    var container = Interlocked.CompareExchange(ref _value, null, valueContainer);
                    if (container != null)
                    {
                        ((IRetainable) container).Release();
                    }
                }
            }

            void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
            {
                Settle(valueContainer);
            }

            void ITreeHandleable.Handle(ref ExecutionScheduler executionScheduler) { throw new System.InvalidOperationException(); }
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