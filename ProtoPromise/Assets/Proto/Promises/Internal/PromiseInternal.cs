#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#endif

#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable RECS0001 // Class is declared partial but has only one part

using System;
using System.Collections.Generic;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise : Promise.Internal.ITreeHandleable, Promise.Internal.IStacktraceable, Promise.Internal.IValueContainerOrPrevious, Promise.Internal.IValueContainerContainer
    {
        private ValueLinkedQueue<Internal.ITreeHandleable> _nextBranches;
        protected Internal.IValueContainerOrPrevious _rejectedOrCanceledValueOrPrevious;
        private uint _retainCounter;
        protected State _state;
        private bool _wasWaitedOn;
        protected bool _dontPool;

        Internal.ITreeHandleable ILinked<Internal.ITreeHandleable>.Next { get; set; }
        Internal.IValueContainerOrPrevious Internal.IValueContainerContainer.ValueContainerOrPrevious { get { return _rejectedOrCanceledValueOrPrevious; } }

        // This breaks Interface Segregation Principle, but cuts down on memory.
        bool Internal.IValueContainerOrPrevious.TryGetValueAs<U>(out U value) { throw new System.InvalidOperationException(); }
        bool Internal.IValueContainerOrPrevious.ContainsType<U>() { throw new System.InvalidOperationException(); }

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
                if (_state == State.Rejected & !_wasWaitedOn)
                {
                    // Rejection wasn't caught.
                    AddRejectionToUnhandledStack((Internal.UnhandledExceptionInternal) _rejectedOrCanceledValueOrPrevious);
                }
                // Promise wasn't released.
                var exception = Internal.UnhandledExceptionException.GetOrCreate(UnreleasedObjectException.instance);
                SetStacktraceFromCreated(this, exception);
                AddRejectionToUnhandledStack(exception);
            }
        }

        protected virtual void Reset(int skipFrames)
        {
            _state = State.Pending;
            _retainCounter = 1;
            _dontPool = Config.ObjectPooling != PoolType.All;
            _wasWaitedOn = false;
            SetNotDisposed(ref _rejectedOrCanceledValueOrPrevious);
            SetCreatedStacktrace(this, skipFrames + 1);
        }

        void Internal.IValueContainerOrPrevious.Retain()
        {
            RetainInternal();
        }

        void Internal.IValueContainerOrPrevious.Release()
        {
            ReleaseInternal();
        }

        protected void RetainInternal()
        {
#if PROMISE_DEBUG
            checked // If this fails, change _retainCounter to ulong.
#endif
            {
                ++_retainCounter;
            }
        }

        protected void ReleaseInternal()
        {
            if (ReleaseWithoutDisposeCheck() == 0 & _state != State.Pending)
            {
                if (_state == State.Rejected & !_wasWaitedOn)
                {
                    // Rejection wasn't caught.
                    _wasWaitedOn = true;
                    AddRejectionToUnhandledStack((Internal.UnhandledExceptionInternal) _rejectedOrCanceledValueOrPrevious);
                }
                Dispose();
            }
        }

        protected virtual void Dispose()
        {
            if (_rejectedOrCanceledValueOrPrevious != null)
            {
                _rejectedOrCanceledValueOrPrevious.Release();
            }
            SetDisposed(ref _rejectedOrCanceledValueOrPrevious);
        }

        protected uint ReleaseWithoutDisposeCheck()
        {
            return --_retainCounter;
        }

        protected virtual Promise GetDuplicate()
        {
            return Internal.DuplicatePromise0.GetOrCreate(2);
        }

        protected void CancelInternal(Internal.IValueContainerOrPrevious cancelValue)
        {
            _state = State.Canceled;
            cancelValue.Retain();
            OnCancel();
            _rejectedOrCanceledValueOrPrevious = cancelValue;
        }

        protected void ResolveInternalWithoutRelease()
        {
            _state = State.Resolved;
            ResolveProgressListeners();
            AddToHandleQueueFront(ref _nextBranches);
        }

        protected void ResolveInternal()
        {
            ResolveInternalWithoutRelease();
            ReleaseInternal();
        }

        protected static Internal.UnhandledExceptionInternal CreateRejection(int skipFrames)
        {
            Internal.UnhandledExceptionInternal rejectValue = Internal.UnhandledExceptionVoid.GetOrCreate();
            SetRejectStacktrace(rejectValue, skipFrames + 1);
            return rejectValue;
        }

        protected static Internal.UnhandledExceptionInternal CreateRejection<TReject>(TReject reason, int skipFrames)
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
            SetRejectStacktrace(rejectValue, skipFrames + 1);
            return rejectValue;
        }

        protected void RejectInternalWithoutRelease(Internal.IValueContainerOrPrevious rejectValue)
        {
            _state = State.Rejected;
            _rejectedOrCanceledValueOrPrevious = rejectValue;
            _rejectedOrCanceledValueOrPrevious.Retain();
            CancelProgressListeners();
            AddToHandleQueueFront(ref _nextBranches);
        }

        protected void RejectInternal(Internal.IValueContainerOrPrevious rejectValue)
        {
            RejectInternalWithoutRelease(rejectValue);
            ReleaseInternal();
        }

        protected void HookupNewPromise(Promise newPromise)
        {
            newPromise._rejectedOrCanceledValueOrPrevious = this;
            SetDepth(newPromise);
            AddWaiter(newPromise);
        }

        protected virtual void Handle()
        {
            var feed = (Promise) _rejectedOrCanceledValueOrPrevious;
            _rejectedOrCanceledValueOrPrevious = null;
            feed._wasWaitedOn = true;
            try
            {
                Handle(feed);
            }
            catch (RethrowException)
            {
                RejectInternalIfNotCanceled(feed._rejectedOrCanceledValueOrPrevious);
                OnHandleCatch();
            }
            catch (Internal.CanceledExceptionInternal e)
            {
                if (_state == State.Pending)
                {
                    _state = State.Canceled;
                    _rejectedOrCanceledValueOrPrevious = e;
                    e.Retain();
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                }
                ReleaseInternal();
                OnHandleCatch();
            }
            catch (Internal.UnhandledExceptionInternal e)
            {
                RejectInternalIfNotCanceled(e);
                OnHandleCatch();
            }
            catch (Exception e)
            {
                var ex = Internal.UnhandledExceptionException.GetOrCreate(e);
                SetStacktraceFromCreated(this, ex);
                RejectInternalIfNotCanceled(ex);
                OnHandleCatch();
            }
            finally
            {
                Internal._invokingResolved = false;
                Internal._invokingRejected = false;
                feed.ReleaseInternal();
            }
        }

        protected virtual void OnHandleCatch() { }

        private void HandleSelf()
        {
            // If this was rejected, the progress listeners were already removed, so we don't need to branch here.
            ResolveProgressListeners();
            AddToHandleQueueFront(ref _nextBranches);
            ReleaseInternal();
        }

        protected void RejectDirect(Internal.IValueContainerOrPrevious rejectValue)
        {
            _state = State.Rejected;
            _rejectedOrCanceledValueOrPrevious = rejectValue;
            _rejectedOrCanceledValueOrPrevious.Retain();
            CancelProgressListeners();
            AddToHandleQueueBack(this);
        }

        protected void HandleSelfWithoutRelease(Promise feed)
        {
            if (feed._state == State.Resolved)
            {
                ResolveInternalWithoutRelease();
            }
            else
            {
                RejectInternalWithoutRelease(feed._rejectedOrCanceledValueOrPrevious);
            }
        }

        protected void HandleSelf(Promise feed)
        {
            if (feed._state == State.Resolved)
            {
                ResolveInternal();
            }
            else
            {
                RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
            }
        }

        protected virtual void Handle(Promise feed) { }

        void Internal.ITreeHandleable.Cancel()
        {
            if (_state == State.Pending)
            {
                _state = State.Canceled;
                CancelInternal(((Promise) _rejectedOrCanceledValueOrPrevious)._rejectedOrCanceledValueOrPrevious);
            }
            ReleaseInternal();
        }

        protected virtual void OnCancel()
        {
            _rejectedOrCanceledValueOrPrevious.Release();
            _rejectedOrCanceledValueOrPrevious = null;
            CancelProgressListeners();
            AddToCancelQueueFront(ref _nextBranches);
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

        // Handle promises in a depth-first manner.
        private static ValueLinkedQueue<Internal.ITreeHandleable> _handleQueue;
        private static bool _runningHandles;

        protected static void AddToHandleQueueBack(Internal.ITreeHandleable handleable)
        {
            _handleQueue.Enqueue(handleable);
        }

        protected static void AddToHandleQueueFront(ref ValueLinkedQueue<Internal.ITreeHandleable> handleables)
        {
            _handleQueue.PushAndClear(ref handleables);
        }
    }

    partial class Promise<T>
    {
        protected T _value;

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
        private
#endif
        protected Promise() { }

        protected override Promise GetDuplicate()
        {
            return Promise.Internal.DuplicatePromise<T>.GetOrCreate(2);
        }

        protected override void Dispose()
        {
            _value = default(T);
            base.Dispose();
        }

        new protected void HandleSelf(Promise feed)
        {
            _value = ((Promise<T>) feed)._value;
            base.HandleSelf(feed);
        }
    }

    partial class Promise
    {
        protected static partial class Internal
        {
            internal static bool _invokingResolved, _invokingRejected;

            internal static Action OnClearPool;

            public abstract class PromiseInternal<T> : Promise<T>
            {
#if CSHARP_7_OR_LATER
                public ref T Value { get { return ref _value; } }
#else
                public T Value { get { return _value; } }
#endif
            }

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

            public abstract class PoolablePromise<T, TPromise> : PromiseInternal<T> where TPromise : PoolablePromise<T, TPromise>
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

            public abstract partial class PromiseWaitDeferred<TPromise> : PoolablePromise<TPromise> where TPromise : PromiseWaitDeferred<TPromise>
            {
                protected readonly DeferredInternal _deferredInternal;
                public new Deferred Deferred { get { return _deferredInternal; } }

                protected PromiseWaitDeferred()
                {
                    _deferredInternal = new DeferredInternal(this);
                }

                protected override void Reset(int skipFrames)
                {
                    base.Reset(skipFrames + 1);
                    _deferredInternal.Reset();
                    // Retain now, release when deferred resolves/rejects/cancels.
                    RetainInternal();
                }

                protected override void OnHandleCatch()
                {
                    _deferredInternal.ReleaseDirect();
                }
            }

            public abstract partial class PromiseWaitDeferred<T, TPromise> : PoolablePromise<T, TPromise> where TPromise : PromiseWaitDeferred<T, TPromise>
            {
                protected readonly Internal.DeferredInternal _deferredInternal;
                public new Deferred Deferred { get { return _deferredInternal; } }

                protected PromiseWaitDeferred()
                {
                    _deferredInternal = new Internal.DeferredInternal(this);
                }

                protected override void Reset(int skipFrames)
                {
                    base.Reset(skipFrames + 1);
                    _deferredInternal.Reset();
                    // Retain now, release when deferred resolves/rejects/cancels.
                    RetainInternal();
                }

                protected override void OnHandleCatch()
                {
                    _deferredInternal.ReleaseDirect();
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

                protected override void Handle()
                {
                    HandleSelf();
                }

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                    AddToHandleQueueBack(this);
                }
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

                protected override void Handle()
                {
                    HandleSelf();
                }

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                    AddToHandleQueueBack(this);
                }
            }

            public sealed class ResolvedPromise : Promise
            {
                private ResolvedPromise() { }

                public static readonly ResolvedPromise instance = new ResolvedPromise() { _state = State.Resolved };

                protected override void Dispose() { }
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

                protected override void Handle()
                {
                    HandleSelf();
                }

#if PROMISE_DEBUG
                public void ResolveDirect()
                {
                    _state = State.Resolved;
                    AddToHandleQueueBack(this);
                }
#endif

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                    AddToHandleQueueBack(this);
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

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
                public void ResolveDirect(in T value)
#else
                public void ResolveDirect(T value)
#endif
                {
                    _state = State.Resolved;
                    _value = value;
                    AddToHandleQueueBack(this);
                }

                protected override void Handle()
                {
                    ReleaseInternal();
                }

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                    AddToHandleQueueBack(this);
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
                    HandleSelf(feed);
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
                    HandleSelf(feed);
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
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolve0) _pool.Pop() : new PromiseVoidResolve0();
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
                        _invokingResolved = true;
                        callback.Invoke();
                        ResolveInternalIfNotCanceled();
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    resolveHandler = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseArgResolve<TArg> : PoolablePromise<PromiseArgResolve<TArg>>
            {
                private Action<TArg> resolveHandler;

                private PromiseArgResolve() { }

                public static PromiseArgResolve<TArg> GetOrCreate(Action<TArg> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolve<TArg>) _pool.Pop() : new PromiseArgResolve<TArg>();
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
                        _invokingResolved = true;
                        callback.Invoke(((PromiseInternal<TArg>) feed).Value);
                        ResolveInternalIfNotCanceled();
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    resolveHandler = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseVoidResolve<TResult> : PoolablePromise<TResult, PromiseVoidResolve<TResult>>
            {
                private Func<TResult> resolveHandler;

                private PromiseVoidResolve() { }

                public static PromiseVoidResolve<TResult> GetOrCreate(Func<TResult> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolve<TResult>) _pool.Pop() : new PromiseVoidResolve<TResult>();
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
                        _invokingResolved = true;
                        _value = callback.Invoke();
                        ResolveInternalIfNotCanceled();
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    resolveHandler = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseArgResolve<TArg, TResult> : PoolablePromise<TResult, PromiseArgResolve<TArg, TResult>>
            {
                private Func<TArg, TResult> resolveHandler;

                private PromiseArgResolve() { }

                public static PromiseArgResolve<TArg, TResult> GetOrCreate(Func<TArg, TResult> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolve<TArg, TResult>) _pool.Pop() : new PromiseArgResolve<TArg, TResult>();
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
                        _invokingResolved = true;
                        _value = callback.Invoke(((PromiseInternal<TArg>) feed).Value);
                        ResolveInternalIfNotCanceled();
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    resolveHandler = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseVoidResolvePromise0 : PromiseWaitPromise<PromiseVoidResolvePromise0>
            {
                private Func<Promise> resolveHandler;

                private PromiseVoidResolvePromise0() { }

                public static PromiseVoidResolvePromise0 GetOrCreate(Func<Promise> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolvePromise0) _pool.Pop() : new PromiseVoidResolvePromise0();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (resolveHandler == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke());
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    resolveHandler = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseArgResolvePromise<TArg> : PromiseWaitPromise<PromiseArgResolvePromise<TArg>>
            {
                private Func<TArg, Promise> resolveHandler;

                private PromiseArgResolvePromise() { }

                public static PromiseArgResolvePromise<TArg> GetOrCreate(Func<TArg, Promise> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolvePromise<TArg>) _pool.Pop() : new PromiseArgResolvePromise<TArg>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (resolveHandler == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke(((PromiseInternal<TArg>) feed).Value));
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    resolveHandler = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseVoidResolvePromise<TPromise> : PromiseWaitPromise<TPromise, PromiseVoidResolvePromise<TPromise>>
            {
                private Func<Promise<TPromise>> resolveHandler;

                private PromiseVoidResolvePromise() { }

                public static PromiseVoidResolvePromise<TPromise> GetOrCreate(Func<Promise<TPromise>> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolvePromise<TPromise>) _pool.Pop() : new PromiseVoidResolvePromise<TPromise>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (resolveHandler == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke());
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    resolveHandler = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseArgResolvePromise<TArg, TPromise> : PromiseWaitPromise<TPromise, PromiseArgResolvePromise<TArg, TPromise>>
            {
                private Func<TArg, Promise<TPromise>> resolveHandler;

                private PromiseArgResolvePromise() { }

                public static PromiseArgResolvePromise<TArg, TPromise> GetOrCreate(Func<TArg, Promise<TPromise>> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolvePromise<TArg, TPromise>) _pool.Pop() : new PromiseArgResolvePromise<TArg, TPromise>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (resolveHandler == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke(((PromiseInternal<TArg>) feed).Value));
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    resolveHandler = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseVoidResolveDeferred0 : PromiseWaitDeferred<PromiseVoidResolveDeferred0>
            {
                private Func<Action<Deferred>> resolveHandler;

                private PromiseVoidResolveDeferred0() { }

                public static PromiseVoidResolveDeferred0 GetOrCreate(Func<Action<Deferred>> resolveHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolveDeferred0) _pool.Pop() : new PromiseVoidResolveDeferred0();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle()
                {
                    if (resolveHandler == null)
                    {
                        HandleSelf();
                    }
                    else
                    {
                        base.Handle();
                    }
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        var deferredAction = callback.Invoke();
                        try
                        {
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
                        _deferredInternal.ReleaseDirect();
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    if (resolveHandler != null)
                    {
                        // Deferred is never used, so just release.
                        _deferredInternal.ReleaseDirect();
                    }
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
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolveDeferred<TArg>) _pool.Pop() : new PromiseArgResolveDeferred<TArg>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle()
                {
                    if (resolveHandler == null)
                    {
                        HandleSelf();
                    }
                    else
                    {
                        base.Handle();
                    }
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        var deferredAction = callback.Invoke(((PromiseInternal<TArg>) feed).Value);
                        try
                        {
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
                        _deferredInternal.ReleaseDirect();
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    if (resolveHandler != null)
                    {
                        // Deferred is never used, so just release.
                        _deferredInternal.ReleaseDirect();
                    }
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
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolveDeferred<TDeferred>) _pool.Pop() : new PromiseVoidResolveDeferred<TDeferred>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle()
                {
                    if (resolveHandler == null)
                    {
                        HandleSelf();
                    }
                    else
                    {
                        base.Handle();
                    }
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        var deferredAction = callback.Invoke();
                        try
                        {
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
                        _deferredInternal.ReleaseDirect();
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    if (resolveHandler != null)
                    {
                        // Deferred is never used, so just release.
                        _deferredInternal.ReleaseDirect();
                    }
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
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolveDeferred<TArg, TDeferred>) _pool.Pop() : new PromiseArgResolveDeferred<TArg, TDeferred>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle()
                {
                    if (resolveHandler == null)
                    {
                        HandleSelf();
                    }
                    else
                    {
                        base.Handle();
                    }
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        var deferredAction = callback.Invoke(((PromiseInternal<TArg>) feed).Value);
                        try
                        {
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
                        _deferredInternal.ReleaseDirect();
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    if (resolveHandler != null)
                    {
                        // Deferred is never used, so just release.
                        _deferredInternal.ReleaseDirect();
                    }
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
                    var promise = _pool.IsNotEmpty ? (PromiseReject0) _pool.Pop() : new PromiseReject0();
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
                        _invokingRejected = true;
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValueOrPrevious))
                        {
                            ResolveInternalIfNotCanceled();
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
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
                    if (rejectHandler != null)
                    {
                        rejectHandler.Dispose();
                        rejectHandler = null;
                    }
                    base.OnCancel();
                }
            }

            public sealed class PromiseReject<T> : PoolablePromise<T, PromiseReject<T>>
            {
                private IDelegate<T> rejectHandler;

                private PromiseReject() { }

                public static PromiseReject<T> GetOrCreate(IDelegate<T> rejectHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseReject<T>) _pool.Pop() : new PromiseReject<T>();
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
                        _invokingRejected = true;
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValueOrPrevious, out _value))
                        {
                            ResolveInternalIfNotCanceled();
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                        }
                    }
                    else
                    {
                        callback.Dispose();
                        _value = ((PromiseInternal<T>) feed).Value;
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

            public sealed class PromiseRejectPromise0 : PromiseWaitPromise<PromiseRejectPromise0>
            {
                private IDelegate<Promise> rejectHandler;

                private PromiseRejectPromise0() { }

                public static PromiseRejectPromise0 GetOrCreate(IDelegate<Promise> rejectHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseRejectPromise0) _pool.Pop() : new PromiseRejectPromise0();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (rejectHandler == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == State.Rejected)
                    {
                        Promise promise;
                        _invokingRejected = true;
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValueOrPrevious, out promise))
                        {
                            WaitFor(promise);
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
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
                    if (rejectHandler != null)
                    {
                        rejectHandler.Dispose();
                        rejectHandler = null;
                    }
                    base.OnCancel();
                }
            }

            public sealed class PromiseRejectPromise<TPromise> : PromiseWaitPromise<TPromise, PromiseRejectPromise<TPromise>>
            {
                private IDelegate<Promise<TPromise>> rejectHandler;

                private PromiseRejectPromise() { }

                public static PromiseRejectPromise<TPromise> GetOrCreate(IDelegate<Promise<TPromise>> rejectHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseRejectPromise<TPromise>) _pool.Pop() : new PromiseRejectPromise<TPromise>();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (rejectHandler == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == State.Rejected)
                    {
                        Promise<TPromise> promise;
                        _invokingRejected = true;
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValueOrPrevious, out promise))
                        {
                            WaitFor(promise);
                        }
                        else
                        {
                            RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                        }
                    }
                    else
                    {
                        callback.Dispose();
                        _value = ((PromiseInternal<TPromise>) feed).Value;
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

            public sealed class PromiseRejectDeferred0 : PromiseWaitDeferred<PromiseRejectDeferred0>
            {
                private IDelegate<Action<Deferred>> rejectHandler;

                private PromiseRejectDeferred0() { }

                public static PromiseRejectDeferred0 GetOrCreate(IDelegate<Action<Deferred>> rejectHandler, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseRejectDeferred0) _pool.Pop() : new PromiseRejectDeferred0();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle()
                {
                    if (rejectHandler == null)
                    {
                        HandleSelf();
                    }
                    else
                    {
                        base.Handle();
                    }
                }

                protected override void Handle(Promise feed)
                {
                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == State.Rejected)
                    {
                        Action<Deferred> deferredDelegate;
                        _invokingRejected = true;
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValueOrPrevious, out deferredDelegate))
                        {
                            ValidateReturn(deferredDelegate);
                            try
                            {
                                deferredDelegate.Invoke(_deferredInternal);
                            }
                            catch (Exception e)
                            {
                                _deferredInternal.RejectWithPromiseStacktrace(e);
                            }
                        }
                        else
                        {
                            // Deferred is never used, so just release.
                            _deferredInternal.ReleaseDirect();
                            RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                        }
                    }
                    else
                    {
                        // Deferred is never used, so just release.
                        _deferredInternal.ReleaseDirect();
                        callback.Dispose();
                        ResolveInternal();
                    }
                }

                protected override void OnCancel()
                {
                    if (rejectHandler != null)
                    {
                        // Deferred is never used, so just release.
                        _deferredInternal.ReleaseDirect();
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
                    var promise = _pool.IsNotEmpty ? (PromiseRejectDeferred<TDeferred>) _pool.Pop() : new PromiseRejectDeferred<TDeferred>();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle()
                {
                    if (rejectHandler == null)
                    {
                        HandleSelf();
                    }
                    else
                    {
                        base.Handle();
                    }
                }

                protected override void Handle(Promise feed)
                {
                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == State.Rejected)
                    {
                        Action<Deferred> deferredDelegate;
                        _invokingRejected = true;
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValueOrPrevious, out deferredDelegate))
                        {
                            ValidateReturn(deferredDelegate);
                            try
                            {
                                deferredDelegate.Invoke(_deferredInternal);
                            }
                            catch (Exception e)
                            {
                                _deferredInternal.RejectWithPromiseStacktrace(e);
                            }
                        }
                        else
                        {
                            // Deferred is never used, so just release.
                            _deferredInternal.ReleaseDirect();
                            RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                        }
                    }
                    else
                    {
                        // Deferred is never used, so just release.
                        _deferredInternal.ReleaseDirect();
                        callback.Dispose();
                        _value = ((PromiseInternal<TDeferred>) feed).Value;
                        ResolveInternal();
                    }
                }

                protected override void OnCancel()
                {
                    if (rejectHandler != null)
                    {
                        // Deferred is never used, so just release.
                        _deferredInternal.ReleaseDirect();
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
                    var promise = _pool.IsNotEmpty ? (PromiseResolveReject0) _pool.Pop() : new PromiseResolveReject0();
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
                    if (feed._state == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        _invokingResolved = true;
                        resolveCallback.DisposeAndInvoke(feed);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        _invokingRejected = true;
                        if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValueOrPrevious))
                        {
                            RejectInternalIfNotCanceled(feed._rejectedOrCanceledValueOrPrevious);
                            return;
                        }
                    }
                    ResolveInternalIfNotCanceled();
                }

                protected override void OnCancel()
                {
                    if (onResolved != null)
                    {
                        onResolved.Dispose();
                        onResolved = null;
                        onRejected.Dispose();
                        onRejected = null;
                    }
                    base.OnCancel();
                }
            }

            public sealed class PromiseResolveReject<T> : PoolablePromise<T, PromiseResolveReject<T>>
            {
                IDelegate<T> onResolved, onRejected;

                private PromiseResolveReject() { }

                public static PromiseResolveReject<T> GetOrCreate(IDelegate<T> onResolved, IDelegate<T> onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveReject<T>) _pool.Pop() : new PromiseResolveReject<T>();
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
                    if (feed._state == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        _invokingResolved = true;
                        _value = resolveCallback.DisposeAndInvoke(feed);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        _invokingRejected = true;
                        if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValueOrPrevious, out _value))
                        {
                            RejectInternalIfNotCanceled(feed._rejectedOrCanceledValueOrPrevious);
                            return;
                        }
                    }
                    ResolveInternalIfNotCanceled();
                }

                protected override void OnCancel()
                {
                    if (onResolved != null)
                    {
                        onResolved.Dispose();
                        onResolved = null;
                        onRejected.Dispose();
                        onRejected = null;
                    }
                    base.OnCancel();
                }
            }

            public sealed class PromiseResolveRejectPromise0 : PromiseWaitPromise<PromiseResolveRejectPromise0>
            {
                IDelegate<Promise> onResolved, onRejected;

                private PromiseResolveRejectPromise0() { }

                public static PromiseResolveRejectPromise0 GetOrCreate(IDelegate<Promise> onResolved, IDelegate<Promise> onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectPromise0) _pool.Pop() : new PromiseResolveRejectPromise0();
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
                        HandleSelf(feed);
                        return;
                    }

                    var resolveCallback = onResolved;
                    onResolved = null;
                    var rejectCallback = onRejected;
                    onRejected = null;
                    Promise promise;
                    if (feed._state == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        _invokingResolved = true;
                        promise = resolveCallback.DisposeAndInvoke(feed);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        _invokingRejected = true;
                        if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValueOrPrevious, out promise))
                        {
                            RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                            return;
                        }
                    }
                    WaitFor(promise);
                }

                protected override void OnCancel()
                {
                    if (onResolved != null)
                    {
                        onResolved.Dispose();
                        onResolved = null;
                        onRejected.Dispose();
                        onRejected = null;
                    }
                    base.OnCancel();
                }
            }

            public sealed class PromiseResolveRejectPromise<TPromise> : PromiseWaitPromise<TPromise, PromiseResolveRejectPromise<TPromise>>
            {
                IDelegate<Promise<TPromise>> onResolved, onRejected;

                private PromiseResolveRejectPromise() { }

                public static PromiseResolveRejectPromise<TPromise> GetOrCreate(IDelegate<Promise<TPromise>> onResolved, IDelegate<Promise<TPromise>> onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectPromise<TPromise>) _pool.Pop() : new PromiseResolveRejectPromise<TPromise>();
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
                        HandleSelf(feed);
                        return;
                    }

                    var resolveCallback = onResolved;
                    onResolved = null;
                    var rejectCallback = onRejected;
                    onRejected = null;
                    Promise<TPromise> promise;
                    if (feed._state == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        _invokingResolved = true;
                        promise = resolveCallback.DisposeAndInvoke(feed);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        _invokingRejected = true;
                        if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValueOrPrevious, out promise))
                        {
                            RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                            return;
                        }
                    }
                    WaitFor(promise);
                }

                protected override void OnCancel()
                {
                    if (onResolved != null)
                    {
                        onResolved.Dispose();
                        onResolved = null;
                        onRejected.Dispose();
                        onRejected = null;
                    }
                    base.OnCancel();
                }
            }

            public sealed class PromiseResolveRejectDeferred0 : PromiseWaitDeferred<PromiseResolveRejectDeferred0>
            {
                IDelegate<Action<Deferred>> onResolved, onRejected;

                private PromiseResolveRejectDeferred0() { }

                public static PromiseResolveRejectDeferred0 GetOrCreate(IDelegate<Action<Deferred>> onResolved, IDelegate<Action<Deferred>> onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectDeferred0) _pool.Pop() : new PromiseResolveRejectDeferred0();
                    promise.onResolved = onResolved;
                    promise.onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle()
                {
                    if (onResolved == null)
                    {
                        HandleSelf();
                    }
                    else
                    {
                        base.Handle();
                    }
                }

                protected override void Handle(Promise feed)
                {
                    var resolveCallback = onResolved;
                    onResolved = null;
                    var rejectCallback = onRejected;
                    onRejected = null;
                    Action<Deferred> deferredDelegate;
                    if (feed._state == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        _invokingResolved = true;
                        deferredDelegate = resolveCallback.DisposeAndInvoke(feed);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        _invokingRejected = true;
                        if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValueOrPrevious, out deferredDelegate))
                        {
                            // Deferred is never used, so just release.
                            _deferredInternal.ReleaseDirect();
                            RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                            return;
                        }
                    }
                    ValidateReturn(deferredDelegate);
                    try
                    {
                        deferredDelegate.Invoke(_deferredInternal);
                    }
                    catch (Exception e)
                    {
                        _deferredInternal.RejectWithPromiseStacktrace(e);
                    }
                }

                protected override void OnCancel()
                {
                    if (onResolved != null)
                    {
                        // Deferred is never used, so just release.
                        _deferredInternal.ReleaseDirect();
                        onResolved.Dispose();
                        onResolved = null;
                        onRejected.Dispose();
                        onRejected = null;
                    }
                    base.OnCancel();
                }
            }

            public sealed class PromiseResolveRejectDeferred<TDeferred> : PromiseWaitDeferred<TDeferred, PromiseResolveRejectDeferred<TDeferred>>
            {
                IDelegate<Action<Deferred>> onResolved, onRejected;

                private PromiseResolveRejectDeferred() { }

                public static PromiseResolveRejectDeferred<TDeferred> GetOrCreate(IDelegate<Action<Deferred>> onResolved, IDelegate<Action<Deferred>> onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectDeferred<TDeferred>) _pool.Pop() : new PromiseResolveRejectDeferred<TDeferred>();
                    promise.onResolved = onResolved;
                    promise.onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle()
                {
                    if (onResolved == null)
                    {
                        HandleSelf();
                    }
                    else
                    {
                        base.Handle();
                    }
                }

                protected override void Handle(Promise feed)
                {
                    var resolveCallback = onResolved;
                    onResolved = null;
                    var rejectCallback = onRejected;
                    onRejected = null;
                    Action<Deferred> deferredDelegate;
                    if (feed._state == State.Resolved)
                    {
                        rejectCallback.Dispose();
                        _invokingResolved = true;
                        deferredDelegate = resolveCallback.DisposeAndInvoke(feed);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        _invokingRejected = true;
                        if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValueOrPrevious, out deferredDelegate))
                        {
                            // Deferred is never used, so just release.
                            _deferredInternal.ReleaseDirect();
                            RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                            return;
                        }
                    }
                    ValidateReturn(deferredDelegate);
                    try
                    {
                        deferredDelegate.Invoke(_deferredInternal);
                    }
                    catch (Exception e)
                    {
                        _deferredInternal.RejectWithPromiseStacktrace(e);
                    }
                }

                protected override void OnCancel()
                {
                    if (onResolved != null)
                    {
                        // Deferred is never used, so just release.
                        _deferredInternal.ReleaseDirect();
                        onResolved.Dispose();
                        onResolved = null;
                        onRejected.Dispose();
                        onRejected = null;
                    }
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
                    var promise = _pool.IsNotEmpty ? (PromiseComplete0) _pool.Pop() : new PromiseComplete0();
                    promise.onComplete = onComplete;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = onComplete;
                    onComplete = null;
                    _invokingResolved = true;
                    callback.Invoke();
                    ResolveInternalIfNotCanceled();
                }

                protected override void OnCancel()
                {
                    onComplete = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseComplete<T> : PoolablePromise<T, PromiseComplete<T>>
            {
                private Func<T> onComplete;

                private PromiseComplete() { }

                public static PromiseComplete<T> GetOrCreate(Func<T> onComplete, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseComplete<T>) _pool.Pop() : new PromiseComplete<T>();
                    promise.onComplete = onComplete;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = onComplete;
                    onComplete = null;
                    _invokingResolved = true;
                    _value = callback.Invoke();
                    ResolveInternalIfNotCanceled();
                }

                protected override void OnCancel()
                {
                    onComplete = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseCompletePromise0 : PromiseWaitPromise<PromiseCompletePromise0>
            {
                private Func<Promise> onComplete;

                private PromiseCompletePromise0() { }

                public static PromiseCompletePromise0 GetOrCreate(Func<Promise> onComplete, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCompletePromise0) _pool.Pop() : new PromiseCompletePromise0();
                    promise.onComplete = onComplete;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (onComplete == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var callback = onComplete;
                    onComplete = null;
                    _invokingResolved = true;
                    WaitFor(callback.Invoke());
                }

                protected override void OnCancel()
                {
                    onComplete = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseCompletePromise<T> : PromiseWaitPromise<T, PromiseCompletePromise<T>>
            {
                private Func<Promise<T>> onComplete;

                private PromiseCompletePromise() { }

                public static PromiseCompletePromise<T> GetOrCreate(Func<Promise<T>> onComplete, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCompletePromise<T>) _pool.Pop() : new PromiseCompletePromise<T>();
                    promise.onComplete = onComplete;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (onComplete == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var callback = onComplete;
                    onComplete = null;
                    _invokingResolved = true;
                    WaitFor(callback.Invoke());
                }

                protected override void OnCancel()
                {
                    onComplete = null;
                    base.OnCancel();
                }
            }

            public sealed class PromiseCompleteDeferred0 : PromiseWaitDeferred<PromiseCompleteDeferred0>
            {
                Func<Action<Deferred>> onComplete;

                private PromiseCompleteDeferred0() { }

                public static PromiseCompleteDeferred0 GetOrCreate(Func<Action<Deferred>> onComplete, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCompleteDeferred0) _pool.Pop() : new PromiseCompleteDeferred0();
                    promise.onComplete = onComplete;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle()
                {
                    if (onComplete == null)
                    {
                        HandleSelf();
                    }
                    else
                    {
                        base.Handle();
                    }
                }

                protected override void Handle(Promise feed)
                {
                    var callback = onComplete;
                    onComplete = null;
                    _invokingResolved = true;
                    var deferredDelegate = callback.Invoke();
                    ValidateReturn(deferredDelegate);
                    try
                    {
                        deferredDelegate.Invoke(_deferredInternal);
                    }
                    catch (Exception e)
                    {
                        _deferredInternal.RejectWithPromiseStacktrace(e);
                    }
                }

                protected override void OnCancel()
                {
                    if (onComplete != null)
                    {
                        // Deferred is never used, so just release.
                        _deferredInternal.ReleaseDirect();
                    }
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
                    var promise = _pool.IsNotEmpty ? (PromiseCompleteDeferred<T>) _pool.Pop() : new PromiseCompleteDeferred<T>();
                    promise.onComplete = onComplete;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle()
                {
                    if (onComplete == null)
                    {
                        HandleSelf();
                    }
                    else
                    {
                        base.Handle();
                    }
                }

                protected override void Handle(Promise feed)
                {
                    var callback = onComplete;
                    onComplete = null;
                    _invokingResolved = true;
                    var deferredDelegate = callback.Invoke();
                    ValidateReturn(deferredDelegate);
                    try
                    {
                        deferredDelegate.Invoke(_deferredInternal);
                    }
                    catch (Exception e)
                    {
                        _deferredInternal.RejectWithPromiseStacktrace(e);
                    }
                }

                protected override void OnCancel()
                {
                    if (onComplete != null)
                    {
                        // Deferred is never used, so just release.
                        _deferredInternal.ReleaseDirect();
                    }
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

                private Action _onFinally;

                private FinallyDelegate() { }

                static FinallyDelegate()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static FinallyDelegate GetOrCreate(Action onFinally, Promise owner, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (FinallyDelegate) _pool.Pop() : new FinallyDelegate();
                    del._onFinally = onFinally;
                    SetCreatedStacktrace(del, skipFrames + 1);
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
                        SetStacktraceFromCreated(this, unhandledException);
                        AddRejectionToUnhandledStack(unhandledException);
                    }
                }

                void Dispose()
                {
                    _onFinally = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void ITreeHandleable.Handle()
                {
                    InvokeAndCatchAndDispose();
                }

                void ITreeHandleable.Cancel()
                {
                    InvokeAndCatchAndDispose();
                }
            }

            public abstract partial class PotentialCancelation : ITreeHandleable
            {
                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

                public virtual void AssignPrevious(IValueContainerOrPrevious cancelValue) { }

                void ITreeHandleable.Handle()
                {
                    Dispose();
                    DisposeBranches();
                }

                protected virtual void TakeBranches(ref ValueLinkedStack<ITreeHandleable> disposeStack) { }

                public abstract void Cancel();

                protected abstract void Dispose();

                protected void DisposeBranches()
                {
                    var branches = new ValueLinkedStack<ITreeHandleable>();
                    TakeBranches(ref branches);
                    while (branches.IsNotEmpty)
                    {
                        var current = (PotentialCancelation) branches.Pop();
                        current.Dispose();
                        current.TakeBranches(ref branches);
                    }
                }
            }

            public sealed class CancelDelegateAny : PotentialCancelation
            {
                private static ValueLinkedStack<ITreeHandleable> _pool;

                private Action _onCanceled;

                private CancelDelegateAny() { }

                static CancelDelegateAny()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static CancelDelegateAny GetOrCreate(Action onCanceled, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegateAny) _pool.Pop() : new CancelDelegateAny();
                    del._onCanceled = onCanceled;
                    SetCreatedStacktrace(del, skipFrames + 1);
                    return del;
                }

                protected override void Dispose()
                {
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
                        callback.Invoke();
                    }
                    catch (Exception e)
                    {
                        UnhandledExceptionException unhandledException = UnhandledExceptionException.GetOrCreate(e);
                        SetStacktraceFromCreated(this, unhandledException);
                        AddRejectionToUnhandledStack(unhandledException);
                    }
                }
            }

            public sealed class CancelDelegate<T> : PotentialCancelation, IPotentialCancelation, IValueContainerOrPrevious, IValueContainerContainer
            {
                private static ValueLinkedStack<ITreeHandleable> _pool;

                private ValueLinkedQueue<ITreeHandleable> _nextBranches;
                private IValueContainerOrPrevious _valueContainerOrPrevious;
                private Action<T> _onCanceled;
                private uint _retainCounter;

                IValueContainerOrPrevious IValueContainerContainer.ValueContainerOrPrevious { get { return _valueContainerOrPrevious; } }

                private CancelDelegate() { }

                static CancelDelegate()
                {
                    OnClearPool += () => _pool.Clear();
                }

                ~CancelDelegate()
                {
                    if (_retainCounter > 0)
                    {
                        // Delegate wasn't released.
                        var exception = UnhandledExceptionException.GetOrCreate(UnreleasedObjectException.instance);
                        SetStacktraceFromCreated(this, exception);
                        AddRejectionToUnhandledStack(exception);
                    }
                }

                public static CancelDelegate<T> GetOrCreate(Action<T> onCanceled, IValueContainerOrPrevious previous, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegate<T>) _pool.Pop() : new CancelDelegate<T>();
                    del._onCanceled = onCanceled;
                    del._valueContainerOrPrevious = previous;
                    SetCreatedStacktrace(del, skipFrames + 1);
                    del.Retain();
                    return del;
                }

                protected override void TakeBranches(ref ValueLinkedStack<ITreeHandleable> disposeStack)
                {
                    disposeStack.PushAndClear(ref _nextBranches);
                }

                protected override void Dispose()
                {
                    _onCanceled = null;
                    Release();
                }

                public override void Cancel()
                {
                    var callback = _onCanceled;
                    _onCanceled = null;
                    var cancelValue = ((IValueContainerContainer) _valueContainerOrPrevious).ValueContainerOrPrevious;
                    cancelValue.Retain();
                    _valueContainerOrPrevious.Release();
                    T arg;
                    if (cancelValue.TryGetValueAs(out arg))
                    {
                        _valueContainerOrPrevious = null;
                        DisposeBranches();
                        try
                        {
                            callback.Invoke(arg);
                        }
                        catch (Exception e)
                        {
                            UnhandledExceptionException unhandledException = UnhandledExceptionException.GetOrCreate(e);
                            SetStacktraceFromCreated(this, unhandledException);
                            AddRejectionToUnhandledStack(unhandledException);
                        }
                    }
                    else
                    {
                        _valueContainerOrPrevious = cancelValue;
                    }
                    AddToCancelQueueFront(ref _nextBranches);
                    Release();
                }

                void IPotentialCancelation.CatchCancelation(Action onCanceled)
                {
                    ValidatePotentialOperation(_valueContainerOrPrevious, 1);
                    ValidateArgument(onCanceled, "onCanceled", 1);

                    if (_valueContainerOrPrevious != null)
                    {
                        _nextBranches.Enqueue(CancelDelegateAny.GetOrCreate(onCanceled, 1));
                    }
                }

                IPotentialCancelation IPotentialCancelation.CatchCancelation<TCancel>(Action<TCancel> onCanceled)
                {
                    ValidatePotentialOperation(_valueContainerOrPrevious, 1);
                    ValidateArgument(onCanceled, "onCanceled", 1);

                    if (_valueContainerOrPrevious == null)
                    {
                        return this;
                    }
                    var cancelation = CancelDelegate<TCancel>.GetOrCreate(onCanceled, this, 1);
                    _nextBranches.Enqueue(cancelation);
                    Retain();
                    return cancelation;
                }

                public void Retain()
                {
#if DEBUG
                    checked
#endif
                    {
                        ++_retainCounter;
                    }
                }

                public void Release()
                {
#if DEBUG
                    checked
#endif
                    {
                        if (--_retainCounter == 0)
                        {
                            if (_valueContainerOrPrevious != null)
                            {
                                _valueContainerOrPrevious.Release();
                            }
                            SetDisposed(ref _valueContainerOrPrevious);
                            if (Config.ObjectPooling == PoolType.All)
                            {
                                _pool.Push(this);
                            }
                        }
                    }
                }

                // This breaks Interface Segregation Principle, but cuts down on memory.
                bool IValueContainerOrPrevious.TryGetValueAs<U>(out U value) { throw new System.InvalidOperationException(); }
                bool IValueContainerOrPrevious.ContainsType<U>() { throw new System.InvalidOperationException(); }
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

                public bool DisposeAndTryInvoke(IValueContainerOrPrevious valueContainer)
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

                public bool DisposeAndTryInvoke(IValueContainerOrPrevious valueContainer)
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

                public bool DisposeAndTryInvoke(IValueContainerOrPrevious valueContainer)
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
                    DisposeAndInvoke(((PromiseInternal<TArg>) feed).Value);
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

                public bool DisposeAndTryInvoke(IValueContainerOrPrevious valueContainer, out TResult result)
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

                public bool DisposeAndTryInvoke(IValueContainerOrPrevious valueContainer, out TResult result)
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

                public bool DisposeAndTryInvoke(IValueContainerOrPrevious valueContainer, out TResult result)
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
                    return DisposeAndInvoke(((PromiseInternal<TArg>) feed).Value);
                }
            }
            #endregion

            public interface ITreeHandleable : ILinked<ITreeHandleable>
            {
                void Handle();
                void Cancel();
            }

            public interface IValueContainerOrPrevious
            {
                bool TryGetValueAs<U>(out U value);
                bool ContainsType<U>();
                void Retain();
                void Release();
            }

            public interface IValueContainerContainer : IValueContainerOrPrevious
            {
                IValueContainerOrPrevious ValueContainerOrPrevious { get; }
            }

            public interface IDelegate
            {
                bool DisposeAndTryInvoke(IValueContainerOrPrevious valueContainer);
                void DisposeAndInvoke(Promise feed);
                void Dispose();
            }

            public interface IDelegate<TResult>
            {
                bool DisposeAndTryInvoke(IValueContainerOrPrevious valueContainer, out TResult result);
                TResult DisposeAndInvoke(Promise feed);
                void Dispose();
            }

            #region Multi Promises
            public partial interface IMultiTreeHandleable : ITreeHandleable
            {
                void Handle(Promise feed, int index);
                void Cancel(IValueContainerOrPrevious cancelValue);
                void ReAdd(PromisePassThrough passThrough);
            }

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

                private uint _retainCounter;
                private int _index;

                public static PromisePassThrough GetOrCreate(Promise owner, IMultiTreeHandleable target, int index)
                {
                    var passThrough = _pool.IsNotEmpty ? _pool.Pop() : new PromisePassThrough();
                    passThrough.Owner = owner;
                    passThrough.Target = target;
                    passThrough._index = index;
                    passThrough._retainCounter = 2u;
                    owner.AddWaiter(passThrough);
                    return passThrough;
                }

                private PromisePassThrough() { }

                void ITreeHandleable.Cancel()
                {
                    var temp = Target;
                    var cancelValue = Owner._rejectedOrCanceledValueOrPrevious;
                    temp.Cancel(cancelValue);
                    Release();
                }

                void ITreeHandleable.Handle()
                {
                    var temp = Target;
                    var feed = Owner;
                    Reset();
                    temp.Handle(feed, _index);
                    feed.ReleaseInternal();
                    Release();
                }

                private void Reset()
                {
                    Owner = null;
                    Target = null;
                }

                public void Retain()
                {
                    ++_retainCounter;
                }

                public void Release()
                {
                    if (--_retainCounter == 0)
                    {
                        if (Owner != null)
                        {
                            Owner.ReleaseInternal();
                        }
                        Reset();
                        _pool.Push(this);
                    }
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
                    ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                    ValidateOperation(target, skipFrames + 1);
                    int promiseIndex = 0;
                    // Hook up pass throughs
                    var passThroughs = new ValueLinkedStack<PromisePassThrough>(PromisePassThrough.GetOrCreate(target, promise, promiseIndex));
                    while (promises.MoveNext())
                    {
                        target = promises.Current;
                        ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                        ValidateOperation(target, skipFrames + 1);
                        passThroughs.Push(PromisePassThrough.GetOrCreate(target, promise, ++promiseIndex));
                    }
                    promise.passThroughs = passThroughs;

                    promise._waitCount = (uint) promiseIndex + 1u;
                    // Retain this until all promises resolve/reject/cancel
                    promise.Reset(skipFrames + 1);
                    promise._retainCounter = promise._waitCount + 1u;

                    return promise;
                }

                private bool ReleaseOne()
                {
                    ReleaseWithoutDisposeCheck();
                    return --_waitCount == 0;
                }

                private void MaybeRelease(bool done)
                {
                    if (done)
                    {
                        while (passThroughs.IsNotEmpty)
                        {
                            passThroughs.Pop().Release();
                        }
                        ReleaseInternal();
                    }
                }

                void IMultiTreeHandleable.Cancel(IValueContainerOrPrevious cancelValue)
                {
                    if (_state == State.Pending)
                    {
                        CancelInternal(cancelValue);
                    }
                    MaybeRelease(ReleaseOne());
                }

                void IMultiTreeHandleable.Handle(Promise feed, int index)
                {
                    bool done = ReleaseOne();
                    if (_state == State.Pending)
                    {
                        feed._wasWaitedOn = true;
                        if (feed._state == State.Rejected)
                        {
                            RejectInternalWithoutRelease(feed._rejectedOrCanceledValueOrPrevious);
                        }
                        else if (done)
                        {
                            ResolveInternalWithoutRelease();
                        }
                        else
                        {
                            IncrementProgress(feed);
                        }
                    }
                    MaybeRelease(done);
                }

                partial void IncrementProgress(Promise feed);

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    passThroughs.Push(passThrough);
                }

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                }
            }

            public sealed partial class AllPromise<T> : PoolablePromise<IList<T>, AllPromise<T>>, IMultiTreeHandleable
            {
                private ValueLinkedStack<PromisePassThrough> passThroughs;
                private uint _waitCount;

                private AllPromise() { }

                public static Promise<IList<T>> GetOrCreate<TEnumerator>(TEnumerator promises, IList<T> valueContainer, int skipFrames) where TEnumerator : IEnumerator<Promise<T>>
                {
                    if (!promises.MoveNext())
                    {
                        // If promises is empty, just return a resolved promise.
                        if (valueContainer.Count > 0) // Count check in case valueContainer is an array .
                        {
                            valueContainer.Clear();
                        }
                        return Resolved(valueContainer);
                    }

                    var promise = _pool.IsNotEmpty ? (AllPromise<T>) _pool.Pop() : new AllPromise<T>();
                    promise._value = valueContainer;

                    var target = promises.Current;
                    ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                    ValidateOperation(target, skipFrames + 1);
                    int promiseIndex = 0;
                    // Hook up pass throughs and make sure the list has space for the values.
                    var passThroughs = new ValueLinkedStack<PromisePassThrough>(PromisePassThrough.GetOrCreate(target, promise, promiseIndex));
                    while (promises.MoveNext())
                    {
                        target = promises.Current;
                        ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                        ValidateOperation(target, skipFrames + 1);
                        passThroughs.Push(PromisePassThrough.GetOrCreate(target, promise, ++promiseIndex));
                    }
                    int length = promiseIndex + 1;
                    promise.passThroughs = passThroughs;

                    // Only change the count of the valueContainer if it's greater or less than the promises count. This allows arrays to be used if they are the proper length.
                    int i = valueContainer.Count;
                    if (i < length)
                    {
                        do
                        {
                            valueContainer.Add(default(T));
                        } while (++i < length);
                    }
                    else while (i > length)
                        {
                            valueContainer.RemoveAt(--i);
                        }

                    promise._waitCount = (uint) length;
                    // Retain this until all promises resolve/reject/cancel
                    promise.Reset(skipFrames + 1);
                    promise._retainCounter = promise._waitCount + 1u;

                    return promise;
                }

                private bool ReleaseOne()
                {
                    ReleaseWithoutDisposeCheck();
                    return --_waitCount == 0;
                }

                private void MaybeRelease(bool done)
                {
                    if (done)
                    {
                        while (passThroughs.IsNotEmpty)
                        {
                            passThroughs.Pop().Release();
                        }
                        ReleaseInternal();
                    }
                }

                void IMultiTreeHandleable.Cancel(IValueContainerOrPrevious cancelValue)
                {
                    if (_state == State.Pending)
                    {
                        CancelInternal(cancelValue);
                    }
                    MaybeRelease(ReleaseOne());
                }

                void IMultiTreeHandleable.Handle(Promise feed, int index)
                {
                    bool done = ReleaseOne();
                    if (_state == State.Pending)
                    {
                        feed._wasWaitedOn = true;
                        if (feed._state == State.Rejected)
                        {
                            RejectInternalWithoutRelease(feed._rejectedOrCanceledValueOrPrevious);
                        }
                        else
                        {
                            _value[index] = ((PromiseInternal<T>) feed).Value;
                            if (done)
                            {
                                ResolveInternalWithoutRelease();
                            }
                            else
                            {
                                IncrementProgress(feed);
                            }
                        }
                    }
                    MaybeRelease(done);
                }

                partial void IncrementProgress(Promise feed);

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    passThroughs.Push(passThrough);
                }

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                }
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
                        throw new EmptyArgumentException("promises", "You must provide at least one element to Race.", GetFormattedStacktrace(skipFrames + 1));
                    }

                    var promise = _pool.IsNotEmpty ? (RacePromise0) _pool.Pop() : new RacePromise0();

                    var target = promises.Current;
                    ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                    ValidateOperation(target, skipFrames + 1);
                    int promiseIndex = 0;
                    // Hook up pass throughs
                    var passThroughs = new ValueLinkedStack<PromisePassThrough>(PromisePassThrough.GetOrCreate(target, promise, promiseIndex));
                    while (promises.MoveNext())
                    {
                        target = promises.Current;
                        ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                        ValidateOperation(target, skipFrames + 1);
                        passThroughs.Push(PromisePassThrough.GetOrCreate(target, promise, ++promiseIndex));
                    }
                    promise.passThroughs = passThroughs;

                    promise._waitCount = (uint) promiseIndex + 1u;
                    // Retain this until all promises resolve/reject/cancel
                    promise.Reset(skipFrames + 1);
                    promise._retainCounter = promise._waitCount + 1u;

                    return promise;
                }

                private bool ReleaseOne()
                {
                    ReleaseWithoutDisposeCheck();
                    return --_waitCount == 0;
                }

                private void MaybeRelease(bool done)
                {
                    if (done)
                    {
                        while (passThroughs.IsNotEmpty)
                        {
                            passThroughs.Pop().Release();
                        }
                        ReleaseInternal();
                    }
                }

                void IMultiTreeHandleable.Cancel(IValueContainerOrPrevious cancelValue)
                {
                    if (_state == State.Pending)
                    {
                        CancelInternal(cancelValue);
                    }
                    MaybeRelease(ReleaseOne());
                }

                void IMultiTreeHandleable.Handle(Promise feed, int index)
                {
                    if (_state == State.Pending)
                    {
                        feed._wasWaitedOn = true;
                        HandleSelfWithoutRelease(feed);
                    }
                    MaybeRelease(ReleaseOne());
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    passThroughs.Push(passThrough);
                }

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                }
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
                        throw new EmptyArgumentException("promises", "You must provide at least one element to Race.", GetFormattedStacktrace(skipFrames + 1));
                    }

                    var promise = _pool.IsNotEmpty ? (RacePromise<T>) _pool.Pop() : new RacePromise<T>();

                    var target = promises.Current;
                    ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                    ValidateOperation(target, skipFrames + 1);
                    int promiseIndex = 0;
                    // Hook up pass throughs
                    var passThroughs = new ValueLinkedStack<PromisePassThrough>(PromisePassThrough.GetOrCreate(target, promise, promiseIndex));
                    while (promises.MoveNext())
                    {
                        target = promises.Current;
                        ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                        ValidateOperation(target, skipFrames + 1);
                        passThroughs.Push(PromisePassThrough.GetOrCreate(target, promise, ++promiseIndex));
                    }
                    promise.passThroughs = passThroughs;

                    promise._waitCount = (uint) promiseIndex + 1u;
                    // Retain this until all promises resolve/reject/cancel
                    promise.Reset(skipFrames + 1);
                    promise._retainCounter = promise._waitCount + 1u;

                    return promise;
                }

                private bool ReleaseOne()
                {
                    ReleaseWithoutDisposeCheck();
                    return --_waitCount == 0;
                }

                private void MaybeRelease(bool done)
                {
                    if (done)
                    {
                        while (passThroughs.IsNotEmpty)
                        {
                            passThroughs.Pop().Release();
                        }
                        ReleaseInternal();
                    }
                }

                void IMultiTreeHandleable.Cancel(IValueContainerOrPrevious cancelValue)
                {
                    if (_state == State.Pending)
                    {
                        CancelInternal(cancelValue);
                    }
                    MaybeRelease(ReleaseOne());
                }

                void IMultiTreeHandleable.Handle(Promise feed, int index)
                {
                    if (_state == State.Pending)
                    {
                        feed._wasWaitedOn = true;
                        _value = ((PromiseInternal<T>) feed).Value;
                        HandleSelfWithoutRelease(feed);
                    }
                    MaybeRelease(ReleaseOne());
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    passThroughs.Push(passThrough);
                }

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                }
            }

            public sealed partial class SequencePromise0 : PromiseWaitPromise<SequencePromise0>
            {
                static partial void GetFirstPromise(ref Promise promise, int skipFrames);

                public static Promise GetOrCreate<TEnumerator>(TEnumerator promiseFuncs, int skipFrames) where TEnumerator : IEnumerator<Func<Promise>>
                {
                    if (!promiseFuncs.MoveNext())
                    {
                        // If promiseFuncs is empty, just return a resolved promise.
                        return Resolved();
                    }

                    Promise promise = promiseFuncs.Current.Invoke();
                    GetFirstPromise(ref promise, skipFrames + 1);

                    while (promiseFuncs.MoveNext())
                    {
                        promise = promise.Then(promiseFuncs.Current);
                    }
#if PROMISE_CANCEL
                    return promise.ThenDuplicate(); // Prevents canceling only the very last callback.
#else
                    return promise;
#endif
                }

                protected override void Handle(Promise feed)
                {
                    HandleSelf(feed);
                }
            }

            public sealed partial class FirstPromise0 : PoolablePromise<FirstPromise0>, IMultiTreeHandleable
            {
                private ValueLinkedStack<PromisePassThrough> passThroughs;
                private uint _waitCount;

                private FirstPromise0() { }

                public static Promise GetOrCreate<TEnumerator>(TEnumerator promises, int skipFrames) where TEnumerator : IEnumerator<Promise>
                {
                    if (!promises.MoveNext())
                    {
                        throw new EmptyArgumentException("promises", "You must provide at least one element to First.", GetFormattedStacktrace(skipFrames + 1));
                    }

                    var promise = _pool.IsNotEmpty ? (FirstPromise0) _pool.Pop() : new FirstPromise0();

                    var target = promises.Current;
                    ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                    ValidateOperation(target, skipFrames + 1);
                    int promiseIndex = 0;
                    // Hook up pass throughs
                    var passThroughs = new ValueLinkedStack<PromisePassThrough>(PromisePassThrough.GetOrCreate(target, promise, promiseIndex));
                    while (promises.MoveNext())
                    {
                        target = promises.Current;
                        ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                        ValidateOperation(target, skipFrames + 1);
                        passThroughs.Push(PromisePassThrough.GetOrCreate(target, promise, ++promiseIndex));
                    }
                    promise.passThroughs = passThroughs;

                    promise._waitCount = (uint) promiseIndex + 1u;
                    // Retain this until all promises resolve/reject/cancel
                    promise.Reset(skipFrames + 1);
                    promise._retainCounter = promise._waitCount + 1u;

                    return promise;
                }

                private bool ReleaseOne()
                {
                    ReleaseWithoutDisposeCheck();
                    return --_waitCount == 0;
                }

                private void MaybeRelease(bool done)
                {
                    if (done)
                    {
                        while (passThroughs.IsNotEmpty)
                        {
                            passThroughs.Pop().Release();
                        }
                        ReleaseInternal();
                    }
                }

                void IMultiTreeHandleable.Cancel(IValueContainerOrPrevious cancelValue)
                {
                    bool done = ReleaseOne();
                    if (_state == State.Pending & done)
                    {
                        CancelInternal(cancelValue);
                    }
                    MaybeRelease(done);
                }

                void IMultiTreeHandleable.Handle(Promise feed, int index)
                {
                    bool done = ReleaseOne();
                    if (_state == State.Pending)
                    {
                        if (feed._state == State.Resolved)
                        {
                            feed._wasWaitedOn = true;
                            ResolveInternalWithoutRelease();
                        }
                        else if (done)
                        {
                            feed._wasWaitedOn = true;
                            HandleSelfWithoutRelease(feed);
                        }
                    }
                    MaybeRelease(done);
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    passThroughs.Push(passThrough);
                }

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                }
            }

            public sealed partial class FirstPromise<T> : PoolablePromise<T, FirstPromise<T>>, IMultiTreeHandleable
            {
                private ValueLinkedStack<PromisePassThrough> passThroughs;
                private uint _waitCount;

                private FirstPromise() { }

                public static Promise<T> GetOrCreate<TEnumerator>(TEnumerator promises, int skipFrames) where TEnumerator : IEnumerator<Promise<T>>
                {
                    if (!promises.MoveNext())
                    {
                        throw new EmptyArgumentException("promises", "You must provide at least one element to First.", GetFormattedStacktrace(skipFrames + 1));
                    }

                    var promise = _pool.IsNotEmpty ? (FirstPromise<T>) _pool.Pop() : new FirstPromise<T>();

                    var target = promises.Current;
                    ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                    ValidateOperation(target, skipFrames + 1);
                    int promiseIndex = 0;
                    // Hook up pass throughs
                    var passThroughs = new ValueLinkedStack<PromisePassThrough>(PromisePassThrough.GetOrCreate(target, promise, promiseIndex));
                    while (promises.MoveNext())
                    {
                        target = promises.Current;
                        ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                        ValidateOperation(target, skipFrames + 1);
                        passThroughs.Push(PromisePassThrough.GetOrCreate(target, promise, ++promiseIndex));
                    }
                    promise.passThroughs = passThroughs;

                    promise._waitCount = (uint) promiseIndex + 1u;
                    // Retain this until all promises resolve/reject/cancel
                    promise.Reset(skipFrames + 1);
                    promise._retainCounter = promise._waitCount + 1u;

                    return promise;
                }

                private bool ReleaseOne()
                {
                    ReleaseWithoutDisposeCheck();
                    return --_waitCount == 0;
                }

                private void MaybeRelease(bool done)
                {
                    if (done)
                    {
                        while (passThroughs.IsNotEmpty)
                        {
                            passThroughs.Pop().Release();
                        }
                        ReleaseInternal();
                    }
                }

                void IMultiTreeHandleable.Cancel(IValueContainerOrPrevious cancelValue)
                {
                    bool done = ReleaseOne();
                    if (_state == State.Pending & done)
                    {
                        CancelInternal(cancelValue);
                    }
                    MaybeRelease(done);
                }

                void IMultiTreeHandleable.Handle(Promise feed, int index)
                {
                    bool done = ReleaseOne();
                    if (_state == State.Pending)
                    {
                        if (feed._state == State.Resolved)
                        {
                            feed._wasWaitedOn = true;
                            _value = ((PromiseInternal<T>) feed).Value;
                            ResolveInternalWithoutRelease();
                        }
                        else if (done)
                        {
                            feed._wasWaitedOn = true;
                            HandleSelfWithoutRelease(feed);
                        }
                    }
                    MaybeRelease(done);
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    passThroughs.Push(passThrough);
                }

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                }
            }
            #endregion
        }
    }
}