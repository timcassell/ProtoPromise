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
    partial class Promise
    {
        private void MakeCanceledFromToken()
        {
            // This might be called synchronously when it's registered to an already canceled token. In that case, _valueOrPrevious will be null.
            if (_valueOrPrevious == null)
            {
                _valueOrPrevious = Internal.ResolveContainerVoid.GetOrCreate();
                return;
            }

            // Otherwise, the promise is either waiting for its previous, or it's in the handle queue.
#if CSHARP_7_OR_LATER
            if (_valueOrPrevious is Promise previous)
#else
            Promise previous = _valueOrPrevious as Promise;
            if (previous != null)
#endif
            {
                // Remove this from previous' next branches.
                previous._nextBranches.Remove(this);
                _valueOrPrevious = Internal.ResolveContainerVoid.GetOrCreate();
                Internal.AddToHandleQueueBack(this);
            }
            else
            {
                // Rejection maybe wasn't caught.
                ((Internal.IValueContainer) _valueOrPrevious).ReleaseAndMaybeAddToUnhandledStack();
                _valueOrPrevious = Internal.ResolveContainerVoid.GetOrCreate();
                // Don't add to handle queue since it's already in it.
            }
        }

        private void CancelDirect()
        {
            _state = State.Canceled;
            var cancelContainer = Internal.CancelContainerVoid.GetOrCreate();
            _valueOrPrevious = cancelContainer;
            AddBranchesToHandleQueueBack(cancelContainer);
            CancelProgressListeners();
            Internal.AddToHandleQueueFront(this);
        }

        private void CancelDirect<TCancel>(ref TCancel reason)
        {
            _state = State.Canceled;
            var cancelContainer = Internal.CreateCancelContainer(ref reason);
            cancelContainer.Retain();
            _valueOrPrevious = cancelContainer;
            AddBranchesToHandleQueueBack(cancelContainer);
            CancelProgressListeners();
            Internal.AddToHandleQueueFront(this);
        }

        protected static CancelationRegistration RegisterForCancelation(Promise promise, CancelationToken cancelationToken)
        {
            return cancelationToken.Register(promise, (p, _) => p.MakeCanceledFromToken());
        }

        private static void ReleaseAndMaybeThrow(CancelationToken cancelationToken)
        {
            try
            {
                cancelationToken.ThrowIfCancelationRequested();
            }
            finally
            {
                if (cancelationToken.CanBeCanceled)
                {
                    cancelationToken.Release();
                }
            }
        }
    }
}