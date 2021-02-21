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
        internal const int ValidIdFromApi = 41265;

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal abstract partial class PromiseRef : ITreeHandleable, ITraceable
        {
            private readonly object _locker = new object();
            private ValueLinkedStack<ITreeHandleable> _nextBranches;
            private object _valueOrPrevious;
            private int _id = 1; // Only using int because Interlocked does not support ushort.
            volatile private Promise.State _state;
            private bool _suppressRejection;
            private bool _wasAwaited;
            private byte _idIncrementer = 1; // Used to increment _id only if not preserved. 0 = preserved, 1 = not preserved.

            ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }
            internal int Id
            {
                [MethodImpl(InlineOption)]
                get { return _id; }
            }
            internal Promise.State State
            {
                [MethodImpl(InlineOption)]
                get { return _state; }
            }
            private bool IsPreserved
            {
                [MethodImpl(InlineOption)]
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
                IncrementId(promiseId, 1); // Always increment, whether preserved or not.
                _wasAwaited = true;
                _idIncrementer = 1; // Mark not preserved.
                MaybeDispose();
            }

            internal PromiseRef GetDuplicate(int promiseId)
            {
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
                    throw new InvalidOperationException("Attempted to use an invalid Promise. This may be because you are attempting to use a promise simultaneously on multiple threads that you have not preserved.",
                        GetFormattedStacktrace(3));
                }
                ThrowIfInPool(this);
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

            // TODO: Set back to private
            internal bool TryRemoveWaiter(ITreeHandleable treeHandleable)
            {
                // TODO: thread synchronization
                ThrowIfInPool(this);
                lock (_locker)
                {
                    return _nextBranches.TryRemove(treeHandleable);
                }
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

            private void HookupNewCancelablePromise(PromiseRef newPromise, ref CancelationHelper cancelationHelper)
            {
                newPromise.SetDepth(this);
                if (Interlocked.CompareExchange(ref newPromise._valueOrPrevious, this, null) == null)
                {
                    AddWaiter(newPromise);
                }
                else
                {
                    cancelationHelper.Release();
                    MaybeDispose();
                }
            }

            private void HookupNewPromise(PromiseRef newPromise)
            {
                newPromise.SetDepth(this);
                newPromise._valueOrPrevious = this;
                AddWaiter(newPromise);
            }

            internal void AddWaiter(ITreeHandleable waiter)
            {
                // TODO: thread synchronization
                ThrowIfInPool(this);
                if (_state == Promise.State.Pending)
                {
                    lock (_locker)
                    {
                        _nextBranches.Push(waiter);
                    }
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
                IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                bool invokingRejected = false;
                SetCurrentInvoker(this);
                try
                {
                    // valueContainer is released deeper in the call stack, so we only release it in this method if an exception is thrown.
                    // (This is in case it is canceled, the valueContainer will be released from the cancelation.)
                    Execute(valueContainer, ref invokingRejected);
                }
                catch (RethrowException e)
                {
                    if (invokingRejected || (e is ForcedRethrowException && valueContainer.GetState() != Promise.State.Resolved))
                    {
                        RejectOrCancelInternal(valueContainer);
                        valueContainer.Release(); // Must release since RejectOrCancelInternal adds an extra retain.
                    }
                    else
                    {
                        valueContainer.ReleaseAndMaybeAddToUnhandledStack(true);
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
                    valueContainer.ReleaseAndMaybeAddToUnhandledStack(!invokingRejected);
                    RejectOrCancelInternal(CreateCancelContainer(ref e));
                }
                catch (Exception e)
                {
                    valueContainer.ReleaseAndMaybeAddToUnhandledStack(!invokingRejected);
                    RejectOrCancelInternal(CreateRejectContainer(ref e, int.MinValue, this));
                }
                ClearCurrentInvoker();
            }

            private void ResolveInternal(IValueContainer valueContainer)
            {
                // TODO: thread synchronization
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;
                _state = Promise.State.Resolved;
                HandleBranches(valueContainer);
                ResolveProgressListeners();

                MaybeDispose();
            }

            private void RejectOrCancelInternal(IValueContainer valueContainer)
            {
                // TODO: thread synchronization
                valueContainer.Retain();
                var previous = _valueOrPrevious;
                _valueOrPrevious = valueContainer;
                _state = valueContainer.GetState();
                HandleBranches(valueContainer);
                CancelProgressListeners(previous);

                MaybeDispose();
            }

            private void HandleSelf(IValueContainer valueContainer)
            {
                // TODO: thread synchronization
                var previous = _valueOrPrevious;
                _valueOrPrevious = valueContainer;
                _state = valueContainer.GetState();

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
                ValueLinkedStack<ITreeHandleable> branches;
                lock (_locker)
                {
                    branches = _nextBranches;
                    _nextBranches.Clear();
                }
                while (branches.IsNotEmpty)
                {
                    branches.Pop().MakeReady(this, valueContainer, ref _handleQueue);
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
                    [MethodImpl(InlineOption)]
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

                [MethodImpl(InlineOption)]
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
                        ResolveInternal(ResolveContainer<T>.GetOrCreate(other._result));
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
                internal int DeferredId
                {
                    [MethodImpl(InlineOption)]
                    get { return _deferredId; }
                }

                protected DeferredPromiseBase() { }

                ~DeferredPromiseBase()
                {
                    if (_state == Promise.State.Pending)
                    {
                        // Deferred wasn't handled.
                        AddRejectionToUnhandledStack(UnhandledDeferredException.instance, this);
                    }
                }

                protected virtual bool TryUnregisterCancelation() { return true; }

                protected bool TryIncrementDeferredIdAndUnregisterCancelation(int comparand)
                {
                    unchecked // We want the id to wrap around.
                    {
                        return Interlocked.CompareExchange(ref _deferredId, comparand + 1, comparand) == comparand
                            && TryUnregisterCancelation(); // If TryUnregisterCancelation returns false, it means the CancelationSource was canceled.
                    }
                }

                internal bool TryReject<TReject>(ref TReject reason, int deferredId, int rejectSkipFrames)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        RejectDirect(ref reason, rejectSkipFrames + 1);
                        return true;
                    }
                    return false;
                }

                internal bool TryCancel<TCancel>(ref TCancel reason, int deferredId)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        CancelDirect(ref reason);
                        return true;
                    }
                    return false;
                }

                internal bool TryCancel(int deferredId)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        CancelDirect();
                        return true;
                    }
                    return false;
                }

                protected void CancelFromToken(ICancelValueContainer valueContainer)
                {
                    ThrowIfInPool(this);
                    // A simple increment is sufficient.
                    // If the CancelationSource was canceled before the Deferred was completed, even if the Deferred was completed before the cancelation was invoked, the cancelation takes precedence.
                    Interlocked.Increment(ref _deferredId);
                    RejectOrCancelInternal(valueContainer);
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
                    [MethodImpl(InlineOption)]
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
                [MethodImpl(InlineOption)]
                protected void SuperDispose()
                {
                    base.Dispose();
                }

                internal bool TryResolve(int deferredId)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        ResolveDirect();
                        return true;
                    }
                    return false;
                }

                internal static DeferredPromiseVoid GetOrCreate()
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
            internal class DeferredPromise<T> : DeferredPromiseBase
            {
                private struct Creator : ICreator<DeferredPromise<T>>
                {
                    [MethodImpl(InlineOption)]
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
                [MethodImpl(InlineOption)]
                protected void SuperDispose()
                {
                    base.Dispose();
                }

                internal bool TryResolve(ref T value, int deferredId)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        ResolveDirect(ref value);
                        return true;
                    }
                    return false;
                }

                internal static DeferredPromise<T> GetOrCreate()
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<DeferredPromise<T>, Creator>(new Creator());
                    promise.Reset();
                    promise.ResetDepth();
                    return promise;
                }
            }

            // IDelegate to reduce the amount of classes I would have to write(Composition Over Inheritance).
            // Using generics with constraints allows us to use structs to get composition for "free"
            // (no extra object allocation or extra memory overhead, and the compiler will generate the Promise classes for us).
            // The only downside is that more classes are created than if we just used straight interfaces (not a problem with JIT, but makes the code size larger with AOT).

            // Resolve types for more common .Then(onResolved) calls to be more efficient.

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class PromiseResolve<TResolver> : PromiseRef where TResolver : IDelegateResolve
            {
                private struct Creator : ICreator<PromiseResolve<TResolver>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseResolve<TResolver> Create()
                    {
                        return new PromiseResolve<TResolver>();
                    }
                }

                private TResolver _resolver;

                private PromiseResolve() { }

                [MethodImpl(InlineOption)]
                public static PromiseResolve<TResolver> GetOrCreate(TResolver resolver)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolve<TResolver>, Creator>(new Creator());
                    promise.Reset();
                    promise._resolver = resolver;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
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
            private sealed class PromiseResolvePromise<TResolver> : PromiseWaitPromise where TResolver : IDelegateResolvePromise
            {
                private struct Creator : ICreator<PromiseResolvePromise<TResolver>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseResolvePromise<TResolver> Create()
                    {
                        return new PromiseResolvePromise<TResolver>();
                    }
                }

                private TResolver _resolver;

                private PromiseResolvePromise() { }

                [MethodImpl(InlineOption)]
                public static PromiseResolvePromise<TResolver> GetOrCreate(TResolver resolver)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolvePromise<TResolver>, Creator>(new Creator());
                    promise.Reset();
                    promise._resolver = resolver;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
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
            private sealed class PromiseResolveReject<TResolver, TRejecter> : PromiseRef
                where TResolver : IDelegateResolve
                where TRejecter : IDelegateReject
            {
                private struct Creator : ICreator<PromiseResolveReject<TResolver, TRejecter>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseResolveReject<TResolver, TRejecter> Create()
                    {
                        return new PromiseResolveReject<TResolver, TRejecter>();
                    }
                }

                private TResolver _resolver;
                private TRejecter _rejecter;

                private PromiseResolveReject() { }

                [MethodImpl(InlineOption)]
                public static PromiseResolveReject<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolveReject<TResolver, TRejecter>, Creator>(new Creator());
                    promise.Reset();
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    _rejecter = default(TRejecter);
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
            private sealed class PromiseResolveRejectPromise<TResolver, TRejecter> : PromiseWaitPromise
                where TResolver : IDelegateResolvePromise
                where TRejecter : IDelegateRejectPromise
            {
                private struct Creator : ICreator<PromiseResolveRejectPromise<TResolver, TRejecter>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseResolveRejectPromise<TResolver, TRejecter> Create()
                    {
                        return new PromiseResolveRejectPromise<TResolver, TRejecter>();
                    }
                }

                private TResolver _resolver;
                private TRejecter _rejecter;

                private PromiseResolveRejectPromise() { }

                [MethodImpl(InlineOption)]
                public static PromiseResolveRejectPromise<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolveRejectPromise<TResolver, TRejecter>, Creator>(new Creator());
                    promise.Reset();
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    _rejecter = default(TRejecter);
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
            private sealed class PromiseContinue<TContinuer> : PromiseRef
                where TContinuer : IDelegateContinue
            {
                private struct Creator : ICreator<PromiseContinue<TContinuer>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseContinue<TContinuer> Create()
                    {
                        return new PromiseContinue<TContinuer>();
                    }
                }

                private TContinuer _continuer;

                private PromiseContinue() { }

                [MethodImpl(InlineOption)]
                public static PromiseContinue<TContinuer> GetOrCreate(TContinuer continuer)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseContinue<TContinuer>, Creator>(new Creator());
                    promise.Reset();
                    promise._continuer = continuer;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    callback.Invoke(valueContainer, this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class PromiseContinuePromise<TContinuer> : PromiseWaitPromise
                where TContinuer : IDelegateContinuePromise
            {
                private struct Creator : ICreator<PromiseContinuePromise<TContinuer>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseContinuePromise<TContinuer> Create()
                    {
                        return new PromiseContinuePromise<TContinuer>();
                    }
                }

                private TContinuer _continuer;

                private PromiseContinuePromise() { }

                [MethodImpl(InlineOption)]
                public static PromiseContinuePromise<TContinuer> GetOrCreate(TContinuer continuer)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseContinuePromise<TContinuer>, Creator>(new Creator());
                    promise.Reset();
                    promise._continuer = continuer;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    callback.Invoke(valueContainer, this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class PromiseFinally<TFinalizer> : PromiseRef
                where TFinalizer : IDelegateFinally
            {
                private struct Creator : ICreator<PromiseFinally<TFinalizer>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseFinally<TFinalizer> Create()
                    {
                        return new PromiseFinally<TFinalizer>();
                    }
                }

                private TFinalizer _finalizer;

                private PromiseFinally() { }

                [MethodImpl(InlineOption)]
                public static PromiseFinally<TFinalizer> GetOrCreate(TFinalizer finalizer)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseFinally<TFinalizer>, Creator>(new Creator());
                    promise.Reset();
                    promise._finalizer = finalizer;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    var callback = _finalizer;
                    _finalizer = default(TFinalizer);
                    callback.Invoke(valueContainer, this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromisePassThrough : ITreeHandleable, ILinked<PromisePassThrough>
            {
                private struct Creator : ICreator<PromisePassThrough>
                {
                    [MethodImpl(InlineOption)]
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

        [MethodImpl(InlineOption)]
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

        [MethodImpl(InlineOption)]
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