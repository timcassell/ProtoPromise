#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
using System;

namespace ProtoPromise
{
	partial class Promise : Promise.Internal.ITreeHandleAble
    {
        Internal.ITreeHandleAble ILinked<Internal.ITreeHandleAble>.Next { get; set; }

		private ValueLinkedQueue<Internal.ITreeHandleAble> _nextBranches;
		protected Internal.IValueContainer _rejectedOrCanceledValue;
        private uint _retainCounter;
        protected PromiseState _state;
        private bool _wasWaitedOn; // Tells the handler that another promise waited on this promise (either by .Then/.Catch from this promise, or by returning this promise in another promise's .Then/.Catch)
        private bool _notHandling = true; // Is not already being handled in ContinueHandling or ContinueCanceling.
        protected bool _dontPool;

        protected virtual U GetValue<U>()
        {
            throw new InvalidOperationException();
        }

        private void Reset(int skipFrames)
        {
            _state = PromiseState.Pending;
            _dontPool = Config.ObjectPooling != PoolType.All;
            SetNotDisposed();
            SetCreatedStackTrace(skipFrames + 1);
        }

        protected virtual void Dispose()
		{
			if (_rejectedOrCanceledValue != null)
			{
				_rejectedOrCanceledValue.Release();
            }
            SetDisposed();
		}

		protected virtual Promise GetDuplicate()
		{
			return Internal.LitePromise.GetOrCreate();
		}

		protected void Resolve()
		{
			_state = PromiseState.Resolved;
            AddToHandleQueue(this);
		}

		protected void Reject(int skipFrames)
		{
			Internal.UnhandledExceptionVoid rejectValue = Internal.UnhandledExceptionVoid.GetOrCreate();
            SetRejectStackTrace(rejectValue, skipFrames + 1);
            Reject(rejectValue);
		}

		protected void Reject<TReject>(TReject reason, int skipFrames)
		{
			// Is TReject an exception (including if it's null)?
			if (typeof(Exception).IsAssignableFrom(typeof(TReject)))
			{
				// Behave the same way .Net behaves if you throw null.
				var rejectValue = Internal.UnhandledExceptionException.GetOrCreate(reason as Exception ?? new NullReferenceException());
                SetRejectStackTrace(rejectValue, skipFrames + 1);
                Reject(rejectValue);
			}
			else
			{
				var rejectValue = Internal.UnhandledException<TReject>.GetOrCreate(reason);
                SetRejectStackTrace(rejectValue, skipFrames + 1);
                Reject(rejectValue);
			}
        }

        protected void Reject(Internal.IValueContainer rejectValue)
        {
            _state = PromiseState.Rejected;
            rejectValue.Retain();
            _rejectedOrCanceledValue = rejectValue;
            AddToHandleQueue(this);
        }

        protected void HookupNewPromise(Promise newPromise)
		{
            SetDepthAndPrevious(newPromise);
            AddWaiter(newPromise);
		}

		private void AddWaiter(Internal.ITreeHandleAble waiter)
		{
            if ((_state == PromiseState.Resolved | _state == PromiseState.Rejected) & _notHandling)
            {
                // Continue handling if this is resolved or rejected and it's not already being handled.
                _nextBranches.AddLast(waiter);
                AddToHandleQueue(this);
            }
            else if (_state == PromiseState.Canceled)
            {
                waiter.AssignCancelValue(_rejectedOrCanceledValue);
                AddToCancelQueue(waiter);
                ContinueCanceling();
            }
            else
            {
                _nextBranches.AddLast(waiter);
            }
        }

        protected virtual void Handle(Promise feed)
        {
            // TODO: Report progress 1.0. Ignore 1.0 progress reports from deferred.reportprogress.
            _state = feed._state;
            _rejectedOrCanceledValue = feed._rejectedOrCanceledValue;
            AddToHandleQueue(this);
        }

        void Internal.ITreeHandleAble.Handle(Promise feed)
        {
            if (_state == PromiseState.Canceled)
            {
                // Place in the handle queue so it can be repooled.
                AddToHandleQueue(this);
                return;
            }
            feed._wasWaitedOn = true;
            ClearPrevious();
            try
            {
                Handle(feed);
            }
            catch (Exception e)
            {
                var ex = Internal.UnhandledExceptionException.GetOrCreate(e);
                SetStackTraceFromCreated(ex);
                Reject(ex);
            }
        }

        protected virtual void OnCancel()
        {
            if (_nextBranches.IsEmpty)
            {
                return;
            }

            // Add safe for first item.
            var next = _nextBranches.TakeFirst();
            next.AssignCancelValue(_rejectedOrCanceledValue);
            AddToCancelQueue(next);

            // Add risky for remaining items since the queue is not empty.
            while (_nextBranches.IsNotEmpty)
            {
                next = _nextBranches.TakeFirst();
                next.AssignCancelValue(_rejectedOrCanceledValue);
                AddToCancelQueueRisky(next);
            }
            _nextBranches.ClearLast();
        }

        void Internal.ITreeHandleAble.Cancel()
        {
            OnCancel();
            AddToHandleQueue(this);
        }

        void Internal.ITreeHandleAble.AssignCancelValue(Internal.IValueContainer cancelValue)
        {
            // If _rejectedOrCanceledValue is not null, it means this was already canceled with another value.
            if (_rejectedOrCanceledValue != null)
            {
                _rejectedOrCanceledValue = cancelValue;
                _rejectedOrCanceledValue.Retain();
            }
        }

        void Internal.ITreeHandleAble.Repool()
        {
            if (_retainCounter == 0)
            {
                if (!_wasWaitedOn & _state == PromiseState.Rejected)
                {
                    // Rejection wasn't caught.
                    AddRejectionToUnhandledStack((Internal.UnhandledExceptionInternal) _rejectedOrCanceledValue);
                }
                Dispose();
            }
        }

        private static ValueLinkedStack<Internal.UnhandledExceptionInternal> _unhandledExceptions;

        private static void AddRejectionToUnhandledStack(Internal.UnhandledExceptionInternal unhandledValue)
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

            ValueLinkedStack<Internal.UnhandledExceptionInternal> unhandledExceptions = _unhandledExceptions;
            _unhandledExceptions.Clear();
            // Reset handled flag.
            foreach (Internal.UnhandledExceptionInternal unhandled in unhandledExceptions)
            {
                unhandled.handled = false;
                // Allow to re-use.
                unhandled.Release();
            }
            throw new AggregateException(unhandledExceptions);
        }

        // Handle promises in a breadth-first manner.
        private static ValueLinkedQueue<Internal.ITreeHandleAble> _handleQueue;
        private static bool _runningHandles;

        protected static void AddToHandleQueue(Promise promise)
        {
            promise._notHandling = false;
            _handleQueue.AddLast(promise);
        }

        // TODO: Call this.
        // This allows infinite .Then/.Catch callbacks, since it avoids recursion.
        protected static void ContinueHandling()
		{
            if (_runningHandles)
            {
                // ContinueHandling is running higher in the program stack, so just return.
                return;
            }

            _runningHandles = true;

            while (_handleQueue.IsNotEmpty)
			{
                Internal.ITreeHandleAble _current = _handleQueue.TakeFirst();
                Promise current = (Promise) _current;

				while (current._nextBranches.IsNotEmpty)
				{
                    current._nextBranches.TakeFirst().Handle(current);
				}

                current._notHandling = true;
                _current.Repool();
			}

            _handleQueue.ClearLast();
            _runningHandles = false;
        }

		// Cancel promises in a breadth-first manner.
		private static ValueLinkedQueue<Internal.ITreeHandleAble> _cancelQueue;
        private static bool _runningCancels;

        protected static void AddToCancelQueue(Internal.ITreeHandleAble cancelation)
        {
            _cancelQueue.AddLast(cancelation);
        }

        protected static void AddToCancelQueueRisky(Internal.ITreeHandleAble cancelation)
        {
            _cancelQueue.AddLastRisky(cancelation);
        }

        private static void ContinueCanceling()
		{
			if (_runningCancels)
			{
				// ContinueCanceling is running higher in the program stack, so just return.
				return;
            }

            _runningCancels = true;

            while (_handleQueue.IsNotEmpty)
            {
                _handleQueue.TakeFirst().Cancel();
            }

            _cancelQueue.ClearLast();
            _runningCancels = false;
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
			return Promise.Internal.LitePromise<T>.GetOrCreate();
		}

		protected void Resolve(T value)
		{
			_value = value;
            Resolve();
		}

        protected override void Handle(Promise feed)
        {
            _value = ((Promise<T>) feed)._value;
            base.Handle(feed);
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
                protected static ValueLinkedStack<ITreeHandleAble> _pool;

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
                protected static ValueLinkedStack<ITreeHandleAble> _pool;

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

            public sealed class LitePromise : PoolablePromise<LitePromise>
            {
                private LitePromise() { }

                public static LitePromise GetOrCreate()
                {
                    var promise = _pool.IsNotEmpty ? (LitePromise)_pool.Pop() : new LitePromise();
                    promise.Reset(2);
                    promise.ResetDepth();
                    return promise;
                }
            }

            public sealed class LitePromise<T> : PoolablePromise<T, LitePromise<T>>
            {
                private LitePromise() { }

                public static LitePromise<T> GetOrCreate()
                {
                    var promise = _pool.IsNotEmpty ? (LitePromise<T>)_pool.Pop() : new LitePromise<T>();
                    promise.Reset(2);
                    promise.ResetDepth();
                    return promise;
                }

                public new void Resolve(T value)
                {
                    base.Resolve(value);
                }
            }

            public sealed class DeferredPromise : PromiseWaitDeferred<DeferredPromise>
            {
                private DeferredPromise() { }

                public static DeferredPromise GetOrCreate()
                {
                    var promise = _pool.IsNotEmpty ? (DeferredPromise)_pool.Pop() : new DeferredPromise();
                    promise.Reset();
                    promise.ResetDepth();
                    promise.Retain();
                    return promise;
                }
            }

            public sealed class DeferredPromise<T> : PromiseWaitDeferred<T, DeferredPromise<T>>
            {
                private DeferredPromise() { }

                public static DeferredPromise<T> GetOrCreate()
                {
                    var promise = _pool.IsNotEmpty ? (DeferredPromise<T>)_pool.Pop() : new DeferredPromise<T>();
                    promise.Reset();
                    promise.ResetDepth();
                    promise.Retain();
                    return promise;
                }
            }

            #region Resolve Promises
            // Individual types for more common .Then(onResolved) calls to be more efficient.

            public sealed class PromiseVoidResolve : PoolablePromise<PromiseVoidResolve>
            {
                private Action resolveHandler;

                private PromiseVoidResolve() { }

                public static PromiseVoidResolve GetOrCreate(Action resolveHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolve)_pool.Pop() : new PromiseVoidResolve();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    base.Handle(feed);
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (_state == PromiseState.Resolved)
                    {
                        callback.Invoke();
                    }
                    else
                    {
                        _rejectedOrCanceledValue.Retain();
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

                public static PromiseArgResolve<TArg> GetOrCreate(Action<TArg> resolveHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolve<TArg>)_pool.Pop() : new PromiseArgResolve<TArg>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    base.Handle(feed);
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (_state == PromiseState.Resolved)
                    {
                        callback.Invoke(feed.GetValue<TArg>());
                    }
                    else
                    {
                        _rejectedOrCanceledValue.Retain();
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

                public static PromiseVoidResolve<TResult> GetOrCreate(Func<TResult> resolveHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolve<TResult>)_pool.Pop() : new PromiseVoidResolve<TResult>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    base.Handle(feed);
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (_state == PromiseState.Resolved)
                    {
                        _value = callback.Invoke();
                    }
                    else
                    {
                        _rejectedOrCanceledValue.Retain();
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

                public static PromiseArgResolve<TArg, TResult> GetOrCreate(Func<TArg, TResult> resolveHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolve<TArg, TResult>)_pool.Pop() : new PromiseArgResolve<TArg, TResult>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    base.Handle(feed);
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (_state == PromiseState.Resolved)
                    {
                        _value = callback.Invoke(feed.GetValue<TArg>());
                    }
                    else
                    {
                        _rejectedOrCanceledValue.Retain();
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    resolveHandler = null;
                }
            }

            public sealed class PromiseVoidResolvePromise : PromiseWaitPromise<PromiseVoidResolvePromise>
            {
                private Func<Promise> resolveHandler;

                private PromiseVoidResolvePromise() { }

                public static PromiseVoidResolvePromise GetOrCreate(Func<Promise> resolveHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolvePromise)_pool.Pop() : new PromiseVoidResolvePromise();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (resolveHandler == null)
                    {
                        // The returned promise is handling this.
                        base.Handle(feed);
                        if (_rejectedOrCanceledValue != null)
                        {
                            _rejectedOrCanceledValue.Retain();
                        }
                    }
                    else
                    {
                        var callback = resolveHandler;
                        resolveHandler = null;
                        if (feed._state == PromiseState.Resolved)
                        {
                            WaitFor(callback.Invoke());
                        }
                        else
                        {
                            base.Handle(feed);
                            _rejectedOrCanceledValue.Retain();
                        }
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

                public static PromiseArgResolvePromise<TArg> GetOrCreate(Func<TArg, Promise> resolveHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolvePromise<TArg>)_pool.Pop() : new PromiseArgResolvePromise<TArg>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (resolveHandler == null)
                    {
                        // The returned promise is handling this.
                        base.Handle(feed);
                        if (_rejectedOrCanceledValue != null)
                        {
                            _rejectedOrCanceledValue.Retain();
                        }
                    }
                    else
                    {
                        var callback = resolveHandler;
                        resolveHandler = null;
                        if (feed._state == PromiseState.Resolved)
                        {
                            WaitFor(callback.Invoke(feed.GetValue<TArg>()));
                        }
                        else
                        {
                            base.Handle(feed);
                            _rejectedOrCanceledValue.Retain();
                        }
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

                public static PromiseVoidResolvePromise<TPromise> GetOrCreate(Func<Promise<TPromise>> resolveHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolvePromise<TPromise>)_pool.Pop() : new PromiseVoidResolvePromise<TPromise>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (resolveHandler == null)
                    {
                        // The returned promise is handling this.
                        base.Handle(feed);
                        if (_rejectedOrCanceledValue != null)
                        {
                            _rejectedOrCanceledValue.Retain();
                        }
                    }
                    else
                    {
                        var callback = resolveHandler;
                        resolveHandler = null;
                        if (feed._state == PromiseState.Resolved)
                        {
                            WaitFor(callback.Invoke());
                        }
                        else
                        {
                            base.Handle(feed);
                            _rejectedOrCanceledValue.Retain();
                        }
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

                public static PromiseArgResolvePromise<TArg, TPromise> GetOrCreate(Func<TArg, Promise<TPromise>> resolveHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolvePromise<TArg, TPromise>)_pool.Pop() : new PromiseArgResolvePromise<TArg, TPromise>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (resolveHandler == null)
                    {
                        // The returned promise is handling this.
                        base.Handle(feed);
                        if (_rejectedOrCanceledValue != null)
                        {
                            _rejectedOrCanceledValue.Retain();
                        }
                    }
                    else
                    {
                        var callback = resolveHandler;
                        resolveHandler = null;
                        if (feed._state == PromiseState.Resolved)
                        {
                            WaitFor(callback.Invoke(feed.GetValue<TArg>()));
                        }
                        else
                        {
                            base.Handle(feed);
                            _rejectedOrCanceledValue.Retain();
                        }
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    resolveHandler = null;
                }
            }

            public sealed class PromiseVoidResolveDeferred : PromiseWaitDeferred<PromiseVoidResolveDeferred>
            {
                private Func<Action<Deferred>> resolveHandler;

                private PromiseVoidResolveDeferred() { }

                public static PromiseVoidResolveDeferred GetOrCreate(Func<Action<Deferred>> resolveHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolveDeferred)_pool.Pop() : new PromiseVoidResolveDeferred();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset();
                    promise.Retain();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == PromiseState.Resolved)
                    {
                        var deferredAction = callback.Invoke();
                        ValidateReturn(deferredAction);
                        deferredAction.Invoke(deferred);
                    }
                    else
                    {
                        base.Handle(feed);
                        _rejectedOrCanceledValue.Retain();
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    resolveHandler = null;
                }
            }

            public sealed class PromiseArgResolveDeferred<TArg> : PromiseWaitDeferred<PromiseArgResolveDeferred<TArg>>
            {
                private Func<TArg, Action<Deferred>> resolveHandler;

                private PromiseArgResolveDeferred() { }

                public static PromiseArgResolveDeferred<TArg> GetOrCreate(Func<TArg, Action<Deferred>> resolveHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolveDeferred<TArg>)_pool.Pop() : new PromiseArgResolveDeferred<TArg>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset();
                    promise.Retain();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == PromiseState.Resolved)
                    {
                        var deferredAction = callback.Invoke(feed.GetValue<TArg>());
                        ValidateReturn(deferredAction);
                        deferredAction.Invoke(deferred);
                    }
                    else
                    {
                        base.Handle(feed);
                        _rejectedOrCanceledValue.Retain();
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    resolveHandler = null;
                }
            }

            public sealed class PromiseVoidResolveDeferred<TDeferred> : PromiseWaitDeferred<TDeferred, PromiseVoidResolveDeferred<TDeferred>>
            {
                private Func<Action<Deferred>> resolveHandler;

                private PromiseVoidResolveDeferred() { }

                public static PromiseVoidResolveDeferred<TDeferred> GetOrCreate(Func<Action<Deferred>> resolveHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseVoidResolveDeferred<TDeferred>)_pool.Pop() : new PromiseVoidResolveDeferred<TDeferred>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset();
                    promise.Retain();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == PromiseState.Resolved)
                    {
                        var deferredAction = callback.Invoke();
                        ValidateReturn(deferredAction);
                        deferredAction.Invoke(deferred);
                    }
                    else
                    {
                        base.Handle(feed);
                        _rejectedOrCanceledValue.Retain();
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    resolveHandler = null;
                }
            }

            public sealed class PromiseArgResolveDeferred<TArg, TDeferred> : PromiseWaitDeferred<TDeferred, PromiseArgResolveDeferred<TArg, TDeferred>>
            {
                private Func<TArg, Action<Deferred>> resolveHandler;

                private PromiseArgResolveDeferred() { }

                public static PromiseArgResolveDeferred<TArg, TDeferred> GetOrCreate(Func<TArg, Action<Deferred>> resolveHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseArgResolveDeferred<TArg, TDeferred>)_pool.Pop() : new PromiseArgResolveDeferred<TArg, TDeferred>();
                    promise.resolveHandler = resolveHandler;
                    promise.Reset();
                    promise.Retain();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == PromiseState.Resolved)
                    {
                        var deferredAction = callback.Invoke(feed.GetValue<TArg>());
                        ValidateReturn(deferredAction);
                        deferredAction.Invoke(deferred);
                    }
                    else
                    {
                        base.Handle(feed);
                        _rejectedOrCanceledValue.Retain();
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    resolveHandler = null;
                }
            }
            #endregion

            #region Reject Promises
            // Used IDelegate to reduce the amount of classes I would have to write to handle catches (Composition Over Inheritance).
            // I'm less concerned about performance for catches since exceptions are expensive anyway, and they are expected to be used less often than .Then(onResolved).
            public sealed class PromiseReject : PoolablePromise<PromiseReject>
            {
                private IDelegate rejectHandler;

                private PromiseReject() { }

                public static PromiseReject GetOrCreate(IDelegate rejectHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseReject)_pool.Pop() : new PromiseReject();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == PromiseState.Rejected)
                    {
                        _state = feed._state; // Set the state so a Cancel call won't do anything during invoke.
                        _notHandling = false; // Set handling flag so a .Then/.Catch during invoke won't add to handle queue.
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue))
                        {
                            _state = PromiseState.Resolved;
                            AddToHandleQueue(this);
                        }
                        else
                        {
                            base.Handle(feed);
                            _rejectedOrCanceledValue.Retain();
                        }
                    }
                    else
                    {
                        callback.Dispose();
                        base.Handle(feed);
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

                public static PromiseReject<T> GetOrCreate(IDelegate<T> rejectHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseReject<T>)_pool.Pop() : new PromiseReject<T>();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == PromiseState.Rejected)
                    {
                        _state = feed._state; // Set the state so a Cancel call won't do anything during invoke.
                        _notHandling = false; // Set handling flag so a .Then/.Catch during invoke won't add to handle queue.
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out _value))
                        {
                            _state = PromiseState.Resolved;
                            AddToHandleQueue(this);
                        }
                        else
                        {
                            base.Handle(feed);
                            _rejectedOrCanceledValue.Retain();
                        }
                    }
                    else
                    {
                        callback.Dispose();
                        base.Handle(feed);
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    rejectHandler.Dispose();
                    rejectHandler = null;
                }
            }

            public sealed class PromiseRejectPromise : PromiseWaitPromise<PromiseRejectPromise>
            {
                private IDelegate<Promise> rejectHandler;

                private PromiseRejectPromise() { }

                public static PromiseRejectPromise GetOrCreate(IDelegate<Promise> rejectHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseRejectPromise)_pool.Pop() : new PromiseRejectPromise();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (rejectHandler == null)
                    {
                        // The returned promise is handling this.
                        base.Handle(feed);
                        if (_rejectedOrCanceledValue != null)
                        {
                            _rejectedOrCanceledValue.Retain();
                        }
                    }
                    else
                    {
                        var callback = rejectHandler;
                        rejectHandler = null;
                        if (feed._state == PromiseState.Rejected)
                        {
                            Promise promise;
                            if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out promise))
                            {
                                WaitFor(promise);
                            }
                            else
                            {
                                base.Handle(feed);
                                _rejectedOrCanceledValue.Retain();
                            }
                        }
                        else
                        {
                            callback.Dispose();
                            base.Handle(feed);
                        }
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    rejectHandler.Dispose();
                    rejectHandler = null;
                }
            }

            public sealed class PromiseRejectPromise<TPromise> : PromiseWaitPromise<TPromise, PromiseRejectPromise<TPromise>>
            {
                private IDelegate<Promise<TPromise>> rejectHandler;

                private PromiseRejectPromise() { }

                public static PromiseRejectPromise<TPromise> GetOrCreate(IDelegate<Promise<TPromise>> rejectHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseRejectPromise<TPromise>)_pool.Pop() : new PromiseRejectPromise<TPromise>();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (rejectHandler == null)
                    {
                        // The returned promise is handling this.
                        base.Handle(feed);
                        if (_rejectedOrCanceledValue != null)
                        {
                            _rejectedOrCanceledValue.Retain();
                        }
                    }
                    else
                    {
                        var callback = rejectHandler;
                        rejectHandler = null;
                        if (feed._state == PromiseState.Rejected)
                        {
                            Promise<TPromise> promise;
                            if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out promise))
                            {
                                WaitFor(promise);
                            }
                            else
                            {
                                base.Handle(feed);
                                _rejectedOrCanceledValue.Retain();
                            }
                        }
                        else
                        {
                            callback.Dispose();
                            base.Handle(feed);
                        }
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    rejectHandler.Dispose();
                    rejectHandler = null;
                }
            }

            public sealed class PromiseRejectDeferred : PromiseWaitDeferred<PromiseRejectDeferred>
            {
                private IDelegate<Action<Deferred>> rejectHandler;

                private PromiseRejectDeferred() { }

                public static PromiseRejectDeferred GetOrCreate(IDelegate<Action<Deferred>> rejectHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseRejectDeferred)_pool.Pop() : new PromiseRejectDeferred();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset();
                    promise.Retain();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == PromiseState.Rejected)
                    {
                        Action<Deferred> deferredDelegate;
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out deferredDelegate))
                        {
                            ValidateReturn(deferredDelegate);
                            deferredDelegate.Invoke(deferred);
                        }
                        else
                        {
                            base.Handle(feed);
                            _rejectedOrCanceledValue.Retain();
                        }
                    }
                    else
                    {
                        callback.Dispose();
                        base.Handle(feed);
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    rejectHandler.Dispose();
                    rejectHandler = null;
                }
            }

            public sealed class PromiseRejectDeferred<TDeferred> : PromiseWaitDeferred<TDeferred, PromiseRejectDeferred<TDeferred>>
            {
                private IDelegate<Action<Deferred>> rejectHandler;

                private PromiseRejectDeferred() { }

                public static PromiseRejectDeferred<TDeferred> GetOrCreate(IDelegate<Action<Deferred>> rejectHandler)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseRejectDeferred<TDeferred>)_pool.Pop() : new PromiseRejectDeferred<TDeferred>();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == PromiseState.Rejected)
                    {
                        Action<Deferred> deferredDelegate;
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out deferredDelegate))
                        {
                            ValidateReturn(deferredDelegate);
                            deferredDelegate.Invoke(deferred);
                        }
                        else
                        {
                            base.Handle(feed);
                            _rejectedOrCanceledValue.Retain();
                        }
                    }
                    else
                    {
                        callback.Dispose();
                        base.Handle(feed);
                    }
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    rejectHandler.Dispose();
                    rejectHandler = null;
                }
            }
            #endregion

            #region Resolve or Reject Promises
            public sealed class PromiseResolveReject : PoolablePromise<PromiseResolveReject>
            {
                IDelegate onResolved, onRejected;

                private PromiseResolveReject() { }

                public static PromiseResolveReject GetOrCreate(IDelegate onResolved, IDelegate onRejected)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveReject)_pool.Pop() : new PromiseResolveReject();
                    promise.onResolved = onResolved;
                    promise.onRejected = onRejected;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var resolveCallback = onResolved;
                    onResolved = null;
                    var rejectCallback = onRejected;
                    onRejected = null;
                    _state = feed._state; // Set the state so a Cancel call won't do anything during invoke.
                    _notHandling = false; // Set handling flag so a .Then/.Catch during invoke won't add to handle queue.
                    if (feed._state == PromiseState.Resolved)
                    {
                        rejectCallback.Dispose();
                        resolveCallback.DisposeAndInvoke(feed);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue))
                        {
                            base.Handle(feed);
                            _rejectedOrCanceledValue.Retain();
                            return;
                        }
                    }
                    _state = PromiseState.Resolved;
                    AddToHandleQueue(this);
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

                public static PromiseResolveReject<T> GetOrCreate(IDelegate<T> onResolved, IDelegate<T> onRejected)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveReject<T>)_pool.Pop() : new PromiseResolveReject<T>();
                    promise.onResolved = onResolved;
                    promise.onRejected = onRejected;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var resolveCallback = onResolved;
                    onResolved = null;
                    var rejectCallback = onRejected;
                    onRejected = null;
                    _state = feed._state; // Set the state so a Cancel call won't do anything during invoke.
                    _notHandling = false; // Set handling flag so a .Then/.Catch during invoke won't add to handle queue.
                    if (feed._state == PromiseState.Resolved)
                    {
                        rejectCallback.Dispose();
                        _value = resolveCallback.DisposeAndInvoke(feed);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out _value))
                        {
                            base.Handle(feed);
                            _rejectedOrCanceledValue.Retain();
                            return;
                        }
                    }
                    _state = PromiseState.Resolved;
                    AddToHandleQueue(this);
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

            public sealed class PromiseResolveRejectPromise : PromiseWaitPromise<PromiseResolveRejectPromise>
            {
                IDelegate<Promise> onResolved, onRejected;

                private PromiseResolveRejectPromise() { }

                public static PromiseResolveRejectPromise GetOrCreate(IDelegate<Promise> onResolved, IDelegate<Promise> onRejected)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectPromise)_pool.Pop() : new PromiseResolveRejectPromise();
                    promise.onResolved = onResolved;
                    promise.onRejected = onRejected;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (onResolved == null)
                    {
                        // The returned promise is handling this.
                        base.Handle(feed);
                        if (_rejectedOrCanceledValue != null)
                        {
                            _rejectedOrCanceledValue.Retain();
                        }
                    }
                    else
                    {
                        var resolveCallback = onResolved;
                        onResolved = null;
                        var rejectCallback = onRejected;
                        onRejected = null;
                        Promise promise;
                        if (feed._state == PromiseState.Resolved)
                        {
                            rejectCallback.Dispose();
                            promise = resolveCallback.DisposeAndInvoke(feed);
                        }
                        else
                        {
                            resolveCallback.Dispose();
                            if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out promise))
                            {
                                base.Handle(feed);
                                _rejectedOrCanceledValue.Retain();
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

                public static PromiseResolveRejectPromise<TPromise> GetOrCreate(IDelegate<Promise<TPromise>> onResolved, IDelegate<Promise<TPromise>> onRejected)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectPromise<TPromise>)_pool.Pop() : new PromiseResolveRejectPromise<TPromise>();
                    promise.onResolved = onResolved;
                    promise.onRejected = onRejected;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (onResolved == null)
                    {
                        // The returned promise is handling this.
                        base.Handle(feed);
                        if (_rejectedOrCanceledValue != null)
                        {
                            _rejectedOrCanceledValue.Retain();
                        }
                    }
                    else
                    {
                        var resolveCallback = onResolved;
                        onResolved = null;
                        var rejectCallback = onRejected;
                        onRejected = null;
                        Promise<TPromise> promise;
                        if (feed._state == PromiseState.Resolved)
                        {
                            rejectCallback.Dispose();
                            promise = resolveCallback.DisposeAndInvoke(feed);
                        }
                        else
                        {
                            resolveCallback.Dispose();
                            if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out promise))
                            {
                                base.Handle(feed);
                                _rejectedOrCanceledValue.Retain();
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

                public static PromiseResolveRejectDeferred GetOrCreate(IDelegate<Action<Deferred>> onResolved, IDelegate<Action<Deferred>> onRejected)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectDeferred)_pool.Pop() : new PromiseResolveRejectDeferred();
                    promise.onResolved = onResolved;
                    promise.onRejected = onRejected;
                    promise.Reset();
                    promise.Retain();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var resolveCallback = onResolved;
                    onResolved = null;
                    var rejectCallback = onRejected;
                    onRejected = null;
                    Action<Deferred> deferredDelegate;
                    if (feed._state == PromiseState.Resolved)
                    {
                        rejectCallback.Dispose();
                        deferredDelegate = resolveCallback.DisposeAndInvoke(feed);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out deferredDelegate))
                        {
                            base.Handle(feed);
                            _rejectedOrCanceledValue.Retain();
                            return;
                        }
                    }
                    ValidateReturn(deferredDelegate);
                    deferredDelegate.Invoke(deferred);
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

            public sealed class PromiseResolveRejectDeferred<TDeferred> : PromiseWaitDeferred<TDeferred, PromiseResolveRejectDeferred<TDeferred>>
            {
                IDelegate<Action<Deferred>> onResolved, onRejected;

                private PromiseResolveRejectDeferred() { }

                public static PromiseResolveRejectDeferred<TDeferred> GetOrCreate(IDelegate<Action<Deferred>> onResolved, IDelegate<Action<Deferred>> onRejected)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseResolveRejectDeferred<TDeferred>)_pool.Pop() : new PromiseResolveRejectDeferred<TDeferred>();
                    promise.onResolved = onResolved;
                    promise.onRejected = onRejected;
                    promise.Reset();
                    promise.Retain();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var resolveCallback = onResolved;
                    onResolved = null;
                    var rejectCallback = onRejected;
                    onRejected = null;
                    Action<Deferred> deferredDelegate;
                    if (feed._state == PromiseState.Resolved)
                    {
                        rejectCallback.Dispose();
                        deferredDelegate = resolveCallback.DisposeAndInvoke(feed);
                    }
                    else
                    {
                        resolveCallback.Dispose();
                        if (!rejectCallback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out deferredDelegate))
                        {
                            base.Handle(feed);
                            _rejectedOrCanceledValue.Retain();
                            return;
                        }
                    }
                    ValidateReturn(deferredDelegate);
                    deferredDelegate.Invoke(deferred);
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
            #endregion

            #region Complete Promises
            public sealed class PromiseComplete : PoolablePromise<PromiseComplete>
            {
                private Action onComplete;

                private PromiseComplete() { }

                public static PromiseComplete GetOrCreate(Action onComplete)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseComplete)_pool.Pop() : new PromiseComplete();
                    promise.onComplete = onComplete;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    _state = PromiseState.Resolved;
                    AddToHandleQueue(this);
                    var callback = onComplete;
                    onComplete = null;
                    callback.Invoke();
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

                public static PromiseComplete<T> GetOrCreate(Func<T> onComplete)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseComplete<T>)_pool.Pop() : new PromiseComplete<T>();
                    promise.onComplete = onComplete;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    _state = PromiseState.Resolved;
                    AddToHandleQueue(this);
                    var callback = onComplete;
                    onComplete = null;
                    _value = callback.Invoke();
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    onComplete = null;
                }
            }

            public sealed class PromiseCompletePromise : PromiseWaitPromise<PromiseCompletePromise>
            {
                private Func<Promise> onComplete;

                private PromiseCompletePromise() { }

                public static PromiseCompletePromise GetOrCreate(Func<Promise> onComplete)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCompletePromise)_pool.Pop() : new PromiseCompletePromise();
                    promise.onComplete = onComplete;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (onComplete == null)
                    {
                        // The returned promise is handling this.
                        base.Handle(feed);
                        if (_rejectedOrCanceledValue != null)
                        {
                            _rejectedOrCanceledValue.Retain();
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

                public static PromiseCompletePromise<T> GetOrCreate(Func<Promise<T>> onComplete)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCompletePromise<T>)_pool.Pop() : new PromiseCompletePromise<T>();
                    promise.onComplete = onComplete;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    if (onComplete == null)
                    {
                        // The returned promise is handling this.
                        base.Handle(feed);
                        if (_rejectedOrCanceledValue != null)
                        {
                            _rejectedOrCanceledValue.Retain();
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

            public sealed class PromiseCompleteDeferred : PromiseWaitDeferred<PromiseCompleteDeferred>
            {
                Func<Action<Deferred>> onComplete;

                private PromiseCompleteDeferred() { }

                public static PromiseCompleteDeferred GetOrCreate(Func<Action<Deferred>> onComplete)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCompleteDeferred)_pool.Pop() : new PromiseCompleteDeferred();
                    promise.onComplete = onComplete;
                    promise.Reset();
                    promise.Retain();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = onComplete;
                    onComplete = null;
                    var deferredDelegate = callback.Invoke();
                    ValidateReturn(deferredDelegate);
                    deferredDelegate.Invoke(deferred);
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    onComplete = null;
                }
            }

            public sealed class PromiseCompleteDeferred<T> : PromiseWaitDeferred<T, PromiseCompleteDeferred<T>>
            {
                Func<Action<Deferred>> onComplete;

                private PromiseCompleteDeferred() { }

                public static PromiseCompleteDeferred<T> GetOrCreate(Func<Action<Deferred>> onComplete)
                {
                    var promise = _pool.IsNotEmpty ? (PromiseCompleteDeferred<T>)_pool.Pop() : new PromiseCompleteDeferred<T>();
                    promise.onComplete = onComplete;
                    promise.Reset();
                    promise.Retain();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = onComplete;
                    onComplete = null;
                    var deferredDelegate = callback.Invoke();
                    ValidateReturn(deferredDelegate);
                    deferredDelegate.Invoke(deferred);
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    onComplete = null;
                }
            }
            #endregion

            #region Delegate Wrappers
            public sealed class FinallyDelegate : ITreeHandleAble
            {
                ITreeHandleAble ILinked<ITreeHandleAble>.Next { get; set; }

                private static ValueLinkedStack<ITreeHandleAble> _pool;

                private Promise _owner;
                private Action _onFinally;

                private FinallyDelegate() { }

                static FinallyDelegate()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static FinallyDelegate GetOrCreate(Action onFinally, Promise owner)
                {
                    var del = _pool.IsNotEmpty ? (FinallyDelegate)_pool.Pop() : new FinallyDelegate();
                    del._onFinally = onFinally;
                    del._owner = owner;
                    return del;
                }

                void Dispose()
                {
                    _onFinally = null;
                    _owner = null;
                    _pool.Push(this);
                }

                void ITreeHandleAble.Cancel()
                {
                    var callback = _onFinally;
                    Dispose();
                    callback.Invoke();
                }

                void ITreeHandleAble.AssignCancelValue(IValueContainer cancelValue) { }

                void ITreeHandleAble.Handle(Promise feed)
                {
                    var callback = _onFinally;
                    _owner._wasWaitedOn = true;
                    Dispose();
                    callback.Invoke();
                }

                void ITreeHandleAble.Repool()
                {
                    throw new InvalidOperationException();
                }
            }

            public sealed class CancelDelegate : ITreeHandleAble
            {
                ITreeHandleAble ILinked<ITreeHandleAble>.Next { get; set; }

                private static ValueLinkedStack<ITreeHandleAble> _pool;

                private Action _onCanceled;

                private CancelDelegate() { }

                static CancelDelegate()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static CancelDelegate GetOrCreate(Action onCanceled)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegate)_pool.Pop() : new CancelDelegate();
                    del._onCanceled = onCanceled;
                    return del;
                }

                void Dispose()
                {
                    _onCanceled = null;
                    _pool.Push(this);
                }

                void ITreeHandleAble.Cancel()
                {
                    var callback = _onCanceled;
                    Dispose();
                    callback.Invoke();
                }

                void ITreeHandleAble.AssignCancelValue(IValueContainer cancelValue) { }

                void ITreeHandleAble.Handle(Promise feed)
                {
                    Dispose();
                }

                void ITreeHandleAble.Repool()
                {
                    throw new InvalidOperationException();
                }
            }

            public sealed class CancelDelegate<T> : ITreeHandleAble
            {
                ITreeHandleAble ILinked<ITreeHandleAble>.Next { get; set; }

                private static ValueLinkedStack<ITreeHandleAble> _pool;

                private IValueContainer _cancelValue;
                private Action<T> _onCanceled;

                private CancelDelegate() { }

                static CancelDelegate()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static CancelDelegate<T> GetOrCreate(Action<T> onCanceled)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegate<T>)_pool.Pop() : new CancelDelegate<T>();
                    del._onCanceled = onCanceled;
                    return del;
                }

                void Dispose()
                {
                    _onCanceled = null;
                    _cancelValue = null;
                    _pool.Push(this);
                }

                void ITreeHandleAble.Cancel()
                {
                    var callback = _onCanceled;
                    var cancelValue = _cancelValue;
                    Dispose();
                    T arg;
                    if (cancelValue.TryGetValueAs(out arg))
                    {
                        callback.Invoke(arg);
                    }
                }

                void ITreeHandleAble.AssignCancelValue(IValueContainer cancelValue)
                {
                    _cancelValue = cancelValue;
                }

                void ITreeHandleAble.Handle(Promise feed)
                {
                    Dispose();
                }

                void ITreeHandleAble.Repool()
                {
                    throw new InvalidOperationException();
                }
            }

            public class DelegateVoid : IDelegate, ILinked<DelegateVoid>
            {
                DelegateVoid ILinked<DelegateVoid>.Next { get; set; }

                private Action _callback;

                protected static ValueLinkedStack<DelegateVoid> _pool;

                public static DelegateVoid GetOrCreate(Action callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoid();
                    del._callback = callback;
                    return del;
                }

                static DelegateVoid()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private DelegateVoid() { }

                public void Invoke()
                {
                    var temp = _callback;
                    Dispose();
                    temp.Invoke();
                }

                public void Dispose()
                {
                    _callback = null;
                    _pool.Push(this);
                }

                public bool DisposeAndTryInvoke(IValueContainer valueContainer)
                {
                    Invoke();
                    return true;
                }

                public void DisposeAndInvoke(Promise feed)
                {
                    Invoke();
                }
            }

            public sealed class DelegateArg<TArg> : IDelegate, ILinked<DelegateArg<TArg>>
            {
                DelegateArg<TArg> ILinked<DelegateArg<TArg>>.Next { get; set; }

                private Action<TArg> _callback;

                private static ValueLinkedStack<DelegateArg<TArg>> _pool;

                public static DelegateArg<TArg> GetOrCreate(Action<TArg> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArg<TArg>();
                    del._callback = callback;
                    return del;
                }

                static DelegateArg()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private DelegateArg() { }

                public void DisposeAndInvoke(TArg arg)
                {
                    var temp = _callback;
                    Dispose();
                    temp.Invoke(arg);
                }

                public void Dispose()
                {
                    _callback = null;
                    _pool.Push(this);
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

            public sealed class DelegateVoid<TResult> : IDelegate<TResult>, ILinked<DelegateVoid<TResult>>
            {
                DelegateVoid<TResult> ILinked<DelegateVoid<TResult>>.Next { get; set; }

                private Func<TResult> _callback;

                private static ValueLinkedStack<DelegateVoid<TResult>> _pool;

                public static DelegateVoid<TResult> GetOrCreate(Func<TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoid<TResult>();
                    del._callback = callback;
                    return del;
                }

                static DelegateVoid()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private DelegateVoid() { }

                public TResult DisposeAndInvoke()
                {
                    var temp = _callback;
                    Dispose();
                    return temp.Invoke();
                }

                public void Dispose()
                {
                    _callback = null;
                    _pool.Push(this);
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

            public sealed class DelegateArg<TArg, TResult> : IDelegate<TResult>, ILinked<DelegateArg<TArg, TResult>>
            {
                DelegateArg<TArg, TResult> ILinked<DelegateArg<TArg, TResult>>.Next { get; set; }

                private Func<TArg, TResult> _callback;

                private static ValueLinkedStack<DelegateArg<TArg, TResult>> _pool;

                public static DelegateArg<TArg, TResult> GetOrCreate(Func<TArg, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArg<TArg, TResult>();
                    del._callback = callback;
                    return del;
                }

                static DelegateArg()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private DelegateArg() { }

                public TResult DisposeAndInvoke(TArg arg)
                {
                    var temp = _callback;
                    Dispose();
                    return temp.Invoke(arg);
                }

                public void Dispose()
                {
                    _callback = null;
                    _pool.Push(this);
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

            public partial interface ITreeHandleAble : ILinked<ITreeHandleAble>
            {
                void Handle(Promise feed);
                void Cancel();
                void Repool();
                void AssignCancelValue(IValueContainer cancelValue);
            }

            public interface IValueContainer
            {
                bool TryGetValueAs<U>(out U value);
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

                public virtual void Release() { }

                public virtual void Retain() { }

                public virtual bool TryGetValueAs<U>(out U value)
                {
                    value = default(U);
                    return false;
                }
            }

            public sealed class UnhandledExceptionVoid : UnhandledExceptionInternal
            {
                // We can reuse the same object.
                private static readonly UnhandledExceptionVoid obj = new UnhandledExceptionVoid();

                private UnhandledExceptionVoid() { }

                public static UnhandledExceptionVoid GetOrCreate()
                {
                    return obj;
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
                    UnhandledException<T> ex = _pool.IsNotEmpty ? (UnhandledException<T>)_pool.Pop() : new UnhandledException<T>();
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
                    if (--retainCounter == 0)
                    {
                        Value = default(T);
                        _pool.Push(this);
                    }
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

                public void Retain() { }

                public void Release() { }

                public bool IsRetained { get { return false; } }
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
                    CancelValue<T> ex = _pool.IsNotEmpty ? _pool.Pop() : new CancelValue<T>();
                    ex.Value = value;
                    return ex;
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

                public void Retain()
                {
                    ++retainCounter;
                }

                public void Release()
                {
                    if (--retainCounter == 0)
                    {
                        Value = default(T);
                        _pool.Push(this);
                    }
                }

                public bool IsRetained { get { return retainCounter > 0; } }
            }
#endregion

#region Control Promises
            // TODO
            public sealed class AllPromise : PromiseWaitPromise<AllPromise>
            {
            }
#endregion
        }
    }

#region Deferreds
    partial class Promise
    {
        partial class Internal
        {
            public sealed class DeferredInternal : Deferred
            {
                public DeferredInternal(Promise target)
                {
                    Promise = target;
                }

                public override void ReportProgress(float progress)
                {
                    ValidateProgress();
                    ValidateOperation(Promise);
                    ValidateProgress(progress);

                    if (State != PromiseState.Pending)
                    {
                        Logger.LogWarning("Deferred.ReportProgress - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.ReportProgress(progress);
                }

                public override void Resolve()
                {
                    ValidateOperation(Promise);

                    if (State != PromiseState.Pending)
                    {
                        Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.Resolve();
                }

                public override void Cancel()
                {
                    ValidateCancel();
                    ValidateOperation(Promise);

                    if (State != PromiseState.Pending)
                    {
                        Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.Cancel();
                }

                public override void Cancel<TCancel>(TCancel reason)
                {
                    ValidateCancel();
                    ValidateOperation(Promise);

                    if (State != PromiseState.Pending)
                    {
                        Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.Cancel(reason);
                }

                public override void Reject()
                {
                    ValidateOperation(Promise);

                    if (State != PromiseState.Pending)
                    {
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.Reject(1);
                }

                public override void Reject<TReject>(TReject reason)
                {
                    ValidateOperation(Promise);

                    if (State != PromiseState.Pending)
                    {
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state. Attempted reject reason:\n" + reason);
                        return;
                    }

                    Promise.Reject(reason, 1);
                }
            }
        }
    }

    partial class Promise<T>
    {
        protected static new class Internal
        {
            public sealed class DeferredInternal : Deferred
            {
                public DeferredInternal(Promise<T> target)
                {
                    Promise = target;
                }

                public override void ReportProgress(float progress)
                {
                    ValidateProgress();
                    ValidateOperation(Promise);
                    ValidateProgress(progress);

                    if (State != PromiseState.Pending)
                    {
                        Logger.LogWarning("Deferred.ReportProgress - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.ReportProgress(progress);
                }

                public override void Resolve(T arg)
                {
                    ValidateOperation(Promise);

                    if (State != PromiseState.Pending)
                    {
                        Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.Resolve(arg);
                }

                public override void Cancel()
                {
                    ValidateCancel();
                    ValidateOperation(Promise);

                    if (State != PromiseState.Pending)
                    {
                        Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.Cancel();
                }

                public override void Cancel<TCancel>(TCancel reason)
                {
                    ValidateCancel();
                    ValidateOperation(Promise);

                    if (State != PromiseState.Pending)
                    {
                        Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.Cancel(reason);
                }

                public override void Reject()
                {
                    ValidateOperation(Promise);

                    if (State != PromiseState.Pending)
                    {
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.Reject(1);
                }

                public override void Reject<TReject>(TReject reason)
                {
                    ValidateOperation(Promise);

                    if (State != PromiseState.Pending)
                    {
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state. Attempted reject reason:\n" + reason);
                        return;
                    }

                    Promise.Reject(reason, 1);
                }
            }
        }
    }
#endregion
}
#pragma warning restore IDE0034 // Simplify 'default' expression
#pragma warning restore IDE0018 // Inline variable declaration
#pragma warning restore RECS0108 // Warns about static fields in generic types