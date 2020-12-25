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

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Proto.Utils;

namespace Proto.Promises
{
    partial struct Promise
    {
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly Internal.PromiseRef _ref;
        private readonly ushort _id;

        /// <summary>
        /// Internal use.
        /// </summary>
        internal Promise(Internal.PromiseRef promiseRef, ushort id)
        {
            _ref = promiseRef;
            _id = id;
        }
    }

    partial struct Promise<T>
    {
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly Internal.PromiseRef _ref;
        private readonly ushort _id;
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly T _result;

        /// <summary>
        /// Internal use.
        /// </summary>
        internal Promise(Internal.PromiseRef promiseRef, ushort id)
        {
            _ref = promiseRef;
            _id = id;
            _result = default(T);
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        internal Promise(Internal.PromiseRef promiseRef, ushort id, ref T value)
        {
            _ref = promiseRef;
            _id = id;
            _result = value;
        }
    }

    partial class Internal
    {
        // Just a random number that's not zero. Using this in Promise<T> instead of a bool prevents extra memory padding for 2-byte T types in 32-bit environment.
        internal const ushort ValidPromiseIdFromApi = 41265;

        internal abstract partial class PromiseRef : ITreeHandleable, ITraceable, ITreeHandleableCollection
        {
            private ValueLinkedStack<ITreeHandleable> _nextBranches;
            protected object _valueOrPrevious;
            private ushort _retainCounter;
            private ushort _id = 1;
            protected Promise.State _state;
            protected bool _wasWaitedOn;

            ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }
            public ushort Id { get { return _id; } }
            public Promise.State State { get { return _state; } }

            ~PromiseRef()
            {
                if (_retainCounter > 0 & _state != Promise.State.Pending)
                {
                    if (_wasWaitedOn)
                    {
                        ((IValueContainer) _valueOrPrevious).Release();
                    }
                    else
                    {
                        // Rejection maybe wasn't caught.
                        ((IValueContainer) _valueOrPrevious).ReleaseAndAddToUnhandledStack();
                    }
                    // Promise wasn't released.
                    string message = "A Promise object was garbage collected that was not released. You must release all IRetainable objects that you have retained.";
                    AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                }
            }

            internal static void OnCompletedForAwaiter(Promise promise, Action onCompleted)
            {
                // If this is called only from the `await` keyword, the check is unnecessary.
                // The check is added for safety in case users call `promise.GetAwaiter()` and use the awaiter directly.
                if (promise._ref != null)
                {
                    promise._ref.OnCompletedForAwaiter(onCompleted);
                }
                else
                {
                    promise.Finally(onCompleted);
                }
            }

            protected virtual void OnCompletedForAwaiter(Action onCompleted)
            {
                AddWaiter(FinallyDelegate.GetOrCreate(onCompleted));
            }

            internal void GetResultForAwaiter()
            {
                if (_state == Promise.State.Resolved)
                {
                    ReleaseInternal();
                    return;
                }
                // Throw unhandled exception or canceled exception.
                Exception exception = ((IThrowable) _valueOrPrevious).GetException();
                // We're throwing here, no need to throw again.
                _wasWaitedOn = true;
                ReleaseInternal();
                throw exception;
            }

            internal T GetResultForAwaiter<T>()
            {
                if (_state == Promise.State.Resolved)
                {
                    T result = ((ResolveContainer<T>) _valueOrPrevious).value;
                    ReleaseInternal();
                    return result;
                }
                // Throw unhandled exception or canceled exception.
                Exception exception = ((IThrowable) _valueOrPrevious).GetException();
                // We're throwing here, no need to throw again.
                _wasWaitedOn = true;
                ReleaseInternal();
                throw exception;
            }

            internal static void MaybeMarkAwaited(Promise promise)
            {
                var _ref = promise._ref;
                if (_ref == null) return;

                // TODO
                _ref.RetainInternal();

                //lock (_ref)
                //{
                //    ++_ref._id;
                //}
            }

            void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
            {
                owner._wasWaitedOn = true;
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;
                handleQueue.Push(this);
            }

            void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
            {
                owner._wasWaitedOn = true;
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;
                AddToHandleQueueBack(this);
            }

            void ITreeHandleableCollection.Remove(ITreeHandleable treeHandleable)
            {
                _nextBranches.Remove(treeHandleable);
            }

            protected virtual void Reset()
            {
                _state = Promise.State.Pending;
                _retainCounter = 1;
                SetCreatedStacktrace(this, 3);
            }

            private void AddWaiter(ITreeHandleable waiter)
            {
                if (_state == Promise.State.Pending)
                {
                    _nextBranches.Push(waiter);
                }
                else
                {
                    waiter.MakeReadyFromSettled(this, (IValueContainer) _valueOrPrevious);
                }
            }

            private void RetainInternal()
            {
#if PROMISE_DEBUG
                // If this fails, change _retainCounter to uint or ulong.
                // Have to directly check ushort since C# compiler doesn't check integer types smaller than Int32...
                if (_retainCounter == ushort.MaxValue)
                {
                    throw new OverflowException();
                }
                checked
#endif
                {
                    ++_retainCounter;
                }
            }

            protected void ReleaseInternal()
            {
                if (ReleaseWithoutDisposeCheck() == 0)
                {
                    Dispose();
                }
            }

            private ushort ReleaseWithoutDisposeCheck()
            {
#if PROMISE_DEBUG
                // This should never fail, but check in debug mode just in case.
                // Have to directly check ushort since C# compiler doesn't check integer types smaller than Int32...
                if (_retainCounter == 0)
                {
                    throw new OverflowException();
                }
                checked
#endif
                {
                    return --_retainCounter;
                }
            }

            protected virtual void Dispose()
            {
                // TODO: Remove id increment here, move it to MaybeMarkAwaited.
                ++_id;
                if (_valueOrPrevious != null)
                {
                    if (_wasWaitedOn)
                    {
                        ((IValueContainer) _valueOrPrevious).Release();
                    }
                    else
                    {
                        // Rejection maybe wasn't caught.
                        ((IValueContainer) _valueOrPrevious).ReleaseAndAddToUnhandledStack();
                    }
                }
                _valueOrPrevious = null;
            }

            private void ResolveInternal(IValueContainer container)
            {
                _state = Promise.State.Resolved;
                container.Retain();
                _valueOrPrevious = container;
                HandleBranches();
                ResolveProgressListeners();

                ReleaseInternal();
            }

            private void RejectOrCancelInternal(IValueContainer container)
            {
                _state = container.GetState();
                container.Retain();
                _valueOrPrevious = container;
                HandleBranches();
                CancelProgressListeners();

                ReleaseInternal();
            }

            private static void MaybeHookupNewPromise(Promise _this, PromiseRef newPromise)
            {
                // This is called from a Then/Catch/ContinueWith with a valid cancelationToken, which could have been fed an already canceled token.
                if (newPromise._valueOrPrevious != null)
                {
                    // TODO: remove SetDepth if progress only starts counting on pending promises.
                    //SetDepth(newPromise);
                    AddToHandleQueueBack(newPromise);
                }
                else
                {
                    HookupNewPromise(_this, newPromise);
                }
            }

            private static void HookupNewPromise(Promise _this, PromiseRef newPromise)
            {
                if (_this._ref == null)
                {
                    newPromise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                    AddToHandleQueueBack(newPromise);
                }
                else
                {
                    newPromise._valueOrPrevious = _this._ref;
                    newPromise.SetDepth(_this._ref);
                    _this._ref.AddWaiter(newPromise);
                }
            }

            void ITreeHandleable.Handle()
            {
                IValueContainer container = (IValueContainer) _valueOrPrevious;
                _valueOrPrevious = null;
                SetCurrentInvoker(this);
                try
                {
                    Execute(container);
                    container.Release();
                }
                catch (RethrowException e)
                {
                    if (!invokingRejected)
                    {
                        container.Release();
#if PROMISE_DEBUG
                        string stacktrace = FormatStackTrace(new System.Diagnostics.StackTrace[1] { new System.Diagnostics.StackTrace(e, true) });
#else
                        string stacktrace = new System.Diagnostics.StackTrace(e, true).ToString();
#endif
                        Exception exception = new InvalidOperationException("RethrowException is only valid in promise onRejected callbacks.", stacktrace);
                        RejectOrCancelInternal(CreateCancelContainer(ref exception));
                    }
                    else
                    {
                        _state = container.GetState();
                        _valueOrPrevious = container;
                        HandleBranches();
                        CancelProgressListeners();
                        ReleaseInternal();
                    }
                }
                catch (OperationCanceledException e)
                {
                    container.Release();
                    RejectOrCancelInternal(CreateCancelContainer(ref e));
                }
                catch (Exception e)
                {
                    container.Release();
                    RejectOrCancelInternal(CreateRejectContainer(ref e, int.MinValue, this));
                }
                finally
                {
                    invokingRejected = false;
                    ClearCurrentInvoker();
                }
            }

            private void HandleSelf(IValueContainer valueContainer)
            {
                _state = valueContainer.GetState();
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;

                HandleBranches();
                if (_state == Promise.State.Resolved)
                {
                    ResolveProgressListeners();
                }
                else
                {
                    CancelProgressListeners();
                }

                ReleaseInternal();
            }

            protected virtual void Execute(IValueContainer valueContainer) { }

            private void HandleBranches()
            {
                var valueContainer = (IValueContainer) _valueOrPrevious;
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

            private void AddBranchesToHandleQueueBack(IValueContainer valueContainer)
            {
                ValueLinkedQueue<ITreeHandleable> handleQueue = new ValueLinkedQueue<ITreeHandleable>();
                while (_nextBranches.IsNotEmpty)
                {
                    _nextBranches.Pop().MakeReady(this, valueContainer, ref handleQueue);
                }
                AddToHandleQueueBack(ref handleQueue);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class PromiseWaitPromise : PromiseRef
            {
                public void WaitFor(Promise other)
                {
                    ValidateReturn(other);
                    if (other._ref == null)
                    {
                        ResolveInternal(ResolveContainerVoid.GetOrCreate());
                    }
                    else
                    {
                        _valueOrPrevious = other._ref;
                        SubscribeProgressToOther(other._ref);
                        other._ref.AddWaiter(this);
                    }
                }

                public void WaitFor<T>(Promise<T> other)
                {
                    ValidateReturn(other);
                    if (other._ref == null)
                    {
                        T value = other._result;
                        ResolveInternal(ResolveContainer<T>.GetOrCreate(ref value));
                    }
                    else
                    {
                        _valueOrPrevious = other._ref;
                        SubscribeProgressToOther(other._ref);
                        other._ref.AddWaiter(this);
                    }
                }

                partial void SubscribeProgressToOther(PromiseRef other);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class AsyncPromiseBase : PromiseRef, ITreeHandleable
            {
#if CSHARP_7_OR_LATER
                // Optimize for awaits. Adds memory per-object for the delegate, but saves us from having to create a Finally wrapper.
                private Action _onComplete;
                protected override sealed void OnCompletedForAwaiter(Action onCompleted)
                {
                    _onComplete = onCompleted;
                }
#endif

                void ITreeHandleable.Handle()
                {
                    ReleaseInternal();
                }

                protected void RejectDirect<TReject>(ref TReject reason, int rejectSkipFrames)
                {
                    _state = Promise.State.Rejected;
                    var rejection = CreateRejectContainer(ref reason, rejectSkipFrames + 1, this);
                    rejection.Retain();
                    _valueOrPrevious = rejection;
                    AddBranchesToHandleQueueBack(rejection);
                    CancelProgressListeners();
                    AddToHandleQueueFront(this);
                }

                protected void ResolveDirect()
                {
                    _state = Promise.State.Resolved;
                    var resolveValue = ResolveContainerVoid.GetOrCreate();
                    _valueOrPrevious = resolveValue;
                    AddBranchesToHandleQueueBack(resolveValue);
                    ResolveProgressListeners();
                    AddToHandleQueueFront(this);
                }

                protected void ResolveDirect<T>(ref T value)
                {
                    _state = Promise.State.Resolved;
                    var resolveValue = ResolveContainer<T>.GetOrCreate(ref value);
                    resolveValue.Retain();
                    _valueOrPrevious = resolveValue;
                    AddBranchesToHandleQueueBack(resolveValue);
                    ResolveProgressListeners();
                    AddToHandleQueueFront(this);
                }

                protected void CancelDirect<TCancel>(ref TCancel reason)
                {
                    _state = Promise.State.Canceled;
                    var cancelContainer = CreateCancelContainer(ref reason);
                    cancelContainer.Retain();
                    _valueOrPrevious = cancelContainer;
                    AddBranchesToHandleQueueBack(cancelContainer);
                    CancelProgressListeners();
                    AddToHandleQueueFront(this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class DeferredPromiseBase : AsyncPromiseBase
            {
                // Only using int because Interlocked does not support ushort.
                protected int _deferredId = 1;
                public ushort DeferredId { get { return (ushort) _deferredId; } }

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

                protected bool TryIncrementDeferredId(ushort comparand)
                {
                    unchecked // We want the id to wrap around.
                    {
                        return Interlocked.CompareExchange(ref _deferredId, (ushort) (comparand + 1), comparand) == comparand;
                    }
                }

                internal bool TryReject<TReject>(ref TReject reason, ushort deferredId, int rejectSkipFrames)
                {
                    if (TryIncrementDeferredId(deferredId))
                    {
                        MaybeUnregisterCancelation();
                        RejectDirect(ref reason, rejectSkipFrames + 1);
                        return true;
                    }
                    return false;
                }

                internal void CancelDirect()
                {
                    Interlocked.Increment(ref _deferredId);
                    _state = Promise.State.Canceled;
                    var cancelContainer = CancelContainerVoid.GetOrCreate();
                    _valueOrPrevious = cancelContainer;
                    AddBranchesToHandleQueueBack(cancelContainer);
                    CancelProgressListeners();
                    AddToHandleQueueFront(this);
                }

                new internal void CancelDirect<TCancel>(ref TCancel reason)
                {
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
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                public bool TryResolve(ushort deferredId)
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
            internal sealed class DeferredPromiseCancel : DeferredPromiseVoid, ITreeHandleable, ICancelDelegate
            {
                private struct Creator : ICreator<DeferredPromiseCancel>
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public DeferredPromiseCancel Create()
                    {
                        return new DeferredPromiseCancel();
                    }
                }

                private CancelationRegistration _cancelationRegistration;

                private DeferredPromiseCancel() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                public static DeferredPromiseCancel GetOrCreate(CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<DeferredPromiseCancel, Creator>(new Creator());
                    promise.Reset();
                    promise.ResetDepth();
                    promise._cancelationRegistration = cancelationToken.RegisterInternal(promise);
                    return promise;
                }

                void ITreeHandleable.Handle()
                {
                    _cancelationRegistration = default(CancelationRegistration);
                    ReleaseInternal();
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    CancelDirect(ref valueContainer);
                }

                protected override void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal class DeferredPromise<T> : DeferredPromiseBase, ITreeHandleable
            {
                private struct Creator : ICreator<DeferredPromise<T>>
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                public bool TryResolve(ref T value, ushort deferredId)
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
            internal sealed class DeferredPromiseCancel<T> : DeferredPromise<T>, ITreeHandleable, ICancelDelegate
            {
                private struct Creator : ICreator<DeferredPromiseCancel<T>>
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public DeferredPromiseCancel<T> Create()
                    {
                        return new DeferredPromiseCancel<T>();
                    }
                }

                private CancelationRegistration _cancelationRegistration;

                private DeferredPromiseCancel() { }

                protected override void Dispose()
                {
                    base.Dispose();
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

                void ITreeHandleable.Handle()
                {
                    _cancelationRegistration = default(CancelationRegistration);
                    ReleaseInternal();
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    CancelDirect(ref valueContainer);
                }

                protected override void MaybeUnregisterCancelation()
                {
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

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static PromiseRef CreateResolve<TResolver>(TResolver resolver) where TResolver : IDelegateResolve
                {
                    var promise = PromiseResolve<TResolver>.GetOrCreate();
                    promise.resolver = resolver;
                    return promise;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static PromiseRef CreateResolve<TResolver>(TResolver resolver, CancelationToken cancelationToken) where TResolver : IDelegateResolve, ICancelableDelegate
                {
                    var promise = PromiseResolve<TResolver>.GetOrCreate();
                    promise.resolver = resolver;
                    promise.resolver.SetCancelationRegistration(cancelationToken.RegisterInternal(promise)); // Very important, cancelation must be registered after the resolver is set!
                    return promise;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static PromiseRef CreateResolveWait<TResolver>(TResolver resolver) where TResolver : IDelegateResolvePromise
                {
                    var promise = PromiseResolvePromise<TResolver>.GetOrCreate();
                    promise.resolver = resolver;
                    return promise;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static PromiseResolve<TResolver> GetOrCreate()
                    {
                        var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolve<TResolver>, Creator>(new Creator());
                        promise.Reset();
                        return promise;
                    }

                    protected override void Execute(IValueContainer valueContainer)
                    {
                        var resolveCallback = resolver;
                        resolver = default(TResolver);
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
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static PromiseResolvePromise<TResolver> GetOrCreate()
                    {
                        var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolvePromise<TResolver>, Creator>(new Creator());
                        promise.Reset();
                        return promise;
                    }

                    protected override void Execute(IValueContainer valueContainer)
                    {
                        if (resolver.IsNull)
                        {
                            // The returned promise is handling this.
                            HandleSelf(valueContainer);
                            return;
                        }

                        var resolveCallback = resolver;
                        resolver = default(TResolver);
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

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static PromiseRef CreateResolveReject<TResolver, TRejecter>(TResolver resolver, TRejecter rejecter)
                    where TResolver : IDelegateResolve
                    where TRejecter : IDelegateReject
                {
                    var promise = PromiseResolveReject<TResolver, TRejecter>.GetOrCreate();
                    promise.resolver = resolver;
                    promise.rejecter = rejecter;
                    return promise;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static PromiseRef CreateResolveRejectWait<TResolver, TRejecter>(TResolver resolver, TRejecter rejecter)
                    where TResolver : IDelegateResolvePromise
                    where TRejecter : IDelegateRejectPromise
                {
                    var promise = PromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate();
                    promise.resolver = resolver;
                    promise.rejecter = rejecter;
                    return promise;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static PromiseResolveReject<TResolver, TRejecter> GetOrCreate()
                    {
                        var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolveReject<TResolver, TRejecter>, Creator>(new Creator());
                        promise.Reset();
                        return promise;
                    }

                    protected override void Execute(IValueContainer valueContainer)
                    {
                        var resolveCallback = resolver;
                        resolver = default(TResolver);
                        var rejectCallback = rejecter;
                        rejecter = default(TRejecter);
                        resolveCallback.MaybeUnregisterCancelation();
                        Promise.State state = valueContainer.GetState();
                        if (state == Promise.State.Resolved)
                        {
                            resolveCallback.InvokeResolver(valueContainer, this);
                            return;
                        }
                        if (state == Promise.State.Rejected)
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
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static PromiseResolveRejectPromise<TResolver, TRejecter> GetOrCreate()
                    {
                        var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolveRejectPromise<TResolver, TRejecter>, Creator>(new Creator());
                        promise.Reset();
                        return promise;
                    }

                    protected override void Execute(IValueContainer valueContainer)
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
                        resolveCallback.MaybeUnregisterCancelation();
                        Promise.State state = valueContainer.GetState();
                        if (state == Promise.State.Resolved)
                        {
                            resolveCallback.InvokeResolver(valueContainer, this);
                            return;
                        }
#if PROMISE_PROGRESS
                        _suspended = true;
#endif
                        if (state == Promise.State.Rejected)
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

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static PromiseRef CreateContinue<TContinuer>(TContinuer resolver) where TContinuer : IDelegateContinue
                {
                    var promise = PromiseContinue<TContinuer>.GetOrCreate();
                    promise.continuer = resolver;
                    return promise;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static PromiseRef CreateContinue<TContinuer>(TContinuer resolver, CancelationToken cancelationToken) where TContinuer : IDelegateContinue, ICancelableDelegate
                {
                    var promise = PromiseContinue<TContinuer>.GetOrCreate();
                    promise.continuer = resolver;
                    promise.continuer.SetCancelationRegistration(cancelationToken.RegisterInternal(promise)); // Very important, cancelation must be registered after the continuer is set!
                    return promise;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static PromiseRef CreateContinueWait<TContinuer>(TContinuer resolver) where TContinuer : IDelegateContinuePromise
                {
                    var promise = PromiseContinuePromise<TContinuer>.GetOrCreate();
                    promise.continuer = resolver;
                    return promise;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static PromiseContinue<TContinuer> GetOrCreate()
                    {
                        var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseContinue<TContinuer>, Creator>(new Creator());
                        promise.Reset();
                        return promise;
                    }

                    internal override void CancelCallbacks()
                    {
                        continuer.CancelCallback();
                    }

                    protected override void Execute(IValueContainer valueContainer)
                    {
                        var callback = continuer;
                        continuer = default(TContinuer);
                        callback.Invoke(valueContainer, this);
                    }
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [System.Diagnostics.DebuggerNonUserCode]
#endif
                internal sealed class PromiseContinuePromise<TContinuer> : PromiseWaitPromise where TContinuer : IDelegateContinuePromise
                {
                    private struct Creator : ICreator<PromiseContinuePromise<TContinuer>>
                    {
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static PromiseContinuePromise<TContinuer> GetOrCreate()
                    {
                        var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseContinuePromise<TContinuer>, Creator>(new Creator());
                        promise.Reset();
                        return promise;
                    }

                    internal override void CancelCallbacks()
                    {
                        continuer.CancelCallback();
                    }

                    protected override void Execute(IValueContainer valueContainer)
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
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public PromisePassThrough Create()
                    {
                        return new PromisePassThrough();
                    }
                }

                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }
                PromisePassThrough ILinked<PromisePassThrough>.Next { get; set; }

                public PromiseRef Owner { get; private set; }
                internal IMultiTreeHandleable Target { get; private set; }

                private int _index;
                private uint _retainCounter;

                private PromisePassThrough() { }

                public static PromisePassThrough GetOrCreate(PromiseRef owner, int index)
                {
                    var passThrough = ObjectPool<PromisePassThrough>.GetOrCreate<PromisePassThrough, Creator>(new Creator());
                    passThrough.Owner = owner;
                    passThrough._index = index;
                    passThrough._retainCounter = 1u;
                    passThrough.ResetProgress();
                    return passThrough;
                }

                partial void ResetProgress();

                internal void SetTargetAndAddToOwner(IMultiTreeHandleable target)
                {
                    Target = target;
                    Owner.AddWaiter(this);
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    var temp = Target;
                    if (temp.Handle(valueContainer, this, _index))
                    {
                        handleQueue.Push(temp);
                    }
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    var temp = Target;
                    if (temp.Handle(valueContainer, this, _index))
                    {
                        AddToHandleQueueBack(temp);
                    }
                }

                public void Retain()
                {
                    ++_retainCounter;
                }

                public void Release()
                {
#if PROMISE_DEBUG
                    checked
#endif
                    {
                        if (--_retainCounter == 0)
                        {
                            Owner = null;
                            Target = null;
                            ObjectPool<ITreeHandleable>.MaybeRepool(this);
                        }
                    }
                }

                void ITreeHandleable.Handle() { throw new System.InvalidOperationException(); }
            }

            internal static void MaybeRelease(PromiseRef promiseRef)
            {
                if (promiseRef != null)
                {
                    // TODO
                }
            }

            internal static void MaybeMarkAndRelease(PromiseRef promiseRef)
            {
                if (promiseRef != null)
                {
                    promiseRef._wasWaitedOn = true;
                    // TODO
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static PromiseRef.PromisePassThrough CreatePassthrough(Promise promise, int index)
        {
            // promise._ref is checked for nullity before passing into this.
            PromiseRef.MaybeMarkAwaited(promise);
            return PromiseRef.PromisePassThrough.GetOrCreate(promise._ref, index);
        }

        internal static int PrepareForMulti(Promise promise, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index)
        {
            if (promise._ref != null)
            {
                passThroughs.Push(CreatePassthrough(promise, index));
                return 1;
            }
            return 0;
        }

        internal static int PrepareForMulti<T>(Promise<T> promise, ref T value, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index)
        {
            if (promise._ref != null)
            {
                passThroughs.Push(CreatePassthrough(promise, index));
                return 1;
            }
            value = promise._result;
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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