#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0251 // Make member 'readonly'
#pragma warning disable IDE0270 // Use coalesce expression
#pragma warning disable IDE0290 // Use primary constructor

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct CancelDelegateTokenVoid : ICancelable
        {
            private readonly Action _callback;

            [MethodImpl(InlineOption)]
            internal CancelDelegateTokenVoid(Action callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public void Cancel()
                => _callback.Invoke();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct CancelDelegateToken<TCapture> : ICancelable
        {
            private readonly TCapture _capturedValue;
            private readonly Action<TCapture> _callback;

            [MethodImpl(InlineOption)]
            internal CancelDelegateToken(in TCapture capturedValue, Action<TCapture> callback)
            {
                _capturedValue = capturedValue;
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public void Cancel()
                => _callback.Invoke(_capturedValue);
        }

        internal abstract class CancelationLinkedListNode : HandleablePromiseBase
        {
            // _next and _previous are unsafe cast to CancelationLinkedListNode or CancelationCallbackNodeBase
            // so that we can use them for linked list nodes while using the CancelationRef as the sentinel.
            // This uses the least amount of memory while also avoiding null checks when adding and removing nodes.
            internal CancelationLinkedListNode _previous;

            internal virtual void Dispose() => throw new System.InvalidOperationException();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed partial class CancelationRef : CancelationLinkedListNode, ITraceable
        {
            internal static readonly CancelationRef s_canceledSentinel;

#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif

            static CancelationRef()
            {
                // Set _userRetainIncrementor to 0 so _userRetainCounter will never overflow.
                s_canceledSentinel = new CancelationRef(0) { _state = State.CanceledComplete, _internalRetainCounter = 1, _tokenId = -1 };
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
                // If we don't suppress, the finalizer can run when the AppDomain is unloaded, causing a NullReferenceException. This happens in Unity when switching between editmode and playmode.
                GC.SuppressFinalize(s_canceledSentinel);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
            }

            private CancelationRef() : this(1) { }

            private CancelationRef(byte userRetainIncrementor)
            {
                _userRetainIncrementor = userRetainIncrementor;
            }

            ~CancelationRef()
            {
                if (_userRetainCounter > 0)
                {
                    // CancelationToken wasn't released.
                    string message = "A CancelationToken's resources were garbage collected without being released. You must release all CancelationTokens that you have retained.";
                    ReportRejection(new UnreleasedObjectException(message), this);
                }
                // We don't check the disposed state if this was linked to a System.Threading.CancellationToken.
                if (!_linkedToBclToken & _state != State.Disposed)
                {
                    // CancelationSource wasn't disposed.
                    ReportRejection(new UnreleasedObjectException("CancelationSource's resources were garbage collected without being disposed."), this);
                }
            }

            internal enum State : byte
            {
                Pending,
                Disposed,
                Canceled,
                CanceledComplete,
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
            // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
            internal struct SmallFields
            {
                // Semi-unique instance id helps prevent accidents in case a CancelationRegistration is torn. This doesn't need to be fool-proof.
                private static int s_idCounter;

                // Must not be readonly.
                internal SpinLocker _locker;
                internal readonly int _instanceId;

                private SmallFields(int instanceId)
                {
                    _locker = new SpinLocker();
                    _instanceId = instanceId;
                }

                internal static SmallFields Create()
                    => new SmallFields(Interlocked.Increment(ref s_idCounter));
            }

#if NET6_0_OR_GREATER
            // Used to prevent a deadlock from synchronous invoke.
            [ThreadStatic]
            private static bool ts_isLinkingToBclToken;
#endif
            internal Thread _executingThread;
            // These must not be readonly.
            private ValueLinkedStack<LinkedCancelationNode> _links = new ValueLinkedStack<LinkedCancelationNode>();
            internal SmallFields _smallFields = SmallFields.Create();
            // Start with Id 1 instead of 0 to reduce risk of false positives.
            private int _sourceId = 1;
            private int _tokenId = 1;
            private uint _userRetainCounter;
            private readonly byte _userRetainIncrementor; // 0 for s_canceledSentinel, 1 for all others.
            private byte _internalRetainCounter;
            internal bool _linkedToBclToken;
            // There is no Volatile.Read API for enums, so we have to make the field volatile.
            volatile internal State _state;

            internal int SourceId
            {
                [MethodImpl(InlineOption)]
                get => _sourceId;
            }

            internal int TokenId
            {
                [MethodImpl(InlineOption)]
                get => _tokenId;
            }

            internal int VolatileTokenId
            {
                [MethodImpl(InlineOption)]
                get => Volatile.Read(ref _tokenId);
            }

            [MethodImpl(InlineOption)]
            private void Initialize(bool linkedToBclToken)
            {
                ResetLinkedListSentinel();
                _internalRetainCounter = 1; // 1 for Dispose.
                _linkedToBclToken = linkedToBclToken;
                _state = State.Pending;
                SetCreatedStacktrace(this, 2);
            }

            [MethodImpl(InlineOption)]
            private static CancelationRef GetFromPoolOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<CancelationRef>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new CancelationRef()
                    : obj.UnsafeAs<CancelationRef>();
            }

            [MethodImpl(InlineOption)]
            internal static CancelationRef GetOrCreate()
            {
                var cancelRef = GetFromPoolOrCreate();
                cancelRef.Initialize(false);
                return cancelRef;
            }

            [MethodImpl(InlineOption)]
            private void ResetLinkedListSentinel()
            {
                _next = this;
                _previous = this;
            }

            [MethodImpl(InlineOption)]
            internal static bool IsValidSource(CancelationRef _this, int sourceId)
                => _this != null && Volatile.Read(ref _this._sourceId) == sourceId;

            [MethodImpl(InlineOption)]
            internal static bool IsSourceCanceled(CancelationRef _this, int sourceId)
                => _this != null && _this.IsSourceCanceled(sourceId);

            [MethodImpl(InlineOption)]
            private bool IsSourceCanceled(int sourceId)
                // Volatile read the state before the id.
                => _state >= State.Canceled & sourceId == SourceId;

            [MethodImpl(InlineOption)]
            internal static bool CanTokenBeCanceled(CancelationRef _this, int tokenId)
                => _this != null
                    // Volatile read the state before the id.
                    && (_this._state != State.Disposed & _this.TokenId == tokenId);

            [MethodImpl(InlineOption)]
            internal static bool IsTokenCanceled(CancelationRef _this, int tokenId)
                => _this != null && _this.IsTokenCanceled(tokenId);

            [MethodImpl(InlineOption)]
            private bool IsTokenCanceled(int tokenId)
            {
                // Volatile read the state before everything else.
                var state = _state;
                return tokenId == TokenId & (state >= State.Canceled
                    // TODO: Unity hasn't adopted .Net 6+ yet, and they usually use different compilation symbols than .Net SDK, so we'll have to update the compilation symbols here once Unity finally does adopt it.
#if NET6_0_OR_GREATER
                    // This is only necessary in .Net 6 or later, since `CancellationTokenSource.TryReset()` was added.
                    | (_linkedToBclToken && _bclSource.IsCancellationRequested)
#endif
                    );
            }

            internal void MaybeLinkToken(CancelationToken token)
            {
                // If the token is invalid, or if this was already canceled from another token, don't hook it up.
                if (token._ref == null | _state >= State.Canceled)
                {
                    return;
                }

                var linkedNode = token._ref.LinkOrNull(this, token._id);
                if (linkedNode != null)
                {
                    _links.Push(linkedNode);
                }
            }

            [MethodImpl(InlineOption)]
            private LinkedCancelationNode LinkOrNull(CancelationRef other, int tokenId)
            {
                var nodeCreator = new LinkedNodeCreator(other);
                TryRegister(ref nodeCreator, tokenId);
                return nodeCreator._node;
            }

            [MethodImpl(InlineOption)]
            internal static bool TryRegister<TCancelable>(CancelationRef _this, int tokenId, in TCancelable cancelable, out CancelationRegistration registration)
                where TCancelable : ICancelable
            {
                if (_this == null)
                {
                    registration = default;
                    return false;
                }
                return _this.TryRegister(cancelable, tokenId, out registration);
            }

            [MethodImpl(InlineOption)]
            private bool TryRegister<TCancelable>(in TCancelable cancelable, int tokenId, out CancelationRegistration registration)
                where TCancelable : ICancelable
            {
                var nodeCreator = new UserNodeCreator<TCancelable>(cancelable);
                bool success = TryRegister(ref nodeCreator, tokenId);
                registration = nodeCreator._registration;
                return success;
            }

            [MethodImpl(InlineOption)]
            internal static bool TryRegister<TCancelable>(CancelationRef _this, int tokenId, in TCancelable cancelable, out CancelationRegistration registration, out bool alreadyCanceled)
                where TCancelable : ICancelable
            {
                if (_this == null)
                {
                    registration = default;
                    alreadyCanceled = false;
                    return false;
                }
                return _this.TryRegister(cancelable, tokenId, out registration, out alreadyCanceled);
            }

            [MethodImpl(InlineOption)]
            private bool TryRegister<TCancelable>(in TCancelable cancelable, int tokenId, out CancelationRegistration registration, out bool alreadyCanceled)
                where TCancelable : ICancelable
            {
                var nodeCreator = new UserNodeCreatorNoInvoke<TCancelable>(cancelable);
                bool success = TryRegister(ref nodeCreator, tokenId);
                registration = nodeCreator._registration;
                alreadyCanceled = nodeCreator._isCanceled;
                return success;
            }

            private bool TryRegister<TNodeCreator>(ref TNodeCreator nodeCreator, int tokenId)
                where TNodeCreator : INodeCreator
            {
                _smallFields._locker.Enter();
                State state = _state;
                bool isTokenMatched = tokenId == TokenId;
                if (!isTokenMatched | state != State.Pending)
                {
                    if (isTokenMatched & state >= State.Canceled)
                    {
                        ThrowIfInPool(this);
                        _smallFields._locker.Exit();
                        nodeCreator.Invoke();
                        return true;
                    }
                    _smallFields._locker.Exit();
                    return false;
                }

                ThrowIfInPool(this);
                // TODO: Unity hasn't adopted .Net 6+ yet, and they usually use different compilation symbols than .Net SDK, so we'll have to update the compilation symbols here once Unity finally does adopt it.
#if NET6_0_OR_GREATER
                // This is only necessary in .Net 6 or later, since `CancellationTokenSource.TryReset()` was added.
                if (_linkedToBclToken)
                {
                    System.Threading.CancellationToken token;
                    // If the source was disposed, the Token property will throw ObjectDisposedException. Unfortunately, this is the only way to check if it's disposed.
                    try
                    {
                        token = _bclSource.Token;
                    }
                    catch (ObjectDisposedException)
                    {
                        int sourceId = SourceId;
                        bool isCanceled = _bclSource.IsCancellationRequested;
                        if (isCanceled)
                        {
                            _smallFields._locker.Exit();
                            nodeCreator.Invoke();
                        }
                        else
                        {
                            UnregisterAll();
                            _smallFields._locker.Exit();
                        }
                        return isCanceled;
                    }

                    // If we are unable to unregister, it means the source had TryReset() called on it, or the token was canceled on another thread (and the other thread may be waiting on the lock).
                    if (!_bclRegistration.Unregister())
                    {
                        if (token.IsCancellationRequested)
                        {
                            _smallFields._locker.Exit();
                            nodeCreator.Invoke();
                            return true;
                        }
                        UnregisterAll();
                    }
                    // Callback could be invoked synchronously if the token is canceled on another thread,
                    // so we set a flag to prevent a deadlock, then check the flag again after the hookup to see if it was invoked.
                    ts_isLinkingToBclToken = true;
                    _bclRegistration = token.UnsafeRegister(cancelRef =>
                    {
                        // This could be invoked synchronously if the token is canceled, so we check the flag to prevent a deadlock.
                        if (ts_isLinkingToBclToken)
                        {
                            // Reset the flag so that we can tell that this was invoked synchronously.
                            ts_isLinkingToBclToken = false;
                            return;
                        }
                        cancelRef.UnsafeAs<CancelationRef>().Cancel();
                    }, this);

                    if (!ts_isLinkingToBclToken)
                    {
                        // Hook up the node instead of invoking since it might throw, and we need all registered callbacks to be invoked.
                        var node = nodeCreator.CreateNode(this, tokenId);
                        InsertPrevious(node);
                        InvokeCallbacksAlreadyLocked();
                        return true;
                    }
                    ts_isLinkingToBclToken = false;
                }
#endif

                {
                    var node = nodeCreator.CreateNode(this, tokenId);
                    InsertPrevious(node);
                    _smallFields._locker.Exit();
                    return true;
                }
            }

            private void InsertPrevious(CancelationCallbackNodeBase node)
            {
                node._previous = _previous;
                node._next = this;
                _previous._next = node;
                _previous = node;
            }

            [MethodImpl(InlineOption)]
            internal static bool TrySetCanceled(CancelationRef _this, int sourceId)
                => _this != null && _this.TrySetCanceled(sourceId);

            [MethodImpl(InlineOption)]
            private bool TrySetCanceled(int sourceId)
            {
                _smallFields._locker.Enter();
                if (sourceId != SourceId | _state != State.Pending)
                {
                    _smallFields._locker.Exit();
                    return false;
                }
                InvokeCallbacksAlreadyLocked();
                return true;
            }

            private void InvokeCallbacksAlreadyLocked()
            {
                ThrowIfInPool(this);

                _executingThread = Thread.CurrentThread;
                _state = State.Canceled;
                ++_internalRetainCounter;

                // We call the delegates in LIFO order so that callbacks fire 'deepest first'.
                // This is intended to help with nesting scenarios so that child enlisters cancel before their parents.

                List<Exception> exceptions = null;
                while (true)
                {
                    // If the previous points to this, no more registrations exist.
                    if (_previous == this)
                    {
                        break;
                    }
                    var current = _previous.UnsafeAs<CancelationCallbackNodeBase>();
                    current.RemoveFromLinkedList();

                    // Exit the lock before invoking arbitrary code, then re-enter after it completes.
                    _smallFields._locker.Exit();
                    try
                    {
                        current.Invoke();
                    }
                    catch (Exception e)
                    {
                        RecordException(e, ref exceptions);
                    }
                    _smallFields._locker.Enter();
                }

                _executingThread = null;
                _state = State.CanceledComplete;
                MaybeResetAndRepoolAlreadyLocked();
                if (exceptions != null)
                {
                    // Propagate exceptions to caller as aggregate.
                    throw new AggregateException(exceptions);
                }
            }

            [MethodImpl(InlineOption)]
            internal static bool TryDispose(CancelationRef _this, int sourceId)
                => _this != null && _this.TryDispose(sourceId);

            internal bool TryDispose(int sourceId)
            {
                _smallFields._locker.Enter();
                if (!TryIncrementSourceId(sourceId))
                {
                    _smallFields._locker.Exit();
                    return false;
                }

                ThrowIfInPool(this);
                DisposeLocked();
                return true;
            }

            // Internal dispose method skipping the id check.
            internal void DisposeUnsafe()
            {
                ThrowIfInPool(this);
                _smallFields._locker.Enter();
                unchecked { ++_sourceId; }
                DisposeLocked();
            }

            private void DisposeLocked()
            {
                if (_state == State.Pending)
                {
                    _state = State.Disposed;
                    UnregisterAll();
                }
                MaybeResetAndRepoolAlreadyLocked();
            }

            [MethodImpl(InlineOption)]
            internal bool TryIncrementSourceId(int sourceId)
                => Interlocked.CompareExchange(ref _sourceId, unchecked(sourceId + 1), sourceId) == sourceId;

            private void UnregisterAll()
            {
                // If the previous points to this, no registrations exist.
                if (_previous == this)
                {
                    return;
                }

                var previous = _previous.UnsafeAs<CancelationLinkedListNode>();
                // Set the last node's previous to null since null check is faster than reference comparison.
                _next.UnsafeAs<CancelationLinkedListNode>()._previous = null;
                ResetLinkedListSentinel();
                do
                {
                    var current = previous;
                    previous = current._previous;
                    current._previous = null;
                    current.Dispose();
                } while (previous != null);
            }

            [MethodImpl(InlineOption)]
            internal static bool TryRetainUser(CancelationRef _this, int tokenId)
                => _this != null && _this.TryRetainUser(tokenId);

            [MethodImpl(InlineOption)]
            private bool TryRetainUser(int tokenId)
            {
                _smallFields._locker.Enter();
                if (tokenId != TokenId | _state == State.Disposed)
                {
                    _smallFields._locker.Exit();
                    return false;
                }

                ThrowIfInPool(this);
                checked
                {
                    _userRetainCounter += _userRetainIncrementor;
                }
                _smallFields._locker.Exit();
                return true;
            }

            [MethodImpl(InlineOption)]
            internal static bool TryReleaseUser(CancelationRef _this, int tokenId)
                => _this != null && _this.TryReleaseUser(tokenId);

            [MethodImpl(InlineOption)]
            private bool TryReleaseUser(int tokenId)
            {
                _smallFields._locker.Enter();
                if (tokenId != TokenId)
                {
                    _smallFields._locker.Exit();
                    return false;
                }
                checked
                {
                    if ((_userRetainCounter -= _userRetainIncrementor) == 0 & _internalRetainCounter == 0)
                    {
                        unchecked
                        {
                            ++_tokenId;
                        }
                        _smallFields._locker.Exit();
                        ResetAndRepool();
                        return true;
                    }
                }
                _smallFields._locker.Exit();
                return true;
            }

            private void MaybeResetAndRepoolAlreadyLocked()
            {
                if (--_internalRetainCounter == 0 & _userRetainCounter == 0)
                {
                    if (_bclSource != null)
                    {
                        CancelationConverter.DetachCancelationRef(_bclSource);
                        // We should only dispose the source if we were the one that created it.
                        if (!_linkedToBclToken)
                        {
                            // We *could* call _cancellationTokenSource.TryReset() in .Net 6+ instead of always creating a new one,
                            // but if a user still holds an old token after this is reused, its cancelation state would be incorrect,
                            // possibly causing cancelations to be triggered unexpectedly.
                            _bclSource.Dispose();
                        }
                        Volatile.Write(ref _bclSource, null);
                    }
                    unchecked
                    {
                        ++_tokenId;
                    }
                    _smallFields._locker.Exit();
                    ResetAndRepool();
                    return;
                }
                _smallFields._locker.Exit();
            }

            [MethodImpl(InlineOption)]
            private void ResetAndRepool()
            {
                ThrowIfInPool(this);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (_next != this || _previous != this)
                {
                    throw new System.InvalidOperationException("CancelationToken callbacks have not been unregistered.");
                }
#endif
                _state = State.Disposed;
                // Unhook from other tokens before repooling.
                while (_links.IsNotEmpty)
                {
                    _links.Pop().UnhookAndDispose();
                }
                ObjectPool.MaybeRepool(this);
            }

            internal void Cancel()
            {
                // Same as TrySetCanceled, but without checking the SourceId.
                _smallFields._locker.Enter();
                if (_state != State.Pending)
                {
                    _smallFields._locker.Exit();
                    return;
                }
                InvokeCallbacksAlreadyLocked();
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class CallbackNodeImpl<TCancelable> : CancelationCallbackNode, ITraceable
                where TCancelable : ICancelable
            {
#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif

                private TCancelable _cancelable;

                private CallbackNodeImpl() { }

                [MethodImpl(InlineOption)]
                private static CallbackNodeImpl<TCancelable> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CallbackNodeImpl<TCancelable>>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new CallbackNodeImpl<TCancelable>()
                        : obj.UnsafeAs<CallbackNodeImpl<TCancelable>>();
                }

                [MethodImpl(InlineOption)]
                internal static CallbackNodeImpl<TCancelable> GetOrCreate(in TCancelable cancelable, CancelationRef parent)
                {
                    var node = GetOrCreate();
                    node._parentId = parent._smallFields._instanceId;
                    node._cancelable = cancelable;
                    SetCreatedStacktrace(node, 2);
                    return node;
                }

                internal override void Invoke()
                {
                    ThrowIfInPool(this);
                    SetCurrentInvoker(this);
                    try
                    {
                        _cancelable.Cancel();
                    }
                    finally
                    {
                        Dispose();
                        ClearCurrentInvoker();
                    }
                }

                [MethodImpl(InlineOption)]
                internal override void Dispose()
                {
                    ThrowIfInPool(this);
                    unchecked
                    {
                        ++_nodeId;
                    }
                    ClearReferences(ref _cancelable);
                    ObjectPool.MaybeRepool(this);
                }
            } // class CallbackNodeImpl<TCancelable>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class LinkedCancelationNode : CancelationCallbackNodeBase, ILinked<LinkedCancelationNode>
            {
                LinkedCancelationNode ILinked<LinkedCancelationNode>.Next { get; set; }

                private CancelationRef _target;
                private CancelationRef _parent;

                private LinkedCancelationNode() { }

                [MethodImpl(InlineOption)]
                private static LinkedCancelationNode GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<LinkedCancelationNode>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new LinkedCancelationNode()
                        : obj.UnsafeAs<LinkedCancelationNode>();
                }

                [MethodImpl(InlineOption)]
                internal static LinkedCancelationNode GetOrCreate(CancelationRef target, CancelationRef parent)
                {
                    var node = GetOrCreate();
                    node._target = target;
                    node._parent = parent;
                    return node;
                }

                internal override void Invoke()
                {
                    ThrowIfInPool(this);
                    try
                    {
                        _target.Cancel();
                    }
                    finally
                    {
                        Dispose();
                    }
                }

                [MethodImpl(InlineOption)]
                internal override void Dispose()
                {
                    ThrowIfInPool(this);
                    _target = null;
                    if (_parent == null)
                    {
                        // Only repool here if UnhookAndDispose() was called synchronously from inside the invoke.
                        Repool();
                    }
                    else
                    {
                        Volatile.Write(ref _parent, null);
                    }
                }

                [MethodImpl(InlineOption)]
                private void Repool()
                {
                    ObjectPool.MaybeRepool(this);
                }

                internal void UnhookAndDispose()
                {
                    var parent = _parent;
                    if (parent == null)
                    {
                        // This is already unhooked from the token.
                        Repool();
                        return;
                    }

                    // Unhook from the parent token and wait for the invoke to complete before repooling.
                    parent._smallFields._locker.Enter();
                    if (_previous != null)
                    {
                        // This is still registered to the parent, unregister it and dispose.
                        RemoveFromLinkedList();
                        parent._smallFields._locker.Exit();
                        Dispose();
                        Repool();
                        return;
                    }
                    parent._smallFields._locker.Exit();

                    // This is no longer registered to the parent, but the invoke may still be running on another thread,
                    // so wait until it's complete before repooling.
                    if (parent._executingThread == Thread.CurrentThread)
                    {
                        // This was invoked synchronously from the parent, just set the _parent to null so that this will be repooled in Dispose() when the stack unwinds.
                        _parent = null;
                        return;
                    }

                    // Spin until this has been disposed.
                    var spinner = new SpinWait();
                    while (Volatile.Read(ref _parent) != null)
                    {
                        spinner.SpinOnce();
                    }
                    Repool();
                }
            } // class LinkedCancelationNode

            private interface INodeCreator
            {
                CancelationCallbackNodeBase CreateNode(CancelationRef parent, int tokenId);
                void Invoke();
            }

            private struct UserNodeCreator<TCancelable> : INodeCreator
                where TCancelable : ICancelable
            {
                internal CancelationRegistration _registration;
                private readonly TCancelable _cancelable;

                [MethodImpl(InlineOption)]
                public UserNodeCreator(in TCancelable cancelable)
                {
                    _registration = default;
                    _cancelable = cancelable;
                }

                [MethodImpl(InlineOption)]
                public CancelationCallbackNodeBase CreateNode(CancelationRef parent, int tokenId)
                {
                    var node = CallbackNodeImpl<TCancelable>.GetOrCreate(_cancelable, parent);
                    _registration = new CancelationRegistration(parent, node, node.NodeId, tokenId);
                    return node;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                    => _cancelable.Cancel();
            }

            private struct UserNodeCreatorNoInvoke<TCancelable> : INodeCreator
                where TCancelable : ICancelable
            {
                internal CancelationRegistration _registration;
                private readonly TCancelable _cancelable;
                internal bool _isCanceled;

                [MethodImpl(InlineOption)]
                public UserNodeCreatorNoInvoke(in TCancelable cancelable)
                {
                    _registration = default;
                    _cancelable = cancelable;
                    _isCanceled = false;
                }

                [MethodImpl(InlineOption)]
                public CancelationCallbackNodeBase CreateNode(CancelationRef parent, int tokenId)
                {
                    var node = CallbackNodeImpl<TCancelable>.GetOrCreate(_cancelable, parent);
                    _registration = new CancelationRegistration(parent, node, node.NodeId, tokenId);
                    return node;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                    => _isCanceled = true;
            }

            private struct LinkedNodeCreator : INodeCreator
            {
                internal LinkedCancelationNode _node;
                private readonly CancelationRef _target;

                [MethodImpl(InlineOption)]
                internal LinkedNodeCreator(CancelationRef target)
                {
                    _node = null;
                    _target = target;
                }

                [MethodImpl(InlineOption)]
                public CancelationCallbackNodeBase CreateNode(CancelationRef parent, int tokenId)
                    => _node = LinkedCancelationNode.GetOrCreate(_target, parent);

                [MethodImpl(InlineOption)]
                public void Invoke()
                    => _target.Cancel();
            }
        } // class CancelationRef

        internal abstract class CancelationCallbackNodeBase : CancelationLinkedListNode
        {
            internal abstract void Invoke();

            internal void RemoveFromLinkedList()
            {
                _previous._next = _next;
                _next.UnsafeAs<CancelationLinkedListNode>()._previous = _previous;
                _previous = null;
            }
        }

        internal abstract class CancelationCallbackNode : CancelationCallbackNodeBase
        {
            protected int _nodeId = 1; // Start with id 1 instead of 0 to reduce risk of false positives.
            protected int _parentId; // In case the CancelationRegistration is torn from threads.

            internal int NodeId
            {
                [MethodImpl(InlineOption)]
                get => _nodeId;
            }

            [MethodImpl(InlineOption)]
            internal static bool GetIsRegistered(CancelationRef parent, CancelationCallbackNode _this, int nodeId, int tokenId)
            {
                if (_this == null | parent == null)
                {
                    return false;
                }
                // Volatile read the id before everything else.
                return parent.VolatileTokenId == tokenId & parent._smallFields._instanceId == _this._parentId
                    & _this._nodeId == nodeId & _this._previous != null;
            }

            [MethodImpl(InlineOption)]
            internal static bool TryUnregister(CancelationRef parent, CancelationCallbackNode _this, int nodeId, int tokenId)
            {
                if (_this == null | parent == null)
                {
                    return false;
                }

                parent._smallFields._locker.Enter();
                var isRegistered = parent.TokenId == tokenId & parent._smallFields._instanceId == _this._parentId
                    & _this._nodeId == nodeId & _this._previous != null;
                if (!isRegistered)
                {
                    parent._smallFields._locker.Exit();
                    return false;
                }

                _this.RemoveFromLinkedList();
                parent._smallFields._locker.Exit();
                _this.Dispose();
                return true;
            }

            [MethodImpl(InlineOption)]
            private bool GetIsRegisteredAndIsCanceled(CancelationRef parent, int nodeId, int tokenId, out bool isCanceled)
            {
                bool canceled = parent._state >= CancelationRef.State.Canceled;
                // We read state volatile, so we don't need to read anything else volatile.
                bool tokenIdMatches = parent.TokenId == tokenId & parent._smallFields._instanceId == _parentId;
                bool isRegistered = tokenIdMatches & _nodeId == nodeId & _previous != null;
                isCanceled = canceled & tokenIdMatches;
                return isRegistered;
            }

            [MethodImpl(InlineOption)]
            internal static bool GetIsRegisteredAndIsCanceled(CancelationRef parent, CancelationCallbackNode _this, int nodeId, int tokenId, out bool isCanceled)
            {
                if (_this == null | parent == null)
                {
                    isCanceled = false;
                    return false;
                }
                parent._smallFields._locker.Enter();
                bool isRegistered = _this.GetIsRegisteredAndIsCanceled(parent, nodeId, tokenId, out isCanceled);
                parent._smallFields._locker.Exit();
                return isRegistered;
            }

            [MethodImpl(InlineOption)]
            internal static bool TryUnregister(CancelationRef parent, CancelationCallbackNode _this, int nodeId, int tokenId, out bool isCanceled)
            {
                if (_this == null | parent == null)
                {
                    isCanceled = false;
                    return false;
                }

                parent._smallFields._locker.Enter();
                if (!_this.GetIsRegisteredAndIsCanceled(parent, nodeId, tokenId, out isCanceled))
                {
                    parent._smallFields._locker.Exit();
                    return false;
                }

                _this.RemoveFromLinkedList();
                parent._smallFields._locker.Exit();
                _this.Dispose();
                return true;
            }

            [MethodImpl(InlineOption)]
            internal static void TryUnregisterOrWaitForCallbackToComplete(CancelationRef parent, CancelationCallbackNode _this, int nodeId, int tokenId)
            {
                if (_this == null | parent == null)
                {
                    return;
                }

                parent._smallFields._locker.Enter();
                bool idsMatch = parent._smallFields._instanceId == _this._parentId
                    & tokenId == parent.TokenId
                    & nodeId == _this._nodeId;
                if (idsMatch & _this._previous != null)
                {
                    _this.RemoveFromLinkedList();
                    parent._smallFields._locker.Exit();
                    _this.Dispose();
                    return;
                }

                bool parentIsCanceling = parent._state == CancelationRef.State.Canceled;
                parent._smallFields._locker.Exit();
                // If the source is executing callbacks on another thread, we must wait until this callback is complete.
                if (idsMatch & parentIsCanceling
                    & parent._executingThread != Thread.CurrentThread)
                {
                    var spinner = new SpinWait();
                    // _this._nodeId will be incremented when the callback is complete and this is disposed.
                    // parent.TokenId will be incremented when all callbacks are complete and it is disposed.
                    // We really only need to compare the nodeId, the tokenId comparison is just for a little extra safety in case of thread starvation and node re-use.
                    while (nodeId == Volatile.Read(ref _this._nodeId) & tokenId == parent.TokenId)
                    {
                        spinner.SpinOnce(); // Spin, as we assume callback execution is fast and that this situation is rare.
                    }
                }
            }

            [MethodImpl(InlineOption)]
            internal static Promise TryUnregisterOrWaitForCallbackToCompleteAsync(CancelationRef parent, CancelationCallbackNode _this, int nodeId, int tokenId)
            {
                if (_this == null | parent == null)
                {
                    return new Promise();
                }

                parent._smallFields._locker.Enter();
                bool idsMatch = parent._smallFields._instanceId == _this._parentId
                    & tokenId == parent.TokenId
                    & nodeId == _this._nodeId;
                if (idsMatch & _this._previous != null)
                {
                    _this.RemoveFromLinkedList();
                    parent._smallFields._locker.Exit();
                    _this.Dispose();
                    return new Promise();
                }

                bool parentIsCanceling = parent._state == CancelationRef.State.Canceled;
                parent._smallFields._locker.Exit();
                // If the source is executing callbacks on another thread, we must wait until this callback is complete.
                if (idsMatch & parentIsCanceling
                    & parent._executingThread != Thread.CurrentThread)
                {
                    // The specified callback is actually running: queue an async loop that'll poll for the currently executing
                    // callback to complete. While such polling isn't ideal, we expect this to be a rare case (disposing while
                    // the associated callback is running), and brief when it happens (so the polling will be minimal), and making
                    // this work with a callback mechanism will add additional cost to other more common cases.
                    var deferred = Promise.NewDeferred();
                    WaitForInvokeComplete(parent, _this, nodeId, tokenId, deferred);
                    return deferred.Promise;
                }
                return new Promise();
            }

            private static void WaitForInvokeComplete(CancelationRef parent, CancelationCallbackNode node, int nodeId, int tokenId, Promise.Deferred deferred)
            {
                // node._nodeId will be incremented when the callback is complete and it is disposed.
                // parent.TokenId will be incremented when all callbacks are complete and it is disposed.
                // We really only need to compare the nodeId, the tokenId comparison is just for a little extra safety in case of thread starvation and node re-use.
                if (nodeId == Volatile.Read(ref node._nodeId) & tokenId == parent.TokenId)
                {
                    // Queue the check to happen again on a background thread.
                    // Force async so the current thread will be yielded if this is already being executed on a background thread.
                    // This is recursive, but it's done so asynchronously so it will never cause StackOverflowException.
                    Promise.Run((parent, node, nodeId, tokenId, deferred),
                        cv => WaitForInvokeComplete(cv.parent, cv.node, cv.nodeId, cv.tokenId, cv.deferred),
                        Promise.Config.BackgroundContext, forceAsync: true)
                        .Forget();
                }
                else
                {
                    deferred.Resolve();
                }
            }
        } // class CancelationCallbackNode

        partial class CancelationRef
        {
            // A separate class so that static data won't need to be created if it is never used.
            internal static class CancelationConverter
            {
                private static readonly bool s_canExtractSource = GetCanExtractSource();
                // Cache so if ToCancelationToken() is called multiple times on the same token, we don't need to allocate for every call.
                // ConditionalWeakTable so we aren't extending the lifetime of any sources beyond what the user is using them for.
                private static readonly ConditionalWeakTable<CancellationTokenSource, CancelationRef> s_tokenCache = new ConditionalWeakTable<CancellationTokenSource, CancelationRef>();

                private static bool GetCanExtractSource()
                {
                    // This assumes the CancellationToken is implemented like this, and will return false if it's different.
                    // public struct CancellationToken
                    // {
                    //     private CancellationTokenSource m_source;
                    //     ...
                    // }
                    var fields = typeof(System.Threading.CancellationToken).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    return fields.Length == 1 && typeof(CancellationTokenSource).IsAssignableFrom(fields[0].FieldType);
                }

                // Implementation detail, the token wraps the source, so we can retrieve it by placing it in this explicit layout struct and reading the source.
                // This is equivalent to `Unsafe.As`, but also works in older runtimes that don't support Unsafe. It's also more efficient than using reflection (and some runtimes don't support TypedReference).
                // I think it is very unlikely, but the internal implementation of CancellationToken could change in the future (or different runtime) to break this code, which is why we have the s_canExtractSource check.
                [StructLayout(LayoutKind.Explicit)]
                private struct TokenSourceExtractor
                {
                    [FieldOffset(0)]
                    internal System.Threading.CancellationToken _token;
                    [FieldOffset(0)]
                    internal CancellationTokenSource _source;
                }

                internal static void AttachCancelationRef(CancellationTokenSource source, CancelationRef _ref)
                {
                    s_tokenCache.Add(source, _ref);
                }

                internal static void DetachCancelationRef(CancellationTokenSource source)
                {
                    s_tokenCache.Remove(source);
                }

                internal static CancelationToken Convert(System.Threading.CancellationToken token)
                {
                    if (!s_canExtractSource)
                    {
                        throw new System.Reflection.TargetException("Cannot convert System.Threading.CancellationToken to Proto.Promises.CancelationToken due to an implementation change. Please notify the developer.");
                    }

                    if (!token.CanBeCanceled)
                    {
                        return default;
                    }
                    if (token.IsCancellationRequested)
                    {
                        return CancelationToken.Canceled();
                    }

                    // This relies on internal implementation details. If the implementation changes, the s_canExtractSource check should catch it.
                    var source = new TokenSourceExtractor() { _token = token }._source;

                    if (source == null)
                    {
                        // Source should never be null if token.CanBeCanceled returned true.
                        throw new System.Reflection.TargetException("The token's internal source was null.");
                    }

                    // If the source was disposed, the Token property will throw ObjectDisposedException.
                    // Unfortunately, this is the only way to check if it's disposed, since token.CanBeCanceled may still return true after it's disposed in .Net Core.
                    try
                    {
                        token = source.Token;
                    }
                    catch (ObjectDisposedException)
                    {
                        // Check canceled state again in case of a race condition.
                        return source.IsCancellationRequested ? CancelationToken.Canceled() : default;
                    }

                    if (s_tokenCache.TryGetValue(source, out var cancelationRef))
                    {
                        var tokenId = cancelationRef.TokenId;
                        return Volatile.Read(ref cancelationRef._bclSource) == source // In case of race condition on another thread.
                            ? new CancelationToken(cancelationRef, tokenId)
                            : default;
                    }

                    // Lock instead of AddOrUpdate so multiple refs won't be created on separate threads.
                    lock (s_tokenCache)
                    {
                        if (!s_tokenCache.TryGetValue(source, out cancelationRef))
                        {
                            cancelationRef = GetOrCreateForBclTokenConvert(source);
                            s_tokenCache.Add(source, cancelationRef);
                        }
                    }
                    {
                        var tokenId = cancelationRef.TokenId;
                        cancelationRef.HookupBclCancelation(token);
                        return new CancelationToken(cancelationRef, tokenId);
                    }
                }
            } // class CancelationConverter

            private CancellationTokenSource _bclSource;

            partial void SetCancellationTokenRegistration(CancellationTokenRegistration registration);
#if NET6_0_OR_GREATER
            // This is only necessary in .Net 6 or later, since `CancellationTokenSource.TryReset()` was added.
            private CancellationTokenRegistration _bclRegistration;

            [MethodImpl(InlineOption)]
            partial void SetCancellationTokenRegistration(CancellationTokenRegistration registration)
            {
                _bclRegistration = registration;
            }
#endif

            internal static CancelationRef GetOrCreateForBclTokenConvert(CancellationTokenSource source)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                // Don't take from the object pool, just create new (this is so the object pool's tracker won't report this since Dispose is never called on it).
                var cancelRef = new CancelationRef();
#else
                var cancelRef = GetFromPoolOrCreate();
                cancelRef._next = null;
#endif
                cancelRef.Initialize(true);
                cancelRef._bclSource = source;
                return cancelRef;
            }

            private void HookupBclCancelation(System.Threading.CancellationToken token)
            {
                // We don't need the synchronous invoke check when this is created.
#if NETCOREAPP3_0_OR_GREATER
                var registration = token.UnsafeRegister(state => state.UnsafeAs<CancelationRef>().Cancel(), this);
#else
                var registration = token.Register(state => state.UnsafeAs<CancelationRef>().Cancel(), this, false);
#endif
                SetCancellationTokenRegistration(registration);
            }

            internal static System.Threading.CancellationToken GetCancellationToken(CancelationRef _this, int tokenId)
                => _this?.GetCancellationToken(tokenId) ?? default;

            private System.Threading.CancellationToken GetCancellationToken(int tokenId)
            {
                _smallFields._locker.Enter();
                try
                {
                    var state = _state;
                    if (tokenId != TokenId | state == State.Disposed)
                    {
                        return default;
                    }
                    if (state >= State.Canceled)
                    {
                        return new CancellationToken(true);
                    }
                    if (_bclSource == null)
                    {
                        _bclSource = new CancellationTokenSource();
                        CancelationConverter.AttachCancelationRef(_bclSource, this);
                        var del = new CancelDelegateToken<CancellationTokenSource>(_bclSource, source => source.Cancel(false));
                        var node = CallbackNodeImpl<CancelDelegateToken<CancellationTokenSource>>.GetOrCreate(del, this);
                        InsertPrevious(node);
                    }
                    return _bclSource.Token;
                }
                // The original source may be disposed, in which case the Token property will throw ObjectDisposedException.
                catch (ObjectDisposedException)
                {
                    return _bclSource.IsCancellationRequested ? new CancellationToken(true) : default;
                }
                finally
                {
                    _smallFields._locker.Exit();
                }
            }
        } // class CancelationRef
    } // class Internal
} // namespace Proto.Promises