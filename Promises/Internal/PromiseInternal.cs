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

        internal Promise()
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
                string message = "A Promise object was garbage collected that was not released. You must release all IRetainable objects that you have retained.";
                AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
            }
        }

        void Internal.ITreeHandleable.MakeReady(Internal.IValueContainer valueContainer, ref ValueLinkedQueue<Internal.ITreeHandleable> handleQueue)
        {
            ((Promise) _valueOrPrevious)._wasWaitedOn = true;
            valueContainer.Retain();
            _valueOrPrevious = valueContainer;
            handleQueue.Push(this);
        }

        void Internal.ITreeHandleable.MakeReadyFromSettled(Internal.IValueContainer valueContainer)
        {
            ((Promise) _valueOrPrevious)._wasWaitedOn = true;
            valueContainer.Retain();
            _valueOrPrevious = valueContainer;
            AddToHandleQueueBack(this);
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

        protected void ResolveInternal(Internal.IValueContainer container)
        {
            _state = State.Resolved;
            container.Retain();
            _valueOrPrevious = container;
            HandleBranches();
            ResolveProgressListeners();

            ReleaseInternal();
        }

        protected void RejectOrCancelInternal(Internal.IValueContainer container)
        {
            _state = container.GetState();
            container.Retain();
            _valueOrPrevious = container;
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

        void Internal.ITreeHandleable.Handle()
        {
            Internal.IValueContainer container = (Internal.IValueContainer) _valueOrPrevious;
            _valueOrPrevious = null;
            SetCurrentInvoker(this);
            try
            {
                Execute(container);
                container.Release();
            }
            catch (RethrowException)
            {
                _state = container.GetState();
                _valueOrPrevious = container;
                HandleBranches();
                CancelProgressListeners();
                ReleaseInternal();
            }
            catch (OperationCanceledException e)
            {
                container.Release();
                RejectOrCancelInternal(CreateCancelContainer(e));
            }
            catch (Exception e)
            {
                container.Release();
                RejectOrCancelInternal(CreateRejectContainer(e, int.MinValue, this));
            }
            finally
            {
                Internal._invokingResolved = false;
                Internal._invokingRejected = false;
                ClearCurrentInvoker();
            }
        }

        private void ResolveDirect()
        {
            _state = State.Resolved;
            var resolveValue = Internal.ResolveContainerVoid.GetOrCreate();
            _valueOrPrevious = resolveValue;
            AddBranchesToHandleQueueBack(resolveValue);
            ResolveProgressListeners();
            AddToHandleQueueFront(this);
        }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
        private void ResolveDirect<T>(in T value)
#else
        private void ResolveDirect<T>(T value)
#endif
        {
            _state = State.Resolved;
            var resolveValue = Internal.ResolveContainer<T>.GetOrCreate(value);
            resolveValue.Retain();
            _valueOrPrevious = resolveValue;
            AddBranchesToHandleQueueBack(resolveValue);
            ResolveProgressListeners();
            AddToHandleQueueFront(this);
        }

        private void RejectDirect<TReject>(TReject reason, int rejectSkipFrames)
        {
            _state = State.Rejected;
            var rejection = CreateRejectContainer(reason, rejectSkipFrames + 1, this);
            rejection.Retain();
            _valueOrPrevious = rejection;
            AddBranchesToHandleQueueBack(rejection);
            CancelProgressListeners();
            AddToHandleQueueFront(this);
        }

        private static Internal.IRejectValueContainer CreateRejectContainer<TReject>(TReject reason, int rejectSkipFrames, Internal.ITraceable traceable)
        {
            Internal.IRejectValueContainer valueContainer;

            // Avoid boxing value types.
            Type type = typeof(TReject);
            if (type.IsValueType)
            {
                valueContainer = Internal.RejectionContainer<TReject>.GetOrCreate(reason);
            }
            else
            {
#if CSHARP_7_OR_LATER
                if (((object) reason) is Internal.IRejectionToContainer internalRejection)
#else
                Internal.IRejectionToContainer internalRejection = reason as Internal.IRejectionToContainer;
                if (internalRejection != null)
#endif
                {
                    // reason is an internal rejection object, get its container instead of wrapping it.
                    return internalRejection.ToContainer(traceable);
                }

#if CSHARP_7_OR_LATER
                if (((object) reason) is Exception e)
#else
                Exception e = reason as Exception;
                if (e != null)
#endif
                {
                    // reason is a non-null Exception.
                    valueContainer = Internal.RejectionContainer<Exception>.GetOrCreate(e);
                }
                else if (typeof(Exception).IsAssignableFrom(type))
                {
                    // reason is a null Exception, behave the same way .Net behaves if you throw null.
                    valueContainer = Internal.RejectionContainer<Exception>.GetOrCreate(new NullReferenceException());
                }
                else
                {
                    valueContainer = Internal.RejectionContainer<TReject>.GetOrCreate(reason);
                }
            }
            SetCreatedAndRejectedStacktrace(valueContainer, rejectSkipFrames + 1, traceable);
            return valueContainer;
        }

        protected void HandleSelf(Internal.IValueContainer valueContainer)
        {
            _state = valueContainer.GetState();
            valueContainer.Retain();
            _valueOrPrevious = valueContainer;

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

        protected virtual void Execute(Internal.IValueContainer valueContainer) { }

        private static ValueLinkedStackZeroGC<UnhandledException> _unhandledExceptions;

        private static void AddUnhandledException(UnhandledException exception)
        {
            _unhandledExceptions.Push(exception);
        }

        // Generate stacktrace if traceable is null.
        private static void AddRejectionToUnhandledStack<TReject>(TReject unhandledValue, Internal.ITraceable traceable)
        {
#if CSHARP_7_OR_LATER
            if (((object) unhandledValue) is Internal.ICantHandleException ex)
#else
            Internal.ICantHandleException ex = unhandledValue as Internal.ICantHandleException;
            if (ex != null)
#endif
            {
                ex.AddToUnhandledStack(traceable);
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
            while (_nextBranches.IsNotEmpty)
            {
                _nextBranches.Pop().MakeReady(valueContainer, ref handleQueue);
            }
            AddToHandleQueueFront(ref handleQueue);
        }

        private void AddBranchesToHandleQueueBack(Internal.IValueContainer valueContainer)
        {
            ValueLinkedQueue<Internal.ITreeHandleable> handleQueue = new ValueLinkedQueue<Internal.ITreeHandleable>();
            while (_nextBranches.IsNotEmpty)
            {
                _nextBranches.Pop().MakeReady(valueContainer, ref handleQueue);
            }
            AddToHandleQueueBack(ref handleQueue);
        }

        private static bool TryConvert<TConvert>(Internal.IValueContainer valueContainer, out TConvert converted)
        {
            // Avoid boxing value types.
#if CSHARP_7_OR_LATER
            if (((object) valueContainer) is IValueContainer<TConvert> directContainer)
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
                // Unfortunately, this will box if converting from a non-nullable value type to nullable.
                // I couldn't find any way around that without resorting to Expressions (which won't work for this purpose with the IL2CPP compiler),
                // and I didn't want to restrict converting to nullables simply for the sake of no boxing.
                converted = (TConvert) valueContainer.Value;
                return true;
            }

            converted = default(TConvert);
            return false;
        }

        /// <summary>
        /// DON'T CALL THIS FUNCTION IN USER CODE!
        /// </summary>
        internal static void SetInvokingAsyncFunctionInternal(bool invoking)
        {
            Internal._invokingResolved = invoking;
        }
    }

    partial class Promise<T>
    {
        internal Promise() { }
    }

    partial class Promise
    {
        protected static partial class Internal
        {
            internal static bool _invokingResolved, _invokingRejected;

            internal static Action OnClearPool;

            [System.Diagnostics.DebuggerNonUserCode]
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

            [System.Diagnostics.DebuggerNonUserCode]
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

            [System.Diagnostics.DebuggerNonUserCode]
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

            [System.Diagnostics.DebuggerNonUserCode]
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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed partial class DeferredPromise0 : PoolablePromise<DeferredPromise0>, ITreeHandleable
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

                void ITreeHandleable.Handle()
                {
                    ReleaseInternal();
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed partial class DeferredPromise<T> : PoolablePromise<T, DeferredPromise<T>>, ITreeHandleable
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

                void ITreeHandleable.Handle()
                {
                    ReleaseInternal();
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class SettledPromise : Promise
            {
                private SettledPromise() { }

                private static readonly SettledPromise _resolved = new SettledPromise()
                {
                    _state = State.Resolved,
                    _valueOrPrevious = ResolveContainerVoid.GetOrCreate()
                };

                private static readonly SettledPromise _canceled = new SettledPromise()
                {
                    _state = State.Canceled,
                    _valueOrPrevious = CancelContainerVoid.GetOrCreate()
                };

                public static SettledPromise GetOrCreateResolved()
                {
                    return _resolved;
                }

                public static SettledPromise GetOrCreateCanceled()
                {
                    return _canceled;
                }

                protected override void Dispose() { }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class LitePromise0 : PoolablePromise<LitePromise0>, ITreeHandleable
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
                new public void ResolveDirect()
                {
                    _state = State.Resolved;
                    _valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                    AddToHandleQueueFront(this);
                }
#endif

                void ITreeHandleable.Handle()
                {
                    ReleaseInternal();
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class LitePromise<T> : PoolablePromise<T, LitePromise<T>>, ITreeHandleable
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

                void ITreeHandleable.Handle()
                {
                    ReleaseInternal();
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DuplicatePromise0 : PoolablePromise<DuplicatePromise0>
            {
                private DuplicatePromise0() { }

                public static DuplicatePromise0 GetOrCreate(int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (DuplicatePromise0) _pool.Pop() : new DuplicatePromise0();
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute(IValueContainer valueContainer)
                {
                    HandleSelf(valueContainer);
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DuplicatePromise<T> : PoolablePromise<T, DuplicatePromise<T>>
            {
                private DuplicatePromise() { }

                public static DuplicatePromise<T> GetOrCreate(int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (DuplicatePromise<T>) _pool.Pop() : new DuplicatePromise<T>();
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute(IValueContainer valueContainer)
                {
                    HandleSelf(valueContainer);
                }
            }

            #region Resolve Promises
            // Individual types for more common .Then(onResolved) calls to be more efficient.
            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    var callback = _onResolved;
                    _onResolved = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        callback.Invoke();
                        ResolveInternal(ResolveContainerVoid.GetOrCreate());
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    var callback = _onResolved;
                    _onResolved = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                        callback.Invoke(arg);
                        ResolveInternal(ResolveContainerVoid.GetOrCreate());
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    var callback = _onResolved;
                    _onResolved = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TResult result = callback.Invoke();
                        ResolveInternal(ResolveContainer<TResult>.GetOrCreate(result));
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    var callback = _onResolved;
                    _onResolved = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                        TResult result = callback.Invoke(arg);
                        ResolveInternal(ResolveContainer<TResult>.GetOrCreate(result));
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var callback = _onResolved;
                    _onResolved = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke());
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var callback = _onResolved;
                    _onResolved = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                        WaitFor(callback.Invoke(arg));
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var callback = _onResolved;
                    _onResolved = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke());
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var callback = _onResolved;
                    _onResolved = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                        WaitFor(callback.Invoke(arg));
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }


            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        callback.Invoke(value);
                        ResolveInternal(ResolveContainerVoid.GetOrCreate());
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                        callback.Invoke(value, arg);
                        ResolveInternal(ResolveContainerVoid.GetOrCreate());
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TResult result = callback.Invoke(value);
                        ResolveInternal(ResolveContainer<TResult>.GetOrCreate(result));
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                        TResult result = callback.Invoke(value, arg);
                        ResolveInternal(ResolveContainer<TResult>.GetOrCreate(result));
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke(value));
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                        WaitFor(callback.Invoke(value, arg));
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;

                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke(value));
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (valueContainer.GetState() == State.Resolved)
                    {
                        _invokingResolved = true;
                        TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                        WaitFor(callback.Invoke(value, arg));
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }
            #endregion

            #region Resolve or Reject Promises
            // IDelegate to reduce the amount of classes I would have to write to handle catches (Composition Over Inheritance).
            // I'm less concerned about performance for catches since exceptions are expensive anyway, and they are expected to be used less often than .Then(onResolved).
            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    var resolveCallback = _onResolved;
                    _onResolved = null;
                    var rejectCallback = _onRejected;
                    _onRejected = null;
                    State state = valueContainer.GetState();
                    if (state == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        _invokingResolved = true;
                        resolveCallback.DisposeAndInvoke(valueContainer, this);
                    }
                    else if (state == State.Rejected)
                    {
                        resolveCallback.Dispose();
                        _invokingRejected = true;
                        rejectCallback.DisposeAndInvoke(valueContainer, this);
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    var resolveCallback = _onResolved;
                    _onResolved = null;
                    var rejectCallback = _onRejected;
                    _onRejected = null;
                    State state = valueContainer.GetState();
                    if (state == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        _invokingResolved = true;
                        resolveCallback.DisposeAndInvoke(valueContainer, this);
                    }
                    else if (state == State.Rejected)
                    {
                        resolveCallback.Dispose();
                        _invokingRejected = true;
                        rejectCallback.DisposeAndInvoke(valueContainer, this);
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var resolveCallback = _onResolved;
                    _onResolved = null;
                    var rejectCallback = _onRejected;
                    _onRejected = null;
                    State state = valueContainer.GetState();
                    if (state == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        _invokingResolved = true;
                        resolveCallback.DisposeAndInvoke(valueContainer, this);
                    }
                    else if (state == State.Rejected)
                    {
                        resolveCallback.Dispose();
                        _invokingRejected = true;
#if PROMISE_PROGRESS
                        _suspended = true;
#endif
                        rejectCallback.DisposeAndInvoke(valueContainer, this);
                    }
                    else
                    {
#if PROMISE_PROGRESS
                        _suspended = true;
#endif
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var resolveCallback = _onResolved;
                    _onResolved = null;
                    var rejectCallback = _onRejected;
                    _onRejected = null;
                    State state = valueContainer.GetState();
                    if (state == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        _invokingResolved = true;
                        resolveCallback.DisposeAndInvoke(valueContainer, this);
                    }
                    else if (state == State.Rejected)
                    {
                        resolveCallback.Dispose();
                        _invokingRejected = true;
#if PROMISE_PROGRESS
                        _suspended = true;
#endif
                        rejectCallback.DisposeAndInvoke(valueContainer, this);
                    }
                    else
                    {
#if PROMISE_PROGRESS
                        _suspended = true;
#endif
                        RejectOrCancelInternal(valueContainer);
                    }
                }
            }
            #endregion

            #region Continue Promises
            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    var callback = _onContinue;
                    _onContinue = null;
                    _invokingResolved = true;
                    callback.DisposeAndInvoke(valueContainer);
                    ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class PromiseContinue<TResult> : PoolablePromise<TResult, PromiseContinue<TResult>>
            {
                private IDelegateContinue<TResult> _onContinue;

                private PromiseContinue() { }

                public static PromiseContinue<TResult> GetOrCreate(IDelegateContinue<TResult> onContinue, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseContinue<TResult>) _pool.Pop() : new PromiseContinue<TResult>();
                    promise._onContinue = onContinue;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Execute(IValueContainer valueContainer)
                {
                    var callback = _onContinue;
                    _onContinue = null;
                    _invokingResolved = true;
                    TResult result = callback.DisposeAndInvoke(valueContainer);
                    ResolveInternal(ResolveContainer<TResult>.GetOrCreate(result));
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    if (_onContinue == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var callback = _onContinue;
                    _onContinue = null;
                    _invokingResolved = true;
                    Promise result = callback.DisposeAndInvoke(valueContainer);
                    WaitFor(result);
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
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

                protected override void Execute(IValueContainer valueContainer)
                {
                    if (_onContinue == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var callback = _onContinue;
                    _onContinue = null;
                    _invokingResolved = true;
                    Promise<TPromise> result = callback.DisposeAndInvoke(valueContainer);
                    WaitFor(result);
                }
            }
            #endregion

            [System.Diagnostics.DebuggerNonUserCode]
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

                void ITreeHandleable.MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
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
                            if (Config.ObjectPooling != PoolType.None)
                            {
                                _pool.Push(this);
                            }
                        }
                    }
                }

                void ITreeHandleable.Handle() { throw new System.InvalidOperationException(); }
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