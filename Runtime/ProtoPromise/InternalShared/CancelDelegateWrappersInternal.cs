#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Runtime.CompilerServices;
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

            [MethodImpl(InlineOption)]
            public CancelDelegatePromise(Promise.CanceledAction callback)
            {
                _callback = callback;
                _valueContainer = null;
            }

            [MethodImpl(InlineOption)]
            public bool TryMakeReady(IValueContainer valueContainer, IDisposable owner)
            {
                bool canceled = valueContainer.GetState() == Promise.State.Canceled;
                if (canceled)
                {
                    valueContainer.Retain();
                    _valueContainer = valueContainer;
                }
                else
                {
                    owner.Dispose();
                }
                return canceled;
            }

            [MethodImpl(InlineOption)]
            public void InvokeFromPromise(IDisposable owner)
            {
                var temp = this;
                owner.Dispose();
                try
                {
                    temp._callback.Invoke(new ReasonContainer(temp._valueContainer));
                }
                finally
                {
                    temp._valueContainer.Release();
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

            [MethodImpl(InlineOption)]
            public CancelDelegatePromise(ref TCapture captureValue, Promise.CanceledAction<TCapture> callback)
            {
                _captureValue = captureValue;
                _callback = callback;
                _valueContainer = null;
            }

            [MethodImpl(InlineOption)]
            public bool TryMakeReady(IValueContainer valueContainer, IDisposable owner)
            {
                bool canceled = valueContainer.GetState() == Promise.State.Canceled;
                if (canceled)
                {
                    valueContainer.Retain();
                    _valueContainer = valueContainer;
                }
                else
                {
                    owner.Dispose();
                }
                return canceled;
            }

            [MethodImpl(InlineOption)]
            public void InvokeFromPromise(IDisposable owner)
            {
                var temp = this;
                owner.Dispose();
                try
                {
                    temp._callback.Invoke(temp._captureValue, new ReasonContainer(temp._valueContainer));
                }
                finally
                {
                    temp._valueContainer.Release();
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
            volatile private IValueContainer _valueContainer;
            private int _cancelFlag;

            [MethodImpl(InlineOption)]
            public CancelDelegatePromiseCancel(Promise.CanceledAction callback, PromiseRef previous)
            {
                _callback = callback;
                _previous = previous;
                _valueContainer = null;
                cancelationRegistration = default(CancelationRegistration);
                _cancelFlag = 1;
            }

            [MethodImpl(InlineOption)]
            public void InvokeFromToken(IValueContainer valueContainer, IDisposableTreeHandleable owner)
            {
                int oldFlag = Interlocked.Exchange(ref _cancelFlag, 0);
                if (oldFlag == 2)
                {
                    // TryMakeReady was already called without InvokeFromPromise.
                    return;
                }
                if (oldFlag != 3) // If InvokeFromPromise was called before this, just dispose without removing.
                {
                    _previous.Remove(owner);
                }
                owner.Dispose();
            }

            [MethodImpl(InlineOption)]
            public bool TryMakeReady(IValueContainer valueContainer, IDisposable owner)
            {
                if (Interlocked.Exchange(ref _cancelFlag, 2) == 0)
                {
                    // InvokeFromToken was already called.
                    return false;
                }
                // Always make ready, even if state is not canceled. This makes it easier to make this thread-safe.
                valueContainer.Retain();
                _valueContainer = valueContainer;
                return true;
            }

            [MethodImpl(InlineOption)]
            public void InvokeFromPromise(IDisposable owner)
            {
                var tempValueContainer = _valueContainer;
                int oldFlag = Interlocked.Exchange(ref _cancelFlag, 3);
                if (oldFlag == 0)
                {
                    // InvokeFromPromise can only be called after TryMakeReady, so we know that if oldFlag == 0, InvokeFromToken previously saw it == 2 and did nothing.
                    tempValueContainer.Release();
                    owner.Dispose();
                    return;
                }
                if (!cancelationRegistration.TryUnregister())
                {
                    // If we couldn't unregister the cancelation, it means the token was already canceled, and InvokeFromToken maybe hasn't been called yet (or was called after the flag exchange).
                    tempValueContainer.Release();
                    return;
                }
                var tempCallback = _callback;
                owner.Dispose();
                try
                {
                    if (tempValueContainer.GetState() == Promise.State.Canceled) // We have to check canceled state before invoking the callback instead of in TryMakeReady for thread safety reasons.
                    {
                        tempCallback.Invoke(new ReasonContainer(tempValueContainer));
                    }
                }
                finally
                {
                    tempValueContainer.Release();
                }
            }

            [MethodImpl(InlineOption)]
            public void MaybeDispose(IDisposable owner) { }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct CancelDelegatePromiseCancel<TCapture> : IDelegateCancel
        {
            public CancelationRegistration cancelationRegistration;

            private readonly TCapture _capturedValue;
            private readonly Promise.CanceledAction<TCapture> _callback;
            private readonly ITreeHandleableCollection _previous;
            volatile private IValueContainer _valueContainer;
            private int _cancelFlag;

            [MethodImpl(InlineOption)]
            public CancelDelegatePromiseCancel(ref TCapture capturedValue, Promise.CanceledAction<TCapture> callback, PromiseRef previous)
            {
                _capturedValue = capturedValue;
                _callback = callback;
                _previous = previous;
                _valueContainer = null;
                cancelationRegistration = default(CancelationRegistration);
                _cancelFlag = 1;
            }

            [MethodImpl(InlineOption)]
            public void InvokeFromToken(IValueContainer valueContainer, IDisposableTreeHandleable owner)
            {
                int oldFlag = Interlocked.Exchange(ref _cancelFlag, 0);
                if (oldFlag == 2)
                {
                    // TryMakeReady was already called without InvokeFromPromise.
                    return;
                }
                if (oldFlag != 3) // If InvokeFromPromise was called before this, just dispose without removing.
                {
                    _previous.Remove(owner);
                }
                owner.Dispose();
            }

            [MethodImpl(InlineOption)]
            public bool TryMakeReady(IValueContainer valueContainer, IDisposable owner)
            {
                if (Interlocked.Exchange(ref _cancelFlag, 2) == 0)
                {
                    // InvokeFromToken was already called.
                    return false;
                }
                // Always make ready, even if state is not canceled. This makes it easier to make this thread-safe.
                valueContainer.Retain();
                _valueContainer = valueContainer;
                return true;
            }

            [MethodImpl(InlineOption)]
            public void InvokeFromPromise(IDisposable owner)
            {
                var tempValueContainer = _valueContainer;
                int oldFlag = Interlocked.Exchange(ref _cancelFlag, 3);
                if (oldFlag == 0)
                {
                    // InvokeFromPromise can only be called after TryMakeReady, so we know that if oldFlag == 0, InvokeFromToken previously saw it == 2 and did nothing.
                    tempValueContainer.Release();
                    owner.Dispose();
                    return;
                }
                if (!cancelationRegistration.TryUnregister())
                {
                    // If we couldn't unregister the cancelation, it means the token was already canceled, and InvokeFromToken maybe hasn't been called yet (or was called after the flag exchange).
                    tempValueContainer.Release();
                    return;
                }
                var tempCallback = _callback;
                var tempCapturedValue = _capturedValue;
                owner.Dispose();
                try
                {
                    if (tempValueContainer.GetState() == Promise.State.Canceled) // We have to check canceled state before invoking the callback instead of in TryMakeReady for thread safety reasons.
                    {
                        tempCallback.Invoke(tempCapturedValue, new ReasonContainer(tempValueContainer));
                    }
                }
                finally
                {
                    tempValueContainer.Release();
                }
            }

            [MethodImpl(InlineOption)]
            public void MaybeDispose(IDisposable owner) { }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct CancelDelegateToken : IDelegateCancel
        {
            private readonly Promise.CanceledAction _callback;

            [MethodImpl(InlineOption)]
            public CancelDelegateToken(Promise.CanceledAction callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public void InvokeFromToken(IValueContainer valueContainer, IDisposableTreeHandleable owner)
            {
                var tempCallback = _callback;
                owner.Dispose();
                tempCallback.Invoke(new ReasonContainer(valueContainer));
            }

            [MethodImpl(InlineOption)]
            public void MaybeDispose(IDisposable owner)
            {
                owner.Dispose();
            }

            public bool TryMakeReady(IValueContainer valueContainer, IDisposable owner) { throw new System.InvalidOperationException(); }
            public void InvokeFromPromise(IDisposable owner) { throw new System.InvalidOperationException(); }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct CancelDelegateToken<TCapture> : IDelegateCancel
        {
            private readonly TCapture _capturedValue;
            private readonly Promise.CanceledAction<TCapture> _callback;

            [MethodImpl(InlineOption)]
            public CancelDelegateToken(ref TCapture capturedValue, Promise.CanceledAction<TCapture> callback)
            {
                _capturedValue = capturedValue;
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public void InvokeFromToken(IValueContainer valueContainer, IDisposableTreeHandleable owner)
            {
                var tempCallback = _callback;
                var tempCapturedValue = _capturedValue;
                owner.Dispose();
                tempCallback.Invoke(tempCapturedValue, new ReasonContainer(valueContainer));
            }

            [MethodImpl(InlineOption)]
            public void MaybeDispose(IDisposable owner)
            {
                owner.Dispose();
            }

            public bool TryMakeReady(IValueContainer valueContainer, IDisposable owner) { throw new System.InvalidOperationException(); }
            public void InvokeFromPromise(IDisposable owner) { throw new System.InvalidOperationException(); }
        }
    }
}