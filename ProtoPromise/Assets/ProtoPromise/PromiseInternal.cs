#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
using System;
using System.Collections.Generic;

namespace Proto.Promises
{
    partial class Promise : Promise.Internal.ITreeHandleable, Promise.Internal.IStacktraceable
    {
        Internal.ITreeHandleable ILinked<Internal.ITreeHandleable>.Next { get; set; }

        private ValueLinkedQueue<Internal.ITreeHandleable> _nextBranches;
        protected Internal.IValueContainer _rejectedOrCanceledValue;
        private uint _retainCounter;
        protected State _state;
        private bool _wasWaitedOn; // Tells the handler that another promise waited on this promise (either by .Then/.Catch from this promise, or by returning this promise in another promise's .Then/.Catch)
        private bool _handling; // Is not already being handled in HandleComplete.
        protected bool _dontPool;

        protected virtual U GetValue<U>()
        {
            throw new InvalidOperationException();
        }

        protected virtual void Reset(int skipFrames)
        {
            _state = State.Pending;
            _dontPool = Config.ObjectPooling != PoolType.All;
            _wasWaitedOn = false;
            SetNotDisposed(ref _rejectedOrCanceledValue);
            SetCreatedStackTrace(this, skipFrames + 1);
        }

        protected virtual void Dispose()
        {
            if (_rejectedOrCanceledValue != null)
            {
                _rejectedOrCanceledValue.Release();
            }
            SetDisposed(ref _rejectedOrCanceledValue);
        }

        protected virtual Promise GetDuplicate()
        {
            return Internal.DuplicatePromise0.GetOrCreate(2);
        }

        protected void ResolveInternal()
        {
            _state = State.Resolved;
            ResolveProgressListeners();
            AddToHandleQueue(this);
        }

        protected void Reject(int skipFrames)
        {
            Internal.UnhandledExceptionInternal rejectValue = Internal.UnhandledExceptionVoid.GetOrCreate();
            SetRejectStackTrace(rejectValue, skipFrames + 1);
            RejectWithStateCheck(rejectValue);
        }

        protected void Reject<TReject>(TReject reason, int skipFrames)
        {
            Internal.UnhandledExceptionInternal rejectValue;
            // Is TReject an exception (including if it's null)?
            if (typeof(Exception).IsAssignableFrom(typeof(TReject)))
            {
                // Behave the same way .Net behaves if you throw null.
                rejectValue = Internal.UnhandledExceptionException.GetOrCreate(reason as Exception ?? new NullReferenceException());
            }
            else
            {
                rejectValue = Internal.UnhandledException<TReject>.GetOrCreate(reason);
            }
            SetRejectStackTrace(rejectValue, skipFrames + 1);
            RejectWithStateCheck(rejectValue);
        }

        protected void RejectWithStateCheck(Internal.UnhandledExceptionInternal rejectValue)
        {
            if (_state != State.Pending | _rejectedOrCanceledValue != null)
            {
                AddRejectionToUnhandledStack(rejectValue);
            }
            else
            {
                RejectDirect(rejectValue);
            }
        }

        protected void RejectDirect(Internal.IValueContainer rejectValue)
        {
            _rejectedOrCanceledValue = rejectValue;
            _rejectedOrCanceledValue.Retain();
            AddToHandleQueue(this);
        }

        protected void RejectInternal(Internal.IValueContainer rejectValue)
        {
            _state = State.Rejected;
            RejectProgressListeners();
            RejectDirect(rejectValue);
        }

        protected void HookupNewPromise(Promise newPromise)
        {
            SetDepthAndPrevious(newPromise);
            AddWaiter(newPromise);
        }

        private void HandleSelf()
        {
            if (_rejectedOrCanceledValue == null)
            {
                _state = State.Resolved;
                ResolveProgressListeners();
            }
            else
            {
                _state = State.Rejected;
                RejectProgressListeners();
            }
        }

        protected abstract void Handle(Promise feed);

        void Internal.ITreeHandleable.Cancel()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _state = State.Canceled;
#pragma warning restore CS0618 // Type or member is obsolete
            OnCancel();
            // Place in the handle queue so it can be repooled.
            AddToHandleQueue(this);
        }

        protected virtual void AssignCancelValue(Internal.IValueContainer cancelValue)
        {
            // If _rejectedOrCanceledValue is not null, it means this was already canceled with another value.
            if (_rejectedOrCanceledValue != null)
            {
                _rejectedOrCanceledValue = cancelValue;
                _rejectedOrCanceledValue.Retain();
            }
        }

        void Internal.ITreeHandleable.AssignCancelValue(Internal.IValueContainer cancelValue)
        {
            AssignCancelValue(cancelValue);
        }

        void Internal.ITreeHandleable.OnSubscribeToCanceled(Internal.IValueContainer cancelValue)
        {
            _rejectedOrCanceledValue = cancelValue;
            _rejectedOrCanceledValue.Retain();
            CancelProgressListeners();
        }

        private void Repool()
        {
            if (_retainCounter == 0)
            {
                if (!_wasWaitedOn & _state == State.Rejected)
                {
                    // Rejection wasn't caught.
                    // TODO: Check this in Destructor.
                    _wasWaitedOn = true;
                    AddRejectionToUnhandledStack((Internal.UnhandledExceptionInternal) _rejectedOrCanceledValue);
                }
                Dispose();
            }
        }

        private static ValueLinkedStack<Internal.UnhandledExceptionInternal> _unhandledExceptions;

        protected static void AddRejectionToUnhandledStack(Internal.UnhandledExceptionInternal unhandledValue)
        {
            // Prevent the same object from being added twice.
            if (unhandledValue.handled)
            {
                return;
            }
            unhandledValue.handled = true;
            // Make sure it's not re-used before it's thrown.
            unhandledValue.Retain();
            _unhandledExceptions.Push(unhandledValue);
        }

        private static void ThrowUnhandledRejections()
        {
            if (_unhandledExceptions.IsEmpty)
            {
                return;
            }

            var unhandledExceptions = _unhandledExceptions;
            _unhandledExceptions.Clear();
            // Reset handled flag.
            foreach (Internal.UnhandledExceptionInternal unhandled in unhandledExceptions)
            {
                unhandled.handled = false;
                // Allow to re-use.
                unhandled.Release();
            }
            //throw new AggregateException(unhandledExceptions);
        }

        // Handle promises in a breadth-first manner.
        private static ValueLinkedQueue<Internal.ITreeHandleable> _handleQueue;
        private static bool _runningHandles;

        protected static void AddToHandleQueue(Promise promise)
        {
            promise._handling = true;
            _handleQueue.Enqueue(promise);
        }

        // This allows infinite .Then/.Catch callbacks, since it avoids recursion.
        protected static void HandleComplete()
        {
            if (_runningHandles)
            {
                // HandleComplete is running higher in the program stack, so just return.
                return;
            }

            _runningHandles = true;

            // Cancels are high priority, make sure those delegates are invoked before anything else.
            HandleCanceled();

            while (_handleQueue.IsNotEmpty)
            {
                Promise current = (Promise) _handleQueue.DequeueRisky();
                current.HandleBranches();
                current._handling = false;

                // In case a promise was canceled from a callback.
                HandleCanceled();
            }

            _handleQueue.ClearLast();
            _runningHandles = false;
        }

        protected virtual void HandleBranches()
        {
            // If we want, here we can switch to a handle approach that is a hybrid of breadth-first and depth-first,
            // whereby we add to feed._nextBranches from Handle instead of directly to the handle queue, then add this _nextBranches to the front of the handle queue instead of the back.
            // That would cost an extra 2 branches vs the current breadth-first implementation.
            // Doing so would make it so that a single promise's entire tree gets handled before any other promise trees. Currently, multiple trees being handled will "bounce" between their .Then delegates.
            while (_nextBranches.IsNotEmpty)
            {
                _nextBranches.DequeueRisky().Handle(this);
            }
            _nextBranches.ClearLast();
            Repool();
        }
    }

    partial class Promise<T>
    {
        protected T _value;
        protected override U GetValue<U>()
        {
            return (this as Promise<U>)._value;
        }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
        private
#endif
        protected Promise() { }

        protected override Promise GetDuplicate()
        {
            return Promise.Internal.DuplicatePromise<T>.GetOrCreate(2);
        }

        protected void ResolveInternal(T value)
        {
            _value = value;
            ResolveInternal();
        }

        protected override void Dispose()
        {
            base.Dispose();
            _value = default(T);
        }
    }

    partial class Promise
    {
        protected static partial class Internal
        {
            internal static Action OnClearPool;

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
                    if (!_dontPool & Config.ObjectPooling == PoolType.All)
                    {
                        _pool.Push(this);
                    }
                }
            }

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
                    if (!_dontPool & Config.ObjectPooling == PoolType.All)
                    {
                        _pool.Push(this);
                    }
                }
            }

            public sealed class DeferredPromise0 : PromiseWaitDeferred<DeferredPromise0>
            {
                private DeferredPromise0() { }

                public static DeferredPromise0 GetOrCreate(int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (DeferredPromise0) _pool.Pop() : new DeferredPromise0();
                    promise.Reset(skipFrames + 1);
                    promise.ResetDepth();
                    return promise;
                }

                protected override void HandleBranches()
                {
                    HandleSelf();
                    base.HandleBranches();
                }

                protected override void Handle(Promise feed) { throw new InvalidOperationException(); }
            }

            public sealed class DeferredPromise<T> : PromiseWaitDeferred<T, DeferredPromise<T>>
            {
                private DeferredPromise() { }

                public static DeferredPromise<T> GetOrCreate(int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (DeferredPromise<T>) _pool.Pop() : new DeferredPromise<T>();
                    promise.Reset(skipFrames + 1);
                    promise.ResetDepth();
                    return promise;
                }

                protected override void HandleBranches()
                {
                    HandleSelf();
                    base.HandleBranches();
                }

                protected override void Handle(Promise feed) { throw new InvalidOperationException(); }
            }

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

                public new void Resolve()
                {
                    AddToHandleQueue(this);
                }

                protected override void HandleBranches()
                {
                    HandleSelf();
                    base.HandleBranches();
                }

                protected override void Handle(Promise feed)
                {
                    if (feed._state == State.Resolved)
                    {
                        ResolveInternal();
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }
            }

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

                public new void Resolve(T value)
                {
                    _value = value;
                    AddToHandleQueue(this);
                }

                protected override void HandleBranches()
                {
                    HandleSelf();
                    base.HandleBranches();
                }

                protected override void Handle(Promise feed)
                {
                    if (feed._state == State.Resolved)
                    {
                        ResolveInternal(feed.GetValue<T>());
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }
            }

            public sealed class DuplicatePromise0 : PoolablePromise<DuplicatePromise0>
            {
                private DuplicatePromise0() { }

                public static DuplicatePromise0 GetOrCreate(int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (DuplicatePromise0) _pool.Pop() : new DuplicatePromise0();
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (feed._state == State.Resolved)
                    {
                        ResolveInternal();
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }
            }

            public sealed class DuplicatePromise<T> : PoolablePromise<T, DuplicatePromise<T>>
            {
                private DuplicatePromise() { }

                public static DuplicatePromise<T> GetOrCreate(int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (DuplicatePromise<T>) _pool.Pop() : new DuplicatePromise<T>();
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (feed._state == State.Resolved)
                    {
                        ResolveInternal(feed.GetValue<T>());
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }
            }

#region Resolve Promises
            // Individual types for more common .Then(onResolved) calls to be more efficient.

            public sealed class PromiseVoidResolve0 : PoolablePromise<PromiseVoidResolve0>
            {
                private Action resolveHandler;

                private PromiseVoidResolve0() { }

                public static PromiseVoidResolve0 GetOrCreate(Action resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolve0)_pool.Pop() : new PromiseVoidResolve0();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        callback.Invoke();
                        ResolveWithStateCheck();
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    resolveHandler = null;
                }
            }

            public sealed class PromiseArgResolve<TArg> : PoolablePromise<PromiseArgResolve<TArg>>
            {
                private Action<TArg> resolveHandler;

                private PromiseArgResolve() { }

                public static PromiseArgResolve<TArg> GetOrCreate(Action<TArg> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolve<TArg>)_pool.Pop() : new PromiseArgResolve<TArg>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        callback.Invoke(feed.GetValue<TArg>());
                        ResolveWithStateCheck();
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    resolveHandler = null;
                }
            }

            public sealed class PromiseVoidResolve<TResult> : PoolablePromise<TResult, PromiseVoidResolve<TResult>>
            {
                private Func<TResult> resolveHandler;

                private PromiseVoidResolve() { }

                public static PromiseVoidResolve<TResult> GetOrCreate(Func<TResult> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolve<TResult>)_pool.Pop() : new PromiseVoidResolve<TResult>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        ResolveWithStateCheck(callback.Invoke());
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    resolveHandler = null;
                }
            }

            public sealed class PromiseArgResolve<TArg, TResult> : PoolablePromise<TResult, PromiseArgResolve<TArg, TResult>>
            {
                private Func<TArg, TResult> resolveHandler;

                private PromiseArgResolve() { }

                public static PromiseArgResolve<TArg, TResult> GetOrCreate(Func<TArg, TResult> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolve<TArg, TResult>)_pool.Pop() : new PromiseArgResolve<TArg, TResult>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        ResolveWithStateCheck(callback.Invoke(feed.GetValue<TArg>()));
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    resolveHandler = null;
                }
            }

            public sealed class PromiseVoidResolvePromise0 : PromiseWaitPromise<PromiseVoidResolvePromise0>
            {
                private Func<Promise> resolveHandler;

                private PromiseVoidResolvePromise0() { }

                public static PromiseVoidResolvePromise0 GetOrCreate(Func<Promise> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolvePromise0)_pool.Pop() : new PromiseVoidResolvePromise0();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (resolveHandler == null)
                    {
                        // The returned promise is handling this.
                        if (feed._state == State.Resolved)
                        {
                            ResolveInternal();
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                        }
                        return;
                    }

                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        WaitFor(callback.Invoke());
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    resolveHandler = null;
                }
            }

            public sealed class PromiseArgResolvePromise<TArg> : PromiseWaitPromise<PromiseArgResolvePromise<TArg>>
            {
                private Func<TArg, Promise> resolveHandler;

                private PromiseArgResolvePromise() { }

                public static PromiseArgResolvePromise<TArg> GetOrCreate(Func<TArg, Promise> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolvePromise<TArg>)_pool.Pop() : new PromiseArgResolvePromise<TArg>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (resolveHandler == null)
                    {
                        // The returned promise is handling this.
                        if (feed._state == State.Resolved)
                        {
                            ResolveInternal();
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                        }
                        return;
                    }

                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        WaitFor(callback.Invoke(feed.GetValue<TArg>()));
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    resolveHandler = null;
                }
            }

            public sealed class PromiseVoidResolvePromise<TPromise> : PromiseWaitPromise<TPromise, PromiseVoidResolvePromise<TPromise>>
            {
                private Func<Promise<TPromise>> resolveHandler;

                private PromiseVoidResolvePromise() { }

                public static PromiseVoidResolvePromise<TPromise> GetOrCreate(Func<Promise<TPromise>> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolvePromise<TPromise>)_pool.Pop() : new PromiseVoidResolvePromise<TPromise>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (resolveHandler == null)
                    {
                        // The returned promise is handling this.
                        if (feed._state == State.Resolved)
                        {
                            ResolveInternal(feed.GetValue<TPromise>());
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                        }
                        return;
                    }

                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        WaitFor(callback.Invoke());
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    resolveHandler = null;
                }
            }

            public sealed class PromiseArgResolvePromise<TArg, TPromise> : PromiseWaitPromise<TPromise, PromiseArgResolvePromise<TArg, TPromise>>
            {
                private Func<TArg, Promise<TPromise>> resolveHandler;

                private PromiseArgResolvePromise() { }

                public static PromiseArgResolvePromise<TArg, TPromise> GetOrCreate(Func<TArg, Promise<TPromise>> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolvePromise<TArg, TPromise>)_pool.Pop() : new PromiseArgResolvePromise<TArg, TPromise>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (resolveHandler == null)
                    {
                        // The returned promise is handling this.
                        if (feed._state == State.Resolved)
                        {
                            ResolveInternal(feed.GetValue<TPromise>());
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                        }
                        return;
                    }

                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        WaitFor(callback.Invoke(feed.GetValue<TArg>()));
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    resolveHandler = null;
                }
            }

            public sealed class PromiseVoidResolveDeferred0 : PromiseWaitDeferred<PromiseVoidResolveDeferred0>
            {
                private Func<Action<Deferred>> resolveHandler;

                private PromiseVoidResolveDeferred0() { }

                public static PromiseVoidResolveDeferred0 GetOrCreate(Func<Action<Deferred>> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolveDeferred0)_pool.Pop() : new PromiseVoidResolveDeferred0();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void HandleBranches()
                {
                    HandleSelf();
                    base.HandleBranches();
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        try
                        {
                            var deferredAction = callback.Invoke();
                            ValidateReturn(deferredAction);
                            deferredAction.Invoke(_deferredInternal);
                        }
                        catch (Exception e)
                        {
                            _deferredInternal.RejectWithPromiseStacktrace(e);
                        }
                    }
                    else
                    {
                        // Deferred is never used, so just release.
                        Release();
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }

                protected override void OnCancel()
                {
                    resolveHandler = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseArgResolveDeferred<TArg> : PromiseWaitDeferred<PromiseArgResolveDeferred<TArg>>
            {
                private Func<TArg, Action<Deferred>> resolveHandler;

                private PromiseArgResolveDeferred() { }

                public static PromiseArgResolveDeferred<TArg> GetOrCreate(Func<TArg, Action<Deferred>> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolveDeferred<TArg>)_pool.Pop() : new PromiseArgResolveDeferred<TArg>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void HandleBranches()
                {
                    HandleSelf();
                    base.HandleBranches();
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        try
                        {
                            var deferredAction = callback.Invoke(feed.GetValue<TArg>());
                            ValidateReturn(deferredAction);
                            deferredAction.Invoke(_deferredInternal);
                        }
                        catch (Exception e)
                        {
                            _deferredInternal.RejectWithPromiseStacktrace(e);
                        }
                    }
                    else
                    {
                        // Deferred is never used, so just release.
                        Release();
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }

                protected override void OnCancel()
                {
                    resolveHandler = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseVoidResolveDeferred<TDeferred> : PromiseWaitDeferred<TDeferred, PromiseVoidResolveDeferred<TDeferred>>
            {
                private Func<Action<Deferred>> resolveHandler;

                private PromiseVoidResolveDeferred() { }

                public static PromiseVoidResolveDeferred<TDeferred> GetOrCreate(Func<Action<Deferred>> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolveDeferred<TDeferred>)_pool.Pop() : new PromiseVoidResolveDeferred<TDeferred>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void HandleBranches()
                {
                    HandleSelf();
                    base.HandleBranches();
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        try
                        {
                            var deferredAction = callback.Invoke();
                            ValidateReturn(deferredAction);
                            deferredAction.Invoke(_deferredInternal);
                        }
                        catch (Exception e)
                        {
                            _deferredInternal.RejectWithPromiseStacktrace(e);
                        }
                    }
                    else
                    {
                        // Deferred is never used, so just release.
                        Release();
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }

                protected override void OnCancel()
                {
                    resolveHandler = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseArgResolveDeferred<TArg, TDeferred> : PromiseWaitDeferred<TDeferred, PromiseArgResolveDeferred<TArg, TDeferred>>
            {
                private Func<TArg, Action<Deferred>> resolveHandler;

                private PromiseArgResolveDeferred() { }

                public static PromiseArgResolveDeferred<TArg, TDeferred> GetOrCreate(Func<TArg, Action<Deferred>> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolveDeferred<TArg, TDeferred>)_pool.Pop() : new PromiseArgResolveDeferred<TArg, TDeferred>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void HandleBranches()
                {
                    HandleSelf();
                    base.HandleBranches();
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        try
                        {
                            var deferredAction = callback.Invoke(feed.GetValue<TArg>());
                            ValidateReturn(deferredAction);
                            deferredAction.Invoke(_deferredInternal);
                        }
                        catch (Exception e)
                        {
                            _deferredInternal.RejectWithPromiseStacktrace(e);
                        }
                    }
                    else
                    {
                        // Deferred is never used, so just release.
                        Release();
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                }

                protected override void OnCancel()
                {
                    resolveHandler = null;
                    base.OnCancel();
                }
            }
#endregion

#region Reject Promises
            // Used IDelegate to reduce the amount of classes I would have to write to handle catches (Composition Over Inheritance).
            // I'm less concerned about performance for catches since exceptions are expensive anyway, and they are expected to be used less often than .Then(onResolved).
            public sealed class PromiseReject0 : PoolablePromise<PromiseReject0>
            {
                private IDelegate rejectHandler;

                private PromiseReject0() { }

                public static PromiseReject0 GetOrCreate(IDelegate rejectHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseReject0)_pool.Pop() : new PromiseReject0();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == State.Rejected)
                    {
                        _state = State.Rejected; // Set the state so a Cancel call won't do anything during invoke.
                        _handling = true; // Set handling flag so a .Then/.Catch during invoke won't add to handle queue.
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue))
                        {
                            ResolveWithStateCheck();
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                        }
                    }
                    else
                    {
                        callback.Dispose();
                        ResolveInternal();
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    rejectHandler.Dispose();
                    rejectHandler = null;
                }
            }

            public sealed class PromiseReject<T> : PoolablePromise<T, PromiseReject<T>>
            {
                private IDelegate<T> rejectHandler;

                private PromiseReject() { }

                public static PromiseReject<T> GetOrCreate(IDelegate<T> rejectHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseReject<T>)_pool.Pop() : new PromiseReject<T>();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == State.Rejected)
                    {
                        _state = State.Rejected; // Set the state so a Cancel call won't do anything during invoke.
                        _handling = true; // Set handling flag so a .Then/.Catch during invoke won't add to handle queue.
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out _value))
                        {
                            ResolveWithStateCheck();
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                        }
                    }
                    else
                    {
                        callback.Dispose();
                        ResolveInternal(feed.GetValue<T>());
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    rejectHandler.Dispose();
                    rejectHandler = null;
                }
            }

            public sealed class PromiseRejectPromise0 : PromiseWaitPromise<PromiseRejectPromise0>
            {
                private IDelegate<Promise> rejectHandler;

                private PromiseRejectPromise0() { }

                public static PromiseRejectPromise0 GetOrCreate(IDelegate<Promise> rejectHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseRejectPromise0)_pool.Pop() : new PromiseRejectPromise0();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (rejectHandler == null)
                    {
                        // The returned promise is handling this.
                        if (feed._state == State.Resolved)
                        {
                            ResolveInternal();
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                        }
                        return;
                    }

                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == State.Rejected)
                    {
                        Promise promise;
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out promise))
                        {
                            WaitFor(promise);
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                        }
                    }
                    else
                    {
                        callback.Dispose();
                        ResolveInternal();
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    if (rejectHandler != null)
                    {
                        rejectHandler.Dispose();
                        rejectHandler = null;
                    }
                }
            }

            public sealed class PromiseRejectPromise<TPromise> : PromiseWaitPromise<TPromise, PromiseRejectPromise<TPromise>>
            {
                private IDelegate<Promise<TPromise>> rejectHandler;

                private PromiseRejectPromise() { }

                public static PromiseRejectPromise<TPromise> GetOrCreate(IDelegate<Promise<TPromise>> rejectHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseRejectPromise<TPromise>)_pool.Pop() : new PromiseRejectPromise<TPromise>();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (rejectHandler == null)
                    {
                        // The returned promise is handling this.
                        if (feed._state == State.Resolved)
                        {
                            ResolveInternal(feed.GetValue<TPromise>());
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                        }
                        return;
                    }

                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == State.Rejected)
                    {
                        Promise<TPromise> promise;
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out promise))
                        {
                            WaitFor(promise);
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                        }
                    }
                    else
                    {
                        callback.Dispose();
                        ResolveInternal(feed.GetValue<TPromise>());
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    if (rejectHandler != null)
                    {
                        rejectHandler.Dispose();
                        rejectHandler = null;
                    }
                }
            }

            public sealed class PromiseRejectDeferred0 : PromiseWaitDeferred<PromiseRejectDeferred0>
            {
                private IDelegate<Action<Deferred>> rejectHandler;

                private PromiseRejectDeferred0() { }

                public static PromiseRejectDeferred0 GetOrCreate(IDelegate<Action<Deferred>> rejectHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseRejectDeferred0)_pool.Pop() : new PromiseRejectDeferred0();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void HandleBranches()
                {
                    HandleSelf();
                    base.HandleBranches();
                }

                protected override void Handle(Promise feed)
                {
                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == State.Rejected)
                    {
                        try
                        {
                            Action<Deferred> deferredDelegate;
                            if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out deferredDelegate))
                            {
                                ValidateReturn(deferredDelegate);
                                deferredDelegate.Invoke(_deferredInternal);
                            }
                            else
                            {
                                // Deferred is never used, so just release.
                                Release();
                                RejectInternal(feed._rejectedOrCanceledValue);
                            }
                        }
                        catch (Exception e)
                        {
                            _deferredInternal.RejectWithPromiseStacktrace(e);
                        }
                    }
                    else
                    {
                        // Deferred is never used, so just release.
                        Release();
                        callback.Dispose();
                        ResolveInternal();
                    }
                }

                protected override void OnCancel()
                {
                    if (rejectHandler != null)
                    {
                        rejectHandler.Dispose();
                        rejectHandler = null;
                    }
                    base.OnCancel();
                }
            }

            public sealed class PromiseRejectDeferred<TDeferred> : PromiseWaitDeferred<TDeferred, PromiseRejectDeferred<TDeferred>>
            {
                private IDelegate<Action<Deferred>> rejectHandler;

                private PromiseRejectDeferred() { }

                public static PromiseRejectDeferred<TDeferred> GetOrCreate(IDelegate<Action<Deferred>> rejectHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseRejectDeferred<TDeferred>)_pool.Pop() : new PromiseRejectDeferred<TDeferred>();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void HandleBranches()
                {
                    HandleSelf();
                    base.HandleBranches();
                }

                protected override void Handle(Promise feed)
                {
                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == State.Rejected)
                    {
                        try
                        {
                            Action<Deferred> deferredDelegate;
                            if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out deferredDelegate))
                            {
                                ValidateReturn(deferredDelegate);
                                deferredDelegate.Invoke(_deferredInternal);
                            }
                            else
                            {
                                // Deferred is never used, so just release.
                                Release();
                                RejectInternal(feed._rejectedOrCanceledValue);
                            }
                        }
                        catch (Exception e)
                        {
                            _deferredInternal.RejectWithPromiseStacktrace(e);
                        }
                    }
                    else
                    {
                        // Deferred is never used, so just release.
                        Release();
                        callback.Dispose();
                        ResolveInternal(feed.GetValue<TDeferred>());
                    }
                }

                protected override void OnCancel()
                {
                    if (rejectHandler != null)
                    {
                        rejectHandler.Dispose();
                        rejectHandler = null;
                    }
                    base.OnCancel();
                }
            }
#endregion

#region Resolve or Reject Promises
            public sealed class PromiseResolveReject0 : PoolablePromise<PromiseResolveReject0>
            {
                IDelegate onResolved, onRejected;

                private PromiseResolveReject0() { }

                public static PromiseResolveReject0 GetOrCreate(IDelegate onResolved, IDelegate onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveReject0)_pool.Pop() : new PromiseResolveReject0();
                    promise.onResolved = onResolved;
                    promise.onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var resolveCallback = onResolved;
                    onResolved = null;
                    var rejectCallback = onRejected;
                    onRejected = null;
                    _state = feed._state; // Set the state so a Cancel call won't do anything during invoke.
                    _handling = true; // Set handling flag so a .Then/.Catch during invoke won't add to handle queue.
                    if (feed._state == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        resolveCallback.DisposeAndInvoke(feed);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue))
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                            return;
                        }
                    }
                    ResolveWithStateCheck();
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    onResolved.Dispose();
                    onResolved = null;
                    onRejected.Dispose();
                    onRejected = null;
                }
            }

            public sealed class PromiseResolveReject<T> : PoolablePromise<T, PromiseResolveReject<T>>
            {
                IDelegate<T> onResolved, onRejected;

                private PromiseResolveReject() { }

                public static PromiseResolveReject<T> GetOrCreate(IDelegate<T> onResolved, IDelegate<T> onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveReject<T>)_pool.Pop() : new PromiseResolveReject<T>();
                    promise.onResolved = onResolved;
                    promise.onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var resolveCallback = onResolved;
                    onResolved = null;
                    var rejectCallback = onRejected;
                    onRejected = null;
                    _state = feed._state; // Set the state so a Cancel call won't do anything during invoke.
                    _handling = true; // Set handling flag so a .Then/.Catch during invoke won't add to handle queue.
                    T val;
                    if (feed._state == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        val = resolveCallback.DisposeAndInvoke(feed);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out val))
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                            return;
                        }
                    }
                    ResolveWithStateCheck(val);
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    onResolved.Dispose();
                    onResolved = null;
                    onRejected.Dispose();
                    onRejected = null;
                }
            }

            public sealed class PromiseResolveRejectPromise0 : PromiseWaitPromise<PromiseResolveRejectPromise0>
            {
                IDelegate<Promise> onResolved, onRejected;

                private PromiseResolveRejectPromise0() { }

                public static PromiseResolveRejectPromise0 GetOrCreate(IDelegate<Promise> onResolved, IDelegate<Promise> onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectPromise0)_pool.Pop() : new PromiseResolveRejectPromise0();
                    promise.onResolved = onResolved;
                    promise.onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (onResolved == null)
                    {
                        // The returned promise is handling this.
                        if (feed._state == State.Resolved)
                        {
                            ResolveInternal();
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                        }
                    }
                    else
                    {
                        var resolveCallback = onResolved;
                        onResolved = null;
                        var rejectCallback = onRejected;
                        onRejected = null;
                        Promise promise;
                        if (feed._state == State.Resolved)
                        {
                            rejectCallback.Dispose();
                            promise = resolveCallback.DisposeAndInvoke(feed);
                        }
                        else
                        {
                            resolveCallback.Dispose();
                            if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out promise))
                            {
                                RejectInternal(feed._rejectedOrCanceledValue);
                                return;
                            }
                        }
                        WaitFor(promise);
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    onResolved.Dispose();
                    onResolved = null;
                    onRejected.Dispose();
                    onRejected = null;
                }
            }

            public sealed class PromiseResolveRejectPromise<TPromise> : PromiseWaitPromise<TPromise, PromiseResolveRejectPromise<TPromise>>
            {
                IDelegate<Promise<TPromise>> onResolved, onRejected;

                private PromiseResolveRejectPromise() { }

                public static PromiseResolveRejectPromise<TPromise> GetOrCreate(IDelegate<Promise<TPromise>> onResolved, IDelegate<Promise<TPromise>> onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectPromise<TPromise>)_pool.Pop() : new PromiseResolveRejectPromise<TPromise>();
                    promise.onResolved = onResolved;
                    promise.onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (onResolved == null)
                    {
                        // The returned promise is handling this.
                        if (feed._state == State.Resolved)
                        {
                            ResolveInternal(feed.GetValue<TPromise>());
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                        }
                    }
                    else
                    {
                        var resolveCallback = onResolved;
                        onResolved = null;
                        var rejectCallback = onRejected;
                        onRejected = null;
                        Promise<TPromise> promise;
                        if (feed._state == State.Resolved)
                        {
                            rejectCallback.Dispose();
                            promise = resolveCallback.DisposeAndInvoke(feed);
                        }
                        else
                        {
                            resolveCallback.Dispose();
                            if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out promise))
                            {
                                RejectInternal(feed._rejectedOrCanceledValue);
                                return;
                            }
                        }
                        WaitFor(promise);
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    onResolved.Dispose();
                    onResolved = null;
                    onRejected.Dispose();
                    onRejected = null;
                }
            }

            public sealed class PromiseResolveRejectDeferred : PromiseWaitDeferred<PromiseResolveRejectDeferred>
            {
                IDelegate<Action<Deferred>> onResolved, onRejected;

                private PromiseResolveRejectDeferred() { }

                public static PromiseResolveRejectDeferred GetOrCreate(IDelegate<Action<Deferred>> onResolved, IDelegate<Action<Deferred>> onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectDeferred)_pool.Pop() : new PromiseResolveRejectDeferred();
                    promise.onResolved = onResolved;
                    promise.onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void HandleBranches()
                {
                    HandleSelf();
                    base.HandleBranches();
                }

                protected override void Handle(Promise feed)
                {
                    var resolveCallback = onResolved;
                    onResolved = null;
                    var rejectCallback = onRejected;
                    onRejected = null;
                    try
                    {
                        Action<Deferred> deferredDelegate;
                        if (feed._state == State.Resolved)
                        {
                            rejectCallback.Dispose();
                            deferredDelegate = resolveCallback.DisposeAndInvoke(feed);
                        }
                        else
                        {
                            resolveCallback.Dispose();
                            if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out deferredDelegate))
                            {
                                // Deferred is never used, so just release.
                                Release();
                                RejectInternal(feed._rejectedOrCanceledValue);
                                return;
                            }
                        }
                        ValidateReturn(deferredDelegate);
                        deferredDelegate.Invoke(_deferredInternal);
                    }
                    catch (Exception e)
                    {
                        _deferredInternal.RejectWithPromiseStacktrace(e);
                    }
                }

                protected override void OnCancel()
                {
                    onResolved.Dispose();
                    onResolved = null;
                    onRejected.Dispose();
                    onRejected = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseResolveRejectDeferred<TDeferred> : PromiseWaitDeferred<TDeferred, PromiseResolveRejectDeferred<TDeferred>>
            {
                IDelegate<Action<Deferred>> onResolved, onRejected;

                private PromiseResolveRejectDeferred() { }

                public static PromiseResolveRejectDeferred<TDeferred> GetOrCreate(IDelegate<Action<Deferred>> onResolved, IDelegate<Action<Deferred>> onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectDeferred<TDeferred>)_pool.Pop() : new PromiseResolveRejectDeferred<TDeferred>();
                    promise.onResolved = onResolved;
                    promise.onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void HandleBranches()
                {
                    HandleSelf();
                    base.HandleBranches();
                }

                protected override void Handle(Promise feed)
                {
                    var resolveCallback = onResolved;
                    onResolved = null;
                    var rejectCallback = onRejected;
                    onRejected = null;
                    try
                    {
                        Action<Deferred> deferredDelegate;
                        if (feed._state == State.Resolved)
                        {
                            rejectCallback.Dispose();
                            deferredDelegate = resolveCallback.DisposeAndInvoke(feed);
                        }
                        else
                        {
                            resolveCallback.Dispose();
                            if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out deferredDelegate))
                            {
                                // Deferred is never used, so just release.
                                Release();
                                RejectInternal(feed._rejectedOrCanceledValue);
                                return;
                            }
                        }
                        ValidateReturn(deferredDelegate);
                        deferredDelegate.Invoke(_deferredInternal);
                    }
                    catch (Exception e)
                    {
                        _deferredInternal.RejectWithPromiseStacktrace(e);
                    }
                }

                protected override void OnCancel()
                {
                    onResolved.Dispose();
                    onResolved = null;
                    onRejected.Dispose();
                    onRejected = null;
                    base.OnCancel();
                }
            }
#endregion

#region Complete Promises
            public sealed class PromiseComplete0 : PoolablePromise<PromiseComplete0>
            {
                private Action onComplete;

                private PromiseComplete0() { }

                public static PromiseComplete0 GetOrCreate(Action onComplete, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseComplete0)_pool.Pop() : new PromiseComplete0();
                    promise.onComplete = onComplete;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = onComplete;
                    onComplete = null;
                    callback.Invoke();
                    ResolveWithStateCheck();
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    onComplete = null;
                }
            }

            public sealed class PromiseComplete<T> : PoolablePromise<T, PromiseComplete<T>>
            {
                private Func<T> onComplete;

                private PromiseComplete() { }

                public static PromiseComplete<T> GetOrCreate(Func<T> onComplete, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseComplete<T>)_pool.Pop() : new PromiseComplete<T>();
                    promise.onComplete = onComplete;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = onComplete;
                    onComplete = null;
                    _value = callback.Invoke();
                    ResolveWithStateCheck();
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    onComplete = null;
                }
            }

            public sealed class PromiseCompletePromise0 : PromiseWaitPromise<PromiseCompletePromise0>
            {
                private Func<Promise> onComplete;

                private PromiseCompletePromise0() { }

                public static PromiseCompletePromise0 GetOrCreate(Func<Promise> onComplete, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCompletePromise0)_pool.Pop() : new PromiseCompletePromise0();
                    promise.onComplete = onComplete;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (onComplete == null)
                    {
                        // The returned promise is handling this.
                        if (feed._state == State.Resolved)
                        {
                            ResolveInternal();
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                        }
                    }
                    else
                    {
                        var callback = onComplete;
                        onComplete = null;
                        WaitFor(callback.Invoke());
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    onComplete = null;
                }
            }

            public sealed class PromiseCompletePromise<T> : PromiseWaitPromise<T, PromiseCompletePromise<T>>
            {
                private Func<Promise<T>> onComplete;

                private PromiseCompletePromise() { }

                public static PromiseCompletePromise<T> GetOrCreate(Func<Promise<T>> onComplete, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCompletePromise<T>)_pool.Pop() : new PromiseCompletePromise<T>();
                    promise.onComplete = onComplete;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (onComplete == null)
                    {
                        // The returned promise is handling this.
                        if (feed._state == State.Resolved)
                        {
                            ResolveInternal(feed.GetValue<T>());
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValue);
                        }
                    }
                    else
                    {
                        var callback = onComplete;
                        onComplete = null;
                        WaitFor(callback.Invoke());
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    onComplete = null;
                }
            }

            public sealed class PromiseCompleteDeferred0 : PromiseWaitDeferred<PromiseCompleteDeferred0>
            {
                Func<Action<Deferred>> onComplete;

                private PromiseCompleteDeferred0() { }

                public static PromiseCompleteDeferred0 GetOrCreate(Func<Action<Deferred>> onComplete, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCompleteDeferred0)_pool.Pop() : new PromiseCompleteDeferred0();
                    promise.onComplete = onComplete;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void HandleBranches()
                {
                    HandleSelf();
                    base.HandleBranches();
                }

                protected override void Handle(Promise feed)
                {
                    var callback = onComplete;
                    onComplete = null;
                    try
                    {
                        var deferredDelegate = callback.Invoke();
                        ValidateReturn(deferredDelegate);
                        deferredDelegate.Invoke(_deferredInternal);
                    }
                    catch (Exception e)
                    {
                        _deferredInternal.RejectWithPromiseStacktrace(e);
                    }
                }

                protected override void OnCancel()
                {
                    onComplete = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseCompleteDeferred<T> : PromiseWaitDeferred<T, PromiseCompleteDeferred<T>>
            {
                Func<Action<Deferred>> onComplete;

                private PromiseCompleteDeferred() { }

                public static PromiseCompleteDeferred<T> GetOrCreate(Func<Action<Deferred>> onComplete, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCompleteDeferred<T>)_pool.Pop() : new PromiseCompleteDeferred<T>();
                    promise.onComplete = onComplete;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void HandleBranches()
                {
                    HandleSelf();
                    base.HandleBranches();
                }

                protected override void Handle(Promise feed)
                {
                    var callback = onComplete;
                    onComplete = null;
                    try
                    {
                        var deferredDelegate = callback.Invoke();
                        ValidateReturn(deferredDelegate);
                        deferredDelegate.Invoke(_deferredInternal);
                    }
                    catch (Exception e)
                    {
                        _deferredInternal.RejectWithPromiseStacktrace(e);
                    }
                }

                protected override void OnCancel()
                {
                    onComplete = null;
                    base.OnCancel();
                }
            }
#endregion

#region Delegate Wrappers
            public sealed partial class FinallyDelegate : ITreeHandleable
            {
                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

                private static ValueLinkedStack<ITreeHandleable> _pool;

                private Promise _owner;
                private Action _onFinally;

                private FinallyDelegate() { }

                static FinallyDelegate()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static FinallyDelegate GetOrCreate(Action onFinally, Promise owner, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (FinallyDelegate)_pool.Pop() : new FinallyDelegate();
                    del._onFinally = onFinally;
                    del._owner = owner;
                    SetCreatedStackTrace(del, skipFrames + 1);
                    return del;
                }

                private void InvokeAndCatchAndDispose()
                {
                    var callback = _onFinally;
                    Dispose();
                    try
                    {
                        callback.Invoke();
                    }
                    catch (Exception e)
                    {
                        UnhandledExceptionException unhandledException = UnhandledExceptionException.GetOrCreate(e);
                        SetStackTraceFromCreated(this, unhandledException);
                        AddRejectionToUnhandledStack(unhandledException);
                    }
                }

                void Dispose()
                {
                    _onFinally = null;
                    _owner = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void ITreeHandleable.Handle(Promise feed)
                {
                    InvokeAndCatchAndDispose();
                }

                void ITreeHandleable.Cancel()
                {
                    InvokeAndCatchAndDispose();
                }

                void ITreeHandleable.AssignCancelValue(IValueContainer cancelValue) { }
                void ITreeHandleable.OnSubscribeToCanceled(IValueContainer cancelValue) { }
            }

#pragma warning disable RECS0001 // Class is declared partial but has only one part
            public abstract partial class PotentialCancelation : ITreeHandleable, IPotentialCancelation
#pragma warning restore RECS0001 // Class is declared partial but has only one part
            {
                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

                private ValueLinkedQueue<ITreeHandleable> _nextBranches;

                protected IValueContainer cancelValue;

                void ITreeHandleable.AssignCancelValue(IValueContainer cancelValue)
                {
                    this.cancelValue = cancelValue;
                }

                void ITreeHandleable.Handle(Promise feed)
                {
                    Dispose();
                }

                protected void HandleBranches(IValueContainer nextValue)
                {
                    if (_nextBranches.IsNotEmpty)
                    {
                        // Add safe for first item.
                        var next = _nextBranches.DequeueRisky();
                        next.AssignCancelValue(nextValue);
                        AddToCancelQueue(next);

                        // Add risky for remaining items.
                        while (_nextBranches.IsNotEmpty)
                        {
                            next = _nextBranches.DequeueRisky();
                            next.AssignCancelValue(nextValue);
                            AddToCancelQueueRisky(next);
                        }
                        _nextBranches.ClearLast();
                    }
                }

                protected virtual void Dispose()
                {
                    SetDisposed(ref cancelValue);
                }

#pragma warning disable RECS0146 // Member hides static member from outer class
                partial void ValidateOperation();
#pragma warning restore RECS0146 // Member hides static member from outer class

                public abstract void Cancel();

                IPotentialCancelation IPotentialCancelation.CatchCancelation(Action onCanceled)
                {
                    ValidateOperation();
                    ValidateArgument(onCanceled, "onCanceled");

                    var cancelation = CancelDelegate0.GetOrCreate(onCanceled, 1);
                    _nextBranches.Enqueue(cancelation);
                    return cancelation;
                }

                IPotentialCancelation IPotentialCancelation.CatchCancelation<TCancel>(Action<TCancel> onCanceled)
                {
                    ValidateOperation();
                    ValidateArgument(onCanceled, "onCanceled");

                    var cancelation = CancelDelegate<TCancel>.GetOrCreate(onCanceled, 1);
                    _nextBranches.Enqueue(cancelation);
                    return cancelation;
                }

                void ITreeHandleable.OnSubscribeToCanceled(IValueContainer cancelValue) { throw new InvalidOperationException(); }
            }

            public sealed partial class CancelDelegate0 : PotentialCancelation, IPotentialCancelation
            {
                private static ValueLinkedStack<ITreeHandleable> _pool;

                private Action _onCanceled;

                private CancelDelegate0() { }

                static CancelDelegate0()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static CancelDelegate0 GetOrCreate(Action onCanceled, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegate0)_pool.Pop() : new CancelDelegate0();
                    del._onCanceled = onCanceled;
                    SetNotDisposed(ref del.cancelValue);
                    SetCreatedStackTrace(del, skipFrames + 1);
                    return del;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _onCanceled = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                public override void Cancel()
                {
                    var callback = _onCanceled;
                    Dispose();
                    try
                    {
                        if (cancelValue != null)
                        {
                            callback.Invoke();
                        }
                    }
                    catch (Exception e)
                    {
                        UnhandledExceptionException unhandledException = UnhandledExceptionException.GetOrCreate(e);
                        SetStackTraceFromCreated(this, unhandledException);
                        AddRejectionToUnhandledStack(unhandledException);
                    }
                    HandleBranches(null);
                }
            }

            public sealed partial class CancelDelegate<T> : PotentialCancelation, IPotentialCancelation
            {
                private static ValueLinkedStack<ITreeHandleable> _pool;

                private Action<T> _onCanceled;

                private CancelDelegate() { }

                static CancelDelegate()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static CancelDelegate<T> GetOrCreate(Action<T> onCanceled, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegate<T>)_pool.Pop() : new CancelDelegate<T>();
                    del._onCanceled = onCanceled;
                    SetNotDisposed(ref del.cancelValue);
                    SetCreatedStackTrace(del, skipFrames + 1);
                    return del;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _onCanceled = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                public override void Cancel()
                {
                    var callback = _onCanceled;
                    var nextValue = cancelValue;
                    Dispose();
                    T arg;
                    if (cancelValue != null && cancelValue.TryGetValueAs(out arg))
                    {
                        try
                        {
                            callback.Invoke(arg);
                        }
                        catch (Exception e)
                        {
                            UnhandledExceptionException unhandledException = UnhandledExceptionException.GetOrCreate(e);
                            SetStackTraceFromCreated(this, unhandledException);
                            AddRejectionToUnhandledStack(unhandledException);
                        }
                        HandleBranches(nextValue);
                    }
                    else
                    {
                        HandleBranches(null);
                    }
                }
            }

            public sealed class DelegateVoidVoid0 : IDelegate, ILinked<DelegateVoidVoid0>
            {
                DelegateVoidVoid0 ILinked<DelegateVoidVoid0>.Next { get; set; }

                private Action _callback;

                private static ValueLinkedStack<DelegateVoidVoid0> _pool;

                public static DelegateVoidVoid0 GetOrCreate(Action callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidVoid0();
                    del._callback = callback;
                    return del;
                }

                static DelegateVoidVoid0()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private DelegateVoidVoid0() { }

                public void DisposeAndInvoke()
                {
                    var temp = _callback;
                    Dispose();
                    temp.Invoke();
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                public bool DisposeAndTryInvoke(IValueContainer valueContainer)
                {
                    DisposeAndInvoke();
                    return true;
                }

                public void DisposeAndInvoke(Promise feed)
                {
                    DisposeAndInvoke();
                }
            }

            public class DelegateVoidVoid<T> : IDelegate, ILinked<DelegateVoidVoid<T>>
            {
                DelegateVoidVoid<T> ILinked<DelegateVoidVoid<T>>.Next { get; set; }

                private Action _callback;

                protected static ValueLinkedStack<DelegateVoidVoid<T>> _pool;

                public static DelegateVoidVoid<T> GetOrCreate(Action callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidVoid<T>();
                    del._callback = callback;
                    return del;
                }

                static DelegateVoidVoid()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private DelegateVoidVoid() { }

                public void DisposeAndInvoke()
                {
                    var temp = _callback;
                    Dispose();
                    temp.Invoke();
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                public bool DisposeAndTryInvoke(IValueContainer valueContainer)
                {
                    if (valueContainer.ContainsType<T>())
                    {
                        DisposeAndInvoke();
                        return true;
                    }
                    Dispose();
                    return false;
                }

                public void DisposeAndInvoke(Promise feed)
                {
                    DisposeAndInvoke();
                }
            }

            public sealed class DelegateArgVoid<TArg> : IDelegate, ILinked<DelegateArgVoid<TArg>>
            {
                DelegateArgVoid<TArg> ILinked<DelegateArgVoid<TArg>>.Next { get; set; }

                private Action<TArg> _callback;

                private static ValueLinkedStack<DelegateArgVoid<TArg>> _pool;

                public static DelegateArgVoid<TArg> GetOrCreate(Action<TArg> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgVoid<TArg>();
                    del._callback = callback;
                    return del;
                }

                static DelegateArgVoid()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private DelegateArgVoid() { }

                public void DisposeAndInvoke(TArg arg)
                {
                    var temp = _callback;
                    Dispose();
                    temp.Invoke(arg);
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                public bool DisposeAndTryInvoke(IValueContainer valueContainer)
                {
                    TArg arg;
                    if (valueContainer.TryGetValueAs(out arg))
                    {
                        DisposeAndInvoke(arg);
                        return true;
                    }
                    Dispose();
                    return false;
                }

                public void DisposeAndInvoke(Promise feed)
                {
                    DisposeAndInvoke(feed.GetValue<TArg>());
                }
            }

            public sealed class DelegateVoidResult<TResult> : IDelegate<TResult>, ILinked<DelegateVoidResult<TResult>>
            {
                DelegateVoidResult<TResult> ILinked<DelegateVoidResult<TResult>>.Next { get; set; }

                private Func<TResult> _callback;

                private static ValueLinkedStack<DelegateVoidResult<TResult>> _pool;

                public static DelegateVoidResult<TResult> GetOrCreate(Func<TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidResult<TResult>();
                    del._callback = callback;
                    return del;
                }

                static DelegateVoidResult()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private DelegateVoidResult() { }

                public TResult DisposeAndInvoke()
                {
                    var temp = _callback;
                    Dispose();
                    return temp.Invoke();
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                public bool DisposeAndTryInvoke(IValueContainer valueContainer, out TResult result)
                {
                    result = DisposeAndInvoke();
                    return true;
                }

                public TResult DisposeAndInvoke(Promise feed)
                {
                    return DisposeAndInvoke();
                }
            }

            public sealed class DelegateVoidResult<T, TResult> : IDelegate<TResult>, ILinked<DelegateVoidResult<T, TResult>>
            {
                DelegateVoidResult<T, TResult> ILinked<DelegateVoidResult<T, TResult>>.Next { get; set; }

                private Func<TResult> _callback;

                private static ValueLinkedStack<DelegateVoidResult<T, TResult>> _pool;

                public static DelegateVoidResult<T, TResult> GetOrCreate(Func<TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidResult<T, TResult>();
                    del._callback = callback;
                    return del;
                }

                static DelegateVoidResult()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private DelegateVoidResult() { }

                public TResult DisposeAndInvoke()
                {
                    var temp = _callback;
                    Dispose();
                    return temp.Invoke();
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                public bool DisposeAndTryInvoke(IValueContainer valueContainer, out TResult result)
                {
                    if (valueContainer.ContainsType<T>())
                    {
                        result = DisposeAndInvoke();
                        return true;
                    }
                    Dispose();
                    result = default(TResult);
                    return false;
                }

                public TResult DisposeAndInvoke(Promise feed)
                {
                    return DisposeAndInvoke();
                }
            }

            public sealed class DelegateArgResult<TArg, TResult> : IDelegate<TResult>, ILinked<DelegateArgResult<TArg, TResult>>
            {
                DelegateArgResult<TArg, TResult> ILinked<DelegateArgResult<TArg, TResult>>.Next { get; set; }

                private Func<TArg, TResult> _callback;

                private static ValueLinkedStack<DelegateArgResult<TArg, TResult>> _pool;

                public static DelegateArgResult<TArg, TResult> GetOrCreate(Func<TArg, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgResult<TArg, TResult>();
                    del._callback = callback;
                    return del;
                }

                static DelegateArgResult()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private DelegateArgResult() { }

                public TResult DisposeAndInvoke(TArg arg)
                {
                    var temp = _callback;
                    Dispose();
                    return temp.Invoke(arg);
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                public bool DisposeAndTryInvoke(IValueContainer valueContainer, out TResult result)
                {
                    TArg arg;
                    if (valueContainer.TryGetValueAs(out arg))
                    {
                        result = DisposeAndInvoke(arg);
                        return true;
                    }
                    Dispose();
                    result = default(TResult);
                    return false;
                }

                public TResult DisposeAndInvoke(Promise feed)
                {
                    return DisposeAndInvoke(feed.GetValue<TArg>());
                }
            }

            //public sealed class Filter : IFilter, ILinked<Filter>
            //{
            //    Filter ILinked<Filter>.Next { get; set; }

            //    private Func<bool> _callback;

            //    private static ValueLinkedStack<Filter> pool;

            //    public static Filter GetOrCreate(Func<bool> callback)
            //    {
            //        var del = pool.IsNotEmpty ? pool.Pop() : new Filter();
            //        del._callback = callback;
            //        return del;
            //    }

            //    static Filter()
            //    {
            //        OnClearPool += () => pool.Clear();
            //    }

            //    private Filter() { }

            //    public bool RunThroughFilter(IValueContainer valueContainer)
            //    {
            //        try
            //        {
            //            var temp = _callback;
            //            _callback = null;
            //            return temp.Invoke();
            //        }
            //        catch (Exception e)
            //        {
            //            Logger.LogWarning("Caught an exception in a promise onRejectedFilter. Assuming filter returned false. Logging exception next...");
            //            Logger.LogException(e);
            //            return false;
            //        }
            //    }

            //    public void Dispose()
            //    {
            //        pool.Push(this);
            //    }
            //}

            //public sealed class Filter<TArg> : IFilter, ILinked<Filter<TArg>>
            //{
            //    Filter<TArg> ILinked<Filter<TArg>>.Next { get; set; }

            //    private Func<TArg, bool> _callback;

            //    private static ValueLinkedStack<Filter<TArg>> pool;

            //    public static Filter<TArg> GetOrCreate(Func<TArg, bool> callback)
            //    {
            //        var del = pool.IsNotEmpty ? pool.Pop() : new Filter<TArg>();
            //        del._callback = callback;
            //        return del;
            //    }

            //    static Filter()
            //    {
            //        OnClearPool += () => pool.Clear();
            //    }

            //    private Filter() { }

            //    public bool RunThroughFilter(IValueContainer valueContainer)
            //    {
            //        TArg arg;
            //        if (!valueContainer.TryGetValueAs(out arg))
            //        {
            //            return false;
            //        }

            //        try
            //        {
            //            var temp = _callback;
            //            _callback = null;
            //            return temp.Invoke(arg);
            //        }
            //        catch (Exception e)
            //        {
            //            Logger.LogWarning("Caught an exception in a promise onRejectedFilter. Assuming filter returned false. Logging exception next...");
            //            Logger.LogException(e);
            //            return false;
            //        }
            //    }

            //    public void Dispose()
            //    {
            //        pool.Push(this);
            //    }
            //}
#endregion

            public partial interface ITreeHandleable : ILinked<ITreeHandleable>
            {
                void Handle(Promise feed);
                void Cancel();
                void OnSubscribeToCanceled(IValueContainer cancelValue);
                void AssignCancelValue(IValueContainer cancelValue);
            }

            public interface IValueContainer
            {
                bool TryGetValueAs<U>(out U value);
                bool ContainsType<U>();
                void Retain();
                void Release();
            }

            public interface IDelegate
            {
                bool DisposeAndTryInvoke(IValueContainer valueContainer);
                void DisposeAndInvoke(Promise feed);
                void Dispose();
            }

            public interface IDelegate<TResult>
            {
                bool DisposeAndTryInvoke(IValueContainer valueContainer, out TResult result);
                TResult DisposeAndInvoke(Promise feed);
                void Dispose();
            }

#region ValueWrappers
            public abstract class UnhandledExceptionInternal : UnhandledException, IValueContainer, ILinked<UnhandledExceptionInternal>
            {
                UnhandledExceptionInternal ILinked<UnhandledExceptionInternal>.Next { get; set; }

                public bool handled;

                protected UnhandledExceptionInternal() { }
                protected UnhandledExceptionInternal(Exception innerException) : base(innerException) { }

                public void SetStackTrace(string stackTrace)
                {
                    _stackTrace = stackTrace;
                }

                public virtual void Release() { }

                public virtual void Retain() { }

                public virtual bool TryGetValueAs<U>(out U value)
                {
                    value = default(U);
                    return false;
                }

                public abstract bool ContainsType<U>();
            }

            public sealed class UnhandledExceptionVoid : UnhandledExceptionInternal
            {
                private static ValueLinkedStack<UnhandledExceptionInternal> _pool;

                private uint retainCounter;

                static UnhandledExceptionVoid()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private UnhandledExceptionVoid() { }

                public static UnhandledExceptionVoid GetOrCreate()
                {
                    // Have to create new because stack trace can be different.
                    return _pool.IsNotEmpty ? (UnhandledExceptionVoid) _pool.Pop() : new UnhandledExceptionVoid();
                }

                public new UnhandledExceptionVoid SetStackTrace(string stackTrace)
                {
                    base.SetStackTrace(stackTrace);
                    return this;
                }

                public override object GetValue()
                {
                    return null;
                }

                public override string Message
                {
                    get
                    {
                        return "A non-value rejection was not handled.";
                    }
                }

                public override void Retain()
                {
                    ++retainCounter;
                }

                public override void Release()
                {
                    if (--retainCounter == 0 & Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                public override bool ContainsType<U>()
                {
                    return false;
                }
            }

            public sealed class UnhandledException<T> : UnhandledExceptionInternal
            {
                public T Value { get; private set; }

                private static ValueLinkedStack<UnhandledExceptionInternal> _pool;

                private uint retainCounter;

                static UnhandledException()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private UnhandledException() { }

                public static UnhandledException<T> GetOrCreate(T value)
                {
                    UnhandledException<T> ex = _pool.IsNotEmpty ? (UnhandledException<T>) _pool.Pop() : new UnhandledException<T>();
                    ex.Value = value;
                    return ex;
                }

                public override object GetValue()
                {
                    return Value;
                }

                public override bool TryGetValueAs<U>(out U value)
                {
                    // This avoids boxing value types.
#if CSHARP_7_OR_LATER
                    if (this is UnhandledException<U> casted)
#else
                    var casted = this as UnhandledException<U>;
                    if (casted != null)
#endif
                    {
                        value = casted.Value;
                        return true;
                    }
                    // Can it be up-casted or down-casted, null or not?
                    if (typeof(U).IsAssignableFrom(typeof(T)) || Value is U)
                    {
                        value = (U)(object)Value;
                        return true;
                    }
                    value = default(U);
                    return false;
                }

                public override string Message
                {
                    get
                    {
                        return "A rejected value was not handled: " + (Value.ToString() ?? "null");
                    }
                }

                public override void Retain()
                {
                    ++retainCounter;
                }

                public override void Release()
                {
                    if (--retainCounter == 0 & Config.ObjectPooling != PoolType.None)
                    {
                        Value = default(T);
                        _pool.Push(this);
                    }
                }

                public override bool ContainsType<U>()
                {
                    // Can it be up-casted or down-casted, null or not?
                    return typeof(U).IsAssignableFrom(typeof(T)) || Value is U;
                }
            }

            public sealed class UnhandledExceptionException : UnhandledExceptionInternal
            {
                private UnhandledExceptionException(Exception innerException) : base(innerException) { }

                // Don't care about re-using this exception for 2 reasons:
                // exceptions create garbage themselves, creating a little more with this one is negligible,
                // and it's too difficult to try to replicate the formatting for Unity to pick it up by using a cached local variable like in UnhandledException<T>, and prefer not to use reflection to assign innerException
                public static UnhandledExceptionException GetOrCreate(Exception innerException)
                {
                    return new UnhandledExceptionException(innerException);
                }

                public override string Message
                {
                    get
                    {
                        return "An exception was encountered that was not handled.";
                    }
                }

                public override object GetValue()
                {
                    return InnerException;
                }

                public override bool TryGetValueAs<U>(out U value)
                {
#if CSHARP_7_OR_LATER
                    if (InnerException is U val)
                    {
                        value = val;
#else
                    if (InnerException is U)
                    {
                        value = (U) (object) InnerException;
#endif
                        return true;
                    }
                    value = default(U);
                    return false;
                }

                public override bool ContainsType<U>()
                {
                    return InnerException is U;
                }
            }

            public sealed class CancelVoid : IValueContainer
            {
                // We can reuse the same object.
                private static readonly CancelVoid obj = new CancelVoid();

                private CancelVoid() { }

                public static CancelVoid GetOrCreate()
                {
                    return obj;
                }

                public bool TryGetValueAs<U>(out U value)
                {
                    value = default(U);
                    return false;
                }

#pragma warning disable RECS0096 // Type parameter is never used
                public bool ContainsType<U>()
#pragma warning restore RECS0096 // Type parameter is never used
                {
                    return false;
                }

                public void Retain() { }

                public void Release() { }
            }

            public sealed class CancelValue<T> : IValueContainer, ILinked<CancelValue<T>>
            {
                CancelValue<T> ILinked<CancelValue<T>>.Next { get; set; }

                public T Value { get; private set; }

                private static ValueLinkedStack<CancelValue<T>> _pool;

                private uint retainCounter;

                static CancelValue()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private CancelValue() { }

                public static CancelValue<T> GetOrCreate(T value)
                {
                    CancelValue<T> cv = _pool.IsNotEmpty ? _pool.Pop() : new CancelValue<T>();
                    cv.Value = value;
                    return cv;
                }

                public bool TryGetValueAs<U>(out U value)
                {
                    // This avoids boxing value types.
#if CSHARP_7_OR_LATER
                    if (this is CancelValue<U> casted)
#else
                    var casted = this as CancelValue<U>;
                    if (casted != null)
#endif
                    {
                        value = casted.Value;
                        return true;
                    }
                    // Can it be up-casted or down-casted, null or not?
                    if (typeof(U).IsAssignableFrom(typeof(T)) || Value is U)
                    {
                        value = (U)(object)Value;
                        return true;
                    }
                    value = default(U);
                    return false;
                }

                public bool ContainsType<U>()
                {
                    // Can it be up-casted or down-casted, null or not?
                    return typeof(U).IsAssignableFrom(typeof(T)) || Value is U;
                }

                public void Retain()
                {
                    ++retainCounter;
                }

                public void Release()
                {
                    if (--retainCounter == 0 & Config.ObjectPooling != PoolType.None)
                    {
                        Value = default(T);
                        _pool.Push(this);
                    }
                }
            }
#endregion

#region Multi Promises
            public partial interface IMultiTreeHandleable : ITreeHandleable
            {
                void Handle(Promise feed, int index);
                void ReAdd(PromisePassThrough passThrough);
            }
            
            public sealed partial class PromisePassThrough : ITreeHandleable, ILinked<PromisePassThrough>
            {
                private static ValueLinkedStack<PromisePassThrough> _pool;

                static PromisePassThrough()
                {
                    OnClearPool += () => _pool.Clear();
                }

                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }
                PromisePassThrough ILinked<PromisePassThrough>.Next { get; set; }

                public Promise owner;
                public IMultiTreeHandleable target;

                private int _index;

                public static PromisePassThrough GetOrCreate(Promise owner, IMultiTreeHandleable target, int index)
                {
                    var passThrough = _pool.IsNotEmpty ? _pool.Pop() : new PromisePassThrough();
                    passThrough.owner = owner;
                    passThrough.target = target;
                    passThrough._index = index;
                    owner.AddWaiter(passThrough);
                    return passThrough;
                }

                private PromisePassThrough() { }

                public void Reset()
                {
                    owner = null;
                    target = null;
                }

                public static void Repool(ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        while (passThroughs.IsNotEmpty)
                        {
                            var passThrough = passThroughs.Pop();
                            passThrough.Reset();
                            _pool.Push(passThrough);
                        }
                    }
                    else
                    {
                        passThroughs.Clear();
                    }
                }

                void ITreeHandleable.AssignCancelValue(IValueContainer cancelValue)
                {
                    target.AssignCancelValue(cancelValue);
                }
                void ITreeHandleable.Cancel()
                {
                    var temp = target;
                    Reset();
                    temp.Cancel();
                }
                void ITreeHandleable.Handle(Promise feed)
                {
                    var temp = target;
                    Reset();
                    temp.Handle(feed, _index);
                }
                void ITreeHandleable.OnSubscribeToCanceled(IValueContainer cancelValue)
                {
                    target.OnSubscribeToCanceled(cancelValue);
                }
            }

            public sealed partial class AllPromise0 : PoolablePromise<AllPromise0>, IMultiTreeHandleable
            {
                private ValueLinkedStack<PromisePassThrough> passThroughs;
                private uint _waitCount;

                private AllPromise0() { }

                public static Promise GetOrCreate<TEnumerator>(TEnumerator promises, int skipFrames) where TEnumerator : IEnumerator<Promise>
                {
                    if (!promises.MoveNext())
                    {
                        // If promises is empty, just return a resolved promise.
                        return Resolved();
                    }
                    var promise = _pool.IsNotEmpty ? (AllPromise0) _pool.Pop() : new AllPromise0();

                    var target = promises.Current;
                    ValidateOperation(target);
                    int promiseIndex = 0;
                    // Hook up pass throughs
                    var passThroughs = new ValueLinkedStack<PromisePassThrough>(PromisePassThrough.GetOrCreate(target, promise, promiseIndex));
                    while (promises.MoveNext())
                    {
                        target = promises.Current;
                        ValidateOperation(target);
                        passThroughs.Push(PromisePassThrough.GetOrCreate(target, promise, ++promiseIndex));
                    }
                    promise.passThroughs = passThroughs;

                    // Retain this until all promises resolve/reject/cancel
                    promise._waitCount = (uint) promiseIndex + 1u;
                    promise._retainCounter = promise._waitCount;

                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                void IMultiTreeHandleable.Handle(Promise feed, int index)
                {
                    --_waitCount;
#if DEBUG
                    checked
#endif
                    {
                        --_retainCounter;
                    }

                    if (_state != State.Pending)
                    {
                        if (_waitCount == 0)
                        {
                            PromisePassThrough.Repool(ref passThroughs);
                        }
                        return;
                    }

                    feed._wasWaitedOn = true;
                    if (feed._state == State.Rejected)
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                        if (_waitCount == 0)
                        {
                            PromisePassThrough.Repool(ref passThroughs);
                        }
                    }
                    else if (_waitCount == 0)
                    {
                        PromisePassThrough.Repool(ref passThroughs);
                        ResolveInternal();
                    }
                    else
                    {
                        IncrementProgress(feed);
                    }
                }

                partial void IncrementProgress(Promise feed);

                protected override void AssignCancelValue(IValueContainer cancelValue)
                {
#if DEBUG
                    checked
#endif
                    {
                        --_retainCounter;
                    }
                    base.AssignCancelValue(cancelValue);
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    passThroughs.Push(passThrough);
                }

                protected override void Handle(Promise feed) { throw new InvalidOperationException(); }
            }

            public sealed partial class AllPromise<T> : PoolablePromise<IList<T>, AllPromise<T>>, IMultiTreeHandleable
            {
                private ValueLinkedStack<PromisePassThrough> passThroughs;
                private uint _waitCount;

                private AllPromise() { }

                public static Promise<IList<T>> GetOrCreate<TEnumerator>(TEnumerator promises, IList<T> valueContainer, int skipFrames) where TEnumerator : IEnumerator<Promise<T>>
                {
                    valueContainer.Clear();
                    if (!promises.MoveNext())
                    {
                        // If promises is empty, just return a resolved promise.
                        return Resolved(valueContainer);
                    }
                    var promise = _pool.IsNotEmpty ? (AllPromise<T>) _pool.Pop() : new AllPromise<T>();
                    promise._value = valueContainer;

                    var target = promises.Current;
                    ValidateOperation(target);
                    int promiseIndex = 0;
                    // Hook up pass throughs and make sure the list has space for the values.
                    valueContainer.Add(default(T));
                    var passThroughs = new ValueLinkedStack<PromisePassThrough>(PromisePassThrough.GetOrCreate(target, promise, promiseIndex));
                    while (promises.MoveNext())
                    {
                        target = promises.Current;
                        ValidateOperation(target);
                        valueContainer.Add(default(T));
                        passThroughs.Push(PromisePassThrough.GetOrCreate(target, promise, ++promiseIndex));
                    }
                    promise.passThroughs = passThroughs;

                    // Retain this until all promises resolve/reject/cancel
                    promise._waitCount = (uint) promiseIndex + 1u;
                    promise._retainCounter = promise._waitCount;

                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                void IMultiTreeHandleable.Handle(Promise feed, int index)
                {
                    --_waitCount;
#if DEBUG
                    checked
#endif
                    {
                        --_retainCounter;
                    }

                    if (_state != State.Pending)
                    {
                        if (_waitCount == 0)
                        {
                            PromisePassThrough.Repool(ref passThroughs);
                        }
                        return;
                    }

                    feed._wasWaitedOn = true;
                    if (feed._state == State.Rejected)
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                        if (_waitCount == 0)
                        {
                            PromisePassThrough.Repool(ref passThroughs);
                        }
                    }
                    else
                    {
                        _value[index] = feed.GetValue<T>();
                        if (_waitCount == 0)
                        {
                            PromisePassThrough.Repool(ref passThroughs);
                            ResolveInternal();
                        }
                        else
                        {
                            IncrementProgress(feed);
                        }
                    }
                }

                partial void IncrementProgress(Promise feed);

                protected override void AssignCancelValue(IValueContainer cancelValue)
                {
#if DEBUG
                    checked
#endif
                    {
                        --_retainCounter;
                    }
                    base.AssignCancelValue(cancelValue);
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    passThroughs.Push(passThrough);
                }

                protected override void Handle(Promise feed) { throw new InvalidOperationException(); }
            }

            public sealed partial class RacePromise0 : PoolablePromise<RacePromise0>, IMultiTreeHandleable
            {
                private ValueLinkedStack<PromisePassThrough> passThroughs;
                private uint _waitCount;

                private RacePromise0() { }

                public static Promise GetOrCreate<TEnumerator>(TEnumerator promises, int skipFrames) where TEnumerator : IEnumerator<Promise>
                {
                    if (!promises.MoveNext())
                    {
#pragma warning disable RECS0163 // Suggest the usage of the nameof operator
                        throw new ArgumentException("Cannot race 0 promises.", "promises");
#pragma warning restore RECS0163 // Suggest the usage of the nameof operator
                    }
                    var promise = _pool.IsNotEmpty ? (RacePromise0) _pool.Pop() : new RacePromise0();

                    var target = promises.Current;
                    ValidateOperation(target);
                    int promiseIndex = 0;
                    // Hook up pass throughs
                    var passThroughs = new ValueLinkedStack<PromisePassThrough>(PromisePassThrough.GetOrCreate(target, promise, promiseIndex));
                    while (promises.MoveNext())
                    {
                        target = promises.Current;
                        ValidateOperation(target);
                        passThroughs.Push(PromisePassThrough.GetOrCreate(target, promise, ++promiseIndex));
                    }
                    promise.passThroughs = passThroughs;

                    // Retain this until all promises resolve/reject/cancel
                    promise._waitCount = (uint) promiseIndex + 1u;
                    promise._retainCounter = promise._waitCount;

                    promise.ResetDepth();
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                void IMultiTreeHandleable.Handle(Promise feed, int index)
                {
                    --_waitCount;
#if DEBUG
                    checked
#endif
                    {
                        --_retainCounter;
                    }

                    if (_waitCount == 0)
                    {
                        PromisePassThrough.Repool(ref passThroughs);
                    }

                    if (_state != State.Pending)
                    {
                        return;
                    }

                    feed._wasWaitedOn = true;
                    if (feed._state == State.Rejected)
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                    else
                    {
                        ResolveInternal();
                    }
                }

                protected override void AssignCancelValue(IValueContainer cancelValue)
                {
#if DEBUG
                    checked
#endif
                    {
                        --_retainCounter;
                    }
                    base.AssignCancelValue(cancelValue);
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    passThroughs.Push(passThrough);
                }

                protected override void Handle(Promise feed) { throw new InvalidOperationException(); }
            }

            public sealed partial class RacePromise<T> : PoolablePromise<T, RacePromise<T>>, IMultiTreeHandleable
            {
                private ValueLinkedStack<PromisePassThrough> passThroughs;
                private uint _waitCount;

                private RacePromise() { }

                public static Promise<T> GetOrCreate<TEnumerator>(TEnumerator promises, int skipFrames) where TEnumerator : IEnumerator<Promise<T>>
                {
                    if (!promises.MoveNext())
                    {
#pragma warning disable RECS0163 // Suggest the usage of the nameof operator
                        throw new ArgumentException("Cannot race 0 promises.", "promises");
#pragma warning restore RECS0163 // Suggest the usage of the nameof operator
                    }
                    var promise = _pool.IsNotEmpty ? (RacePromise<T>) _pool.Pop() : new RacePromise<T>();

                    var target = promises.Current;
                    ValidateOperation(target);
                    int promiseIndex = 0;
                    // Hook up pass throughs
                    var passThroughs = new ValueLinkedStack<PromisePassThrough>(PromisePassThrough.GetOrCreate(target, promise, promiseIndex));
                    while (promises.MoveNext())
                    {
                        target = promises.Current;
                        ValidateOperation(target);
                        passThroughs.Push(PromisePassThrough.GetOrCreate(target, promise, ++promiseIndex));
                    }
                    promise.passThroughs = passThroughs;

                    // Retain this until all promises resolve/reject/cancel
                    promise._waitCount = (uint) promiseIndex + 1u;
                    promise._retainCounter = promise._waitCount;

                    promise.ResetDepth();
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                void IMultiTreeHandleable.Handle(Promise feed, int index)
                {
                    --_waitCount;
#if DEBUG
                    checked
#endif
                    {
                        --_retainCounter;
                    }

                    if (_waitCount == 0)
                    {
                        PromisePassThrough.Repool(ref passThroughs);
                    }

                    if (_state != State.Pending)
                    {
                        return;
                    }

                    feed._wasWaitedOn = true;
                    if (feed._state == State.Rejected)
                    {
                        RejectInternal(feed._rejectedOrCanceledValue);
                    }
                    else
                    {
                        ResolveInternal(feed.GetValue<T>());
                    }
                }

                protected override void AssignCancelValue(IValueContainer cancelValue)
                {
#if DEBUG
                    checked
#endif
                    {
                        --_retainCounter;
                    }
                    base.AssignCancelValue(cancelValue);
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    passThroughs.Push(passThrough);
                }

                protected override void Handle(Promise feed) { throw new InvalidOperationException(); }
            }
            #endregion
        }
    }
}
#pragma warning restore IDE0034 // Simplify 'default' expression
#pragma warning restore IDE0018 // Inline variable declaration
#pragma warning restore RECS0108 // Warns about static fields in generic types