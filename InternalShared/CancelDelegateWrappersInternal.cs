#pragma warning disable IDE0034 // Simplify 'default' expression

using System;

namespace Proto.Promises
{
    internal static partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct CancelDelegatePromise : IDelegateCancel
        {
            private readonly Action<ReasonContainer> _callback;
            private IValueContainer _valueContainer;

            public CancelDelegatePromise(Action<ReasonContainer> callback)
            {
                _callback = callback;
                _valueContainer = null;
            }

            public void SetValue(IValueContainer valueContainer)
            {
                valueContainer.Retain();
                _valueContainer = valueContainer;
            }

            public void InvokeFromPromise(ITraceable owner)
            {
                try
                {
                    _callback.Invoke(new ReasonContainer(_valueContainer));
                }
                finally
                {
                    _valueContainer.Release();
                }
            }

            public void InvokeFromToken(IValueContainer valueContainer, IDisposableTreeHandleable owner) { throw new System.InvalidOperationException(); }
            public void MaybeDispose(IDisposable owner) { throw new System.InvalidOperationException(); }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct CancelDelegatePromise<TCapture> : IDelegateCancel
        {
            private readonly TCapture _captureValue;
            private readonly Action<TCapture, ReasonContainer> _callback;
            private IValueContainer _valueContainer;

            public CancelDelegatePromise(ref TCapture captureValue, Action<TCapture, ReasonContainer> callback)
            {
                _captureValue = captureValue;
                _callback = callback;
                _valueContainer = null;
            }

            public void SetValue(IValueContainer valueContainer)
            {
                valueContainer.Retain();
                _valueContainer = valueContainer;
            }

            public void InvokeFromPromise(ITraceable owner)
            {
                try
                {
                    _callback.Invoke(_captureValue, new ReasonContainer(_valueContainer));
                }
                finally
                {
                    _valueContainer.Release();
                }
            }

            public void InvokeFromToken(IValueContainer valueContainer, IDisposableTreeHandleable owner) { throw new System.InvalidOperationException(); }
            public void MaybeDispose(IDisposable owner) { throw new System.InvalidOperationException(); }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct CancelDelegatePromiseCancel : IDelegateCancel
        {
            public CancelationRegistration cancelationRegistration;

            private readonly Action<ReasonContainer> _callback;
            private readonly ITreeHandleableCollection _previous;
            private IValueContainer _valueContainer;

            public CancelDelegatePromiseCancel(Action<ReasonContainer> callback, Promise previous)
            {
                _callback = callback;
                _previous = previous;
                _valueContainer = null;
                cancelationRegistration = default(CancelationRegistration);
            }

            public void InvokeFromToken(IValueContainer valueContainer, IDisposableTreeHandleable owner)
            {
                // When token is canceled, don't invoke the callback.
                if (_valueContainer != null)
                {
                    // Owner is in the event queue, just release the container.
                    _valueContainer.Release();
                    _valueContainer = null;
                }
                else
                {
                    _previous.Remove(owner);
                    owner.Dispose();
                }
            }

            public void SetValue(IValueContainer valueContainer)
            {
                valueContainer.Retain();
                _valueContainer = valueContainer;
            }

            public void InvokeFromPromise(ITraceable owner)
            {
                if (_valueContainer == null)
                {
                    // Make sure invocation is still valid in case this is canceled while waiting in the event queue.
                    return;
                }
                try
                {
                    cancelationRegistration.TryUnregister();
                    _callback.Invoke(new ReasonContainer(_valueContainer));
                }
                finally
                {
                    _valueContainer.Release();
                }
            }

            public void MaybeDispose(IDisposable owner)
            {
                if (_valueContainer != null)
                {
                    owner.Dispose();
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct CancelDelegatePromiseCancel<TCapture> : IDelegateCancel
        {
            public CancelationRegistration cancelationRegistration;

            private readonly TCapture _captureValue;
            private readonly Action<TCapture, ReasonContainer> _callback;
            private readonly ITreeHandleableCollection _previous;
            private IValueContainer _valueContainer;

            public CancelDelegatePromiseCancel(ref TCapture captureValue, Action<TCapture, ReasonContainer> callback, Promise previous)
            {
                _captureValue = captureValue;
                _callback = callback;
                cancelationRegistration = default(CancelationRegistration);
                _previous = previous;
                _valueContainer = null;
            }

            public void InvokeFromToken(IValueContainer valueContainer, IDisposableTreeHandleable owner)
            {
                // When token is canceled, don't invoke the callback.
                if (_valueContainer != null)
                {
                    // Owner is in the event queue, just release the container.
                    _valueContainer.Release();
                    _valueContainer = null;
                }
                else
                {
                    _previous.Remove(owner);
                    owner.Dispose();
                }
            }

            public void SetValue(IValueContainer valueContainer)
            {
                valueContainer.Retain();
                _valueContainer = valueContainer;
            }

            public void InvokeFromPromise(ITraceable owner)
            {
                if (_valueContainer == null)
                {
                    // Make sure invocation is still valid in case this is canceled while waiting in the event queue.
                    return;
                }
                try
                {
                    cancelationRegistration.TryUnregister();
                    _callback.Invoke(_captureValue, new ReasonContainer(_valueContainer));
                }
                finally
                {
                    _valueContainer.Release();
                }
            }

            public void MaybeDispose(IDisposable owner)
            {
                if (_valueContainer != null)
                {
                    owner.Dispose();
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct CancelDelegateToken : IDelegateCancel
        {
            private readonly Action<ReasonContainer> _callback;

            public CancelDelegateToken(Action<ReasonContainer> callback)
            {
                _callback = callback;
            }

            public void InvokeFromToken(IValueContainer valueContainer, IDisposableTreeHandleable owner)
            {
                // Disposing the owner sets _callback to null, so copy to stack first.
                var callback = _callback;
                owner.Dispose();
                callback.Invoke(new ReasonContainer(valueContainer));
            }

            public void MaybeDispose(IDisposable owner)
            {
                owner.Dispose();
            }

            public void SetValue(IValueContainer valueContainer) { throw new System.InvalidOperationException(); }
            public void InvokeFromPromise(ITraceable owner) { throw new System.InvalidOperationException(); }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct CancelDelegateToken<TCapture> : IDelegateCancel
        {
            private readonly TCapture _captureValue;
            private readonly Action<TCapture, ReasonContainer> _callback;

            public CancelDelegateToken(ref TCapture captureValue, Action<TCapture, ReasonContainer> callback)
            {
                _captureValue = captureValue;
                _callback = callback;
            }

            public void InvokeFromToken(IValueContainer valueContainer, IDisposableTreeHandleable owner)
            {
                // Disposing the owner sets fields to default, so copy to stack first.
                var callback = _callback;
                var capturevalue = _captureValue;
                owner.Dispose();
                callback.Invoke(capturevalue, new ReasonContainer(valueContainer));
            }

            public void MaybeDispose(IDisposable owner)
            {
                owner.Dispose();
            }

            public void SetValue(IValueContainer valueContainer) { throw new System.InvalidOperationException(); }
            public void InvokeFromPromise(ITraceable owner) { throw new System.InvalidOperationException(); }
        }
    }
}