#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Proto.Utils;

namespace Proto.Promises
{
    internal static partial class Internal
    {
#if PROTO_PROMISE_DEVELOPER_MODE
        internal const MethodImplOptions InlineOption = MethodImplOptions.NoInlining;
#else
        internal const MethodImplOptions InlineOption = (MethodImplOptions) 256; // AggressiveInlining
#endif

#if PROMISE_DEBUG
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal class CausalityTrace
        {
            private readonly StackTrace _stackTrace;
            private readonly CausalityTrace _next;

            public CausalityTrace(StackTrace stackTrace, CausalityTrace higherStacktrace)
            {
                _stackTrace = stackTrace;
                _next = higherStacktrace;
            }

            public override string ToString()
            {
                if (_stackTrace == null)
                {
                    return null;
                }
                List<StackTrace> stackTraces = new List<StackTrace>();
                for (CausalityTrace current = this; current != null; current = current._next)
                {
                    if (current._stackTrace == null)
                    {
                        break;
                    }
                    stackTraces.Add(current._stackTrace);
                }
                return FormatStackTrace(stackTraces);
            }
        }
#endif

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class CancelDelegate<TCanceler> : ICancelDelegate, IDisposableTreeHandleable, ITraceable where TCanceler : IDelegateCancel
        {
            private struct Creator : ICreator<CancelDelegate<TCanceler>>
            {
                [MethodImpl(InlineOption)]
                public CancelDelegate<TCanceler> Create()
                {
                    return new CancelDelegate<TCanceler>();
                }
            }

#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif
            ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

            public TCanceler canceler;

            private CancelDelegate() { }

            [MethodImpl(InlineOption)]
            public static CancelDelegate<TCanceler> GetOrCreate()
            {
                var del = ObjectPool<ITreeHandleable>.GetOrCreate<CancelDelegate<TCanceler>, Creator>(new Creator());
                SetCreatedStacktrace(del, 2);
                return del;
            }

            void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
            {
                ThrowIfInPool(this);
                if (canceler.TryMakeReady(valueContainer, this))
                {
                    handleQueue.Push(this);
                }
            }

            void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
            {
                ThrowIfInPool(this);
                if (canceler.TryMakeReady(valueContainer, this))
                {
                    AddToHandleQueueBack(this);
                }
            }

            void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
            {
                ThrowIfInPool(this);
                SetCurrentInvoker(this);
                try
                {
                    // Canceler may dispose this.
                    canceler.InvokeFromToken(valueContainer, this);
                }
                finally
                {
                    ClearCurrentInvoker();
                }
            }

            void ITreeHandleable.Handle()
            {
                ThrowIfInPool(this);
                SetCurrentInvoker(this);
#if PROMISE_DEBUG
                var traceContainer = new CausalityTraceContainer(this); // Store the causality trace so that this can be disposed before the callback is invoked.
#endif
                try
                {
                    // Canceler may dispose this.
                    canceler.InvokeFromPromise(this);
                }
                catch (Exception e)
                {
#if PROMISE_DEBUG
                    AddRejectionToUnhandledStack(e, traceContainer);
#else
                    AddRejectionToUnhandledStack(e, null);
#endif
                }
                finally
                {
                    ClearCurrentInvoker();
                }
            }

            public void Dispose()
            {
                canceler = default(TCanceler);
                ObjectPool<ITreeHandleable>.MaybeRepool(this);
            }

            void ICancelDelegate.Dispose()
            {
                ThrowIfInPool(this);
                canceler.MaybeDispose(this);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class CancelationRef : ICancelDelegate, ILinked<CancelationRef>, ITraceable
        {
            private struct Creator : ICreator<CancelationRef>
            {
                [MethodImpl(InlineOption)]
                public CancelationRef Create()
                {
                    return new CancelationRef();
                }
            }

            private struct RegisteredDelegate : IComparable<RegisteredDelegate>
            {
                public readonly ICancelDelegate callback;
                public readonly uint order;

                public RegisteredDelegate(uint order, ICancelDelegate callback)
                {
                    this.callback = callback;
                    this.order = order;
                }

                public RegisteredDelegate(uint order) : this(order, null) { }

                public int CompareTo(RegisteredDelegate other)
                {
                    return order.CompareTo(other.order);
                }
            }

            // Used as a reference holder for _valueContainer for thread safety purposes and to let the finalizer know that the source was disposed.
            private class DisposedRef : ICancelValueContainer
            {
                internal static readonly DisposedRef instance = new DisposedRef();

                private DisposedRef() { }

                void IValueContainer.Retain() { }
                void IValueContainer.Release() { }

                Type IValueContainer.ValueType { get { throw new System.InvalidOperationException(); } }
                object IValueContainer.Value { get { throw new System.InvalidOperationException(); } }
                Exception IThrowable.GetException() { throw new System.InvalidOperationException(); }
                Promise.State IValueContainer.GetState() { throw new System.InvalidOperationException(); }
                void IValueContainer.ReleaseAndAddToUnhandledStack() { throw new System.InvalidOperationException(); }
                void IValueContainer.ReleaseAndMaybeAddToUnhandledStack() { throw new System.InvalidOperationException(); }

            }

#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif

            ~CancelationRef()
            {
                if (ValueContainer != null)
                {
                    ValueContainer.Release();
                }
                if (_retainCounter > 0)
                {
                    // CancelationToken wasn't released.
                    string message = "A CancelationToken's resources were garbage collected without being released. You must release all IRetainable objects that you have retained.";
                    AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                }
                if (_valueContainer != DisposedRef.instance)
                {
                    // CancelationSource wasn't disposed.
                    AddRejectionToUnhandledStack(new Exception("CancelationSource's resources were garbage collected without being disposed."), this);
                }
            }

            CancelationRef ILinked<CancelationRef>.Next { get; set; }

            private readonly List<RegisteredDelegate> _registeredCallbacks = new List<RegisteredDelegate>();
            private ValueLinkedStackZeroGC<CancelationRegistration> _links;
            private ICancelValueContainer _valueContainer;
            private int _registeredCount;
            private int _retainCounter;
            private int _sourceId;
            volatile private int _tokenId;

            private int _callbacksLockCount;

            internal ICancelValueContainer ValueContainer
            {
                [MethodImpl(InlineOption)]
                get { return _valueContainer; }
            }
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
                var cancelRef = ObjectPool<CancelationRef>.GetOrCreate<CancelationRef, Creator>(new Creator());
                // Leftmost bit is for dispose.
                cancelRef._retainCounter = 1 << 31;
                cancelRef._valueContainer = null;
                SetCreatedStacktrace(cancelRef, 2);
                return cancelRef;
            }

            [MethodImpl(InlineOption)]
            internal bool IsSourceCanceled(int sourceId)
            {
                var temp = _valueContainer;
                return sourceId == _sourceId & temp != null & temp != DisposedRef.instance;
            }

            [MethodImpl(InlineOption)]
            internal bool IsTokenCanceled(int tokenId)
            {
                var temp = _valueContainer;
                return tokenId == _tokenId & temp != null & temp != DisposedRef.instance;
            }

            [MethodImpl(InlineOption)]
            internal void MaybeAddLinkedCancelation(CancelationRef listener)
            {
                // No need to invoke since this is only called from CancelationSource.New.
                Register(listener, false);
                Thread.MemoryBarrier(); // Make sure we get a fresh read of _valueContainer.
                var temp = _valueContainer;
                if (temp == null | temp == DisposedRef.instance)
                {
                    return;
                }
                temp.Retain();
                if (Interlocked.CompareExchange(ref listener._valueContainer, temp, null) == null)
                {
                    Unlink();
                }
                else
                {
                    temp.Release();
                }
            }

            [MethodImpl(InlineOption)]
            private void RetainFromLink(CancelationRegistration registration)
            {
                int _;
                if (!InterlockedTryRetain(out _))
                {
                    throw new OverflowException();
                }
                _links.Push(registration);
            }

            internal CancelationRegistration Register(ICancelDelegate callback, bool invoke)
            {
                Interlocked.Increment(ref _callbacksLockCount);
                ThrowIfInPool(this);
                int sourceId = _sourceId;
                int newOrder;
                if (!InterlockedAddIfNotEqual(ref _registeredCount, 1, -1, out newOrder))
                {
                    throw new OverflowException();
                }
                uint order = (uint) newOrder;
                Thread.MemoryBarrier(); // Make sure we get a fresh read of _valueContainer.
                var temp = _valueContainer;
                if (temp != null)
                {
                    Interlocked.Decrement(ref _callbacksLockCount);
                    if (invoke & temp != DisposedRef.instance)
                    {
                        callback.Invoke(temp);
                    }
                    return default(CancelationRegistration);
                }
                var registration = new CancelationRegistration(this, sourceId, order);
                if (!invoke) // callback is a linked CancelationRef
                {
                    ((CancelationRef) callback).RetainFromLink(registration);
                }
                lock (_registeredCallbacks)
                {
                    _registeredCallbacks.Add(new RegisteredDelegate(order, callback));
                    Interlocked.Decrement(ref _callbacksLockCount);
                }
                return registration;
            }

            [MethodImpl(InlineOption)]
            internal CancelationRegistration Register(Promise.CanceledAction callback)
            {
                var temp = _valueContainer;
                if (temp != null)
                {
                    if (temp != DisposedRef.instance)
                    {
                        callback.Invoke(new ReasonContainer(temp));
                    }
                    return default(CancelationRegistration);
                }
                var cancelDelegate = CancelDelegate<CancelDelegateToken>.GetOrCreate();
                cancelDelegate.canceler = new CancelDelegateToken(callback);
                return Register(cancelDelegate, true);
            }

            [MethodImpl(InlineOption)]
            internal CancelationRegistration Register<TCapture>(ref TCapture capturedValue, Promise.CanceledAction<TCapture> callback)
            {
                var temp = _valueContainer;
                if (temp != null)
                {
                    if (temp == DisposedRef.instance)
                    {
                        callback.Invoke(capturedValue, new ReasonContainer(temp));
                    }
                    return default(CancelationRegistration);
                }
                var cancelDelegate = CancelDelegate<CancelDelegateToken<TCapture>>.GetOrCreate();
                cancelDelegate.canceler = new CancelDelegateToken<TCapture>(ref capturedValue, callback);
                return Register(cancelDelegate, true);
            }

            [MethodImpl(InlineOption)]
            internal bool IsRegistered(int registrationId, uint order)
            {
                Interlocked.Increment(ref _callbacksLockCount);
                if (registrationId != _sourceId | _valueContainer != null)
                {
                    Interlocked.Decrement(ref _callbacksLockCount);
                    return false;
                }
                lock (_registeredCallbacks)
                {
                    bool isRegistered = IndexOf(order) >= 0;
                    Interlocked.Decrement(ref _callbacksLockCount);
                    return isRegistered;
                }
            }

            [MethodImpl(InlineOption)]
            internal bool TryUnregister(int registrationId, uint order)
            {
                Interlocked.Increment(ref _callbacksLockCount);
                if (registrationId != _sourceId | _valueContainer != null)
                {
                    Interlocked.Decrement(ref _callbacksLockCount);
                    return false;
                }
                ICancelDelegate del;
                lock (_registeredCallbacks)
                {
                    int index = IndexOf(order);
                    if (index >= 0)
                    {
                        ThrowIfInPool(this);
                        del = _registeredCallbacks[index].callback;
                        _registeredCallbacks.RemoveAt(index);
                    }
                    else
                    {
                        del = null;
                    }
                    Interlocked.Decrement(ref _callbacksLockCount);
                }
                bool notNull = del != null;
                if (notNull)
                {
                    del.Dispose();
                }
                return notNull;
            }

            [MethodImpl(InlineOption)]
            private int IndexOf(uint order)
            {
                return _registeredCallbacks.BinarySearch(new RegisteredDelegate(order));
            }

            [MethodImpl(InlineOption)]
            internal bool TrySetCanceled(int sourceId)
            {
                if (sourceId == _sourceId)
                {
                    ThrowIfInPool(this);
                    var container = CancelContainerVoid.GetOrCreate();
                    if (Interlocked.CompareExchange(ref _valueContainer, container, null) == null)
                    {
                        InvokeCallbacks(container);
                        return true;
                    }
                }
                return false;
            }

            [MethodImpl(InlineOption)]
            internal bool TrySetCanceled<T>(ref T cancelValue, int sourceId)
            {
                if (sourceId == _sourceId)
                {
                    ThrowIfInPool(this);
                    var container = CreateCancelContainer(ref cancelValue);
                    container.Retain();
                    if (Interlocked.CompareExchange(ref _valueContainer, container, null) == null)
                    {
                        InvokeCallbacks(container);
                        return true;
                    }
                    else
                    {
                        container.Release();
                    }
                }
                return false;
            }

            private void InvokeCallbacks(ICancelValueContainer valueContainer)
            {
                List<Exception> exceptions = null;
                WaitForCallbacks();
                Unlink();
                // No need to lock on _registeredCallbacks since it won't be modified after WaitForCallbacks().
                for (int i = 0, max = _registeredCallbacks.Count; i < max; ++i)
                {
                    try
                    {
                        _registeredCallbacks[i].callback.Invoke(valueContainer);
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
                _registeredCallbacks.Clear();
                if (exceptions != null)
                {
                    // Propagate exceptions to caller as aggregate.
                    throw new AggregateException(exceptions);
                }
            }

            [MethodImpl(InlineOption)]
            internal bool TryRetain(int tokenId)
            {
                if (tokenId != _tokenId)
                {
                    return false;
                }
                int oldRetain;
                if (InterlockedTryRetain(out oldRetain))
                {
                    ThrowIfInPool(this);
                    return true;
                }
                else if (oldRetain > 0)
                {
                    throw new OverflowException();
                }
                return false;
            }

            [MethodImpl(InlineOption)]
            internal bool TryRelease(int tokenId)
            {
                if (tokenId != _tokenId)
                {
                    return false;
                }
                ThrowIfInPool(this);
                int newRetainValue;
                bool didRelease = InterlockedTryRelease(out newRetainValue);
                if (didRelease & newRetainValue == 0)
                {
                    ResetAndRepool();
                }
                return didRelease;
            }

            internal void ReleaseAfterRetain()
            {
                if (Interlocked.Decrement(ref _retainCounter) == 0)
                {
                    ResetAndRepool();
                }
            }

            [MethodImpl(InlineOption)]
            internal bool TryDispose(int sourceId)
            {
                if (Interlocked.CompareExchange(ref _sourceId, sourceId + 1, sourceId) != sourceId)
                {
                    return false;
                }
                ThrowIfInPool(this);
                // In case Dispose is called concurrently with Cancel.
                if (Interlocked.CompareExchange(ref _valueContainer, DisposedRef.instance, null) == null)
                {
                    WaitForCallbacks();
                    Unlink();
                    // No need to lock on _registeredCallbacks since it won't be modified after WaitForCallbacks().
                    for (int i = 0, max = _registeredCallbacks.Count; i < max; ++i)
                    {
                        _registeredCallbacks[i].callback.Dispose();
                    }
                    _registeredCallbacks.Clear();
                }
                _registeredCount = 0;
                if (InterlockedReleaseInternal() == 0)
                {
                    ResetAndRepool();
                }
                return true;
            }

            private void WaitForCallbacks()
            {
                // Wait for callbacks being added/removed in other threads.
                var spinner = new SpinWait();
                while (_callbacksLockCount > 0)
                {
                    spinner.SpinOnce();
                    // Make sure it's a fresh read on every iteration.
                    Thread.MemoryBarrier();
                }
            }

            private void ResetAndRepool()
            {
                ThrowIfInPool(this);
                _tokenId = _sourceId;
                if (_valueContainer != null)
                {
                    _valueContainer.Release();
                }
                _valueContainer = DisposedRef.instance;
                ObjectPool<CancelationRef>.MaybeRepool(this);
            }

            private void Unlink()
            {
                while (_links.IsNotEmpty)
                {
                    _links.Pop().TryUnregister();
                }
            }

            void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
            {
                ThrowIfInPool(this);
                valueContainer.Retain();
                if (Interlocked.CompareExchange(ref _valueContainer, valueContainer, null) == null)
                {
                    InvokeCallbacks(valueContainer);
                }
                else
                {
                    valueContainer.Release();
                }
                ReleaseAfterRetain();
            }

            void ICancelDelegate.Dispose()
            {
                ReleaseAfterRetain();
            }

            // These helpers provide a thread-safe mechanism for checking if the user called Release too many times.
            // Leftmost bit is for dispose, right 31 bits are for user retains, leftmost bit is for dispose retain.
            [MethodImpl(InlineOption)]
            private bool InterlockedTryRetain(out int initialValue)
            {
                // Right 31 bits are for user retains.
                int newValue;
                do
                {
                    initialValue = _retainCounter;
                    newValue = initialValue + 1;
                    uint max = (1u << 31) - 1u;
                    uint oldCount = (uint) initialValue & max;
                    // Make sure user retain doesn't encroach on dispose retain.
                    // Checking for zero also handles the case if this is called concurrently with TryDispose and/or InvokeCallbacks.
                    if (oldCount >= max | initialValue == 0) return false;
                }
                while (Interlocked.CompareExchange(ref _retainCounter, newValue, initialValue) != initialValue);
                return true;
            }

            [MethodImpl(InlineOption)]
            private bool InterlockedTryRelease(out int newValue)
            {
                // Right 31 bits are for user retains.
                int initialValue;
                do
                {
                    initialValue = _retainCounter;
                    newValue = initialValue - 1;
                    uint min = 0;
                    uint max = (1u << 31) - 1u;
                    uint oldCount = (uint) initialValue & max;
                    if (oldCount <= min) return false;
                }
                while (Interlocked.CompareExchange(ref _retainCounter, newValue, initialValue) != initialValue);
                return true;
            }

            [MethodImpl(InlineOption)]
            private int InterlockedReleaseInternal()
            {
                // Leftmost bit is for dispose.
                int addValue = 1 << 31;
                return Interlocked.Add(ref _retainCounter, -addValue);
            }
        }
    }
}