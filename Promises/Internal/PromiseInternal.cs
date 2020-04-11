#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable RECS0001 // Class is declared partial but has only one part
#pragma warning disable IDE0041 // Use 'is null' check

using System;
using System.Collections.Generic;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise : Promise.Internal.ITreeHandleable, Promise.Internal.ITraceable
    {
        private ValueLinkedStack<Internal.ITreeHandleable> _nextBranches;
        protected object _valueOrPrevious;
        private ushort _retainCounter;
        protected State _state;
        protected bool _wasWaitedOn;

        Internal.ITreeHandleable ILinked<Internal.ITreeHandleable>.Next { get; set; }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
        private
#endif
        protected Promise()
        {
#if PROMISE_DEBUG
            _id = idCounter++;
#endif
        }

        ~Promise()
        {
            if (_retainCounter > 0 & _state != State.Pending)
            {
                if (_wasWaitedOn)
                {
                    ((Internal.IValueContainer) _valueOrPrevious).Release();
                }
                else
                {
                    // Rejection maybe wasn't caught.
                    ((Internal.IValueContainer) _valueOrPrevious).ReleaseAndAddToUnhandledStack();
                }
                // Promise wasn't released.
                AddRejectionToUnhandledStack(UnreleasedObjectException.instance, this);
            }
        }

        void Internal.ITreeHandleable.MakeReady(Internal.IValueContainer valueContainer,
            ref ValueLinkedQueue<Internal.ITreeHandleable> handleQueue,
            ref ValueLinkedQueue<Internal.ITreeHandleable> cancelQueue)
        {
#if PROMISE_CANCEL
            if (_state != State.Pending)
            {
                ReleaseInternal();
            }
            else
#endif
            {
                ((Promise) _valueOrPrevious)._wasWaitedOn = true;
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;
                handleQueue.Push(this);
            }
        }

        void Internal.ITreeHandleable.MakeReadyFromSettled(Internal.IValueContainer valueContainer)
        {
            ((Promise) _valueOrPrevious)._wasWaitedOn = true;
            valueContainer.Retain();
            _valueOrPrevious = valueContainer;
#if PROMISE_CANCEL
            if (valueContainer.GetState() == State.Canceled)
            {
                AddToCancelQueueBack(this);
            }
            else
#endif
            {
                AddToHandleQueueBack(this);
            }
        }

        protected virtual void Reset(int skipFrames)
        {
            _state = State.Pending;
            _retainCounter = 1;
            SetNotDisposed();
            SetCreatedStacktrace(this, skipFrames + 1);
        }

        protected void AddWaiter(Internal.ITreeHandleable waiter)
        {
            if (_state == State.Pending)
            {
                _nextBranches.Push(waiter);
            }
            else
            {
                waiter.MakeReadyFromSettled((Internal.IValueContainer) _valueOrPrevious);
            }
        }

        protected void RetainInternal()
        {
#if PROMISE_DEBUG
            checked // If this fails, change _retainCounter to uint or ulong.
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
            checked // This should never fail, but check in debug mode just in case.
#endif
            {
                return --_retainCounter;
            }
        }

        protected virtual void Dispose()
        {
            if (_valueOrPrevious != null)
            {
                if (_wasWaitedOn)
                {
                    ((Internal.IValueContainer) _valueOrPrevious).Release();
                }
                else
                {
                    // Rejection maybe wasn't caught.
                    ((Internal.IValueContainer) _valueOrPrevious).ReleaseAndAddToUnhandledStack();
                }
            }
            _valueOrPrevious = DisposedObject;
        }

        void Internal.ITreeHandleable.Handle()
        {
            if (_state == State.Pending)
            {
                Handle();
            }
            else
            {
                ReleaseInternal();
            }
        }

        protected virtual Promise GetDuplicate()
        {
            return Internal.DuplicatePromise0.GetOrCreate(2);
        }

        protected virtual void HandleCancel()
        {
            CancelInternal();
        }

        protected void CancelInternal(Internal.IValueContainer cancelValue)
        {
            cancelValue.Retain();
            _valueOrPrevious = cancelValue;
            CancelInternal();
        }

        protected void CancelInternal()
        {
            _state = State.Canceled;
            OnCancel();
            CancelBranches();
            CancelProgressListeners();

            ReleaseInternal();
        }

        protected void ResolveInternal(Internal.IValueContainer container)
        {
            container.Retain();
            _valueOrPrevious = container;
            ResolveInternal();
        }

        protected void ResolveInternal()
        {
            _state = State.Resolved;
            HandleBranches();
            ResolveProgressListeners();

            ReleaseInternal();
        }

        protected void RejectInternal(Internal.IValueContainer container)
        {
            container.Retain();
            _valueOrPrevious = container;
            RejectInternal();
        }

        protected void RejectInternal()
        {
            _state = State.Rejected;
            HandleBranches();
            CancelProgressListeners();

            ReleaseInternal();
        }

        protected void HookupNewPromise(Promise newPromise)
        {
            newPromise._valueOrPrevious = this;
            SetDepth(newPromise);
            AddWaiter(newPromise);
        }

        protected virtual void Handle()
        {
            try
            {
                SetCurrentInvoker(this);
                Execute();
            }
            catch (RethrowException)
            {
#if PROMISE_CANCEL
                if (_state == State.Pending)
                {
                    _state = ((Internal.IValueContainer) _valueOrPrevious).GetState();
                    if (_state == State.Rejected)
                    {
                        HandleBranches();
                    }
                    else
                    {
                        CancelBranches();
                    }
                    CancelProgressListeners();
                }
#else
                _state = State.Rejected;
                HandleBranches();
                CancelProgressListeners();
#endif
                ReleaseInternal();
            }
#if PROMISE_CANCEL
            catch (CancelException e)
            {
                if (_state == State.Pending)
                {
                    CancelInternal(((Internal.IExceptionToContainer) e).ToContainer(this));
                }
                else
                {
                    ReleaseInternal();
                }
            }
            catch (Internal.CanceledExceptionInternal e)
            {
                if (_state == State.Pending)
                {
                    // reason is an internal cancelation object (CanceledException), don't wrap it.
                    CancelInternal(e);
                }
                else
                {
                    ReleaseInternal();
                }
            }
            catch (OperationCanceledException) // Built-in system cancelation (or Task cancelation)
            {
                if (_state == State.Pending)
                {
                    CancelInternal(Internal.CancelContainerVoid.GetOrCreate());
                }
                else
                {
                    ReleaseInternal();
                }
            }
#endif
            catch (RejectException e)
            {
#if PROMISE_CANCEL
                if (_state == State.Canceled)
                {
                    ((Internal.ICantHandleException) e).AddToUnhandledStack(this);
                    ReleaseInternal();
                }
                else
#endif
                {
                    RejectInternal(((Internal.IExceptionToContainer) e).ToContainer(this));
                }
            }
            catch (Exception e)
            {
#if PROMISE_CANCEL
                if (_state == State.Canceled)
                {
                    AddRejectionToUnhandledStack(e, this);
                    ReleaseInternal();
                }
                else
#endif
                {
                    var rejection = CreateRejection(e);
                    SetCreatedAndRejectedStacktrace(rejection, false);
                    RejectInternal(rejection);
                }
            }
            finally
            {
                Internal._invokingResolved = false;
                Internal._invokingRejected = false;
                ClearCurrentInvoker();
            }
        }

        private void RejectDirect<TReject>(TReject reason, bool generateStacktrace)
        {
            _state = State.Rejected;
            var rejection = CreateRejection(reason);
            SetCreatedAndRejectedStacktrace(rejection, generateStacktrace);
            rejection.Retain();
            _valueOrPrevious = rejection;
            AddBranchesToHandleQueueBack(rejection);
            CancelProgressListeners();
            AddToHandleQueueFront(this);
        }

        protected static Internal.IRejectionContainer CreateRejection<TReject>(TReject reason)
        {
#if CSHARP_7_OR_LATER
            if (((object) reason) is Internal.IRejectionContainer internalReason)
#else
            Internal.IRejectionContainer internalReason = reason as Internal.IRejectionContainer;
            if (internalReason != null)
#endif
            {
                // reason is an internal rejection object (UnhandledException), use it directly instead of wrapping it.
                return internalReason;
            }

            // Avoid boxing value types.
            Type type = typeof(TReject);
            if (type.IsClass)
            {
#if CSHARP_7_OR_LATER
                if (((object) reason) is Exception e)
#else
                Exception e = reason as Exception;
                if (e != null)
#endif
                {
                    // reason is a non-null Exception.
                    return Internal.RejectionContainer<Exception>.GetOrCreate(e);
                }
                if (typeof(Exception).IsAssignableFrom(type))
                {
                    // reason is a null Exception, behave the same way .Net behaves if you throw null.
                    return Internal.RejectionContainer<Exception>.GetOrCreate(new NullReferenceException());
                }
            }
            return Internal.RejectionContainer<TReject>.GetOrCreate(reason);
        }

        protected void HandleSelf()
        {
            _state = ((Internal.IValueContainer) _valueOrPrevious).GetState();

            HandleBranches();
            if (_state == State.Resolved)
            {
                ResolveProgressListeners();
            }
            else
            {
                CancelProgressListeners();
            }

            ReleaseInternal();
        }

        protected virtual void Execute() { }

        protected virtual void OnCancel() { }

        private static ValueLinkedStackZeroGC<UnhandledException> _unhandledExceptions;

        private static void AddUnhandledException(UnhandledException exception)
        {
            _unhandledExceptions.Push(exception);
        }

        // Generate stacktrace if traceable is null.
        private static void AddRejectionToUnhandledStack<TReject>(TReject unhandledValue, Internal.ITraceable traceable)
        {
            if (unhandledValue is UnhandledException)
            {
                AddUnhandledException(unhandledValue as UnhandledException);
                return;
            }

#if PROMISE_DEBUG
            string stacktrace =
                traceable != null
                    ? GetFormattedStacktrace(traceable)
                    : Config.DebugCausalityTracer != TraceLevel.None
                        ? FormatStackTrace(new System.Diagnostics.StackTrace[1] { GetStackTrace(1) })
                        : null;
#else
            string stacktrace = null;
#endif
            string message;
            Exception innerException;
            bool valueIsNull = ReferenceEquals(unhandledValue, null);

            if (unhandledValue is Exception)
            {
                message = "An exception was not handled.";
                innerException = unhandledValue as Exception;
            }
            else if (typeof(Exception).IsAssignableFrom(typeof(TReject)))
            {
                // unhandledValue is a null Exception, behave the same way .Net behaves if you throw null.
                message = "An exception was not handled.";
                NullReferenceException nullRefEx = new NullReferenceException();
                AddUnhandledException(new Internal.UnhandledExceptionInternal(nullRefEx, typeof(NullReferenceException), message, stacktrace, nullRefEx));
                return;
            }
            else
            {
                Type type = typeof(TReject);
                message = "A rejected value was not handled, type: " + type + ", value: " + (valueIsNull ? "NULL" : unhandledValue.ToString());
                innerException = null;
            }
            AddUnhandledException(new Internal.UnhandledExceptionInternal(unhandledValue, valueIsNull ? typeof(TReject) : unhandledValue.GetType(), message, stacktrace, innerException));
        }

        // Handle promises in a depth-first manner.
        private static ValueLinkedQueue<Internal.ITreeHandleable> _handleQueue;
        private static bool _runningHandles;

        private static void AddToHandleQueueFront(Internal.ITreeHandleable handleable)
        {
            _handleQueue.Push(handleable);
        }

        private static void AddToHandleQueueBack(Internal.ITreeHandleable handleable)
        {
            _handleQueue.Enqueue(handleable);
        }

        private static void AddToHandleQueueFront(ref ValueLinkedQueue<Internal.ITreeHandleable> handleables)
        {
            _handleQueue.PushAndClear(ref handleables);
        }

        private static void AddToHandleQueueBack(ref ValueLinkedQueue<Internal.ITreeHandleable> handleables)
        {
            _handleQueue.EnqueueAndClear(ref handleables);
        }

        private void HandleBranches()
        {
            var valueContainer = (Internal.IValueContainer) _valueOrPrevious;
            ValueLinkedQueue<Internal.ITreeHandleable> handleQueue = new ValueLinkedQueue<Internal.ITreeHandleable>();
            ValueLinkedQueue<Internal.ITreeHandleable> cancelQueue = new ValueLinkedQueue<Internal.ITreeHandleable>();
            while (_nextBranches.IsNotEmpty)
            {
                _nextBranches.Pop().MakeReady(valueContainer, ref handleQueue, ref cancelQueue);
            }
            handleQueue.PushAndClear(ref cancelQueue);
            AddToHandleQueueFront(ref handleQueue);
        }

        private void AddBranchesToHandleQueueBack(Internal.IValueContainer valueContainer)
        {
            ValueLinkedQueue<Internal.ITreeHandleable> handleQueue = new ValueLinkedQueue<Internal.ITreeHandleable>();
            ValueLinkedQueue<Internal.ITreeHandleable> cancelQueue = new ValueLinkedQueue<Internal.ITreeHandleable>();
            while (_nextBranches.IsNotEmpty)
            {
                _nextBranches.Pop().MakeReady(valueContainer, ref handleQueue, ref cancelQueue);
            }
            AddToHandleQueueFront(ref cancelQueue);
            AddToHandleQueueBack(ref handleQueue);
        }

        private void CancelBranches()
        {
            var valueContainer = (Internal.IValueContainer) _valueOrPrevious;
            ValueLinkedQueue<Internal.ITreeHandleable> handleQueue = new ValueLinkedQueue<Internal.ITreeHandleable>();
            ValueLinkedQueue<Internal.ITreeHandleable> cancelQueue = new ValueLinkedQueue<Internal.ITreeHandleable>();
            while (_nextBranches.IsNotEmpty)
            {
                _nextBranches.Pop().MakeReady(valueContainer, ref handleQueue, ref cancelQueue);
            }
            AddToCancelQueueFront(ref cancelQueue);
            AddToCancelQueueBack(ref handleQueue);
        }

        private void AddBranchesToCancelQueueBack(Internal.IValueContainer valueContainer)
        {
            ValueLinkedQueue<Internal.ITreeHandleable> handleQueue = new ValueLinkedQueue<Internal.ITreeHandleable>();
            ValueLinkedQueue<Internal.ITreeHandleable> cancelQueue = new ValueLinkedQueue<Internal.ITreeHandleable>();
            while (_nextBranches.IsNotEmpty)
            {
                _nextBranches.Pop().MakeReady(valueContainer, ref handleQueue, ref cancelQueue);
            }
            cancelQueue.EnqueueAndClear(ref handleQueue);
            AddToCancelQueueBack(ref cancelQueue);
        }

        private static bool TryConvert<TConvert>(Internal.IValueContainer valueContainer, out TConvert converted)
        {
            // Avoid boxing value types.
#if CSHARP_7_OR_LATER
            if (valueContainer is IValueContainer<TConvert> directContainer)
#else
            var directContainer = valueContainer as IValueContainer<TConvert>;
            if (directContainer != null)
#endif
            {
                converted = directContainer.Value;
                return true;
            }

            if (typeof(TConvert).IsAssignableFrom(valueContainer.ValueType))
            {
                converted = (TConvert) valueContainer.Value;
                return true;
            }

            converted = default(TConvert);
            return false;
        }

        /// <summary>
        /// DON"T CALL THIS FUNCTION IN USER CODE!
        /// </summary>
        internal static void SetInvokingAsyncFunctionInternal(bool invoking)
        {
            Internal._invokingResolved = invoking;
        }
    }

    partial class Promise<T>
    {
#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
        private
#endif
        protected Promise() { }

        protected override sealed Promise GetDuplicate()
        {
            return Internal.DuplicatePromise<T>.GetOrCreate(2);
        }
    }

    partial class Promise
    {
        protected static partial class Internal
        {
            internal static bool _invokingResolved, _invokingRejected;

            internal static Action OnClearPool;

            [System.Diagnostics.DebuggerStepThrough]
            public abstract class PoolablePromise<TPromise> : Promise where TPromise : PoolablePromise<TPromise>
            {
                protected static ValueLinkedStack<ITreeHandleable> _pool;

                static PoolablePromise()
                {
                    OnClearPool += () => _pool.Clear();
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    if (Config.ObjectPooling == PoolType.All)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public abstract class PoolablePromise<T, TPromise> : Promise<T> where TPromise : PoolablePromise<T, TPromise>
            {
                protected static ValueLinkedStack<ITreeHandleable> _pool;

                static PoolablePromise()
                {
                    OnClearPool += () => _pool.Clear();
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    if (Config.ObjectPooling == PoolType.All)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public abstract partial class PromiseWaitPromise<TPromise> : PoolablePromise<TPromise> where TPromise : PromiseWaitPromise<TPromise>
            {
                public void WaitFor(Promise other)
                {
                    ValidateReturn(other);
#if PROMISE_CANCEL
                    if (_state == State.Canceled)
                    {
                        ReleaseInternal();
                    }
                    else
#endif
                    {
                        _valueOrPrevious = other;
#if PROMISE_PROGRESS
                        _secondPrevious = true;
                        if (_progressListeners.IsNotEmpty)
                        {
                            SubscribeProgressToBranchesAndRoots(other, this);
                        }
#endif
                        other.AddWaiter(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public abstract partial class PromiseWaitPromise<T, TPromise> : PoolablePromise<T, TPromise> where TPromise : PromiseWaitPromise<T, TPromise>
            {
                public void WaitFor(Promise<T> other)
                {
                    ValidateReturn(other);
#if PROMISE_CANCEL
                    if (_state == State.Canceled)
                    {
                        ReleaseInternal();
                    }
                    else
#endif
                    {
                        _valueOrPrevious = other;
#if PROMISE_PROGRESS
                        _secondPrevious = true;
                        if (_progressListeners.IsNotEmpty)
                        {
                            SubscribeProgressToBranchesAndRoots(other, this);
                        }
#endif
                        other.AddWaiter(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed partial class DeferredPromise0 : PoolablePromise<DeferredPromise0>
            {
                public readonly DeferredInternal0 deferred;

                private DeferredPromise0()
                {
                    deferred = new DeferredInternal0(this);
                }

                public static DeferredPromise0 GetOrCreate(int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (DeferredPromise0) _pool.Pop() : new DeferredPromise0();
                    promise.Reset(skipFrames + 1);
                    promise.deferred.Reset();
                    // Retain now, release when deferred resolves/rejects/cancels.
                    promise.RetainInternal();
                    promise.ResetDepth();
                    return promise;
                }

                protected override void OnCancel()
                {
                    AddToHandleQueueFront(this);
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed partial class DeferredPromise<T> : PoolablePromise<T, DeferredPromise<T>>
            {
                public readonly DeferredInternal<T> deferred;

                private DeferredPromise()
                {
                    deferred = new DeferredInternal<T>(this);
                }

                public static DeferredPromise<T> GetOrCreate(int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (DeferredPromise<T>) _pool.Pop() : new DeferredPromise<T>();
                    promise.Reset(skipFrames + 1);
                    promise.deferred.Reset();
                    // Retain now, release when deferred resolves/rejects/cancels.
                    promise.RetainInternal();
                    promise.ResetDepth();
                    return promise;
                }

                protected override void OnCancel()
                {
                    AddToHandleQueueFront(this);
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class SettledPromise : Promise
            {
                private SettledPromise() { }

                private static readonly SettledPromise _resolved = new SettledPromise()
                {
                    _state = State.Resolved,
                    _valueOrPrevious = ResolveContainerVoid.GetOrCreate()
                };

                public static SettledPromise GetOrCreateResolved()
                {
                    return _resolved;
                }

                protected override void Dispose() { }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class LitePromise0 : PoolablePromise<LitePromise0>
            {
                private LitePromise0() { }

                public static LitePromise0 GetOrCreate(int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (LitePromise0) _pool.Pop() : new LitePromise0();
                    promise.Reset(skipFrames + 1);
                    promise.ResetDepth();
                    return promise;
                }

#if PROMISE_DEBUG
                public void ResolveDirect()
                {
                    _state = State.Resolved;
                    _valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                    AddToHandleQueueFront(this);
                }
#endif

                protected override void OnCancel()
                {
                    AddToHandleQueueFront(this);
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class LitePromise<T> : PoolablePromise<T, LitePromise<T>>
            {
                private LitePromise() { }

                public static LitePromise<T> GetOrCreate(int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (LitePromise<T>) _pool.Pop() : new LitePromise<T>();
                    promise.Reset(skipFrames + 1);
                    promise.ResetDepth();
                    return promise;
                }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
                public void ResolveDirect(in T value)
#else
                public void ResolveDirect(T value)
#endif
                {
                    _state = State.Resolved;
                    var val = ResolveContainer<T>.GetOrCreate(value);
                    val.Retain();
                    _valueOrPrevious = val;
                    AddToHandleQueueFront(this);
                }

                protected override void OnCancel()
                {
                    AddToHandleQueueFront(this);
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DuplicatePromise0 : PoolablePromise<DuplicatePromise0>
            {
                private DuplicatePromise0() { }

                public static DuplicatePromise0 GetOrCreate(int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (DuplicatePromise0) _pool.Pop() : new DuplicatePromise0();
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle()
                {
                    HandleSelf();
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DuplicatePromise<T> : PoolablePromise<T, DuplicatePromise<T>>
            {
                private DuplicatePromise() { }

                public static DuplicatePromise<T> GetOrCreate(int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (DuplicatePromise<T>) _pool.Pop() : new DuplicatePromise<T>();
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle()
                {
                    HandleSelf();
                }
            }

#region Resolve Promises
            // Individual types for more common .Then(onResolved) calls to be more efficient.
            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseVoidResolve0 : PoolablePromise<PromiseVoidResolve0>
            {
                private Action _onResolved;

                private PromiseVoidResolve0() { }

                public static PromiseVoidResolve0 GetOrCreate(Action onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolve0) _pool.Pop() : new PromiseVoidResolve0();
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    var callback = _onResolved;
                    _onResolved = null;
                    State state = ((IValueContainer) _valueOrPrevious).GetState();
                    if (state == State.Resolved)
                    {
                        _invokingResolved = true;
                        callback.Invoke();
                        ResolveInternalIfNotCanceled();
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _onResolved = null;
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseArgResolve<TArg> : PoolablePromise<PromiseArgResolve<TArg>>
            {
                private Action<TArg> _onResolved;

                private PromiseArgResolve() { }

                public static PromiseArgResolve<TArg> GetOrCreate(Action<TArg> onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolve<TArg>) _pool.Pop() : new PromiseArgResolve<TArg>();
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    var callback = _onResolved;
                    _onResolved = null;
                    if (((IValueContainer) _valueOrPrevious).GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) _valueOrPrevious).value;
                        callback.Invoke(arg);
                        ResolveInternalIfNotCanceled();
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _onResolved = null;
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseVoidResolve<TResult> : PoolablePromise<TResult, PromiseVoidResolve<TResult>>
            {
                private Func<TResult> _onResolved;

                private PromiseVoidResolve() { }

                public static PromiseVoidResolve<TResult> GetOrCreate(Func<TResult> onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolve<TResult>) _pool.Pop() : new PromiseVoidResolve<TResult>();
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    var callback = _onResolved;
                    _onResolved = null;
                    State state = ((IValueContainer) _valueOrPrevious).GetState();
                    if (state == State.Resolved)
                    {
                        _invokingResolved = true;
                        TResult result = callback.Invoke();
                        ResolveInternalIfNotCanceled(result);
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _onResolved = null;
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseArgResolve<TArg, TResult> : PoolablePromise<TResult, PromiseArgResolve<TArg, TResult>>
            {
                private Func<TArg, TResult> _onResolved;

                private PromiseArgResolve() { }

                public static PromiseArgResolve<TArg, TResult> GetOrCreate(Func<TArg, TResult> onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolve<TArg, TResult>) _pool.Pop() : new PromiseArgResolve<TArg, TResult>();
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    var callback = _onResolved;
                    _onResolved = null;
                    if (((IValueContainer) _valueOrPrevious).GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) _valueOrPrevious).value;
                        TResult result = callback.Invoke(arg);
                        ResolveInternalIfNotCanceled(result);
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _onResolved = null;
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseVoidResolvePromise0 : PromiseWaitPromise<PromiseVoidResolvePromise0>
            {
                private Func<Promise> _onResolved;

                private PromiseVoidResolvePromise0() { }

                public static PromiseVoidResolvePromise0 GetOrCreate(Func<Promise> onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolvePromise0) _pool.Pop() : new PromiseVoidResolvePromise0();
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf();
                        return;
                    }

                    var callback = _onResolved;
                    _onResolved = null;
                    State state = ((IValueContainer) _valueOrPrevious).GetState();
                    if (state == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke());
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _onResolved = null;
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseArgResolvePromise<TArg> : PromiseWaitPromise<PromiseArgResolvePromise<TArg>>
            {
                private Func<TArg, Promise> _onResolved;

                private PromiseArgResolvePromise() { }

                public static PromiseArgResolvePromise<TArg> GetOrCreate(Func<TArg, Promise> onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolvePromise<TArg>) _pool.Pop() : new PromiseArgResolvePromise<TArg>();
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf();
                        return;
                    }

                    var callback = _onResolved;
                    _onResolved = null;
                    if (((IValueContainer) _valueOrPrevious).GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) _valueOrPrevious).value;
                        WaitFor(callback.Invoke(arg));
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _onResolved = null;
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseVoidResolvePromise<TPromise> : PromiseWaitPromise<TPromise, PromiseVoidResolvePromise<TPromise>>
            {
                private Func<Promise<TPromise>> _onResolved;

                private PromiseVoidResolvePromise() { }

                public static PromiseVoidResolvePromise<TPromise> GetOrCreate(Func<Promise<TPromise>> onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolvePromise<TPromise>) _pool.Pop() : new PromiseVoidResolvePromise<TPromise>();
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf();
                        return;
                    }

                    var callback = _onResolved;
                    _onResolved = null;
                    State state = ((IValueContainer) _valueOrPrevious).GetState();
                    if (state == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke());
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _onResolved = null;
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseArgResolvePromise<TArg, TPromise> : PromiseWaitPromise<TPromise, PromiseArgResolvePromise<TArg, TPromise>>
            {
                private Func<TArg, Promise<TPromise>> _onResolved;

                private PromiseArgResolvePromise() { }

                public static PromiseArgResolvePromise<TArg, TPromise> GetOrCreate(Func<TArg, Promise<TPromise>> onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolvePromise<TArg, TPromise>) _pool.Pop() : new PromiseArgResolvePromise<TArg, TPromise>();
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf();
                        return;
                    }

                    var callback = _onResolved;
                    _onResolved = null;
                    if (((IValueContainer) _valueOrPrevious).GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) _valueOrPrevious).value;
                        WaitFor(callback.Invoke(arg));
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _onResolved = null;
                }
            }


            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseCaptureVoidResolve<TCapture> : PoolablePromise<PromiseCaptureVoidResolve<TCapture>>
            {
                private TCapture _capturedValue;
                private Action<TCapture> resolveHandler;

                private PromiseCaptureVoidResolve() { }

                public static PromiseCaptureVoidResolve<TCapture> GetOrCreate(TCapture capturedValue, Action<TCapture> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCaptureVoidResolve<TCapture>) _pool.Pop() : new PromiseCaptureVoidResolve<TCapture>();
                    promise._capturedValue = capturedValue;
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = resolveHandler;
                    resolveHandler = null;
                    State state = ((IValueContainer) _valueOrPrevious).GetState();
                    if (state == State.Resolved)
                    {
                        _invokingResolved = true;
                        callback.Invoke(value);
                        ResolveInternalIfNotCanceled();
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    resolveHandler = null;
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseCaptureArgResolve<TCapture, TArg> : PoolablePromise<PromiseCaptureArgResolve<TCapture, TArg>>
            {
                private TCapture _capturedValue;
                private Action<TCapture, TArg> _onResolved;

                private PromiseCaptureArgResolve() { }

                public static PromiseCaptureArgResolve<TCapture, TArg> GetOrCreate(TCapture capturedValue, Action<TCapture, TArg> onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCaptureArgResolve<TCapture, TArg>) _pool.Pop() : new PromiseCaptureArgResolve<TCapture, TArg>();
                    promise._capturedValue = capturedValue;
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (((IValueContainer) _valueOrPrevious).GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) _valueOrPrevious).value;
                        callback.Invoke(value, arg);
                        ResolveInternalIfNotCanceled();
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    _onResolved = null;
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseCaptureVoidResolve<TCapture, TResult> : PoolablePromise<TResult, PromiseCaptureVoidResolve<TCapture, TResult>>
            {
                private TCapture _capturedValue;
                private Func<TCapture, TResult> _onResolved;

                private PromiseCaptureVoidResolve() { }

                public static PromiseCaptureVoidResolve<TCapture, TResult> GetOrCreate(TCapture capturedValue, Func<TCapture, TResult> onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCaptureVoidResolve<TCapture, TResult>) _pool.Pop() : new PromiseCaptureVoidResolve<TCapture, TResult>();
                    promise._capturedValue = capturedValue;
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    State state = ((IValueContainer) _valueOrPrevious).GetState();
                    if (state == State.Resolved)
                    {
                        _invokingResolved = true;
                        TResult result = callback.Invoke(value);
                        ResolveInternalIfNotCanceled(result);
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    _onResolved = null;
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseCaptureArgResolve<TCapture, TArg, TResult> : PoolablePromise<TResult, PromiseCaptureArgResolve<TCapture, TArg, TResult>>
            {
                private TCapture _capturedValue;
                private Func<TCapture, TArg, TResult> _onResolved;

                private PromiseCaptureArgResolve() { }

                public static PromiseCaptureArgResolve<TCapture, TArg, TResult> GetOrCreate(TCapture capturedValue, Func<TCapture, TArg, TResult> onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCaptureArgResolve<TCapture, TArg, TResult>) _pool.Pop() : new PromiseCaptureArgResolve<TCapture, TArg, TResult>();
                    promise._capturedValue = capturedValue;
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (((IValueContainer) _valueOrPrevious).GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) _valueOrPrevious).value;
                        TResult result = callback.Invoke(value, arg);
                        ResolveInternalIfNotCanceled(result);
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    _onResolved = null;
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseCaptureVoidResolvePromise<TCapture> : PromiseWaitPromise<PromiseCaptureVoidResolvePromise<TCapture>>
            {
                private TCapture _capturedValue;
                private Func<TCapture, Promise> _onResolved;

                private PromiseCaptureVoidResolvePromise() { }

                public static PromiseCaptureVoidResolvePromise<TCapture> GetOrCreate(TCapture capturedValue, Func<TCapture, Promise> onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCaptureVoidResolvePromise<TCapture>) _pool.Pop() : new PromiseCaptureVoidResolvePromise<TCapture>();
                    promise._capturedValue = capturedValue;
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf();
                        return;
                    }

                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    State state = ((IValueContainer) _valueOrPrevious).GetState();
                    if (state == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke(value));
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    _onResolved = null;
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseCaptureArgResolvePromise<TCapture, TArg> : PromiseWaitPromise<PromiseCaptureArgResolvePromise<TCapture, TArg>>
            {
                private TCapture _capturedValue;
                private Func<TCapture, TArg, Promise> _onResolved;

                private PromiseCaptureArgResolvePromise() { }

                public static PromiseCaptureArgResolvePromise<TCapture, TArg> GetOrCreate(TCapture capturedValue, Func<TCapture, TArg, Promise> onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCaptureArgResolvePromise<TCapture, TArg>) _pool.Pop() : new PromiseCaptureArgResolvePromise<TCapture, TArg>();
                    promise._capturedValue = capturedValue;
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf();
                        return;
                    }

                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (((IValueContainer) _valueOrPrevious).GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) _valueOrPrevious).value;
                        WaitFor(callback.Invoke(value, arg));
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    _onResolved = null;
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseCaptureVoidResolvePromise<TCapture, TPromise> : PromiseWaitPromise<TPromise, PromiseCaptureVoidResolvePromise<TCapture, TPromise>>
            {
                private TCapture _capturedValue;
                private Func<TCapture, Promise<TPromise>> _onResolved;

                private PromiseCaptureVoidResolvePromise() { }

                public static PromiseCaptureVoidResolvePromise<TCapture, TPromise> GetOrCreate(TCapture capturedValue, Func<TCapture, Promise<TPromise>> onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCaptureVoidResolvePromise<TCapture, TPromise>) _pool.Pop() : new PromiseCaptureVoidResolvePromise<TCapture, TPromise>();
                    promise._capturedValue = capturedValue;
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf();
                        return;
                    }

                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    State state = ((IValueContainer) _valueOrPrevious).GetState();
                    if (state == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke(value));
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    _onResolved = null;
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseCaptureArgResolvePromise<TCapture, TArg, TPromise> : PromiseWaitPromise<TPromise, PromiseCaptureArgResolvePromise<TCapture, TArg, TPromise>>
            {
                private TCapture _capturedValue;
                private Func<TCapture, TArg, Promise<TPromise>> _onResolved;

                private PromiseCaptureArgResolvePromise() { }

                public static PromiseCaptureArgResolvePromise<TCapture, TArg, TPromise> GetOrCreate(TCapture capturedValue, Func<TCapture, TArg, Promise<TPromise>> onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCaptureArgResolvePromise<TCapture, TArg, TPromise>) _pool.Pop() : new PromiseCaptureArgResolvePromise<TCapture, TArg, TPromise>();
                    promise._capturedValue = capturedValue;
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf();
                        return;
                    }

                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (((IValueContainer) _valueOrPrevious).GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) _valueOrPrevious).value;
                        WaitFor(callback.Invoke(value, arg));
                    }
                    else
                    {
                        RejectInternal();
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    _onResolved = null;
                }
            }
#endregion

#region Resolve or Reject Promises
            // IDelegate to reduce the amount of classes I would have to write to handle catches (Composition Over Inheritance).
            // I'm less concerned about performance for catches since exceptions are expensive anyway, and they are expected to be used less often than .Then(onResolved).
            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseResolveReject0 : PoolablePromise<PromiseResolveReject0>
            {
                private IDelegateResolve _onResolved;
                private IDelegateReject _onRejected;

                private PromiseResolveReject0() { }

                public static PromiseResolveReject0 GetOrCreate(IDelegateResolve onResolved, IDelegateReject onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveReject0) _pool.Pop() : new PromiseResolveReject0();
                    promise._onResolved = onResolved;
                    promise._onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    var resolveCallback = _onResolved;
                    _onResolved = null;
                    var rejectCallback = _onRejected;
                    _onRejected = null;
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        _invokingResolved = true;
                        resolveCallback.DisposeAndInvoke(valueContainer, this);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        _invokingRejected = true;
                        rejectCallback.DisposeAndInvoke(valueContainer, this);
                    }
                }

                protected override void OnCancel()
                {
                    if (_onResolved != null)
                    {
                        _onResolved.Dispose();
                        _onResolved = null;
                        _onRejected.Dispose();
                        _onRejected = null;
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseResolveReject<T> : PoolablePromise<T, PromiseResolveReject<T>>
            {
                private IDelegateResolve _onResolved;
                private IDelegateReject _onRejected;

                private PromiseResolveReject() { }

                public static PromiseResolveReject<T> GetOrCreate(IDelegateResolve onResolved, IDelegateReject onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveReject<T>) _pool.Pop() : new PromiseResolveReject<T>();
                    promise._onResolved = onResolved;
                    promise._onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    var resolveCallback = _onResolved;
                    _onResolved = null;
                    var rejectCallback = _onRejected;
                    _onRejected = null;
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        _invokingResolved = true;
                        resolveCallback.DisposeAndInvoke(valueContainer, this);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        _invokingRejected = true;
                        rejectCallback.DisposeAndInvoke(valueContainer, this);
                    }
                }

                protected override void OnCancel()
                {
                    if (_onResolved != null)
                    {
                        _onResolved.Dispose();
                        _onResolved = null;
                        _onRejected.Dispose();
                        _onRejected = null;
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseResolveRejectPromise0 : PromiseWaitPromise<PromiseResolveRejectPromise0>
            {
                private IDelegateResolvePromise _onResolved;
                private IDelegateRejectPromise _onRejected;

                private PromiseResolveRejectPromise0() { }

                public static PromiseResolveRejectPromise0 GetOrCreate(IDelegateResolvePromise onResolved, IDelegateRejectPromise onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectPromise0) _pool.Pop() : new PromiseResolveRejectPromise0();
                    promise._onResolved = onResolved;
                    promise._onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf();
                        return;
                    }

                    var resolveCallback = _onResolved;
                    _onResolved = null;
                    var rejectCallback = _onRejected;
                    _onRejected = null;
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        _invokingResolved = true;
                        resolveCallback.DisposeAndInvoke(valueContainer, this);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        _invokingRejected = true;
#if PROMISE_PROGRESS
                        _suspended = true;
#endif
                        rejectCallback.DisposeAndInvoke(valueContainer, this);
                    }
                }

                protected override void OnCancel()
                {
                    if (_onResolved != null)
                    {
                        _onResolved.Dispose();
                        _onResolved = null;
                        _onRejected.Dispose();
                        _onRejected = null;
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseResolveRejectPromise<TPromise> : PromiseWaitPromise<TPromise, PromiseResolveRejectPromise<TPromise>>
            {
                private IDelegateResolvePromise _onResolved;
                private IDelegateRejectPromise _onRejected;

                private PromiseResolveRejectPromise() { }

                public static PromiseResolveRejectPromise<TPromise> GetOrCreate(IDelegateResolvePromise onResolved, IDelegateRejectPromise onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectPromise<TPromise>) _pool.Pop() : new PromiseResolveRejectPromise<TPromise>();
                    promise._onResolved = onResolved;
                    promise._onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf();
                        return;
                    }

                    var resolveCallback = _onResolved;
                    _onResolved = null;
                    var rejectCallback = _onRejected;
                    _onRejected = null;
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        _invokingResolved = true;
                        resolveCallback.DisposeAndInvoke(valueContainer, this);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        _invokingRejected = true;
#if PROMISE_PROGRESS
                        _suspended = true;
#endif
                        rejectCallback.DisposeAndInvoke(valueContainer, this);
                    }
                }

                protected override void OnCancel()
                {
                    if (_onResolved != null)
                    {
                        _onResolved.Dispose();
                        _onResolved = null;
                        _onRejected.Dispose();
                        _onRejected = null;
                    }
                }
            }
            #endregion

            #region Continue Promises
            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseContinue0 : PoolablePromise<PromiseContinue0>
            {
                private IDelegateContinue _onContinue;

                private PromiseContinue0() { }

                public static PromiseContinue0 GetOrCreate(IDelegateContinue onContinue, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseContinue0) _pool.Pop() : new PromiseContinue0();
                    promise._onContinue = onContinue;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    var callback = _onContinue;
                    _onContinue = null;
                    _invokingResolved = true;
                    callback.DisposeAndInvoke((IValueContainer) _valueOrPrevious);
                    ResolveInternalIfNotCanceled();
                }

                protected override void HandleCancel()
                {
                    Handle();
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseContinue<T> : PoolablePromise<T, PromiseContinue<T>>
            {
                private IDelegateContinue<T> _onContinue;

                private PromiseContinue() { }

                public static PromiseContinue<T> GetOrCreate(IDelegateContinue<T> onContinue, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseContinue<T>) _pool.Pop() : new PromiseContinue<T>();
                    promise._onContinue = onContinue;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    var callback = _onContinue;
                    _onContinue = null;
                    _invokingResolved = true;
                    T result = callback.DisposeAndInvoke((IValueContainer) _valueOrPrevious);
                    ResolveInternalIfNotCanceled(result);
                }

                protected override void HandleCancel()
                {
                    Handle();
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseContinuePromise0 : PromiseWaitPromise<PromiseContinuePromise0>
            {
                private IDelegateContinue<Promise> _onContinue;

                private PromiseContinuePromise0() { }

                public static PromiseContinuePromise0 GetOrCreate(IDelegateContinue<Promise> onContinue, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseContinuePromise0) _pool.Pop() : new PromiseContinuePromise0();
                    promise._onContinue = onContinue;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    if (_onContinue == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf();
                        return;
                    }

                    var callback = _onContinue;
                    _onContinue = null;
                    _invokingResolved = true;
                    Promise result = callback.DisposeAndInvoke((IValueContainer) _valueOrPrevious);
                    WaitFor(result);
                }

                protected override void HandleCancel()
                {
                    Handle();
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class PromiseContinuePromise<TPromise> : PromiseWaitPromise<TPromise, PromiseContinuePromise<TPromise>>
            {
                private IDelegateContinue<Promise<TPromise>> _onContinue;

                private PromiseContinuePromise() { }

                public static PromiseContinuePromise<TPromise> GetOrCreate(IDelegateContinue<Promise<TPromise>> onContinue, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseContinuePromise<TPromise>) _pool.Pop() : new PromiseContinuePromise<TPromise>();
                    promise._onContinue = onContinue;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute()
                {
                    if (_onContinue == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf();
                        return;
                    }

                    var callback = _onContinue;
                    _onContinue = null;
                    _invokingResolved = true;
                    Promise<TPromise> result = callback.DisposeAndInvoke((IValueContainer) _valueOrPrevious);
                    WaitFor(result);
                }

                protected override void HandleCancel()
                {
                    Handle();
                }
            }
            #endregion

            [System.Diagnostics.DebuggerStepThrough]
            public sealed partial class PromisePassThrough : ITreeHandleable, IRetainable, ILinked<PromisePassThrough>
            {
                private static ValueLinkedStack<PromisePassThrough> _pool;

                static PromisePassThrough()
                {
                    OnClearPool += () => _pool.Clear();
                }

                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }
                PromisePassThrough ILinked<PromisePassThrough>.Next { get; set; }

                public Promise Owner { get; private set; }
                public IMultiTreeHandleable Target { get; private set; }

                private int _index;
                private uint _retainCounter;

                public static PromisePassThrough GetOrCreate(Promise owner, int index, int skipFrames)
                {
                    ValidateElementNotNull(owner, "promises", "A promise was null", skipFrames + 1);
                    ValidateOperation(owner, skipFrames + 1);

                    var passThrough = _pool.IsNotEmpty ? _pool.Pop() : new PromisePassThrough();
                    passThrough.Owner = owner;
                    passThrough._index = index;
                    passThrough._retainCounter = 1u;
                    return passThrough;
                }

                private PromisePassThrough() { }

                public void SetTargetAndAddToOwner(IMultiTreeHandleable target)
                {
                    Target = target;
                    Owner.AddWaiter(this);
                }

                void ITreeHandleable.MakeReady(IValueContainer valueContainer,
                    ref ValueLinkedQueue<ITreeHandleable> handleQueue,
                    ref ValueLinkedQueue<ITreeHandleable> cancelQueue)
                {
                    var temp = Target;
                    if (temp.Handle(valueContainer, Owner, _index))
                    {
                        handleQueue.Push(temp);
                    }
                }

                void ITreeHandleable.MakeReadyFromSettled(IValueContainer valueContainer)
                {
                    var temp = Target;
                    if (temp.Handle(valueContainer, Owner, _index))
                    {
#if PROMISE_CANCEL
                        if (valueContainer.GetState() == State.Canceled)
                        {
                            AddToCancelQueueBack(temp);
                        }
                        else
#endif
                        {
                            AddToHandleQueueBack(temp);
                        }
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
                            if (Config.ObjectPooling != PoolType.None)
                            {
                                _pool.Push(this);
                            }
                        }
                    }
                }

                void ITreeHandleable.Handle() { throw new System.InvalidOperationException(); }
                void ITreeHandleable.Cancel() { throw new System.InvalidOperationException(); }
            }

            public static ValueLinkedStack<PromisePassThrough> WrapInPassThroughs<TEnumerator>(TEnumerator promises, out int count, int skipFrames) where TEnumerator : IEnumerator<Promise>
            {
                // Assumes promises.MoveNext() was already called once before this.
                int index = 0;
                var passThroughs = new ValueLinkedStack<PromisePassThrough>(PromisePassThrough.GetOrCreate(promises.Current, index, skipFrames + 1));
                while (promises.MoveNext())
                {
                    passThroughs.Push(PromisePassThrough.GetOrCreate(promises.Current, ++index, skipFrames + 1));
                }
                count = index + 1;
                return passThroughs;
            }

#pragma warning disable RECS0096 // Type parameter is never used
            public static ValueLinkedStack<PromisePassThrough> WrapInPassThroughs<T, TEnumerator>(TEnumerator promises, out int count, int skipFrames) where TEnumerator : IEnumerator<Promise<T>>
#pragma warning restore RECS0096 // Type parameter is never used
            {
                // Assumes promises.MoveNext() was already called once before this.
                int index = 0;
                var passThroughs = new ValueLinkedStack<PromisePassThrough>(PromisePassThrough.GetOrCreate(promises.Current, index, skipFrames + 1));
                while (promises.MoveNext())
                {
                    passThroughs.Push(PromisePassThrough.GetOrCreate(promises.Current, ++index, skipFrames + 1));
                }
                count = index + 1;
                return passThroughs;
            }
        }
    }
}