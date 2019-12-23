#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#endif

#pragma warning disable RECS0001 // Class is declared partial but has only one part
#pragma warning disable RECS0096 // Type parameter is never used
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter

using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
        // Calls to this get compiled away when CANCEL is defined.
        static partial void ValidateCancel(int skipFrames);

        static partial void AddToCancelQueueBack(Internal.ITreeHandleable cancelation);
        static partial void AddToCancelQueueFront(ref ValueLinkedQueue<Internal.ITreeHandleable> cancelations);
        static partial void HandleCanceled();
#if PROMISE_CANCEL
        // Cancel promises in a depth-first manner.
        private static ValueLinkedQueue<Internal.ITreeHandleable> _cancelQueue;

        static partial void AddToCancelQueueBack(Internal.ITreeHandleable cancelation)
        {
            _cancelQueue.Enqueue(cancelation);
        }

        static partial void AddToCancelQueueFront(ref ValueLinkedQueue<Internal.ITreeHandleable> cancelations)
        {
            _cancelQueue.PushAndClear(ref cancelations);
        }

        static partial void HandleCanceled()
        {
            while (_cancelQueue.IsNotEmpty)
            {
                _cancelQueue.DequeueRisky().Cancel();
            }
            _cancelQueue.ClearLast();
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
            public abstract partial class PromiseWaitPromise<TPromise> : PoolablePromise<TPromise> where TPromise : PromiseWaitPromise<TPromise>
            {
                protected void WaitFor(Promise other)
                {
                    ValidateReturn(other);
#if PROMISE_CANCEL
                    if (_state == State.Canceled)
                    {
                        ReleaseInternal();
                    }
                    else
#endif
                    {
                        _rejectedOrCanceledValueOrPrevious = other;
#if PROMISE_PROGRESS
                        _secondPrevious = true;
                        if (_progressListeners.IsNotEmpty)
                        {
                            SubscribeProgressToBranchesAndRoots(other, this);
                        }
#endif
                        other.AddWaiter(this);
                    }
                }
            }

            public abstract partial class PromiseWaitPromise<T, TPromise> : PoolablePromise<T, TPromise> where TPromise : PromiseWaitPromise<T, TPromise>
            {
                protected void WaitFor(Promise other)
                {
                    ValidateReturn(other);
#if PROMISE_CANCEL
                    if (_state == State.Canceled)
                    {
                        ReleaseInternal();
                    }
                    else
#endif
                    {
                        _rejectedOrCanceledValueOrPrevious = other;
#if PROMISE_PROGRESS
                        _secondPrevious = true;
                        if (_progressListeners.IsNotEmpty)
                        {
                            SubscribeProgressToBranchesAndRoots(other, this);
                        }
#endif
                        other.AddWaiter(this);
                    }
                }
            }
        }

        private void ResolveDirectIfNotCanceled()
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                ReleaseInternal();
            }
            else
#endif
            {
                _state = State.Resolved;
                AddToHandleQueueBack(this);
            }
        }

        protected void RejectDirectIfNotCanceled(Internal.IValueContainerOrPrevious rejectValue)
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                AddRejectionToUnhandledStack((Internal.UnhandledExceptionInternal) rejectValue);
                ReleaseInternal();
            }
            else
#endif
            {
                RejectDirect(rejectValue);
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
                ResolveInternal();
            }
        }

        protected void RejectInternalIfNotCanceled(Internal.IValueContainerOrPrevious rejectValue)
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                AddRejectionToUnhandledStack((Internal.UnhandledExceptionInternal) rejectValue);
                ReleaseInternal();
            }
            else
#endif
            {
                RejectInternal(rejectValue);
            }
        }

        protected void AddWaiter(Internal.ITreeHandleable waiter)
        {
            RetainInternal();
            if (_state == State.Pending)
            {
                _nextBranches.Enqueue(waiter);
            }
#if PROMISE_CANCEL
            else if (_state == State.Canceled)
            {
                AddToCancelQueueBack(waiter);
            }
#endif
            else
            {
                AddToHandleQueueBack(waiter);
            }
        }

        void Internal.ITreeHandleable.Handle()
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                ReleaseInternal();
            }
            else
#endif
            {
                Handle();
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

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
        protected void ResolveDirectIfNotCanceled(in T value)
#else
        protected void ResolveDirectIfNotCanceled(T value)
#endif
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                ReleaseInternal();
            }
            else
#endif
            {
                _state = State.Resolved;
                ((Promise.Internal.PromiseInternal<T>) this)._value = value;
                AddToHandleQueueBack(this);
            }
        }
    }
}