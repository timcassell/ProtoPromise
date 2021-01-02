#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Threading;

namespace Proto.Promises
{
    internal static partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct CancelDelegatePromise : IDelegateCancel
        {
            private readonly Promise.CanceledAction _callback;
            private IValueContainer _valueContainer;

            public CancelDelegatePromise(Promise.CanceledAction callback)
            {
                _callback = callback;
                _valueContainer = null;
            }

            public bool TrySetValue(IValueContainer valueContainer)
            {
                valueContainer.Retain();
                _valueContainer = valueContainer;
                return true;
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
            private readonly Promise.CanceledAction<TCapture> _callback;
            private IValueContainer _valueContainer;

            public CancelDelegatePromise(ref TCapture captureValue, Promise.CanceledAction<TCapture> callback)
            {
                _captureValue = captureValue;
                _callback = callback;
                _valueContainer = null;
            }

            public bool TrySetValue(IValueContainer valueContainer)
            {
                valueContainer.Retain();
                _valueContainer = valueContainer;
                return true;
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

            private readonly Promise.CanceledAction _callback;
            private readonly ITreeHandleableCollection _previous;
            private IValueContainer _valueContainer;
            // TODO
            //volatile private bool _isSettingValue;

            public CancelDelegatePromiseCancel(Promise.CanceledAction callback, PromiseRef previous)
            {
                _callback = callback;
                _previous = previous;
                _valueContainer = null;
                cancelationRegistration = default(CancelationRegistration);
                //_isSettingValue = false;
            }

            public void InvokeFromToken(IValueContainer valueContainer, IDisposableTreeHandleable owner)
            {
                //SpinWait spinner = new SpinWait();
                //while (_isSettingValue)
                //{
                //    spinner.SpinOnce();
                //}
                //Thread.MemoryBarrier();
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

            public bool TrySetValue(IValueContainer valueContainer)
            {
                //_isSettingValue = true;
                bool isStillRegistered = cancelationRegistration.IsRegistered;
                if (isStillRegistered)
                {
                    valueContainer.Retain();
                    _valueContainer = valueContainer;
                    //Thread.MemoryBarrier();
                }
                //_isSettingValue = false;
                return isStillRegistered;
            }

            public void InvokeFromPromise(ITraceable owner)
            {
                if (!cancelationRegistration.TryUnregister())
                {
                    // If we couldn't unregister the cancelation, it means the cancelation already ran. Don't invoke the callback.
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
            private readonly Promise.CanceledAction<TCapture> _callback;
            private readonly ITreeHandleableCollection _previous;
            private IValueContainer _valueContainer;
            // TODO
            //volatile private bool _isSettingValue;

            public CancelDelegatePromiseCancel(ref TCapture captureValue, Promise.CanceledAction<TCapture> callback, PromiseRef previous)
            {
                _captureValue = captureValue;
                _callback = callback;
                cancelationRegistration = default(CancelationRegistration);
                _previous = previous;
                _valueContainer = null;
                //_isSettingValue = false;
            }

            public void InvokeFromToken(IValueContainer valueContainer, IDisposableTreeHandleable owner)
            {
                //SpinWait spinner = new SpinWait();
                //while (_isSettingValue)
                //{
                //    spinner.SpinOnce();
                //}
                //Thread.MemoryBarrier();
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

            public bool TrySetValue(IValueContainer valueContainer)
            {
                //_isSettingValue = true;
                bool isStillRegistered = cancelationRegistration.IsRegistered;
                if (isStillRegistered)
                {
                    valueContainer.Retain();
                    _valueContainer = valueContainer;
                    //Thread.MemoryBarrier();
                }
                //_isSettingValue = false;
                return isStillRegistered;
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
            private readonly Promise.CanceledAction _callback;

            public CancelDelegateToken(Promise.CanceledAction callback)
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

            public bool TrySetValue(IValueContainer valueContainer) { throw new System.InvalidOperationException(); }
            public void InvokeFromPromise(ITraceable owner) { throw new System.InvalidOperationException(); }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct CancelDelegateToken<TCapture> : IDelegateCancel
        {
            private readonly TCapture _captureValue;
            private readonly Promise.CanceledAction<TCapture> _callback;

            public CancelDelegateToken(ref TCapture captureValue, Promise.CanceledAction<TCapture> callback)
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

            public bool TrySetValue(IValueContainer valueContainer) { throw new System.InvalidOperationException(); }
            public void InvokeFromPromise(ITraceable owner) { throw new System.InvalidOperationException(); }
        }
    }
}