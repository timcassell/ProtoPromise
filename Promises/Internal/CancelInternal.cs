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

#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable IDE0018 // Inline variable declarations
#pragma warning disable IDE0034 // Simplify 'default' expression

namespace Proto.Promises
{
    partial class Promise : Internal.ICancelDelegate
    {
        private void CancelDirect()
        {
            _state = State.Canceled;
            var cancelContainer = Internal.CancelContainerVoid.GetOrCreate();
            _valueOrPrevious = cancelContainer;
            AddBranchesToHandleQueueBack(cancelContainer);
            CancelProgressListeners();
            Internal.AddToHandleQueueFront(this);
        }

        protected void CancelDirect<TCancel>(ref TCancel reason)
        {
            _state = State.Canceled;
            var cancelContainer = Internal.CreateCancelContainer(ref reason);
            cancelContainer.Retain();
            _valueOrPrevious = cancelContainer;
            AddBranchesToHandleQueueBack(cancelContainer);
            CancelProgressListeners();
            Internal.AddToHandleQueueFront(this);
        }

        protected virtual void CancelCallbacks() { }

        void Internal.ICancelDelegate.Invoke(Internal.ICancelValueContainer valueContainer)
        {
            CancelCallbacks();

            object currentValue = _valueOrPrevious;
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
                ((Internal.ITreeHandleableCollection) currentValue).Remove(this);
                Internal.AddToHandleQueueBack(this);
            }
            else
            {
                // Rejection maybe wasn't caught.
                ((Internal.IValueContainer) currentValue).ReleaseAndMaybeAddToUnhandledStack();
                // Don't add to handle queue since it's already in it.
            }
        }

        void Internal.ICancelDelegate.Dispose() { }
    }
}