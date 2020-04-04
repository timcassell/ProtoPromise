#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable RECS0001 // Class is declared partial but has only one part
#pragma warning disable RECS0096 // Type parameter is never used
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
        // Calls to this get compiled away when CANCEL is defined.
        static partial void ValidateCancel(int skipFrames);

        static partial void AddToCancelQueueBack(Internal.ITreeHandleable cancelation);
        static partial void AddToCancelQueueFront(Internal.ITreeHandleable cancelation);
        static partial void AddToCancelQueueFront(ref ValueLinkedQueue<Internal.ITreeHandleable> cancelations);
        static partial void AddToCancelQueueBack(ref ValueLinkedQueue<Internal.ITreeHandleable> cancelations);
        static partial void HandleCanceled();
        partial void CancelDirectIfPending();
#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
        partial void CancelDirectIfPending<TCancel>(in TCancel reason);
#else
        partial void CancelDirectIfPending<TCancel>(TCancel reason);
#endif

#if PROMISE_CANCEL
        // Cancel promises in a depth-first manner.
        private static ValueLinkedQueue<Internal.ITreeHandleable> _cancelQueue;

        static partial void AddToCancelQueueFront(Internal.ITreeHandleable cancelation)
        {
            _cancelQueue.Push(cancelation);
        }

        static partial void AddToCancelQueueBack(Internal.ITreeHandleable cancelation)
        {
            _cancelQueue.Enqueue(cancelation);
        }

        static partial void AddToCancelQueueFront(ref ValueLinkedQueue<Internal.ITreeHandleable> cancelations)
        {
            _cancelQueue.PushAndClear(ref cancelations);
        }

        static partial void AddToCancelQueueBack(ref ValueLinkedQueue<Internal.ITreeHandleable> cancelations)
        {
            _cancelQueue.EnqueueAndClear(ref cancelations);
        }

        static partial void HandleCanceled()
        {
            while (_cancelQueue.IsNotEmpty)
            {
                _cancelQueue.DequeueRisky().Cancel();
            }
            _cancelQueue.ClearLast();
        }

        void Internal.ITreeHandleable.Cancel()
        {
            if (_state == State.Pending)
            {
                CancelInternal();
            }
        }

        partial void CancelDirectIfPending()
        {
            if (_state != State.Pending)
            {
                if (_retainCounter == 0)
                {
                    AddToHandleQueueFront(this);
                }
            }
            else
            {
                _state = State.Canceled;
                var cancelValue = Internal.CancelContainerVoid.GetOrCreate();
                _valueOrPrevious = cancelValue;
                AddBranchesToCancelQueueBack(cancelValue);
                CancelProgressListeners();
                OnCancel();
            }
        }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
        partial void CancelDirectIfPending<TCancel>(in TCancel reason)
#else
        partial void CancelDirectIfPending<TCancel>(TCancel reason)
#endif
        {
            if (_state != State.Pending)
            {
                if (_retainCounter == 0)
                {
                    AddToHandleQueueFront(this);
                }
            }
            else
            {
                _state = State.Canceled;
                var cancelValue = Internal.CancelContainer<TCancel>.GetOrCreate(reason);
                cancelValue.Retain();
                _valueOrPrevious = cancelValue;
                AddBranchesToCancelQueueBack(cancelValue);
                CancelProgressListeners();
                OnCancel();
            }
        }
#else
        static protected void ThrowCancelException(int skipFrames)
        {
            throw new InvalidOperationException("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", GetFormattedStacktrace(skipFrames + 1));
        }

        static partial void ValidateCancel(int skipFrames)
        {
            ThrowCancelException(skipFrames + 1);
        }
#endif

        partial class Internal
        {
            public sealed partial class CancelDelegate : ITreeHandleable
            {
                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

                private static ValueLinkedStack<ITreeHandleable> _pool;

                private Action<CancelReason> _onCanceled;
                private object _valueContainer;

                private CancelDelegate() { }

                static CancelDelegate()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static CancelDelegate GetOrCreate(Action<CancelReason> onCanceled, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegate) _pool.Pop() : new CancelDelegate();
                    del._onCanceled = onCanceled;
                    SetCreatedStacktrace(del, skipFrames + 1);
                    return del;
                }

                void ITreeHandleable.MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue, ref ValueLinkedQueue<ITreeHandleable> cancelQueue)
                {
                    if (valueContainer.GetState() == State.Canceled)
                    {
                        valueContainer.Retain();
                        _valueContainer = valueContainer;
                        cancelQueue.Push(this);
                    }
                    else
                    {
                        Dispose();
                    }
                }

                void ITreeHandleable.MakeReadyFromSettled(IValueContainer valueContainer)
                {
                    if (valueContainer.GetState() == State.Canceled)
                    {
                        valueContainer.Retain();
                        _valueContainer = valueContainer;
                        AddToCancelQueueBack(this);
                    }
                    else
                    {
                        Dispose();
                    }
                }

                void ITreeHandleable.Cancel()
                {
                    SetCurrentInvoker(this);
                    var callback = _onCanceled;
                    var container = (IValueContainer) _valueContainer;
                    SetCurrentInvoker(this);
                    Dispose();
                    try
                    {
                        SetInvokingCancel();
                        callback.Invoke(new CancelReason(container));
                    }
                    catch (Exception e)
                    {
                        AddRejectionToUnhandledStack(e, this);
                    }
                    container.Release();
                    ClearCurrentInvoker();
                }

                void Dispose()
                {
                    _onCanceled = null;
                    _valueContainer = DisposedObject;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void ITreeHandleable.Handle() { throw new System.InvalidOperationException(); }
            }

            public sealed partial class CancelDelegateCapture<TCapture> : ITreeHandleable
            {
                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

                private static ValueLinkedStack<ITreeHandleable> _pool;

                private TCapture _capturedValue;
                private Action<TCapture, CancelReason> _onCanceled;
                private object _valueContainer;

                private CancelDelegateCapture() { }

                static CancelDelegateCapture()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static CancelDelegateCapture<TCapture> GetOrCreate(TCapture capturedValue, Action<TCapture, CancelReason> onCanceled, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegateCapture<TCapture>) _pool.Pop() : new CancelDelegateCapture<TCapture>();
                    del._onCanceled = onCanceled;
                    del._capturedValue = capturedValue;
                    SetCreatedStacktrace(del, skipFrames + 1);
                    return del;
                }

                void ITreeHandleable.MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue, ref ValueLinkedQueue<ITreeHandleable> cancelQueue)
                {
                    if (valueContainer.GetState() == State.Canceled)
                    {
                        valueContainer.Retain();
                        _valueContainer = valueContainer;
                        cancelQueue.Push(this);
                    }
                    else
                    {
                        Dispose();
                    }
                }

                void ITreeHandleable.MakeReadyFromSettled(IValueContainer valueContainer)
                {
                    if (valueContainer.GetState() == State.Canceled)
                    {
                        valueContainer.Retain();
                        _valueContainer = valueContainer;
                        AddToCancelQueueBack(this);
                    }
                    else
                    {
                        Dispose();
                    }
                }

                void ITreeHandleable.Cancel()
                {
                    SetCurrentInvoker(this);
                    var value = _capturedValue;
                    var callback = _onCanceled;
                    var container = (IValueContainer) _valueContainer;
                    SetCurrentInvoker(this);
                    Dispose();
                    try
                    {
                        SetInvokingCancel();
                        callback.Invoke(value, new CancelReason(container));
                    }
                    catch (Exception e)
                    {
                        AddRejectionToUnhandledStack(e, this);
                    }
                    container.Release();
                    ClearCurrentInvoker();
                }

                void Dispose()
                {
                    _capturedValue = default(TCapture);
                    _onCanceled = null;
                    _valueContainer = DisposedObject;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void ITreeHandleable.Handle() { throw new System.InvalidOperationException(); }
            }
        }

        private void ResolveDirectIfNotCanceled()
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                if (_retainCounter == 0)
                {
                    AddToHandleQueueFront(this);
                }
            }
            else
#endif
            {
                _state = State.Resolved;
                var resolveValue = Internal.ResolveContainerVoid.GetOrCreate();
                _valueOrPrevious = resolveValue;
                AddBranchesToHandleQueueBack(resolveValue);
                ResolveProgressListeners();
                AddToHandleQueueFront(this);
            }
        }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
        private void ResolveDirectIfNotCanceled<T>(in T value)
#else
        private void ResolveDirectIfNotCanceled<T>(T value)
#endif
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                if (_retainCounter == 0)
                {
                    AddToHandleQueueFront(this);
                }
            }
            else
#endif
            {
                _state = State.Resolved;
                var resolveValue = Internal.ResolveContainer<T>.GetOrCreate(value);
                resolveValue.Retain();
                _valueOrPrevious = resolveValue;
                AddBranchesToHandleQueueBack(resolveValue);
                ResolveProgressListeners();
                AddToHandleQueueFront(this);
            }
        }

        protected void RejectDirectIfNotCanceled<TReject>(TReject reason, bool generateStacktrace)
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                AddRejectionToUnhandledStack(reason, generateStacktrace ? null : this);
                if (_retainCounter == 0)
                {
                    AddToHandleQueueFront(this);
                }
            }
            else
#endif
            {
                RejectDirect(reason, generateStacktrace);
            }
        }

        protected void ResolveInternalIfNotCanceled()
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                ReleaseInternal();
            }
            else
#endif
            {
                ResolveInternal(Internal.ResolveContainerVoid.GetOrCreate());
            }
        }

        protected void ResolveInternalIfNotCanceled<T>(T value)
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                ReleaseInternal();
            }
            else
#endif
            {
                ResolveInternal(Internal.ResolveContainer<T>.GetOrCreate(value));
            }
        }
    }

    partial class Promise<T>
    {
        // Calls to this get compiled away when CANCEL is defined.
        static partial void ValidateCancel(int skipFrames);
#if !PROMISE_CANCEL
        static partial void ValidateCancel(int skipFrames)
        {
            ThrowCancelException(skipFrames + 1);
        }
#endif
    }
}