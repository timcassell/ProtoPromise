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

        protected virtual void ResolveInternal(Promise feed)
        {
            ResolveInternal();
        }

        protected static Internal.UnhandledExceptionInternal CreateRejection<TReject>(TReject reason, int skipFrames)
        {
            Internal.UnhandledExceptionInternal rejectValue;
            // Avoid boxing value types.
            Type type = typeof(TReject);
#if CSHARP_7_OR_LATER
            if (type.IsClass && ((object) reason) is Exception e)
            {
                // reason is a non-null Exception.
                rejectValue = Internal.UnhandledExceptionException.GetOrCreate(e);
            }
#else
            if (type.IsClass && reason is Exception)
            {
                // reason is a non-null Exception.
                rejectValue = Internal.UnhandledExceptionException.GetOrCreate(reason as Exception);
            }
#endif
            else if (typeof(Exception).IsAssignableFrom(type))
            {
                // reason is a null Exception, behave the same way .Net behaves if you throw null.
                rejectValue = Internal.UnhandledExceptionException.GetOrCreate(new NullReferenceException());
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
            catch (OperationCanceledException) // Built-in system cancelation (or Task cancelation)
            {
                if (_state == State.Pending)
                {
                    _state = State.Canceled;
                    _rejectedOrCanceledValueOrPrevious = Internal.CancelVoid.GetOrCreate();
                    _rejectedOrCanceledValueOrPrevious.Retain();
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
            // If this is canceled while the callback is being invoked, previous could be null.
            if (_rejectedOrCanceledValueOrPrevious != null)
            {
                _rejectedOrCanceledValueOrPrevious.Release();
                _rejectedOrCanceledValueOrPrevious = null;
            }
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

            public abstract class PromiseInternal<T> : Promise<T>
            {
                public T _value;

                protected override void Dispose()
                {
                    _value = default(T);
                    base.Dispose();
                }

                new protected void HandleSelf(Promise feed)
                {
                    _value = ((PromiseInternal<T>) feed)._value;
                    base.HandleSelf(feed);
                }

                protected override sealed void ResolveInternal(Promise feed)
                {
                    _value = ((PromiseInternal<T>) feed)._value;
                    base.ResolveInternal(feed);
                }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
                public void ResolveDirectIfNotCanceled(in T value)
#else
                public void ResolveDirectIfNotCanceled(T value)
#endif
                {
#if PROMISE_CANCEL
                    if (_state == State.Canceled)
                    {
                        ReleaseInternal();
                    }
                    else
#endif
                    {
                        _state = State.Resolved;
                        _value = value;
                        AddToHandleQueueBack(this);
                    }
                }
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
                        _rejectedOrCanceledValueOrPrevious = other;
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

            public abstract partial class PromiseWaitPromise<T, TPromise> : PoolablePromise<T, TPromise> where TPromise : PromiseWaitPromise<T, TPromise>
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
                        _rejectedOrCanceledValueOrPrevious = other;
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

                protected override void OnHandleCatch()
                {
                    deferred.ReleaseDirect();
                }

                protected override void Handle()
                {
                    // If this was rejected, the progress listeners were already removed, so we don't need an extra branch here.
                    ResolveProgressListeners();
                    AddToHandleQueueFront(ref _nextBranches);
                    ReleaseInternal();
                }

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                    AddToHandleQueueBack(this);
                }
            }

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

                protected override void OnHandleCatch()
                {
                    deferred.ReleaseDirect();
                }

                protected override void Handle()
                {
                    // If this was rejected, the progress listeners were already removed, so we don't need an extra branch here.
                    ResolveProgressListeners();
                    AddToHandleQueueFront(ref _nextBranches);
                    ReleaseInternal();
                }

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                    AddToHandleQueueBack(this);
                }
            }

            public sealed class SettledPromise : Promise
            {
                private SettledPromise() { }

                private static readonly SettledPromise _resolved = new SettledPromise() { _state = State.Resolved };

                public static SettledPromise GetOrCreateResolved()
                {
                    return _resolved;
                }

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

#if PROMISE_DEBUG
                public void ResolveDirect()
                {
                    _state = State.Resolved;
                    AddToHandleQueueBack(this);
                }
#endif

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
                private Action _onResolved;

                private PromiseVoidResolve0() { }

                public static PromiseVoidResolve0 GetOrCreate(Action onResolved, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolve0) _pool.Pop() : new PromiseVoidResolve0();
                    promise._onResolved = onResolved;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = _onResolved;
                    _onResolved = null;
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
                    _onResolved = null;
                    base.OnCancel();
                }
            }

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

                protected override void Handle(Promise feed)
                {
                    var callback = _onResolved;
                    _onResolved = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        callback.Invoke(((PromiseInternal<TArg>) feed)._value);
                        ResolveInternalIfNotCanceled();
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    _onResolved = null;
                    base.OnCancel();
                }
            }

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

                protected override void Handle(Promise feed)
                {
                    var callback = _onResolved;
                    _onResolved = null;
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
                    _onResolved = null;
                    base.OnCancel();
                }
            }

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

                protected override void Handle(Promise feed)
                {
                    var callback = _onResolved;
                    _onResolved = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        _value = callback.Invoke(((PromiseInternal<TArg>) feed)._value);
                        ResolveInternalIfNotCanceled();
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    _onResolved = null;
                    base.OnCancel();
                }
            }

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

                protected override void Handle(Promise feed)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var callback = _onResolved;
                    _onResolved = null;
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
                    _onResolved = null;
                    base.OnCancel();
                }
            }

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

                protected override void Handle(Promise feed)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var callback = _onResolved;
                    _onResolved = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke(((PromiseInternal<TArg>) feed)._value));
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    _onResolved = null;
                    base.OnCancel();
                }
            }

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

                protected override void Handle(Promise feed)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var callback = _onResolved;
                    _onResolved = null;
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
                    _onResolved = null;
                    base.OnCancel();
                }
            }

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

                protected override void Handle(Promise feed)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var callback = _onResolved;
                    _onResolved = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke(((PromiseInternal<TArg>) feed)._value));
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    _onResolved = null;
                    base.OnCancel();
                }
            }


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

                protected override void Handle(Promise feed)
                {
                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        callback.Invoke(value);
                        ResolveInternalIfNotCanceled();
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    resolveHandler = null;
                    base.OnCancel();
                }
            }

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

                protected override void Handle(Promise feed)
                {
                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        callback.Invoke(value, ((PromiseInternal<TArg>) feed)._value);
                        ResolveInternalIfNotCanceled();
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    _onResolved = null;
                    base.OnCancel();
                }
            }

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

                protected override void Handle(Promise feed)
                {
                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        _value = callback.Invoke(value);
                        ResolveInternalIfNotCanceled();
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    _onResolved = null;
                    base.OnCancel();
                }
            }

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

                protected override void Handle(Promise feed)
                {
                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        _value = callback.Invoke(value, ((PromiseInternal<TArg>) feed)._value);
                        ResolveInternalIfNotCanceled();
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    _onResolved = null;
                    base.OnCancel();
                }
            }

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

                protected override void Handle(Promise feed)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke(value));
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    _onResolved = null;
                    base.OnCancel();
                }
            }

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

                protected override void Handle(Promise feed)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke(value, ((PromiseInternal<TArg>) feed)._value));
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    _onResolved = null;
                    base.OnCancel();
                }
            }

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

                protected override void Handle(Promise feed)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke(value));
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    _onResolved = null;
                    base.OnCancel();
                }
            }

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

                protected override void Handle(Promise feed)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onResolved;
                    _onResolved = null;
                    if (feed._state == State.Resolved)
                    {
                        _invokingResolved = true;
                        WaitFor(callback.Invoke(value, ((PromiseInternal<TArg>) feed)._value));
                    }
                    else
                    {
                        RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                    }
                }

                protected override void OnCancel()
                {
                    _capturedValue = default(TCapture);
                    _onResolved = null;
                    base.OnCancel();
                }
            }
            #endregion

            #region Resolve or Reject Promises
            // IDelegate to reduce the amount of classes I would have to write to handle catches (Composition Over Inheritance).
            // I'm less concerned about performance for catches since exceptions are expensive anyway, and they are expected to be used less often than .Then(onResolved).
            public sealed class PromiseResolveReject0 : PoolablePromise<PromiseResolveReject0>
            {
                private IDelegateResolve _onResolved;
                private IDelegateReject _onRejected;

                private PromiseResolveReject0() { }

                public static PromiseResolveReject0 GetOrCreate(IDelegateResolve onResolved, IDelegateReject onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveReject0) _pool.Pop() : new PromiseResolveReject0();
                    onResolved.Retain();
                    onRejected.Retain();
                    promise._onResolved = onResolved;
                    promise._onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var resolveCallback = _onResolved;
                    _onResolved = null;
                    var rejectCallback = _onRejected;
                    _onRejected = null;
                    if (feed._state == State.Resolved)
                    {
                        rejectCallback.Release();
                        _invokingResolved = true;
                        resolveCallback.ReleaseAndInvoke(feed, this);
                    }
                    else
                    {
                        resolveCallback.Release();
                        _invokingRejected = true;
                        rejectCallback.ReleaseAndInvoke(feed, this);
                    }
                }

                protected override void OnCancel()
                {
                    if (_onResolved != null)
                    {
                        _onResolved.Release();
                        _onResolved = null;
                        _onRejected.Release();
                        _onRejected = null;
                    }
                    base.OnCancel();
                }
            }

            public sealed class PromiseResolveReject<T> : PoolablePromise<T, PromiseResolveReject<T>>
            {
                private IDelegateResolve _onResolved;
                private IDelegateReject _onRejected;

                private PromiseResolveReject() { }

                public static PromiseResolveReject<T> GetOrCreate(IDelegateResolve onResolved, IDelegateReject onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveReject<T>) _pool.Pop() : new PromiseResolveReject<T>();
                    onResolved.Retain();
                    onRejected.Retain();
                    promise._onResolved = onResolved;
                    promise._onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var resolveCallback = _onResolved;
                    _onResolved = null;
                    var rejectCallback = _onRejected;
                    _onRejected = null;
                    if (feed._state == State.Resolved)
                    {
                        rejectCallback.Release();
                        _invokingResolved = true;
                        resolveCallback.ReleaseAndInvoke(feed, this);
                    }
                    else
                    {
                        resolveCallback.Release();
                        _invokingRejected = true;
                        rejectCallback.ReleaseAndInvoke(feed, this);
                    }
                }

                protected override void OnCancel()
                {
                    if (_onResolved != null)
                    {
                        _onResolved.Release();
                        _onResolved = null;
                        _onRejected.Release();
                        _onRejected = null;
                    }
                    base.OnCancel();
                }
            }

            public sealed class PromiseResolveRejectPromise0 : PromiseWaitPromise<PromiseResolveRejectPromise0>
            {
                private IDelegateResolvePromise _onResolved;
                private IDelegateRejectPromise _onRejected;

                private PromiseResolveRejectPromise0() { }

                public static PromiseResolveRejectPromise0 GetOrCreate(IDelegateResolvePromise onResolved, IDelegateRejectPromise onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectPromise0) _pool.Pop() : new PromiseResolveRejectPromise0();
                    onResolved.Retain();
                    onRejected.Retain();
                    promise._onResolved = onResolved;
                    promise._onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var resolveCallback = _onResolved;
                    _onResolved = null;
                    var rejectCallback = _onRejected;
                    _onRejected = null;
                    if (feed._state == State.Resolved)
                    {
                        rejectCallback.Release();
                        _invokingResolved = true;
                        resolveCallback.ReleaseAndInvoke(feed, this);
                    }
                    else
                    {
                        resolveCallback.Release();
                        _invokingRejected = true;
#if PROMISE_PROGRESS
                        _suspended = true;
#endif
                        rejectCallback.ReleaseAndInvoke(feed, this);
                    }
                }

                protected override void OnCancel()
                {
                    if (_onResolved != null)
                    {
                        _onResolved.Release();
                        _onResolved = null;
                        _onRejected.Release();
                        _onRejected = null;
                    }
                    base.OnCancel();
                }
            }

            public sealed class PromiseResolveRejectPromise<TPromise> : PromiseWaitPromise<TPromise, PromiseResolveRejectPromise<TPromise>>
            {
                private IDelegateResolvePromise _onResolved;
                private IDelegateRejectPromise _onRejected;

                private PromiseResolveRejectPromise() { }

                public static PromiseResolveRejectPromise<TPromise> GetOrCreate(IDelegateResolvePromise onResolved, IDelegateRejectPromise onRejected, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectPromise<TPromise>) _pool.Pop() : new PromiseResolveRejectPromise<TPromise>();
                    onResolved.Retain();
                    onRejected.Retain();
                    promise._onResolved = onResolved;
                    promise._onRejected = onRejected;
                    promise.Reset(skipFrames + 1);
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (_onResolved == null)
                    {
                        // The returned promise is handling this.
                        HandleSelf(feed);
                        return;
                    }

                    var resolveCallback = _onResolved;
                    _onResolved = null;
                    var rejectCallback = _onRejected;
                    _onRejected = null;
                    if (feed._state == State.Resolved)
                    {
                        rejectCallback.Release();
                        _invokingResolved = true;
                        resolveCallback.ReleaseAndInvoke(feed, this);
                    }
                    else
                    {
                        resolveCallback.Release();
                        _invokingRejected = true;
#if PROMISE_PROGRESS
                        _suspended = true;
#endif
                        rejectCallback.ReleaseAndInvoke(feed, this);
                    }
                }

                protected override void OnCancel()
                {
                    if (_onResolved != null)
                    {
                        _onResolved.Release();
                        _onResolved = null;
                        _onRejected.Release();
                        _onRejected = null;
                    }
                    base.OnCancel();
                }
            }
            #endregion

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
                public IMultiTreeHandleable target;

                private uint _retainCounter;
                private int _index;

                public static PromisePassThrough GetOrCreate(Promise owner, int index, int skipFrames)
                {
                    ValidateElementNotNull(owner, "promises", "A promise was null", skipFrames + 1);
                    ValidateOperation(owner, skipFrames + 1);

                    var passThrough = _pool.IsNotEmpty ? _pool.Pop() : new PromisePassThrough();
                    passThrough.Owner = owner;
                    passThrough._index = index;
                    passThrough._retainCounter = 2u;
                    owner.AddWaiter(passThrough);
                    return passThrough;
                }

                private PromisePassThrough() { }

                void ITreeHandleable.Cancel()
                {
                    var temp = target;
                    var cancelValue = Owner._rejectedOrCanceledValueOrPrevious;
                    temp.Cancel(cancelValue);
                    Release();
                }

                void ITreeHandleable.Handle()
                {
                    var temp = target;
                    var feed = Owner;
                    Reset();
                    temp.Handle(feed, _index);
                    feed.ReleaseInternal();
                    Release();
                }

                private void Reset()
                {
                    Owner = null;
                    target = null;
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