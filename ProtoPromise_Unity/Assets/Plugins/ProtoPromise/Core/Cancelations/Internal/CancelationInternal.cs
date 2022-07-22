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
        [DebuggerNonUserCode, StackTraceHidden]
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
        [DebuggerNonUserCode, StackTraceHidden]
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
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed partial class CancelationRef : HandleablePromiseBase, ICancelable, ITraceable
        {
            internal static readonly CancelationRef s_canceledSentinel = new CancelationRef() { _state = State.Canceled, _internalRetainCounter = 1, _tokenId = -1 };

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
                    // We don't check the disposed state if this was linked to a System.Threading.CancellationToken.
                    if (!_linkedToBclToken & _state != State.Disposed)
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

#if NET6_0_OR_GREATER
            // Used to prevent a deadlock from synchronous invoke.
            [ThreadStatic]
            private static bool ts_isLinkingToBclToken;
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
            private bool _linkedToBclToken;
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
            private void Initialize(bool linkedToBclToken)
            {
                _internalRetainCounter = 1; // 1 for Dispose.
                _linkedToBclToken = linkedToBclToken;
                _state = State.Pending;
                SetCreatedStacktrace(this, 2);
            }

            [MethodImpl(InlineOption)]
            internal static CancelationRef GetOrCreate()
            {
                var cancelRef = ObjectPool.TryTake<CancelationRef>()
                    ?? new CancelationRef();
                cancelRef.Initialize(false);
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
                if (tokenId != TokenId)
                {
                    _locker.Exit();
                    registration = default(CancelationRegistration);
                    return false;
                }

                State state = _state;
                if (state == State.Pending)
                {
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
                                _locker.Exit();
                                cancelable.Cancel();
                            }
                            else
                            {
                                UnregisterAll();
                                _locker.Exit();
                            }
                            registration = default(CancelationRegistration);
                            return isCanceled;
                        }

                        // If we are unable to unregister, it means the source had TryReset() called on it, or the token was canceled on another thread (and the other thread may be waiting on the lock).
                        if (!_bclRegistration.Unregister())
                        {
                            if (token.IsCancellationRequested)
                            {
                                _locker.Exit();
                                cancelable.Cancel();
                                registration = default(CancelationRegistration);
                                return true;
                            }
                            UnregisterAll();
                        }
                        // Callback could be invoked synchronously if the token is canceled on another thread,
                        // so we set a flag to prevent a deadlock, then check the flag again after the hookup to see if it was invoked.
                        ts_isLinkingToBclToken = true;
                        _bclRegistration = token.Register(state =>
                        {
                            // This could be invoked synchronously if the token is canceled, so we check the flag to prevent a deadlock.
                            if (ts_isLinkingToBclToken)
                            {
                                // Reset the flag so that we can tell that this was invoked synchronously.
                                ts_isLinkingToBclToken = false;
                                return;
                            }
                            state.UnsafeAs<CancelationRef>().Cancel();
                        }, this, false);

                        if (!ts_isLinkingToBclToken)
                        {
                            // Hook up the node instead of invoking since it might throw, and we need all registered callbacks to be invoked.
                            var node = CallbackNodeImpl<TCancelable>.GetOrCreate(cancelable, tokenId, !_linkedToBclToken);
                            int oldNodeId = node.NodeId;
                            _registeredCallbacksHead.InsertPrevious(node);

                            _state = State.Canceled;
                            ++_internalRetainCounter;
                            _locker.Exit();

                            InvokeCallbacks();
                            registration = new CancelationRegistration(this, node, oldNodeId, tokenId);
                            return true;
                        }
                        ts_isLinkingToBclToken = false;
                    }
#endif

                    {
                        var node = CallbackNodeImpl<TCancelable>.GetOrCreate(cancelable, tokenId, !_linkedToBclToken);
                        int oldNodeId = node.NodeId;
                        _registeredCallbacksHead.InsertPrevious(node);
                        _locker.Exit();
                        registration = new CancelationRegistration(this, node, oldNodeId, tokenId);
                        return true;
                    }
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

                // We call the delegates in LIFO order so that callbacks fire 'deepest first'.
                // This is intended to help with nesting scenarios so that child enlisters cancel before their parents.

                var previous = _registeredCallbacksHead._previous;
                // If the previous references itself, then it is the sentinel and no registrations exist.
                if (previous == _registeredCallbacksHead)
                {
                    MaybeResetAndRepool();
                    return;
                }
                // Set the last node's previous to null since null check is faster than reference comparison.
                _registeredCallbacksHead._next.UnsafeAs<CancelationCallbackNode>()._previous = null;
                _registeredCallbacksHead.ResetSentinel();
                List<Exception> exceptions = null;
                do
                {
                    var current = previous;
                    previous = current._previous;
                    try
                    {
                        current.Invoke(this);
                    }
                    catch (Exception e)
                    {
                        if (exceptions == null)
                        {
                            exceptions = new List<Exception>();
                        }
                        exceptions.Add(e);
                    }
                } while (previous != null);

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
                UnregisterAll();

                MaybeResetAndRepoolAlreadyLocked();
                return true;
            }

            private void UnregisterAll()
            {
                var previous = _registeredCallbacksHead._previous;
                // If the previous references itself, then it is the sentinel and no registrations exist.
                if (previous == _registeredCallbacksHead)
                {
                    return;
                }
                // Set the last node's previous to null since null check is faster than reference comparison.
                _registeredCallbacksHead._next.UnsafeAs<CancelationCallbackNode>()._previous = null;
                _registeredCallbacksHead.ResetSentinel();
                do
                {
                    var current = previous;
                    previous = current._previous;
                    current.Dispose();
                } while (previous != null);
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
                if (tokenId != TokenId | _state == State.Disposed)
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
                    if (_bclSource != null)
                    {
                        CancelationConverter.DetachCancelationRef(_bclSource);
                        // We should only dispose the source if we were the one that created it.
                        if (!_linkedToBclToken)
                        {
                            // TODO: We can call _cancellationTokenSource.TryReset() in .Net 6+ instead of always creating a new one.
                            // But this should only be done if we add a TryReset() API to our own CancelationSource, because if a user still holds an old token after this is reused, it could have cancelations triggered unexpectedly.
                            _bclSource.Dispose();
                        }
                        _bclSource = null;
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
                    TCancelable cancelable, int tokenId, bool shouldCheckForDisposed)
                {
                    var del = ObjectPool.TryTake<CallbackNodeImpl<TCancelable>>()
                        ?? new CallbackNodeImpl<TCancelable>();
                    del._tokenId = tokenId;
                    del._cancelable = cancelable;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // If the CancelationRef was attached to a BCL token, it is possible this will not be disposed, so we won't check for it.
                    del._disposed = !shouldCheckForDisposed;
#endif
                    SetCreatedStacktrace(del, 2);
                    return del;
                }

                internal override void Invoke(CancelationRef parent)
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
            protected int _nodeId = 1; // Start with id 1 instead of 0 to reduce risk of false positives.
            protected int _tokenId; // In case the CancelationRegistration is torn from threads.

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

            internal void ResetSentinel()
            {
                _next = this;
                _previous = this;
            }

            internal void InsertPrevious(CancelationCallbackNode node)
            {
                node._previous = _previous;
                node._next = this;
                _previous._next = node;
                _previous = node;
            }

            internal virtual void Invoke(CancelationRef parent) { throw new System.InvalidOperationException(); }
            internal virtual void Dispose() { throw new System.InvalidOperationException(); }

            [MethodImpl(InlineOption)]
            internal static bool GetIsRegisteredAndIsCanceled(CancelationRef parent, CancelationCallbackNode _this, int nodeId, int tokenId, out bool isCanceled)
            {
                if (_this == null | parent == null)
                {
                    isCanceled = false;
                    return false;
                }
                return _this.GetIsRegistered(parent, nodeId, tokenId, out isCanceled);
            }

            [MethodImpl(InlineOption)]
            private bool GetIsRegistered(CancelationRef parent, int nodeId, int tokenId, out bool isCanceled)
            {
                bool canceled = parent._state == CancelationRef.State.Canceled;
                bool tokenIdMatches = parent.TokenId == tokenId & _tokenId == tokenId;
                isCanceled = canceled & tokenIdMatches;
                return !canceled & tokenIdMatches & _nodeId == nodeId;
            }

            [MethodImpl(InlineOption)]
            internal static bool TryUnregister(CancelationRef parent, CancelationCallbackNode _this, int nodeId, int tokenId, out bool isCanceled)
            {
                if (_this == null | parent == null)
                {
                    isCanceled = false;
                    return false;
                }
                return _this.TryUnregister(parent, nodeId, tokenId, out isCanceled);
            }

            [MethodImpl(InlineOption)]
            private bool TryUnregister(CancelationRef parent, int nodeId, int tokenId, out bool isCanceled)
            {
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
        } // class CancelationCallbackNode

        partial class CancelationRef
        {
#if !NET_LEGACY || NET40
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
                        return default(CancelationToken);
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
                        return source.IsCancellationRequested ? CancelationToken.Canceled() : default(CancelationToken);
                    }

                    if (s_tokenCache.TryGetValue(source, out var cancelationRef))
                    {
                        var tokenId = cancelationRef.TokenId;
                        Thread.MemoryBarrier();
                        return cancelationRef._bclSource != source // In case of race condition on another thread.
                            ? default(CancelationToken)
                            : new CancelationToken(cancelationRef, tokenId);
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
            private CancellationTokenRegistration _bclRegistration;

            internal static CancelationRef GetOrCreateForBclTokenConvert(CancellationTokenSource source)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                // Don't take from the object pool, just create new (this is so the object pool's tracker won't report this since Dispose is never called on it).
                var cancelRef = new CancelationRef();
#else
                var cancelRef = ObjectPool.TryTake<CancelationRef>()
                    ?? new CancelationRef();
#endif
                cancelRef.Initialize(true);
                cancelRef._bclSource = source;
                return cancelRef;
            }

            private void HookupBclCancelation(System.Threading.CancellationToken token)
            {
                // We don't need the synchronous invoke check when this is created.
                _bclRegistration = token.Register(state => state.UnsafeAs<CancelationRef>().Cancel(), this, false);
            }

            internal static CancellationToken GetCancellationToken(CancelationRef _this, int tokenId)
            {
                return _this == null ? default(CancellationToken) : _this.GetCancellationToken(tokenId);
            }

            private CancellationToken GetCancellationToken(int tokenId)
            {
                _locker.Enter();
                try
                {
                    var state = _state;
                    if (tokenId != TokenId | state == State.Disposed)
                    {
                        return default(CancellationToken);
                    }
                    if (state == State.Canceled)
                    {
                        return new CancellationToken(true);
                    }
                    if (_bclSource == null)
                    {
                        _bclSource = new CancellationTokenSource();
                        CancelationConverter.AttachCancelationRef(_bclSource, this);
                        var del = new CancelDelegateToken<CancellationTokenSource>(_bclSource, source => source.Cancel(false));
                        var node = CallbackNodeImpl<CancelDelegateToken<CancellationTokenSource>>.GetOrCreate(del, tokenId, true);
                        _registeredCallbacksHead.InsertPrevious(node);
                    }
                    return _bclSource.Token;
                }
                // The original source may be disposed, in which case the Token property will throw ObjectDisposedException.
                catch (ObjectDisposedException)
                {
                    return _bclSource.IsCancellationRequested ? new CancellationToken(true) : default(CancellationToken);
                }
                finally
                {
                    _locker.Exit();
                }
            }
#endif // !NET_LEGACY || NET40
        } // class CancelationRef
    } // class Internal
} // namespace Proto.Promises