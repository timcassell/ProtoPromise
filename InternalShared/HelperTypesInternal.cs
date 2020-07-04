#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Proto.Utils;

namespace Proto.Promises
{
    internal static partial class Internal
    {
#if PROMISE_DEBUG
        [DebuggerNonUserCode]
        public class CausalityTrace
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

        [DebuggerNonUserCode]
        public sealed class CancelDelegate : ICancelDelegate, ITreeHandleable, ITraceable
        {
#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif
            ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

            private static ValueLinkedStack<ITreeHandleable> _pool;

            private Action<ReasonContainer> _onCanceled;
            private IValueContainer _valueContainer;

            private CancelDelegate() { }

            static CancelDelegate()
            {
                OnClearPool += () => _pool.Clear();
            }

            public static CancelDelegate GetOrCreate(Action<ReasonContainer> onCanceled)
            {
                var del = _pool.IsNotEmpty ? (CancelDelegate) _pool.Pop() : new CancelDelegate();
                del._onCanceled = onCanceled;
                _SetCreatedStacktrace(del, 2);
                return del;
            }

            void ITreeHandleable.MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
            {
                if (valueContainer.GetState() == Promise.State.Canceled)
                {
                    valueContainer.Retain();
                    _valueContainer = valueContainer;
                    handleQueue.Push(this);
                }
                else
                {
                    Dispose();
                }
            }

            void ITreeHandleable.MakeReadyFromSettled(IValueContainer valueContainer)
            {
                if (valueContainer.GetState() == Promise.State.Canceled)
                {
                    valueContainer.Retain();
                    _valueContainer = valueContainer;
                    AddToHandleQueueBack(this);
                }
                else
                {
                    Dispose();
                }
            }

            void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
            {
                _valueContainer = valueContainer;
                _valueContainer.Retain();
                Invoke();
            }

            void ITreeHandleable.Handle()
            {
                Invoke();
            }

            private void Invoke()
            {
                _SetCurrentInvoker(this);
                var callback = _onCanceled;
                var container = _valueContainer;
                Dispose();
                try
                {
                    callback.Invoke(new ReasonContainer(container));
                }
                catch (Exception e)
                {
                    AddRejectionToUnhandledStack(e, this);
                }
                container.Release();
                _ClearCurrentInvoker();
            }

            public void Dispose()
            {
                _onCanceled = null;
                _valueContainer = null;
                if (Promise.Config.ObjectPooling != Promise.PoolType.None)
                {
                    _pool.Push(this);
                }
            }
        }

        [DebuggerNonUserCode]
        public sealed class CancelDelegateCapture<TCapture> : ICancelDelegate, ITreeHandleable, ITraceable
        {
#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif
            ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

            private static ValueLinkedStack<ITreeHandleable> _pool;

            private TCapture _capturedValue;
            private Action<TCapture, ReasonContainer> _onCanceled;
            private IValueContainer _valueContainer;

            private CancelDelegateCapture() { }

            static CancelDelegateCapture()
            {
                OnClearPool += () => _pool.Clear();
            }

            public static CancelDelegateCapture<TCapture> GetOrCreate(TCapture capturedValue, Action<TCapture, ReasonContainer> onCanceled)
            {
                var del = _pool.IsNotEmpty ? (CancelDelegateCapture<TCapture>) _pool.Pop() : new CancelDelegateCapture<TCapture>();
                del._onCanceled = onCanceled;
                del._capturedValue = capturedValue;
                _SetCreatedStacktrace(del, 2);
                return del;
            }

            void ITreeHandleable.MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
            {
                if (valueContainer.GetState() == Promise.State.Canceled)
                {
                    valueContainer.Retain();
                    _valueContainer = valueContainer;
                    handleQueue.Push(this);
                }
                else
                {
                    Dispose();
                }
            }

            void ITreeHandleable.MakeReadyFromSettled(IValueContainer valueContainer)
            {
                if (valueContainer.GetState() == Promise.State.Canceled)
                {
                    valueContainer.Retain();
                    _valueContainer = valueContainer;
                    AddToHandleQueueBack(this);
                }
                else
                {
                    Dispose();
                }
            }

            void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
            {
                _valueContainer = valueContainer;
                _valueContainer.Retain();
                Invoke();
            }

            void ITreeHandleable.Handle()
            {
                Invoke();
            }

            private void Invoke()
            {
                _SetCurrentInvoker(this);
                var value = _capturedValue;
                var callback = _onCanceled;
                var container = _valueContainer;
                Dispose();
                try
                {
                    callback.Invoke(value, new ReasonContainer(container));
                }
                catch (Exception e)
                {
                    AddRejectionToUnhandledStack(e, this);
                }
                container.Release();
                _ClearCurrentInvoker();
            }

            public void Dispose()
            {
                _capturedValue = default(TCapture);
                _onCanceled = null;
                _valueContainer = null;
                if (Promise.Config.ObjectPooling != Promise.PoolType.None)
                {
                    _pool.Push(this);
                }
            }
        }

        [DebuggerNonUserCode]
        internal sealed class CancelationRef : ICancelDelegate, ILinked<CancelationRef>, ITraceable
        {
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
                if (!_isDisposed)
                {
                    // CancelationSource wasn't disposed.
                    AddRejectionToUnhandledStack(new Exception("CancelationSource's resources were garbage collected without being disposed."), this);
                }
            }

            CancelationRef ILinked<CancelationRef>.Next { get; set; }

            private static ValueLinkedStack<CancelationRef> _pool;

            private readonly List<RegisteredDelegate> _registeredCallbacks = new List<RegisteredDelegate>();
            private ValueLinkedStackZeroGC<CancelationRegistration> _links;
            public ICancelValueContainer ValueContainer { get; private set; }
            private uint _registeredCount;
            private ushort _retainCounter;

            public ushort SourceId { get; private set; }
            public ushort TokenId { get; private set; }

            private bool _isDisposed;
            private bool _isInvoking;

            public bool IsCanceled { get { return ValueContainer != null; } }

            static CancelationRef()
            {
                OnClearPool += ValueLinkedStackZeroGC<CancelationRegistration>.ClearPooledNodes;
            }

            public static CancelationRef GetOrCreate()
            {
                var cancelRef = _pool.IsNotEmpty ? _pool.Pop() : new CancelationRef();
                cancelRef._isDisposed = false;
                _SetCreatedStacktrace(cancelRef, 2);
                return cancelRef;
            }

            public void AddLinkedCancelation(CancelationRef listener)
            {
                if (IsCanceled)
                {
                    // Don't need to worry about invoking callbacks here since this is only called from CancelationSource.CreateLinkedSource.
                    listener.ValueContainer = ValueContainer;
                    ValueContainer.Retain();
                }
                else
                {
                    listener._links.Push(new CancelationRegistration(this, listener));
                }
            }

            public uint Register(ICancelDelegate callback)
            {
                if (ValueContainer != null)
                {
                    callback.Invoke(ValueContainer);
                    return 0;
                }
                checked
                {
                    uint order = ++_registeredCount;
                    _registeredCallbacks.Add(new RegisteredDelegate(order, callback));
                    return order;
                }
            }

            public void Unregister(uint order)
            {
                _registeredCallbacks.RemoveAt(IndexOf(order));
            }

            public bool TryUnregister(uint order)
            {
                int index = IndexOf(order);
                if (index >= 0)
                {
                    _registeredCallbacks.RemoveAt(index);
                    return true;
                }
                return false;
            }

            public int IndexOf(uint order)
            {
                if (_isDisposed | ValueContainer != null)
                {
                    return -1;
                }
                return _registeredCallbacks.BinarySearch(new RegisteredDelegate(order));
            }

            public void SetCanceled()
            {
                ValueContainer = CancelContainerVoid.GetOrCreate();
                InvokeCallbacks();
            }

            public void SetCanceled<T>(ref T cancelValue)
            {
                ValueContainer = CreateCancelContainer(ref cancelValue);
                ValueContainer.Retain();
                InvokeCallbacks();
            }

            private void InvokeCallbacks()
            {
                // Retain in case this is disposed while executing callbacks.
                Retain();
                Unlink();
                _isInvoking = true;
                foreach (var del in _registeredCallbacks)
                {
                    del.callback.Invoke(ValueContainer);
                }
                _registeredCallbacks.Clear();
                _isInvoking = false;
                Release();
            }

            public void Retain()
            {
                // Make sure Retain doesn't overflow the ushort. 1 retain is reserved for internal use.
                if (_retainCounter == ushort.MaxValue - 1)
                {
                    throw new OverflowException();
                }
                ++_retainCounter;
            }

            public void Release()
            {
                if (_retainCounter == 0 | (_isInvoking & _retainCounter == 1))
                {
                    throw new InvalidOperationException("You must call Retain before you call Release!", GetFormattedStacktrace(1));
                }
                // If SourceId is different from TokenId, Dispose was called while this was retained.
                if (--_retainCounter == 0 & SourceId != TokenId)
                {
                    ResetAndRepool();
                }
            }

            public void Dispose()
            {
                _isDisposed = true;
                ++SourceId;
                _registeredCount = 0;
                Unlink();
                // In case Dispose is called from a callback.
                if (!_isInvoking)
                {
                    foreach (var del in _registeredCallbacks)
                    {
                        del.callback.Dispose();
                    }
                    _registeredCallbacks.Clear();
                }
                if (_retainCounter == 0)
                {
                    ResetAndRepool();
                }
            }

            private void ResetAndRepool()
            {
                TokenId = SourceId;
                if (ValueContainer != null)
                {
                    ValueContainer.Release();
                    ValueContainer = null;
                }
                if (Promise.Config.ObjectPooling != Promise.PoolType.None)
                {
                    _pool.Push(this);
                }
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
                // In case this is called recursively from another callback.
                if (_isDisposed | IsCanceled) return;

                ValueContainer = valueContainer;
                ValueContainer.Retain();
                InvokeCallbacks();
            }
        }
    }
}