using System;

namespace ProtoPromise
{
	partial class Promise : Promise.Internal.ITreeHandleAble
    {
        Internal.ITreeHandleAble ILinked<Internal.ITreeHandleAble>.Next { get; set; }

		private ValueLinkedQueue<Internal.ITreeHandleAble> _nextBranches;
		protected Internal.IValueContainer _rejectedOrCanceledValue;
        private uint _retainCounter;
        protected DeferredState _state;
        private bool _wasWaitedOn; // Tells the handler that another promise waited on this promise (either by .Then/.Catch from this promise, or by returning this promise in another promise's .Then/.Catch)
        private bool _notHandling = true; // Is not already being handled in ContinueHandling or ContinueCanceling.

#if DEBUG
        private static long _currentFrame;
		private long _releasedFrame; // If this is smaller than current frame and it's not retained, no further actions are allowed.
        private string _createdStackTrace;
        private static int idCounter;
        protected readonly int _id;

        protected Promise()
		{
			_id = idCounter++;
        }
#else
        protected Promise() { }
#endif

        protected virtual U GetValue<U>()
        {
            throw new InvalidOperationException();
        }

        private void Reset(int skipFrames = 4)
		{
			if (!Manager.PoolObjects)
			{
				Retain();
			}
			_state = DeferredState.Pending;
#if DEBUG
            _releasedFrame = _currentFrame;
			if (Manager.DebugStacktraceGenerator == GeneratedStacktrace.All)
			{
				_createdStackTrace = GetStackTrace(skipFrames);
			}
#endif
		}

        // Calls to these will be compiled away in RELEASE mode.
        partial void Validate(Promise other);
        static partial void Validate(Delegate other);
#if DEBUG
        partial void Validate(Promise other)
		{
            if (other == null)
			{
				// Returning a null from the callback is not allowed.
				throw new InvalidReturnException("A null promise was returned.");
			}

            // Validate returned promise as not disposed.
            ValidateOperation(other);

            // A promise cannot wait on itself.
            for (var prev = other; prev != null; prev = prev._previous)
			{
				if (prev == this)
				{
					throw new InvalidReturnException("Circular Promise chain detected.", other._createdStackTrace);
				}
			}
		}

		static partial void Validate(Delegate other)
		{
			if (other == null)
			{
				// Returning a null from the callback is not allowed.
				throw new InvalidReturnException("A null delegate was returned.");
			}
		}

		public override string ToString()
		{
			return string.Format("Type: Promise, Id: {0}, State: {1}", _id, _state);
		}
#else
		public override string ToString()
		{
			return string.Format("Type: Promise, State: {0}", _state);
		}
#endif

        protected virtual void Dispose()
		{
			if (_rejectedOrCanceledValue != null)
			{
				_rejectedOrCanceledValue.Release();
				_rejectedOrCanceledValue = null;
			}
		}

		protected virtual Promise GetDuplicate()
		{
			return Internal.LitePromise.GetOrCreate();
		}

		protected void Resolve()
		{
			_state = DeferredState.Resolved;
            AddToHandleQueue(this);
		}

		protected void Reject(int skipFrames)
		{
			Internal.UnhandledExceptionVoid rejectValue = Internal.UnhandledExceptionVoid.GetOrCreate();
            SetFormattedStackTrace(rejectValue, skipFrames + 1);
            Reject(rejectValue);
		}

		protected void Reject<TReject>(TReject reason, int skipFrames)
		{
			// Is TReject an exception (including if it's null)?
			if (typeof(Exception).IsAssignableFrom(typeof(TReject)))
			{
				// Behave the same way .Net behaves if you throw null.
				var rejectValue = Internal.UnhandledExceptionException.GetOrCreate(reason as Exception ?? new NullReferenceException());
                SetFormattedStackTrace(rejectValue, skipFrames + 1);
                Reject(rejectValue);
			}
			else
			{
				var rejectValue = Internal.UnhandledException<TReject>.GetOrCreate(reason);
                SetFormattedStackTrace(rejectValue, skipFrames + 1);
                Reject(rejectValue);
			}
        }

        protected void Reject(Internal.IValueContainer rejectValue)
        {
            _state = DeferredState.Rejected;
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
            if ((_state == DeferredState.Resolved | _state == DeferredState.Rejected) & _notHandling)
            {
                // Continue handling if this is resolved or rejected and it's not already being handled.
                _nextBranches.AddLast(waiter);
                AddToHandleQueue(this);
                ContinueHandling();
            }
            else if (_state == DeferredState.Canceled)
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
            if (_state == DeferredState.Canceled)
            {
                // Canceled promises are never placed in the handle queue and don't get resolved or rejected.
                AddToDisposePool(this);
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

            // Add quick for remaining items since we know the queue will not be empty.
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
                if (!_wasWaitedOn)
                {
                    // TODO: throw rejection
                }
                Dispose();
            }
        }

        // TODO: Repool these
        private static ValueLinkedStack<Internal.ITreeHandleAble> pendingDisposePool;

        private static void AddToDisposePool(Internal.ITreeHandleAble disposable)
        {
            pendingDisposePool.Push(disposable);
        }

        // Handle promises in a breadth-first manner.
        private static ValueLinkedQueue<Internal.ITreeHandleAble> _handleQueue;
        private static bool _runningHandles;

        protected static void AddToHandleQueue(Promise promise)
        {
            promise._notHandling = false;
            _handleQueue.AddLast(promise);
        }

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

		protected Promise() : base() { }

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

		public override string ToString()
		{
#if DEBUG
			return string.Format("Type: Promise<{0}>, Id: {1}, State: {2}", typeof(T), _id, _state);
#else
			return string.Format("Type: Promise<{0}>, State: {1}", typeof(T), _state);
#endif
        }
    }

	partial class Promise
	{
		protected static partial class Internal
		{
			internal static Action OnClearPool;

			public abstract class PoolablePromise<TPromise> : Promise where TPromise : PoolablePromise<TPromise>
			{
#pragma warning disable RECS0108 // Warns about static fields in generic types
				protected static ValueLinkedStack<ITreeHandleAble> _pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

				static PoolablePromise()
				{
					OnClearPool += () => _pool.Clear();
				}

				protected override void Dispose()
				{
					base.Dispose();
					_pool.Push(this);
				}
			}

			public abstract class PoolablePromise<T, TPromise> : Promise<T> where TPromise : PoolablePromise<T, TPromise>
			{
#pragma warning disable RECS0108 // Warns about static fields in generic types
				protected static ValueLinkedStack<ITreeHandleAble> _pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

				static PoolablePromise()
				{
					OnClearPool += () => _pool.Clear();
				}

				protected override void Dispose()
				{
					base.Dispose();
					_pool.Push(this);
				}
			}

            public sealed class LitePromise : PoolablePromise<LitePromise>
			{
				private LitePromise() { }

				public static LitePromise GetOrCreate()
				{
					var promise = _pool.IsNotEmpty ? (LitePromise) _pool.Pop() : new LitePromise();
					promise.Reset();
                    promise.ResetDepth();
                    return promise;
				}
			}

			public sealed class LitePromise<T> : PoolablePromise<T, LitePromise<T>>
			{
				private LitePromise() { }

				public static LitePromise<T> GetOrCreate()
				{
					var promise = _pool.IsNotEmpty ? (LitePromise<T>) _pool.Pop() : new LitePromise<T>();
					promise.Reset();
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
					var promise = _pool.IsNotEmpty ? (DeferredPromise) _pool.Pop() : new DeferredPromise();
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
					var promise = _pool.IsNotEmpty ? (DeferredPromise<T>) _pool.Pop() : new DeferredPromise<T>();
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
					var promise = _pool.IsNotEmpty ? (PromiseVoidResolve) _pool.Pop() : new PromiseVoidResolve();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
				}

                protected override void Handle(Promise feed)
                {
                    base.Handle(feed);
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (_state == DeferredState.Resolved)
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
					var promise = _pool.IsNotEmpty ? (PromiseArgResolve<TArg>) _pool.Pop() : new PromiseArgResolve<TArg>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
                }

                protected override void Handle(Promise feed)
                {
                    base.Handle(feed);
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (_state == DeferredState.Resolved)
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
					var promise = _pool.IsNotEmpty ? (PromiseVoidResolve<TResult>) _pool.Pop() : new PromiseVoidResolve<TResult>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
                }

                protected override void Handle(Promise feed)
                {
                    base.Handle(feed);
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (_state == DeferredState.Resolved)
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
					var promise = _pool.IsNotEmpty ? (PromiseArgResolve<TArg, TResult>) _pool.Pop() : new PromiseArgResolve<TArg, TResult>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
                }

                protected override void Handle(Promise feed)
                {
                    base.Handle(feed);
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (_state == DeferredState.Resolved)
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
					var promise = _pool.IsNotEmpty ? (PromiseVoidResolvePromise) _pool.Pop() : new PromiseVoidResolvePromise();
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
                        if (feed._state == DeferredState.Resolved)
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
					var promise = _pool.IsNotEmpty ? (PromiseArgResolvePromise<TArg>) _pool.Pop() : new PromiseArgResolvePromise<TArg>();
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
                        if (feed._state == DeferredState.Resolved)
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
					var promise = _pool.IsNotEmpty ? (PromiseVoidResolvePromise<TPromise>) _pool.Pop() : new PromiseVoidResolvePromise<TPromise>();
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
                        if (feed._state == DeferredState.Resolved)
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
					var promise = _pool.IsNotEmpty ? (PromiseArgResolvePromise<TArg, TPromise>) _pool.Pop() : new PromiseArgResolvePromise<TArg, TPromise>();
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
                        if (feed._state == DeferredState.Resolved)
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
					var promise = _pool.IsNotEmpty ? (PromiseVoidResolveDeferred) _pool.Pop() : new PromiseVoidResolveDeferred();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
                    promise.Retain();
					return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == DeferredState.Resolved)
                    {
                        var deferredAction = callback.Invoke();
                        Validate(deferredAction);
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
					var promise = _pool.IsNotEmpty ? (PromiseArgResolveDeferred<TArg>) _pool.Pop() : new PromiseArgResolveDeferred<TArg>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
                    promise.Retain();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == DeferredState.Resolved)
                    {
                        var deferredAction = callback.Invoke(feed.GetValue<TArg>());
                        Validate(deferredAction);
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
					var promise = _pool.IsNotEmpty ? (PromiseVoidResolveDeferred<TDeferred>) _pool.Pop() : new PromiseVoidResolveDeferred<TDeferred>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
                    promise.Retain();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == DeferredState.Resolved)
                    {
                        var deferredAction = callback.Invoke();
                        Validate(deferredAction);
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
					var promise = _pool.IsNotEmpty ? (PromiseArgResolveDeferred<TArg, TDeferred>) _pool.Pop() : new PromiseArgResolveDeferred<TArg, TDeferred>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
                    promise.Retain();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = resolveHandler;
                    resolveHandler = null;
                    if (feed._state == DeferredState.Resolved)
                    {
                        var deferredAction = callback.Invoke(feed.GetValue<TArg>());
                        Validate(deferredAction);
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
					var promise = _pool.IsNotEmpty ? (PromiseReject) _pool.Pop() : new PromiseReject();
					promise.rejectHandler = rejectHandler;
					promise.Reset();
					return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == DeferredState.Rejected)
                    {
                        _state = feed._state; // Set the state so a Cancel call won't do anything during invoke.
                        _notHandling = false; // Set handling flag so a .Then/.Catch during invoke won't add to handle queue.
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue))
                        {
                            _state = DeferredState.Resolved;
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
					var promise = _pool.IsNotEmpty ? (PromiseReject<T>) _pool.Pop() : new PromiseReject<T>();
					promise.rejectHandler = rejectHandler;
					promise.Reset();
					return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == DeferredState.Rejected)
                    {
                        _state = feed._state; // Set the state so a Cancel call won't do anything during invoke.
                        _notHandling = false; // Set handling flag so a .Then/.Catch during invoke won't add to handle queue.
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out _value))
                        {
                            _state = DeferredState.Resolved;
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
					var promise = _pool.IsNotEmpty ? (PromiseRejectPromise) _pool.Pop() : new PromiseRejectPromise();
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
                        if (feed._state == DeferredState.Rejected)
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
					var promise = _pool.IsNotEmpty ? (PromiseRejectPromise<TPromise>) _pool.Pop() : new PromiseRejectPromise<TPromise>();
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
                        if (feed._state == DeferredState.Rejected)
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
					var promise = _pool.IsNotEmpty ? (PromiseRejectDeferred) _pool.Pop() : new PromiseRejectDeferred();
					promise.rejectHandler = rejectHandler;
					promise.Reset();
                    promise.Retain();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == DeferredState.Rejected)
                    {
                        Action<Deferred> deferredDelegate;
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out deferredDelegate))
                        {
                            Validate(deferredDelegate);
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
                    var promise = _pool.IsNotEmpty ? (PromiseRejectDeferred<TDeferred>) _pool.Pop() : new PromiseRejectDeferred<TDeferred>();
                    promise.rejectHandler = rejectHandler;
                    promise.Reset();
                    return promise;
                }

                protected override void Handle(Promise feed)
                {
                    var callback = rejectHandler;
                    rejectHandler = null;
                    if (feed._state == DeferredState.Rejected)
                    {
                        Action<Deferred> deferredDelegate;
                        if (callback.DisposeAndTryInvoke(feed._rejectedOrCanceledValue, out deferredDelegate))
                        {
                            Validate(deferredDelegate);
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
					var promise = _pool.IsNotEmpty ? (PromiseResolveReject) _pool.Pop() : new PromiseResolveReject();
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
                    if (feed._state == DeferredState.Resolved)
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
                    _state = DeferredState.Resolved;
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
					var promise = _pool.IsNotEmpty ? (PromiseResolveReject<T>) _pool.Pop() : new PromiseResolveReject<T>();
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
                    if (feed._state == DeferredState.Resolved)
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
                    _state = DeferredState.Resolved;
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
					var promise = _pool.IsNotEmpty ? (PromiseResolveRejectPromise) _pool.Pop() : new PromiseResolveRejectPromise();
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
                        if (feed._state == DeferredState.Resolved)
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
					var promise = _pool.IsNotEmpty ? (PromiseResolveRejectPromise<TPromise>) _pool.Pop() : new PromiseResolveRejectPromise<TPromise>();
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
                        if (feed._state == DeferredState.Resolved)
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
					var promise = _pool.IsNotEmpty ? (PromiseResolveRejectDeferred) _pool.Pop() : new PromiseResolveRejectDeferred();
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
                    if (feed._state == DeferredState.Resolved)
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
                    Validate(deferredDelegate);
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
					var promise = _pool.IsNotEmpty ? (PromiseResolveRejectDeferred<TDeferred>) _pool.Pop() : new PromiseResolveRejectDeferred<TDeferred>();
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
                    if (feed._state == DeferredState.Resolved)
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
                    Validate(deferredDelegate);
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
					var promise = _pool.IsNotEmpty ? (PromiseComplete) _pool.Pop() : new PromiseComplete();
					promise.onComplete = onComplete;
					promise.Reset();
					return promise;
				}

                protected override void Handle(Promise feed)
                {
                    _state = DeferredState.Resolved;
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
					var promise = _pool.IsNotEmpty ? (PromiseComplete<T>) _pool.Pop() : new PromiseComplete<T>();
					promise.onComplete = onComplete;
					promise.Reset();
					return promise;
                }

                protected override void Handle(Promise feed)
                {
                    _state = DeferredState.Resolved;
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
					var promise = _pool.IsNotEmpty ? (PromiseCompletePromise) _pool.Pop() : new PromiseCompletePromise();
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
					var promise = _pool.IsNotEmpty ? (PromiseCompletePromise<T>) _pool.Pop() : new PromiseCompletePromise<T>();
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
					var promise = _pool.IsNotEmpty ? (PromiseCompleteDeferred) _pool.Pop() : new PromiseCompleteDeferred();
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
                    Validate(deferredDelegate);
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
					var promise = _pool.IsNotEmpty ? (PromiseCompleteDeferred<T>) _pool.Pop() : new PromiseCompleteDeferred<T>();
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
                    Validate(deferredDelegate);
                    deferredDelegate.Invoke(deferred);
                }

                protected override void OnCancel()
                {
                    base.OnCancel();
                    onComplete = null;
                }
            }
            #endregion

#pragma warning disable RECS0001 // Class is declared partial but has only one part
            public sealed partial class FinallyDelegate : ITreeHandleAble
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
                    var del = _pool.IsNotEmpty ? (FinallyDelegate) _pool.Pop() : new FinallyDelegate();
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

            public sealed partial class CancelDelegate : ITreeHandleAble
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
                    var del = _pool.IsNotEmpty ? (CancelDelegate) _pool.Pop() : new CancelDelegate();
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

            public sealed partial class CancelDelegate<T> : ITreeHandleAble
            {
                ITreeHandleAble ILinked<ITreeHandleAble>.Next { get; set; }

#pragma warning disable RECS0108 // Warns about static fields in generic types
                private static ValueLinkedStack<ITreeHandleAble> _pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

                private IValueContainer _cancelValue;
                private Action<T> _onCanceled;

                private CancelDelegate() { }

                static CancelDelegate()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static CancelDelegate<T> GetOrCreate(Action<T> onCanceled)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegate<T>) _pool.Pop() : new CancelDelegate<T>();
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

                    if (State != DeferredState.Pending)
                    {
                        Logger.LogWarning("Deferred.ReportProgress - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.ReportProgress(progress);
                }

                public override void Resolve()
                {
                    ValidateOperation(Promise);

                    if (State != DeferredState.Pending)
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

                    if (State != DeferredState.Pending)
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

                    if (State != DeferredState.Pending)
                    {
                        Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.Cancel(reason);
                }

                public override void Reject()
                {
                    ValidateOperation(Promise);

                    if (State != DeferredState.Pending)
                    {
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.Reject(1);
                }

                public override void Reject<TReject>(TReject reason)
                {
                    ValidateOperation(Promise);

                    if (State != DeferredState.Pending)
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
        protected static new partial class Internal
        {
            public sealed partial class DeferredInternal : Deferred
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

                    if (State != DeferredState.Pending)
                    {
                        Logger.LogWarning("Deferred.ReportProgress - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.ReportProgress(progress);
                }

                public override void Resolve(T arg)
                {
                    ValidateOperation(Promise);

                    if (State != DeferredState.Pending)
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

                    if (State != DeferredState.Pending)
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

                    if (State != DeferredState.Pending)
                    {
                        Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.Cancel(reason);
                }

                public override void Reject()
                {
                    ValidateOperation(Promise);

                    if (State != DeferredState.Pending)
                    {
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                        return;
                    }

                    Promise.Reject(1);
                }

                public override void Reject<TReject>(TReject reason)
                {
                    ValidateOperation(Promise);

                    if (State != DeferredState.Pending)
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