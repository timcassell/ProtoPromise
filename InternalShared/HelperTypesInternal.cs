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
        public sealed class CancelDelegate : ICancelDelegate, ITraceable
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

            void ICancelDelegate.Invoke(IValueContainer valueContainer)
            {
                valueContainer.Retain();
                _valueContainer = valueContainer;
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
        public sealed class CancelDelegateCapture<TCapture> : ICancelDelegate, ITraceable
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

            void ICancelDelegate.Invoke(IValueContainer valueContainer)
            {
                valueContainer.Retain();
                _valueContainer = valueContainer;
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
        internal sealed class CancelationRef : ILinked<CancelationRef>, ITraceable
        {
            private struct RegisteredDelegate : IComparable<RegisteredDelegate>
            {
                public readonly ICancelDelegate callback;
                public readonly int order;

                public RegisteredDelegate(int order)
                {
                    callback = null;
                    this.order = order;
                }

                public RegisteredDelegate(int order, ICancelDelegate callback)
                {
                    this.callback = callback;
                    this.order = order;
                }

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
                if (_retainCount > 0)
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
            private int _registeredCount;
            private int _retainCount;

            public ICancelValueContainer ValueContainer { get; private set; }
            public int SourceId { get; private set; }
            public int TokenId { get; private set; }

            public bool IsCanceled { get { return ValueContainer != null; } }

            private bool _isDisposed;

            public static CancelationRef GetOrCreate()
            {
                var cancelRef = _pool.IsNotEmpty ? _pool.Pop() : new CancelationRef();
                cancelRef._isDisposed = false;
                _SetCreatedStacktrace(cancelRef, 2);
                return cancelRef;
            }

            public int Register(ICancelDelegate callback)
            {
                if (ValueContainer != null)
                {
                    callback.Invoke(ValueContainer);
                    return 0;
                }
                checked
                {
                    int order = ++_registeredCount;
                    _registeredCallbacks.Add(new RegisteredDelegate(order, callback));
                    return order;
                }
            }

            public void Unregister(int order)
            {
                int index = _registeredCallbacks.BinarySearch(new RegisteredDelegate(order));
                _registeredCallbacks.RemoveAt(index);
            }

            public bool IsRegistered(int order)
            {
                int index = _registeredCallbacks.BinarySearch(new RegisteredDelegate(order));
                return index >= 0;
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
                foreach (var del in _registeredCallbacks)
                {
                    del.callback.Invoke(ValueContainer);
                }
                _registeredCallbacks.Clear();
            }

            public void Dispose()
            {
                ++SourceId;
                _registeredCount = 0;
                foreach (var del in _registeredCallbacks)
                {
                    del.callback.Dispose();
                }
                _registeredCallbacks.Clear();
                if (_retainCount == 0)
                {
                    ResetAndRepool();
                }
                _isDisposed = true;
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

            public void Retain()
            {
                checked
                {
                    ++_retainCount;
                }
            }

            public void Release()
            {
                checked
                {
                    // If SourceId is different from TokenId, Dispose was called while this was retained.
                    if (--_retainCount == 0 & SourceId != TokenId)
                    {
                        ResetAndRepool();
                    }
                }
            }
        }
    }
}