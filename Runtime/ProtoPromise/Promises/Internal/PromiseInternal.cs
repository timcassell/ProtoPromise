#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable RECS0001 // Class is declared partial but has only one part

using Proto.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Proto.Promises
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    [StructLayout(LayoutKind.Auto)]
    partial struct Promise
    {
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly Internal.PromiseRef _ref;
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly int _id; // Only using int because Interlocked does not support ushort.

        /// <summary>
        /// Internal use.
        /// </summary>
        internal Promise(Internal.PromiseRef promiseRef, int id)
        {
            _ref = promiseRef;
            _id = id;
        }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    [StructLayout(LayoutKind.Auto)]
    partial struct Promise<T>
    {
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly Internal.PromiseRef _ref;
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly int _id; // Only using int because Interlocked does not support ushort.
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly T _result;

        /// <summary>
        /// Internal use.
        /// </summary>
        internal Promise(Internal.PromiseRef promiseRef, int id)
        {
            _ref = promiseRef;
            _id = id;
            _result = default(T);
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        internal Promise(Internal.PromiseRef promiseRef, int id, ref T value)
        {
            _ref = promiseRef;
            _id = id;
            _result = value;
        }
    }

    partial class Internal
    {
        // Just a random number that's not zero. Using this in Promise(<T>) instead of a bool prevents extra memory padding.
        internal const int ValidPromiseIdFromApi = 41265;

        internal abstract partial class PromiseRef : ITreeHandleable, ITraceable, ITreeHandleableCollection
        {
            private ValueLinkedStack<ITreeHandleable> _nextBranches;
            private object _valueOrPrevious;
            private int _id = 1; // Only using int because Interlocked does not support ushort.
            private Promise.State _state;
            private bool _suppressRejection;
            private bool _wasAwaited;
            private byte _idIncrementer = 1; // Used to increment _id only if not preserved. 0 = preserved, 1 = not preserved.

            ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }
            internal int Id { get { return _id; } }
            internal Promise.State State
            {
                [MethodImpl((MethodImplOptions) 256)]
                get { return _state; }
            }
            private bool IsPreserved
            {
                [MethodImpl((MethodImplOptions) 256)]
                get { return _idIncrementer == 0; }
            }

            ~PromiseRef()
            {
                if (IsPreserved)
                {
                    // Promise was preserved without being forgotten.
                    string message = "A preserved Promise's resources were garbage collected without it being forgotten. You must call Forget() on each preserved promise when you are finished with it.";
                    AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                }
                else if (!_wasAwaited)
                {
                    // Promise was not awaited or forgotten.
                    string message = "A Promise's resources were garbage collected without it being awaited. You must await, return, or forget each promise.";
                    AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                }
                if (_state != Promise.State.Pending & _valueOrPrevious != null)
                {
                    if (_suppressRejection)
                    {
                        ((IValueContainer) _valueOrPrevious).Release();
                    }
                    else
                    {
                        // Rejection maybe wasn't caught.
                        ((IValueContainer) _valueOrPrevious).ReleaseAndAddToUnhandledStack();
                    }
                }
            }

            internal PromiseRef GetPreserved(int promiseId)
            {
                var duplicate = GetDuplicate(promiseId);
                duplicate._idIncrementer = 0; // Mark preserved.
                return duplicate;
            }

            internal void Forget(int promiseId)
            {
                ThrowIfInPool(this);
                IncrementId(promiseId, 1); // Always increment, whether preserved or not.
                _wasAwaited = true;
                _idIncrementer = 1; // Mark not preserved.
                MaybeDispose();
            }

            internal PromiseRef GetDuplicate(int promiseId)
            {
                ThrowIfInPool(this);
                // If new id is same as old, this is preserved and we must create a new object.
                // Otherwise, the simple increment is enough and we can reuse this object.
                var newId = IncrementId(promiseId, _idIncrementer);
                if (newId != promiseId)
                {
                    // Reset stack trace.
                    SetCreatedStacktrace(this, 2);
                    return this;
                }
                _wasAwaited = true;
                _suppressRejection = true;
                var newPromise = PromiseDuplicate.GetOrCreate();
                HookupNewPromise(newPromise);
                return newPromise;
            }

            private void MarkAwaited(int promiseId)
            {
                ThrowIfInPool(this);
                IncrementId(promiseId, _idIncrementer);
                _wasAwaited = true;
            }

            private int IncrementId(int promiseId, int increment)
            {
                int newId;
                unchecked // We want the id to wrap around.
                {
                    newId = promiseId + increment;
                }
                if (Interlocked.CompareExchange(ref _id, newId, promiseId) != promiseId)
                {
                    // Public APIs do a simple validation check in DEBUG mode, this is an extra thread-safe validation in case the same object is concurrently used and/or forgotten at the same time.
                    // This is left in RELEASE mode because concurrency issues can be very difficult to track down, and might not show up in DEBUG mode.
                    throw new InvalidOperationException("Attempted to mark an invalid Promise as awaited. This may be because you are attempting to use a promise simultaneously on multiple threads that you have not preserved.",
                        GetFormattedStacktrace(3));
                }
                return newId;
            }

            internal void MarkAwaitedAndMaybeDispose(int promiseId, bool suppressRejection)
            {
                ThrowIfInPool(this);
                MarkAwaited(promiseId);
                _suppressRejection |= suppressRejection;
                MaybeDispose();
            }

            void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
            {
                ThrowIfInPool(this);
                owner._suppressRejection = true;
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;
                handleQueue.Push(this);
            }

            void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
            {
                ThrowIfInPool(this);
                owner._suppressRejection = true;
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;
                AddToHandleQueueBack(this);
            }

            void ITreeHandleableCollection.Remove(ITreeHandleable treeHandleable)
            {
                ThrowIfInPool(this);
                _nextBranches.Remove(treeHandleable);
            }

            protected void Reset()
            {
                _state = Promise.State.Pending;
                _suppressRejection = false;
                _wasAwaited = false;
                SetCreatedStacktrace(this, 3);
            }

            private void MaybeDispose()
            {
                ThrowIfInPool(this);
                // TODO: thread synchronization
                if (_wasAwaited & !IsPreserved & _state != Promise.State.Pending)
                {
                    Dispose();
                }
            }

            protected virtual void Dispose()
            {
                if (_valueOrPrevious != null)
                {
                    if (_suppressRejection)
                    {
                        ((IValueContainer) _valueOrPrevious).Release();
                    }
                    else
                    {
                        // Rejection maybe wasn't caught.
                        ((IValueContainer) _valueOrPrevious).ReleaseAndAddToUnhandledStack();
                    }
                    _valueOrPrevious = null;
                }
            }

            private static void MaybeHookupNewPromise(Promise _this, PromiseRef newPromise)
            {
                ThrowIfInPool(_this._ref);
                // This is called from a Then/Catch/ContinueWith with a valid cancelationToken, which could have been fed an already canceled token.
                if (newPromise._valueOrPrevious != null)
                {
                    CancelFromHookup(_this, newPromise);
                }
                else
                {
                    HookupNewPromise(_this, newPromise);
                }
            }

            private static void HookupNewPromise(Promise _this, PromiseRef newPromise)
            {
                ThrowIfInPool(_this._ref);
                if (_this._ref == null)
                {
                    newPromise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                    newPromise.SetDepth();
                    AddToHandleQueueBack(newPromise);
                }
                else
                {
                    _this._ref.HookupNewPromise(newPromise);
                }
            }

            private static void MaybeHookupNewPromise<T>(Promise<T> _this, PromiseRef newPromise)
            {
                ThrowIfInPool(_this._ref);
                // This is called from a Then/Catch/ContinueWith with a valid cancelationToken, which could have been fed an already canceled token.
                if (newPromise._valueOrPrevious != null)
                {
                    CancelFromHookup(_this, newPromise);
                }
                else
                {
                    HookupNewPromise(_this, newPromise);
                }
            }

            private static void HookupNewPromise<T>(Promise<T> _this, PromiseRef newPromise)
            {
                ThrowIfInPool(_this._ref);
                if (_this._ref == null)
                {
                    T value = _this._result;
                    var container = ResolveContainer<T>.GetOrCreate(ref value);
                    container.Retain();
                    newPromise._valueOrPrevious = container;
                    newPromise.SetDepth();
                    AddToHandleQueueBack(newPromise);
                }
                else
                {
                    _this._ref.HookupNewPromise(newPromise);
                }
            }

            private static void CancelFromHookup(Promise _this, PromiseRef newPromise)
            {
                if (_this._ref != null)
                {
                    newPromise.SetDepth(_this._ref);
                    _this._ref.MaybeDispose();
                }
                else
                {
                    newPromise.SetDepth();
                }
                // TODO: don't add to handle queue, call a separate ExecuteCancelation method. (Rectify with CancelInternal.cs)
                AddToHandleQueueBack(newPromise);
            }

            private void HookupNewPromise(PromiseRef newPromise)
            {
                newPromise._valueOrPrevious = this;
                newPromise.SetDepth(this);
                AddWaiter(newPromise);
            }

            internal void AddWaiter(ITreeHandleable waiter)
            {
                // TODO: thread synchronization
                ThrowIfInPool(this);
                if (_state == Promise.State.Pending)
                {
                    _nextBranches.Push(waiter);
                }
                else
                {
                    waiter.MakeReadyFromSettled(this, (IValueContainer) _valueOrPrevious);
                }
                MaybeDispose();
            }

            void ITreeHandleable.Handle()
            {
                ThrowIfInPool(this);
                // TODO: thread synchronization, maybe cancelationToken.TryUnregister here?
                IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                _valueOrPrevious = null;
                SetCurrentInvoker(this);
                bool invokingRejected = false;
                try
                {
                    Execute(valueContainer, ref invokingRejected);
                    valueContainer.Release();
                }
                catch (RethrowException e)
                {
                    if (invokingRejected || e is ForcedRethrowException)
                    {
                        _state = valueContainer.GetState();
                        var previous = _valueOrPrevious;
                        _valueOrPrevious = valueContainer;
                        HandleBranches(valueContainer);
                        CancelProgressListeners(previous);

                        MaybeDispose();
                    }
                    else
                    {
                        valueContainer.Release();
#if PROMISE_DEBUG
                        string stacktrace = FormatStackTrace(new System.Diagnostics.StackTrace[1] { new System.Diagnostics.StackTrace(e, true) });
#else
                        string stacktrace = new System.Diagnostics.StackTrace(e, true).ToString();
#endif
                        object exception = new InvalidOperationException("RethrowException is only valid in promise onRejected callbacks.", stacktrace);
                        RejectOrCancelInternal(RejectionContainer<object>.GetOrCreate(ref exception));
                    }
                }
                catch (OperationCanceledException e)
                {
                    valueContainer.Release();
                    RejectOrCancelInternal(CreateCancelContainer(ref e));
                }
                catch (Exception e)
                {
                    valueContainer.Release();
                    RejectOrCancelInternal(CreateRejectContainer(ref e, int.MinValue, this));
                }
                finally
                {
                    invokingRejected = false;
                    ClearCurrentInvoker();
                }
            }

            private void ResolveInternal(IValueContainer valueContainer)
            {
                // TODO: thread synchronization
                _state = Promise.State.Resolved;
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;
                HandleBranches(valueContainer);
                ResolveProgressListeners();

                MaybeDispose();
            }

            private void RejectOrCancelInternal(IValueContainer valueContainer)
            {
                // TODO: thread synchronization
                _state = valueContainer.GetState();
                valueContainer.Retain();
                var previous = _valueOrPrevious;
                _valueOrPrevious = valueContainer;
                HandleBranches(valueContainer);
                CancelProgressListeners(previous);

                MaybeDispose();
            }

            private void HandleSelf(IValueContainer valueContainer)
            {
                // TODO: thread synchronization
                _state = valueContainer.GetState();
                var previous = _valueOrPrevious;
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;

                HandleBranches(valueContainer);
                if (_state == Promise.State.Resolved)
                {
                    ResolveProgressListeners();
                }
                else
                {
                    CancelProgressListeners(previous);
                }

                MaybeDispose();
            }

            protected virtual void Execute(IValueContainer valueContainer, ref bool invokingRejected) { }

            private void HandleBranches(IValueContainer valueContainer)
            {
                while (_nextBranches.IsNotEmpty)
                {
                    _nextBranches.Pop().MakeReady(this, valueContainer, ref _handleQueue);
                }

                //// TODO: keeping this code around for when background threaded tasks are implemented.
                //ValueLinkedQueue<ITreeHandleable> handleQueue = new ValueLinkedQueue<ITreeHandleable>();
                //while (_nextBranches.IsNotEmpty)
                //{
                //    _nextBranches.Pop().MakeReady(this, valueContainer, ref handleQueue);
                //}
                //AddToHandleQueueFront(ref handleQueue);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed class PromiseDuplicate : PromiseRef
            {
                private struct Creator : ICreator<PromiseDuplicate>
                {
                    [MethodImpl((MethodImplOptions) 256)]
                    public PromiseDuplicate Create()
                    {
                        return new PromiseDuplicate();
                    }
                }

                private PromiseDuplicate() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static PromiseDuplicate GetOrCreate()
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseDuplicate, Creator>(new Creator());
                    promise.Reset();
                    return promise;
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    HandleSelf(valueContainer);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class PromiseWaitPromise : PromiseRef
            {
                internal void WaitFor(Promise other)
                {
                    ThrowIfInPool(this);
                    ValidateReturn(other);
                    var _ref = other._ref;
                    if (_ref == null)
                    {
                        ResolveInternal(ResolveContainerVoid.GetOrCreate());
                    }
                    else
                    {
                        _ref.MarkAwaited(other._id);
                        _valueOrPrevious = _ref;
                        SubscribeProgressToOther(_ref);
                        _ref.AddWaiter(this);
                    }
                }

                internal void WaitFor<T>(Promise<T> other)
                {
                    ThrowIfInPool(this);
                    ValidateReturn(other);
                    var _ref = other._ref;
                    if (_ref == null)
                    {
                        T value = other._result;
                        ResolveInternal(ResolveContainer<T>.GetOrCreate(ref value));
                    }
                    else
                    {
                        _ref.MarkAwaited(other._id);
                        _valueOrPrevious = _ref;
                        SubscribeProgressToOther(_ref);
                        _ref.AddWaiter(this);
                    }
                }

                partial void SubscribeProgressToOther(PromiseRef other);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class AsyncPromiseBase : PromiseRef
            {
                protected void ResolveDirect()
                {
                    ThrowIfInPool(this);
                    ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                protected void ResolveDirect<T>(ref T value)
                {
                    ThrowIfInPool(this);
                    ResolveInternal(ResolveContainer<T>.GetOrCreate(ref value));
                }

                protected void RejectDirect<TReject>(ref TReject reason, int rejectSkipFrames)
                {
                    ThrowIfInPool(this);
                    RejectOrCancelInternal(CreateRejectContainer(ref reason, rejectSkipFrames + 1, this));
                }

                protected void CancelDirect()
                {
                    ThrowIfInPool(this);
                    RejectOrCancelInternal(CancelContainerVoid.GetOrCreate());
                }

                protected void CancelDirect<TCancel>(ref TCancel reason)
                {
                    ThrowIfInPool(this);
                    RejectOrCancelInternal(CreateCancelContainer(ref reason));
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class DeferredPromiseBase : AsyncPromiseBase
            {
                // Only using int because Interlocked does not support ushort.
                private int _deferredId = 1;
                public int DeferredId { get { return _deferredId; } }

                protected DeferredPromiseBase() { }

                ~DeferredPromiseBase()
                {
                    if (_state == Promise.State.Pending)
                    {
                        // Deferred wasn't handled.
                        AddRejectionToUnhandledStack(UnhandledDeferredException.instance, this);
                    }
                }

                protected virtual void MaybeUnregisterCancelation() { }

                protected bool TryIncrementDeferredId(int comparand)
                {
                    unchecked // We want the id to wrap around.
                    {
                        return Interlocked.CompareExchange(ref _deferredId, comparand + 1, comparand) == comparand;
                    }
                }

                internal bool TryReject<TReject>(ref TReject reason, int deferredId, int rejectSkipFrames)
                {
                    if (TryIncrementDeferredId(deferredId))
                    {
                        MaybeUnregisterCancelation();
                        RejectDirect(ref reason, rejectSkipFrames + 1);
                        return true;
                    }
                    return false;
                }

                new internal void CancelDirect()
                {
                    ThrowIfInPool(this);
                    // TODO: proper thread synchronization
                    Interlocked.Increment(ref _deferredId);
                    base.CancelDirect();
                }

                new internal void CancelDirect<TCancel>(ref TCancel reason)
                {
                    ThrowIfInPool(this);
                    // TODO: proper thread synchronization
                    Interlocked.Increment(ref _deferredId);
                    base.CancelDirect(ref reason);
                }
            }

            // The only purpose of this is to cast the ref when converting a DeferredBase to a Deferred(<T>) to avoid extra checks.
            // Otherwise, DeferredPromise<T> would be unnecessary and this would be implemented in the base class.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal class DeferredPromiseVoid : DeferredPromiseBase
            {
                private struct Creator : ICreator<DeferredPromiseVoid>
                {
                    [MethodImpl((MethodImplOptions) 256)]
                    public DeferredPromiseVoid Create()
                    {
                        return new DeferredPromiseVoid();
                    }
                }

                protected DeferredPromiseVoid() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                // Used for child to call base dispose without repooling for both types.
                // This is necessary because C# doesn't allow `base.base.Dispose()`.
                [MethodImpl((MethodImplOptions) 256)]
                protected void SuperDispose()
                {
                    base.Dispose();
                }

                public bool TryResolve(int deferredId)
                {
                    if (TryIncrementDeferredId(deferredId))
                    {
                        MaybeUnregisterCancelation();
                        ResolveDirect();
                        return true;
                    }
                    return false;
                }

                public static DeferredPromiseVoid GetOrCreate()
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<DeferredPromiseVoid, Creator>(new Creator());
                    promise.Reset();
                    promise.ResetDepth();
                    return promise;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed class DeferredPromiseVoidCancel : DeferredPromiseVoid, ICancelDelegate
            {
                private struct Creator : ICreator<DeferredPromiseVoidCancel>
                {
                    [MethodImpl((MethodImplOptions) 256)]
                    public DeferredPromiseVoidCancel Create()
                    {
                        return new DeferredPromiseVoidCancel();
                    }
                }

                private CancelationRegistration _cancelationRegistration;

                private DeferredPromiseVoidCancel() { }

                protected override void Dispose()
                {
                    SuperDispose();
                    _cancelationRegistration = default(CancelationRegistration);
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                public static DeferredPromiseVoidCancel GetOrCreate(CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<DeferredPromiseVoidCancel, Creator>(new Creator());
                    promise.Reset();
                    promise.ResetDepth();
                    promise._cancelationRegistration = cancelationToken.RegisterInternal(promise);
                    return promise;
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    CancelDirect(ref valueContainer);
                }

                protected override void MaybeUnregisterCancelation()
                {
                    ThrowIfInPool(this);
                    _cancelationRegistration.TryUnregister();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal class DeferredPromise<T> : DeferredPromiseBase
            {
                private struct Creator : ICreator<DeferredPromise<T>>
                {
                    [MethodImpl((MethodImplOptions) 256)]
                    public DeferredPromise<T> Create()
                    {
                        return new DeferredPromise<T>();
                    }
                }

                protected DeferredPromise() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                // Used for child to call base dispose without repooling for both types.
                // This is necessary because C# doesn't allow `base.base.Dispose()`.
                [MethodImpl((MethodImplOptions) 256)]
                protected void SuperDispose()
                {
                    base.Dispose();
                }

                public bool TryResolve(ref T value, int deferredId)
                {
                    if (TryIncrementDeferredId(deferredId))
                    {
                        MaybeUnregisterCancelation();
                        ResolveDirect(ref value);
                        return true;
                    }
                    return false;
                }

                public static DeferredPromise<T> GetOrCreate()
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<DeferredPromise<T>, Creator>(new Creator());
                    promise.Reset();
                    promise.ResetDepth();
                    return promise;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed class DeferredPromiseCancel<T> : DeferredPromise<T>, ICancelDelegate
            {
                private struct Creator : ICreator<DeferredPromiseCancel<T>>
                {
                    [MethodImpl((MethodImplOptions) 256)]
                    public DeferredPromiseCancel<T> Create()
                    {
                        return new DeferredPromiseCancel<T>();
                    }
                }

                private CancelationRegistration _cancelationRegistration;

                private DeferredPromiseCancel() { }

                protected override void Dispose()
                {
                    SuperDispose();
                    _cancelationRegistration = default(CancelationRegistration);
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                public static DeferredPromiseCancel<T> GetOrCreate(CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<DeferredPromiseCancel<T>, Creator>(new Creator());
                    promise.Reset();
                    promise.ResetDepth();
                    promise._cancelationRegistration = cancelationToken.RegisterInternal(promise);
                    return promise;
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    CancelDirect(ref valueContainer);
                }

                protected override void MaybeUnregisterCancelation()
                {
                    ThrowIfInPool(this);
                    _cancelationRegistration.TryUnregister();
                }
            }

            internal static class RefCreator
            {
                // IDelegate to reduce the amount of classes I would have to write(Composition Over Inheritance).
                // Using generics with constraints allows us to use structs to get composition for "free"
                // (no extra object allocation or extra memory overhead, and the compiler will generate the Promise classes for us).
                // The only downside is that more classes are created than if we just used straight interfaces (not a problem with JIT, but makes the code size larger with AOT).

#region Resolve Promises
                // Resolve types for more common .Then(onResolved) calls to be more efficient.

                [MethodImpl((MethodImplOptions) 256)]
                public static PromiseRef CreateResolve<TResolver>(TResolver resolver) where TResolver : IDelegateResolve
                {
                    var promise = PromiseResolve<TResolver>.GetOrCreate();
                    promise.resolver = resolver;
                    return promise;
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static PromiseRef CreateResolve<TResolver>(TResolver resolver, CancelationToken cancelationToken) where TResolver : IDelegateResolve, ICancelableDelegate
                {
                    var promise = PromiseResolve<TResolver>.GetOrCreate();
                    promise.resolver = resolver;
                    promise.resolver.SetCancelationRegistration(cancelationToken.RegisterInternal(promise)); // Very important, cancelation must be registered after the resolver is set!
                    return promise;
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static PromiseRef CreateResolveWait<TResolver>(TResolver resolver) where TResolver : IDelegateResolvePromise
                {
                    var promise = PromiseResolvePromise<TResolver>.GetOrCreate();
                    promise.resolver = resolver;
                    return promise;
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static PromiseRef CreateResolveWait<TResolver>(TResolver resolver, CancelationToken cancelationToken) where TResolver : IDelegateResolvePromise, ICancelableDelegate
                {
                    var promise = PromiseResolvePromise<TResolver>.GetOrCreate();
                    promise.resolver = resolver;
                    promise.resolver.SetCancelationRegistration(cancelationToken.RegisterInternal(promise)); // Very important, cancelation must be registered after the resolver is set!
                    return promise;
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [System.Diagnostics.DebuggerNonUserCode]
#endif
                internal sealed class PromiseResolve<TResolver> : PromiseRef where TResolver : IDelegateResolve
                {
                    private struct Creator : ICreator<PromiseResolve<TResolver>>
                    {
                        [MethodImpl((MethodImplOptions) 256)]
                        public PromiseResolve<TResolver> Create()
                        {
                            return new PromiseResolve<TResolver>();
                        }
                    }

                    public TResolver resolver;

                    private PromiseResolve() { }

                    protected override void Dispose()
                    {
                        base.Dispose();
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }

                    [MethodImpl((MethodImplOptions) 256)]
                    public static PromiseResolve<TResolver> GetOrCreate()
                    {
                        var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolve<TResolver>, Creator>(new Creator());
                        promise.Reset();
                        return promise;
                    }

                    protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                    {
                        var resolveCallback = resolver;
                        resolver = default(TResolver);
                        // TODO: thread synchronization. CancelationToken could be canceled while in this method from another thread. Use TryUnregister and return if false.
                        resolveCallback.MaybeUnregisterCancelation();
                        if (valueContainer.GetState() == Promise.State.Resolved)
                        {
                            resolveCallback.InvokeResolver(valueContainer, this);
                        }
                        else
                        {
                            RejectOrCancelInternal(valueContainer);
                        }
                    }
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [System.Diagnostics.DebuggerNonUserCode]
#endif
                internal sealed class PromiseResolvePromise<TResolver> : PromiseWaitPromise where TResolver : IDelegateResolvePromise
                {
                    private struct Creator : ICreator<PromiseResolvePromise<TResolver>>
                    {
                        [MethodImpl((MethodImplOptions) 256)]
                        public PromiseResolvePromise<TResolver> Create()
                        {
                            return new PromiseResolvePromise<TResolver>();
                        }
                    }

                    public TResolver resolver;

                    private PromiseResolvePromise() { }

                    protected override void Dispose()
                    {
                        base.Dispose();
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }

                    [MethodImpl((MethodImplOptions) 256)]
                    public static PromiseResolvePromise<TResolver> GetOrCreate()
                    {
                        var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolvePromise<TResolver>, Creator>(new Creator());
                        promise.Reset();
                        return promise;
                    }

                    protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                    {
                        if (resolver.IsNull)
                        {
                            // The returned promise is handling this.
                            HandleSelf(valueContainer);
                            return;
                        }

                        var resolveCallback = resolver;
                        resolver = default(TResolver);
                        // TODO: thread synchronization. CancelationToken could be canceled while in this method from another thread.
                        resolveCallback.MaybeUnregisterCancelation();
                        if (valueContainer.GetState() == Promise.State.Resolved)
                        {
                            resolveCallback.InvokeResolver(valueContainer, this);
                        }
                        else
                        {
                            RejectOrCancelInternal(valueContainer);
                        }
                    }
                }
#endregion

#region Resolve or Reject Promises

                [MethodImpl((MethodImplOptions) 256)]
                public static PromiseRef CreateResolveReject<TResolver, TRejecter>(TResolver resolver, TRejecter rejecter)
                    where TResolver : IDelegateResolve
                    where TRejecter : IDelegateReject
                {
                    var promise = PromiseResolveReject<TResolver, TRejecter>.GetOrCreate();
                    promise.resolver = resolver;
                    promise.rejecter = rejecter;
                    return promise;
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static PromiseRef CreateResolveReject<TResolver, TRejecter>(TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken)
                    where TResolver : IDelegateResolve, ICancelableDelegate
                    where TRejecter : IDelegateReject
                {
                    var promise = PromiseResolveReject<TResolver, TRejecter>.GetOrCreate();
                    promise.resolver = resolver;
                    promise.rejecter = rejecter;
                    promise.resolver.SetCancelationRegistration(cancelationToken.RegisterInternal(promise)); // Very important, cancelation must be registered after the resolver is set!
                    return promise;
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static PromiseRef CreateResolveRejectWait<TResolver, TRejecter>(TResolver resolver, TRejecter rejecter)
                    where TResolver : IDelegateResolvePromise
                    where TRejecter : IDelegateRejectPromise
                {
                    var promise = PromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate();
                    promise.resolver = resolver;
                    promise.rejecter = rejecter;
                    return promise;
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static PromiseRef CreateResolveRejectWait<TResolver, TRejecter>(TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken)
                    where TResolver : IDelegateResolvePromise, ICancelableDelegate
                    where TRejecter : IDelegateRejectPromise
                {
                    var promise = PromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate();
                    promise.resolver = resolver;
                    promise.rejecter = rejecter;
                    promise.resolver.SetCancelationRegistration(cancelationToken.RegisterInternal(promise)); // Very important, cancelation must be registered after the resolver is set!
                    return promise;
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [System.Diagnostics.DebuggerNonUserCode]
#endif
                internal sealed class PromiseResolveReject<TResolver, TRejecter> : PromiseRef where TResolver : IDelegateResolve where TRejecter : IDelegateReject
                {
                    private struct Creator : ICreator<PromiseResolveReject<TResolver, TRejecter>>
                    {
                        [MethodImpl((MethodImplOptions) 256)]
                        public PromiseResolveReject<TResolver, TRejecter> Create()
                        {
                            return new PromiseResolveReject<TResolver, TRejecter>();
                        }
                    }

                    public TResolver resolver;
                    public TRejecter rejecter;

                    private PromiseResolveReject() { }

                    protected override void Dispose()
                    {
                        base.Dispose();
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }

                    [MethodImpl((MethodImplOptions) 256)]
                    public static PromiseResolveReject<TResolver, TRejecter> GetOrCreate()
                    {
                        var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolveReject<TResolver, TRejecter>, Creator>(new Creator());
                        promise.Reset();
                        return promise;
                    }

                    protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                    {
                        var resolveCallback = resolver;
                        resolver = default(TResolver);
                        var rejectCallback = rejecter;
                        rejecter = default(TRejecter);
                        // TODO: thread synchronization. CancelationToken could be canceled while in this method from another thread.
                        resolveCallback.MaybeUnregisterCancelation();
                        Promise.State state = valueContainer.GetState();
                        if (state == Promise.State.Resolved)
                        {
                            resolveCallback.InvokeResolver(valueContainer, this);
                        }
                        else if (state == Promise.State.Rejected)
                        {
                            invokingRejected = true;
                            rejectCallback.InvokeRejecter(valueContainer, this);
                        }
                        else
                        {
                            RejectOrCancelInternal(valueContainer);
                        }
                    }
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [System.Diagnostics.DebuggerNonUserCode]
#endif
                internal sealed class PromiseResolveRejectPromise<TResolver, TRejecter> : PromiseWaitPromise where TResolver : IDelegateResolvePromise where TRejecter : IDelegateRejectPromise
                {
                    private struct Creator : ICreator<PromiseResolveRejectPromise<TResolver, TRejecter>>
                    {
                        [MethodImpl((MethodImplOptions) 256)]
                        public PromiseResolveRejectPromise<TResolver, TRejecter> Create()
                        {
                            return new PromiseResolveRejectPromise<TResolver, TRejecter>();
                        }
                    }

                    public TResolver resolver;
                    public TRejecter rejecter;

                    private PromiseResolveRejectPromise() { }

                    protected override void Dispose()
                    {
                        base.Dispose();
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }

                    [MethodImpl((MethodImplOptions) 256)]
                    public static PromiseResolveRejectPromise<TResolver, TRejecter> GetOrCreate()
                    {
                        var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolveRejectPromise<TResolver, TRejecter>, Creator>(new Creator());
                        promise.Reset();
                        return promise;
                    }

                    protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                    {
                        if (rejecter.IsNull)
                        {
                            // The returned promise is handling this.
                            HandleSelf(valueContainer);
                            return;
                        }

                        var resolveCallback = resolver;
                        resolver = default(TResolver);
                        var rejectCallback = rejecter;
                        rejecter = default(TRejecter);
                        // TODO: thread synchronization. CancelationToken could be canceled while in this method from another thread.
                        resolveCallback.MaybeUnregisterCancelation();
                        Promise.State state = valueContainer.GetState();
                        if (state == Promise.State.Resolved)
                        {
                            resolveCallback.InvokeResolver(valueContainer, this);
                        }
                        else if (state == Promise.State.Rejected)
                        {
                            invokingRejected = true;
                            rejectCallback.InvokeRejecter(valueContainer, this);
                        }
                        else
                        {
                            RejectOrCancelInternal(valueContainer);
                        }
                    }
                }
#endregion

#region Continue Promises

                [MethodImpl((MethodImplOptions) 256)]
                public static PromiseRef CreateContinue<TContinuer>(TContinuer resolver) where TContinuer : IDelegateContinue
                {
                    var promise = PromiseContinue<TContinuer>.GetOrCreate();
                    promise.continuer = resolver;
                    return promise;
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static PromiseRef CreateContinue<TContinuer>(TContinuer resolver, CancelationToken cancelationToken) where TContinuer : IDelegateContinue, ICancelableDelegate
                {
                    var promise = PromiseContinue<TContinuer>.GetOrCreate();
                    promise.continuer = resolver;
                    promise.continuer.SetCancelationRegistration(cancelationToken.RegisterInternal(promise)); // Very important, cancelation must be registered after the continuer is set!
                    return promise;
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static PromiseRef CreateContinueWait<TContinuer>(TContinuer resolver) where TContinuer : IDelegateContinuePromise
                {
                    var promise = PromiseContinuePromise<TContinuer>.GetOrCreate();
                    promise.continuer = resolver;
                    return promise;
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static PromiseRef CreateContinueWait<TContinuer>(TContinuer resolver, CancelationToken cancelationToken) where TContinuer : IDelegateContinuePromise, ICancelableDelegate
                {
                    var promise = PromiseContinuePromise<TContinuer>.GetOrCreate();
                    promise.continuer = resolver;
                    promise.continuer.SetCancelationRegistration(cancelationToken.RegisterInternal(promise)); // Very important, cancelation must be registered after the continuer is set!
                    return promise;
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [System.Diagnostics.DebuggerNonUserCode]
#endif
                internal sealed class PromiseContinue<TContinuer> : PromiseRef where TContinuer : IDelegateContinue
                {
                    private struct Creator : ICreator<PromiseContinue<TContinuer>>
                    {
                        [MethodImpl((MethodImplOptions) 256)]
                        public PromiseContinue<TContinuer> Create()
                        {
                            return new PromiseContinue<TContinuer>();
                        }
                    }

                    public TContinuer continuer;

                    private PromiseContinue() { }

                    protected override void Dispose()
                    {
                        base.Dispose();
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }

                    [MethodImpl((MethodImplOptions) 256)]
                    public static PromiseContinue<TContinuer> GetOrCreate()
                    {
                        var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseContinue<TContinuer>, Creator>(new Creator());
                        promise.Reset();
                        return promise;
                    }

                    protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                    {
                        var callback = continuer;
                        continuer = default(TContinuer);
                        callback.Invoke(valueContainer, this);
                    }

                    protected override void CancelCallbacks()
                    {
                        continuer.CancelCallback();
                    }
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [System.Diagnostics.DebuggerNonUserCode]
#endif
                internal sealed class PromiseContinuePromise<TContinuer> : PromiseWaitPromise where TContinuer : IDelegateContinuePromise
                {
                    private struct Creator : ICreator<PromiseContinuePromise<TContinuer>>
                    {
                        [MethodImpl((MethodImplOptions) 256)]
                        public PromiseContinuePromise<TContinuer> Create()
                        {
                            return new PromiseContinuePromise<TContinuer>();
                        }
                    }

                    public TContinuer continuer;

                    private PromiseContinuePromise() { }

                    protected override void Dispose()
                    {
                        base.Dispose();
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }

                    [MethodImpl((MethodImplOptions) 256)]
                    public static PromiseContinuePromise<TContinuer> GetOrCreate()
                    {
                        var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseContinuePromise<TContinuer>, Creator>(new Creator());
                        promise.Reset();
                        return promise;
                    }

                    protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                    {
                        if (continuer.IsNull)
                        {
                            // The returned promise is handling this.
                            HandleSelf(valueContainer);
                            return;
                        }

                        var callback = continuer;
                        continuer = default(TContinuer);
                        callback.Invoke(valueContainer, this);
                    }

                    protected override void CancelCallbacks()
                    {
                        continuer.CancelCallback();
                        base.CancelCallbacks();
                    }
                }
#endregion
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromisePassThrough : ITreeHandleable, ILinked<PromisePassThrough>
            {
                private struct Creator : ICreator<PromisePassThrough>
                {
                    [MethodImpl((MethodImplOptions) 256)]
                    public PromisePassThrough Create()
                    {
                        return new PromisePassThrough();
                    }
                }

                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }
                PromisePassThrough ILinked<PromisePassThrough>.Next { get; set; }

                internal PromiseRef Owner
                {
                    get
                    {
                        ThrowIfInPool(this);
                        return _owner;
                    }
                }
                internal IMultiTreeHandleable Target
                {
                    get
                    {
                        ThrowIfInPool(this);
                        return _target;
                    }
                }

                private int _index;
                private PromiseRef _owner;
                private IMultiTreeHandleable _target;

                private PromisePassThrough() { }

                public static PromisePassThrough GetOrCreate(Promise owner, int index)
                {
                    // owner._ref is checked for nullity before passing into this.
                    owner._ref.MarkAwaited(owner._id);
                    var passThrough = ObjectPool<PromisePassThrough>.GetOrCreate<PromisePassThrough, Creator>(new Creator());
                    passThrough._owner = owner._ref;
                    passThrough._index = index;
                    passThrough.ResetProgress();
                    return passThrough;
                }

                partial void ResetProgress();

                internal void SetTargetAndAddToOwner(IMultiTreeHandleable target)
                {
                    ThrowIfInPool(this);
                    _target = target;
                    Owner.AddWaiter(this);
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    ThrowIfInPool(this);
                    var temp = Target;
                    if (temp.Handle(valueContainer, this, _index))
                    {
                        handleQueue.Push(temp);
                    }
                    Release();
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    ThrowIfInPool(this);
                    var temp = Target;
                    if (temp.Handle(valueContainer, this, _index))
                    {
                        AddToHandleQueueBack(temp);
                    }
                    Release();
                }

                internal void Release()
                {
                    ThrowIfInPool(this);
#if PROMISE_PROGRESS
                    if (!_progressListening)
#endif
                    {
                        _owner = null;
                        _target = null;
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }
                }

                void ITreeHandleable.Handle() { throw new System.InvalidOperationException(); }
            }

            internal static void MaybeMarkAwaitedAndDispose(Promise promise, bool suppressRejection)
            {
                if (promise._ref != null)
                {
                    promise._ref.MarkAwaitedAndMaybeDispose(promise._id, suppressRejection);
                }
            }
        }

        internal static uint PrepareForMulti(Promise promise, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index)
        {
            if (promise._ref != null)
            {
                passThroughs.Push(PromiseRef.PromisePassThrough.GetOrCreate(promise, index));
                return 1;
            }
            return 0;
        }

        internal static uint PrepareForMulti(Promise promise, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index, ref ulong completedProgress)
        {
            if (promise._ref != null)
            {
                passThroughs.Push(PromiseRef.PromisePassThrough.GetOrCreate(promise, index));
                return 1;
            }
            // TODO: store depthAndProgress in Promise structs.
            ++completedProgress;
            return 0;
        }

        internal static uint PrepareForMulti<T>(Promise<T> promise, ref T value, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index)
        {
            if (promise._ref != null)
            {
                passThroughs.Push(PromiseRef.PromisePassThrough.GetOrCreate(promise, index));
                return 1;
            }
            value = promise._result;
            return 0;
        }

        internal static uint PrepareForMulti<T>(Promise<T> promise, ref T value, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index, ref ulong completedProgress)
        {
            if (promise._ref != null)
            {
                passThroughs.Push(PromiseRef.PromisePassThrough.GetOrCreate(promise, index));
                return 1;
            }
            // TODO: store depthAndProgress in Promise structs.
            ++completedProgress;
            value = promise._result;
            return 0;
        }

        [MethodImpl((MethodImplOptions) 256)]
        internal static Promise CreateResolved()
        {
#if PROMISE_DEBUG
            // Make a promise on the heap to capture causality trace and help with debugging in the finalizer.
            var deferred = Promise.NewDeferred();
            deferred.Resolve();
            return deferred.Promise;
#else
            // Make a promise on the stack for efficiency.
            return new Promise(null, ValidPromiseIdFromApi);
#endif
        }

        [MethodImpl((MethodImplOptions) 256)]
        internal static Promise<T> CreateResolved<T>(ref T value)
        {
#if PROMISE_DEBUG
            // Make a promise on the heap to capture causality trace and help with debugging in the finalizer.
            var deferred = Promise.NewDeferred<T>();
            deferred.Resolve(value);
            return deferred.Promise;
#else
            // Make a promise on the stack for efficiency.
            return new Promise<T>(null, ValidPromiseIdFromApi, ref value);
#endif
        }
    }
}