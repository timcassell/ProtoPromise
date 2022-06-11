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
                    if (_state != State.Disposed)
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

            private ValueLinkedStackZeroGC<CancelationRegistration> _links = ValueLinkedStackZeroGC<CancelationRegistration>.Create();
            // Use a sentinel for the linked list so we don't need to null check.
            private readonly CancelationCallbackNode _registeredCallbacksHead = CancelationCallbackNode.CreateLinkedListSentinel();
            internal SpinLocker _spinner = new SpinLocker();
            // Start with Id 1 instead of 0 to reduce risk of false positives.
            private int _sourceId = 1;
            private int _tokenId = 1;
            private uint _userRetainCounter;
            private byte _internalRetainCounter;
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

            internal static CancelationRef GetOrCreate()
            {
                var cancelRef = ObjectPool.TryTake<CancelationRef>()
                    ?? new CancelationRef();
                cancelRef._internalRetainCounter = 1; // 1 for Dispose.
                cancelRef._state = State.Pending;
                SetCreatedStacktrace(cancelRef, 2);
                return cancelRef;
            }

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

            [MethodImpl(InlineOption)]
            internal static void MaybeAddLinkedCancelation(CancelationRef listener, CancelationRef _this, int tokenId)
            {
                if (_this != null)
                {
                    _this.MaybeAddLinkedCancelation(listener, tokenId);
                }
            }

            [MethodImpl(InlineOption)]
            internal void MaybeAddLinkedCancelation(CancelationRef listener, int tokenId)
            {
                _spinner.Enter();
                if (tokenId != _tokenId)
                {
                    _spinner.Exit();
                    return;
                }

                State state = _state;
                if (state == State.Pending)
                {
                    listener._spinner.Enter();
                    if (listener._state == State.Pending) // Make sure listener wasn't canceled from another token on another thread.
                    {
                        var node = CallbackNodeImpl<CancelationRef>.GetOrCreate(listener, this);
                        _registeredCallbacksHead.InsertPrevious(node);
                        listener._links.Push(new CancelationRegistration(node, node.NodeId, TokenId));
                    }
                    listener._spinner.Exit();
                    _spinner.Exit();
                    return;
                }
                _spinner.Exit();

                if (state == State.Canceled)
                {
                    listener.TryInvokeCallbacks();
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
                _spinner.Enter();
                int oldTokenId = TokenId;
                if (tokenId != oldTokenId)
                {
                    _spinner.Exit();
                    registration = default(CancelationRegistration);
                    return false;
                }

                State state = _state;
                if (state == State.Pending)
                {
                    var node = CallbackNodeImpl<TCancelable>.GetOrCreate(cancelable, this);
                    _registeredCallbacksHead.InsertPrevious(node);
                    _spinner.Exit();
                    registration = new CancelationRegistration(node, node.NodeId, oldTokenId);
                    return true;
                }

                _spinner.Exit();
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
                _spinner.Enter();
                if (sourceId != SourceId | _state != State.Pending)
                {
                    _spinner.Exit();
                    return false;
                }
                _state = State.Canceled;
                ++_internalRetainCounter;
                _spinner.Exit();

                InvokeCallbacks();
                return true;
            }

            private bool TryInvokeCallbacks()
            {
                _spinner.Enter();
                if (_state != State.Pending)
                {
                    _spinner.Exit();
                    return false;
                }
                _state = State.Canceled;
                ++_internalRetainCounter;
                _spinner.Exit();

                InvokeCallbacks();
                return true;
            }

            private bool InvokeCallbacks()
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
                return true;
            }

            [MethodImpl(InlineOption)]
            internal static bool TryDispose(CancelationRef _this, int sourceId)
            {
                return _this != null && _this.TryDispose(sourceId);
            }

            [MethodImpl(InlineOption)]
            internal bool TryDispose(int sourceId)
            {
                _spinner.Enter();
                if (sourceId != SourceId)
                {
                    _spinner.Exit();
                    return false;
                }

                ++_sourceId;
                if (_state != State.Pending)
                {
                    MaybeResetAndRepoolNoLock();
                    _spinner.Exit();
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
                _spinner.Exit();

                MaybeResetAndRepool();
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
                _spinner.Enter();
                if (tokenId != TokenId)
                {
                    _spinner.Exit();
                    return false;
                }

                ThrowIfInPool(this);
                checked
                {
                    ++_userRetainCounter;
                }
                _spinner.Exit();
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
                _spinner.Enter();
                if (tokenId != TokenId)
                {
                    _spinner.Exit();
                    return false;
                }
                checked
                {
                    if (--_userRetainCounter == 0 & _internalRetainCounter == 0)
                    {
                        ++_tokenId;
                        _spinner.Exit();
                        ResetAndRepool();
                        return true;
                    }
                }
                _spinner.Exit();
                return true;
            }

            private void MaybeResetAndRepool()
            {
                _spinner.Enter();
                MaybeResetAndRepoolNoLock();
                _spinner.Exit();
            }

            private void MaybeResetAndRepoolNoLock()
            {
                if (--_internalRetainCounter == 0 & _userRetainCounter == 0)
                {
                    ++_tokenId;
                    _spinner.Exit();
                    ResetAndRepool();
                    return;
                }
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

            void ICancelable.Cancel()
            {
                TryInvokeCallbacks();
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
                        _parent._spinner.Enter();
                        Dispose();
                        _parent._spinner.Exit();
                    }
#else
                    Dispose();
                    canceler.Cancel();
#endif
                }

                [MethodImpl(InlineOption)]
                internal override void Dispose()
                {
                    ThrowIfInPool(this);
                    ++_nodeId;
                    _previous = null;
                    _cancelable = default(TCancelable);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _disposed = true;
#endif
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
                bool canceled = _parent._state == CancelationRef.State.Canceled;
                bool tokenIdMatches = _parent.TokenId == tokenId;
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
                _parent._spinner.Enter();
                bool canceled;
                bool isRegistered = IsRegistered(nodeId, tokenId, out canceled);
                if (!isRegistered)
                {
                    _parent._spinner.Exit();
                    isCanceled = canceled;
                    return false;
                }

                _previous._next = _next;
                _next.UnsafeAs<CancelationCallbackNode>()._previous = _previous;

                Dispose();
                _parent._spinner.Exit();
                isCanceled = canceled;
                return true;
            }
        }
    } // class Internal
} // namespace Proto.Promises