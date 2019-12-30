#pragma warning disable IDE0034 // Simplify 'default' expression

using System;

namespace Proto.Promises
{
    partial class Promise
    {
        partial class Internal
        {
            // Individual types for more common .ThenCapture(onResolved) calls to be more efficient.

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
        }
    }
}