using System;
using System.Diagnostics;

namespace Proto.Promises
{
    internal static partial class Internal
    {
        [DebuggerNonUserCode]
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

            public void MaybeUnregisterCancelation() { }

            public void InvokeFromToken(IValueContainer valueContainer, ITreeHandleable owner) { throw new System.InvalidOperationException(); }
        }

        [DebuggerNonUserCode]
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

            public void MaybeUnregisterCancelation() { }

            public void InvokeFromToken(IValueContainer valueContainer, ITreeHandleable owner) { throw new System.InvalidOperationException(); }
        }

        [DebuggerNonUserCode]
        internal struct CancelDelegatePromiseCancel : IDelegateCancel
        {
            private readonly Action<ReasonContainer> _callback;
            private readonly CancelationRegistration _cancelationRegistration;
            private readonly ITreeHandleableCollection _owner;
            private IValueContainer _valueContainer;

            public CancelDelegatePromiseCancel(Action<ReasonContainer> callback, Promise owner, CancelationRegistration cancelationRegistration)
            {
                _callback = callback;
                _cancelationRegistration = cancelationRegistration;
                _owner = owner;
                _valueContainer = null;
            }

            public void InvokeFromToken(IValueContainer valueContainer, ITreeHandleable owner)
            {
                // When token is canceled, don't invoke the callback.
                _owner.Remove(owner);
                if (_valueContainer != null)
                {
                    _valueContainer.Release();
                    _valueContainer = null;
                }
            }

            public void SetValue(IValueContainer valueContainer)
            {
                valueContainer.Retain();
                _valueContainer = valueContainer;
            }

            public void InvokeFromPromise(ITraceable owner)
            {
                if (_valueContainer != null)
                {
                    // Make sure invocation is still valid in case this is canceled while waiting in the event queue.
                    return;
                }
                try
                {
                    _callback.Invoke(new ReasonContainer(_valueContainer));
                }
                finally
                {
                    _valueContainer.Release();
                }
            }

            public void MaybeUnregisterCancelation()
            {
                _cancelationRegistration.TryUnregister();
            }
        }

        [DebuggerNonUserCode]
        internal struct CancelDelegatePromiseCancel<TCapture> : IDelegateCancel
        {
            private readonly TCapture _captureValue;
            private readonly Action<TCapture, ReasonContainer> _callback;
            private readonly CancelationRegistration _cancelationRegistration;
            private readonly ITreeHandleableCollection _owner;
            private IValueContainer _valueContainer;

            public CancelDelegatePromiseCancel(ref TCapture captureValue, Action<TCapture, ReasonContainer> callback, Promise owner, CancelationRegistration cancelationRegistration)
            {
                _captureValue = captureValue;
                _callback = callback;
                _cancelationRegistration = cancelationRegistration;
                _owner = owner;
                _valueContainer = null;
            }

            public void InvokeFromToken(IValueContainer valueContainer, ITreeHandleable owner)
            {
                // When token is canceled, don't invoke the callback.
                _owner.Remove(owner);
                if (_valueContainer != null)
                {
                    _valueContainer.Release();
                    _valueContainer = null;
                }
            }

            public void SetValue(IValueContainer valueContainer)
            {
                valueContainer.Retain();
                _valueContainer = valueContainer;
            }

            public void InvokeFromPromise(ITraceable owner)
            {
                if (_valueContainer != null)
                {
                    // Make sure invocation is still valid in case this is canceled while waiting in the event queue.
                    return;
                }
                try
                {
                    _callback.Invoke(_captureValue, new ReasonContainer(_valueContainer));
                }
                finally
                {
                    _valueContainer.Release();
                }
            }

            public void MaybeUnregisterCancelation()
            {
                _cancelationRegistration.TryUnregister();
            }
        }

        [DebuggerNonUserCode]
        internal struct CancelDelegateToken : IDelegateCancel
        {
            private readonly Action<ReasonContainer> _callback;

            public CancelDelegateToken(Action<ReasonContainer> callback)
            {
                _callback = callback;
            }

            public void InvokeFromToken(IValueContainer valueContainer, ITreeHandleable owner)
            {
                _callback.Invoke(new ReasonContainer(valueContainer));
            }

            public void SetValue(IValueContainer valueContainer) { throw new System.InvalidOperationException(); }
            public void InvokeFromPromise(ITraceable owner) { throw new System.InvalidOperationException(); }
            public void MaybeUnregisterCancelation() { throw new System.InvalidOperationException(); }
        }

        [DebuggerNonUserCode]
        internal struct CancelDelegateToken<TCapture> : IDelegateCancel
        {
            private readonly TCapture _captureValue;
            private readonly Action<TCapture, ReasonContainer> _callback;

            public CancelDelegateToken(ref TCapture captureValue, Action<TCapture, ReasonContainer> callback)
            {
                _captureValue = captureValue;
                _callback = callback;
            }

            public void InvokeFromToken(IValueContainer valueContainer, ITreeHandleable owner)
            {
                _callback.Invoke(_captureValue, new ReasonContainer(valueContainer));
            }

            public void SetValue(IValueContainer valueContainer) { throw new System.InvalidOperationException(); }
            public void InvokeFromPromise(ITraceable owner) { throw new System.InvalidOperationException(); }
            public void MaybeUnregisterCancelation() { throw new System.InvalidOperationException(); }
        }
    }
}