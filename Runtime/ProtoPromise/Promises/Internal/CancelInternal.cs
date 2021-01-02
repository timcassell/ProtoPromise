#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef : ICancelDelegate
        {
            protected virtual void CancelCallbacks() { }

            void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
            {
                CancelCallbacks();
                CancelProgressListeners();

                object currentValue = _valueOrPrevious;
                // TODO: don't set _valueOrPrevious, send it into ExecuteCancelation method.
                _valueOrPrevious = valueContainer;
                valueContainer.Retain();

                // This might be called synchronously when it's registered to an already canceled token. In that case, _valueOrPrevious will be null.
                if (currentValue == null)
                {
                    return;
                }

                // Otherwise, the promise is either waiting for its previous, or it's in the handle queue.
                if (currentValue is Promise)
                {
                    // Remove this from previous' next branches.
                    ((ITreeHandleableCollection) currentValue).Remove(this);
                    // TODO: don't add to handle queue, call a separate ExecuteCancelation method.
                    AddToHandleQueueBack(this);
                }
                else
                {
                    // Rejection maybe wasn't caught.
                    ((IValueContainer) currentValue).ReleaseAndMaybeAddToUnhandledStack();
                    // Don't add to handle queue since it's already in it.
                }
            }

            void ICancelDelegate.Dispose() { }
        }
    }
}