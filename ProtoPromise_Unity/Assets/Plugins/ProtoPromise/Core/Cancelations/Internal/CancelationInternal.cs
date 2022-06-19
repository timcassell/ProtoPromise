#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Proto.Promises
{
    internal static partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal struct CancelDelegateTokenVoid : ICancelable
        {
            private readonly Action _callback;

            [MethodImpl(InlineOption)]
            internal CancelDelegateTokenVoid(Action callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public void Cancel()
            {
                _callback.Invoke();
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal struct CancelDelegateToken<TCapture> : ICancelable
        {
            private readonly TCapture _capturedValue;
            private readonly Action<TCapture> _callback;

            [MethodImpl(InlineOption)]
            internal CancelDelegateToken(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                TCapture capturedValue, Action<TCapture> callback)
            {
                _capturedValue = capturedValue;
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public void Cancel()
            {
                _callback.Invoke(_capturedValue);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class CancelationRef : HandleablePromiseBase, ICancelable, ITraceable
        {
            internal static readonly CancelationRef s_canceledSentinel = new CancelationRef() { _state = State.Canceled, _internalRetainCounter = 1 };

#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif

            ~CancelationRef()
            {
                try
                {
                    if (_userRetainCounter > 0)
                    {
                        // CancelationToken wasn't released.
                        string message = "A CancelationToken's resources were garbage collected without being released. You must release all IRetainable objects that you have retained.";
                        ReportRejection(new UnreleasedObjectException(message), this);
                    }
                    if (_checkForDisposed & _state != State.Disposed)
                    {
                        // CancelationSource wasn't disposed.
                        ReportRejection(new UnreleasedObjectException("CancelationSource's resources were garbage collected without being disposed."), this);
                    }
                }
                catch (Exception e)
                {
                    // This should never happen.
                    ReportRejection(e, this);
                }
            }

            internal enum State : int
            {
                Pending,
                Canceled,
                Disposed
            }

#if !NET_LEGACY || NET40
            internal CancellationTokenSource _cancellationTokenSource;
#endif
            private ValueLinkedStackZeroGC<CancelationRegistration> _links = ValueLinkedStackZeroGC<CancelationRegistration>.Create();
            // Use a sentinel for the linked list so we don't need to null check.
            private readonly CancelationCallbackNode _registeredCallbacksHead = CancelationCallbackNode.CreateLinkedListSentinel();
            internal SpinLocker _locker = new SpinLocker();
            // Start with Id 1 instead of 0 to reduce risk of false positives.
            private int _sourceId = 1;
            private int _tokenId = 1;
            private uint _userRetainCounter;
            private byte _internalRetainCounter;
            private bool _checkForDisposed;
            internal State _state;

            internal int SourceId
            {
                [MethodImpl(InlineOption)]
                get { return _sourceId; }
            }
            internal int TokenId
            {
                [MethodImpl(InlineOption)]
                get { return _tokenId; }
            }

            [MethodImpl(InlineOption)]
            private void Initialize(bool checkForDisposed)
            {
                _internalRetainCounter = 1; // 1 for Dispose.
                _checkForDisposed = checkForDisposed;
                _state = State.Pending;
                SetCreatedStacktrace(this, 2);
            }

            internal static CancelationRef GetOrCreateWithoutDisposedCheck()
            {
                // Don't take from the object pool, just create new.
                var cancelRef = new CancelationRef();
                cancelRef.Initialize(false);
                return cancelRef;
            }

            [MethodImpl(InlineOption)]
            internal static CancelationRef GetOrCreate()
            {
                var cancelRef = ObjectPool.TryTake<CancelationRef>()
                    ?? new CancelationRef();
                cancelRef.Initialize(true);
                return cancelRef;
            }

#if !NET_LEGACY || NET40
            internal static CancellationToken GetCancellationToken(CancelationRef _this, int _tokenId)
            {
                return _this == null ? default(CancellationToken) : _this.GetCancellationToken(_tokenId);
            }

            private CancellationToken GetCancellationToken(int _tokenId)
            {
                _locker.Enter();
                try
                {
                    var state = _state;
                    if (_tokenId != TokenId | state == State.Disposed)
                    {
                        return default(CancellationToken);
                    }
                    if (state == State.Canceled)
                    {
                        return new CancellationToken(true);
                    }
                    if (_cancellationTokenSource == null)
                    {
                        _cancellationTokenSource = new CancellationTokenSource();
                        _cancellationTokenSource.AttachCancelationRef(this);
                        var del = new CancelDelegateToken<CancellationTokenSource>(_cancellationTokenSource, source => source.Cancel(false));
                        var node = CallbackNodeImpl<CancelDelegateToken<CancellationTokenSource>>.GetOrCreate(del, this);
                        _registeredCallbacksHead.InsertPrevious(node);
                    }
                    return _cancellationTokenSource.Token;
                }
                finally
                {
                    _locker.Exit();
                }
            }
#endif

            [MethodImpl(InlineOption)]
            internal static bool IsValidSource(CancelationRef _this, int sourceId)
            {
                return _this != null && _this.SourceId == sourceId;
            }

            [MethodImpl(InlineOption)]
            internal static bool IsSourceCanceled(CancelationRef _this, int sourceId)
            {
                return _this != null && _this.IsSourceCanceled(sourceId);
            }

            [MethodImpl(InlineOption)]
            private bool IsSourceCanceled(int sourceId)
            {
                return sourceId == SourceId & _state == State.Canceled;
            }

            [MethodImpl(InlineOption)]
            internal static bool CanTokenBeCanceled(CancelationRef _this, int tokenId)
            {
                return _this != null && _this.TokenId == tokenId;
            }

            [MethodImpl(InlineOption)]
            internal static bool IsTokenCanceled(CancelationRef _this, int tokenId)
            {
                return _this != null && _this.IsTokenCanceled(tokenId);
            }

            [MethodImpl(InlineOption)]
            private bool IsTokenCanceled(int tokenId)
            {
                return tokenId == TokenId & _state == State.Canceled;
            }

            [MethodImpl(InlineOption)]
            internal static void ThrowIfCanceled(CancelationRef _this, int tokenId)
            {
                if (_this != null && _this.IsTokenCanceled(tokenId))
                {
                    throw CanceledExceptionInternal.GetOrCreate();
                }
            }

            internal void MaybeLinkToken(CancelationToken token)
            {
                CancelationRegistration linkedRegistration;
                if (token.TryRegister(this, out linkedRegistration))
                {
                    _locker.Enter();
                    // Register may have invoked Cancel synchronously or on another thread, so we check the state here before adding the registration for later unlinking.
                    if (_state == State.Pending)
                    {
                        _links.Push(linkedRegistration);
                    }
                    _locker.Exit();
                }
            }

            [MethodImpl(InlineOption)]
            internal static bool TryRegister<TCancelable>(CancelationRef _this, int tokenId,
#if CSHARP_7_3_OR_NEWER
                in
#endif
                TCancelable cancelable, out CancelationRegistration registration) where TCancelable : ICancelable
            {
                if (_this == null)
                {
                    registration = default(CancelationRegistration);
                    return false;
                }
                return _this.TryRegister(cancelable, tokenId, out registration);
            }

            [MethodImpl(InlineOption)]
            private bool TryRegister<TCancelable>(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                TCancelable cancelable, int tokenId, out CancelationRegistration registration) where TCancelable : ICancelable
            {
                _locker.Enter();
                int oldTokenId = TokenId;
                if (tokenId != oldTokenId)
                {
                    _locker.Exit();
                    registration = default(CancelationRegistration);
                    return false;
                }

                State state = _state;
                if (state == State.Pending)
                {
                    var node = CallbackNodeImpl<TCancelable>.GetOrCreate(cancelable, this);
                    _registeredCallbacksHead.InsertPrevious(node);
                    _locker.Exit();
                    registration = new CancelationRegistration(node, node.NodeId, oldTokenId);
                    return true;
                }

                _locker.Exit();
                registration = default(CancelationRegistration);
                if (state == State.Canceled)
                {
                    cancelable.Cancel();
                    return true;
                }
                return false;
            }

            [MethodImpl(InlineOption)]
            internal static bool TrySetCanceled(CancelationRef _this, int sourceId)
            {
                return _this != null && _this.TrySetCanceled(sourceId);
            }

            [MethodImpl(InlineOption)]
            private bool TrySetCanceled(int sourceId)
            {
                _locker.Enter();
                if (sourceId != SourceId | _state != State.Pending)
                {
                    _locker.Exit();
                    return false;
                }
                _state = State.Canceled;
                ++_internalRetainCounter;
                _locker.Exit();

                InvokeCallbacks();
                return true;
            }

            private void InvokeCallbacks()
            {
                ThrowIfInPool(this);
                Unlink();
                List<Exception> exceptions = null;
                var next = _registeredCallbacksHead._next;
                _registeredCallbacksHead._next = _registeredCallbacksHead;
                _registeredCallbacksHead._previous = _registeredCallbacksHead;
                while (next != _registeredCallbacksHead)
                {
                    var current = next.UnsafeAs<CancelationCallbackNode>();
                    next = current._next;
                    try
                    {
                        current.Invoke();
                    }
                    catch (Exception e)
                    {
                        if (exceptions == null)
                        {
                            exceptions = new List<Exception>();
                        }
                        exceptions.Add(e);
                    }
                }

                MaybeResetAndRepool();
                if (exceptions != null)
                {
                    // Propagate exceptions to caller as aggregate.
                    throw new AggregateException(exceptions);
                }
            }

            [MethodImpl(InlineOption)]
            internal static bool TryDispose(CancelationRef _this, int sourceId)
            {
                return _this != null && _this.TryDispose(sourceId);
            }

            [MethodImpl(InlineOption)]
            internal bool TryDispose(int sourceId)
            {
                _locker.Enter();
                if (sourceId != SourceId)
                {
                    _locker.Exit();
                    return false;
                }

                ++_sourceId;
                if (_state != State.Pending)
                {
                    MaybeResetAndRepoolAlreadyLocked();
                    return true;
                }

                ThrowIfInPool(this);
                _state = State.Disposed;
                Unlink();
                var next = _registeredCallbacksHead._next;
                _registeredCallbacksHead._next = _registeredCallbacksHead;
                _registeredCallbacksHead._previous = _registeredCallbacksHead;
                while (next != _registeredCallbacksHead)
                {
                    var current = next.UnsafeAs<CancelationCallbackNode>();
                    next = current._next;
                    current.Dispose();
                }

                MaybeResetAndRepoolAlreadyLocked();
                return true;
            }

            private void Unlink()
            {
                while (_links.IsNotEmpty)
                {
                    _links.Pop().TryUnregister();
                }
            }

            [MethodImpl(InlineOption)]
            internal static bool TryRetainUser(CancelationRef _this, int tokenId)
            {
                return _this != null && _this.TryRetainUser(tokenId);
            }

            [MethodImpl(InlineOption)]
            private bool TryRetainUser(int tokenId)
            {
                _locker.Enter();
                if (tokenId != TokenId)
                {
                    _locker.Exit();
                    return false;
                }

                ThrowIfInPool(this);
                checked
                {
                    ++_userRetainCounter;
                }
                _locker.Exit();
                return true;
            }

            [MethodImpl(InlineOption)]
            internal static bool TryReleaseUser(CancelationRef _this, int tokenId)
            {
                return _this != null && _this.TryReleaseUser(tokenId);
            }

            [MethodImpl(InlineOption)]
            private bool TryReleaseUser(int tokenId)
            {
                _locker.Enter();
                if (tokenId != TokenId)
                {
                    _locker.Exit();
                    return false;
                }
                checked
                {
                    if (--_userRetainCounter == 0 & _internalRetainCounter == 0)
                    {
                        ++_tokenId;
                        _locker.Exit();
                        ResetAndRepool();
                        return true;
                    }
                }
                _locker.Exit();
                return true;
            }

            private void MaybeResetAndRepool()
            {
                _locker.Enter();
                MaybeResetAndRepoolAlreadyLocked();
            }

            private void MaybeResetAndRepoolAlreadyLocked()
            {
                if (--_internalRetainCounter == 0 & _userRetainCounter == 0)
                {
#if !NET_LEGACY || NET40
                    if (_cancellationTokenSource != null)
                    {
                        // TODO: We can call _cancellationTokenSource.TryReset() in .Net 6+ instead of always creating a new one.
                        // But this should only be done if we add a TryReset() API to our own CancelationSource, because if a user still holds an old token after this is reused, it could have cancelations triggered unexpectedly.
                        _cancellationTokenSource.Dispose();
                        _cancellationTokenSource = null;
                        Thread.MemoryBarrier();
                    }
#endif
                    ++_tokenId;
                    _locker.Exit();
                    ResetAndRepool();
                    return;
                }
                _locker.Exit();
            }

            [MethodImpl(InlineOption)]
            private void ResetAndRepool()
            {
                ThrowIfInPool(this);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (_registeredCallbacksHead._next != _registeredCallbacksHead || _registeredCallbacksHead._previous != _registeredCallbacksHead)
                {
                    throw new System.InvalidOperationException("CancelationToken callbacks have not been unregistered.");
                }
#endif
                _state = State.Disposed;
                ObjectPool.MaybeRepool(this);
            }

            public void Cancel()
            {
                TrySetCanceled(SourceId);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode]
#endif
            private sealed class CallbackNodeImpl<TCancelable> : CancelationCallbackNode, ITraceable
                where TCancelable : ICancelable
            {
#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif

                private TCancelable _cancelable;

                private CallbackNodeImpl() { }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                volatile private bool _disposed;

                ~CallbackNodeImpl()
                {
                    try
                    {
                        if (!_disposed)
                        {
                            // For debugging. This should never happen.
                            string message = "A " + GetType() + " was garbage collected without it being disposed.";
                            ReportRejection(new UnreleasedObjectException(message), this);
                        }
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        ReportRejection(e, this);
                    }
                }
#endif

                [MethodImpl(InlineOption)]
                internal static CallbackNodeImpl<TCancelable> GetOrCreate(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCancelable cancelable, CancelationRef parent)
                {
                    var del = ObjectPool.TryTake<CallbackNodeImpl<TCancelable>>()
                        ?? new CallbackNodeImpl<TCancelable>();
                    del._cancelable = cancelable;
                    del._parent = parent;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    del._disposed = false;
#endif
                    SetCreatedStacktrace(del, 2);
                    return del;
                }

                internal override void Invoke()
                {
                    ThrowIfInPool(this);
                    var parent = _parent;
                    var canceler = _cancelable;
#if PROMISE_DEBUG
                    SetCurrentInvoker(this);
                    try
                    {
                        canceler.Cancel();
                    }
                    finally
                    {
                        ClearCurrentInvoker();
                        parent._locker.Enter();
                        DisposeAlreadyLocked(parent);
                    }
#else
                    parent._locker.Enter();
                    DisposeAlreadyLocked(parent);
                    canceler.Cancel();
#endif
                }

                [MethodImpl(InlineOption)]
                internal override void Dispose()
                {
                    ResetForDispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private void ResetForDispose()
                {
                    ThrowIfInPool(this);
                    ++_nodeId;
                    _previous = null;
                    _cancelable = default(TCancelable);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _disposed = true;
#endif
                }

                protected override void DisposeAlreadyLocked(CancelationRef parent)
                {
                    ResetForDispose();
                    parent._locker.Exit();
                    ObjectPool.MaybeRepool(this);
                }
            }
        } // class CancelationRef

        internal class CancelationCallbackNode : HandleablePromiseBase
        {
            internal CancelationCallbackNode _previous; // _next is HandleablePromiseBase which we just unsafe cast to CallbackNode.
            protected CancelationRef _parent;
            protected int _nodeId = 1; // Start with id 1 instead of 0 to reduce risk of false positives.

            internal CancelationRef Parent
            {
                [MethodImpl(InlineOption)]
                get { return _parent; }
            }

            internal int NodeId
            {
                [MethodImpl(InlineOption)]
                get { return _nodeId; }
            }

            protected CancelationCallbackNode() { }

            internal static CancelationCallbackNode CreateLinkedListSentinel()
            {
                var sentinel = new CancelationCallbackNode();
                sentinel._next = sentinel;
                sentinel._previous = sentinel;
                return sentinel;
            }

            internal void InsertPrevious(CancelationCallbackNode node)
            {
                node._previous = _previous;
                node._next = this;
                _previous._next = node;
                _previous = node;
            }

            internal virtual void Invoke() { throw new System.InvalidOperationException(); }
            internal virtual void Dispose() { throw new System.InvalidOperationException(); }

            [MethodImpl(InlineOption)]
            internal static bool GetIsRegisteredAndIsCanceled(CancelationCallbackNode _this, int nodeId, int tokenId, out bool isCanceled)
            {
                if (_this == null)
                {
                    isCanceled = false;
                    return false;
                }
                return _this.IsRegistered(nodeId, tokenId, out isCanceled);
            }

            [MethodImpl(InlineOption)]
            private bool IsRegistered(int nodeId, int tokenId, out bool isCanceled)
            {
                return GetIsRegistered(_parent, nodeId, tokenId, out isCanceled);
            }

            [MethodImpl(InlineOption)]
            private bool GetIsRegistered(CancelationRef parent, int nodeId, int tokenId, out bool isCanceled)
            {
                bool canceled = parent._state == CancelationRef.State.Canceled;
                bool tokenIdMatches = parent.TokenId == tokenId;
                isCanceled = canceled & tokenIdMatches;
                return !canceled & tokenIdMatches && _nodeId == nodeId;
            }

            [MethodImpl(InlineOption)]
            internal static bool TryUnregister(CancelationCallbackNode _this, int nodeId, int tokenId, out bool isCanceled)
            {
                if (_this == null)
                {
                    isCanceled = false;
                    return false;
                }
                return _this.TryUnregister(nodeId, tokenId, out isCanceled);
            }

            [MethodImpl(InlineOption)]
            private bool TryUnregister(int nodeId, int tokenId, out bool isCanceled)
            {
                var parent = _parent;
                parent._locker.Enter();
                bool canceled;
                bool isRegistered = GetIsRegistered(parent, nodeId, tokenId, out canceled);
                if (!isRegistered)
                {
                    parent._locker.Exit();
                    isCanceled = canceled;
                    return false;
                }

                _previous._next = _next;
                _next.UnsafeAs<CancelationCallbackNode>()._previous = _previous;
                DisposeAlreadyLocked(parent);

                isCanceled = canceled;
                return true;
            }

            protected virtual void DisposeAlreadyLocked(CancelationRef parent) { throw new System.InvalidOperationException(); }
        }
    } // class Internal
} // namespace Proto.Promises