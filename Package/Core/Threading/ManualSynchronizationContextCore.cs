#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

#pragma warning disable IDE0016 // Use 'throw' expression
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0290 // Use primary constructor

namespace Proto.Promises.Threading
{
    /// <summary>
    /// The core implementation of a <see cref="SynchronizationContext"/>, used to schedule callbacks to a certain context.
    /// You may use this type as a base to build your own custom <see cref="SynchronizationContext"/>.
    /// </summary>
    /// <remarks>
    /// This type is a mutable struct, and so should not be used as a readonly field.
    /// <para/>
    /// <see cref="Post(SendOrPostCallback, object)"/> and <see cref="Send(SendOrPostCallback, object)"/> methods are thread-safe, as long as the field of this type is not directly overwritten.
    /// <see cref="ExecuteExhaustive"/> and <see cref="ExecuteNonExhaustive"/> may not be called recursively or concurrently on separate threads.
    /// <para/>
    /// This type does not check the <see cref="Thread.CurrentThread"/> or <see cref="SynchronizationContext.Current"/>.
    /// If your context requires it (i.e. executing callbacks synchronously), you must implement that yourself.
    /// </remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public struct ManualSynchronizationContextCore
    {
        // Send callbacks are implemented as a linked-list, because we have to make sure the ExceptionDispatchInfo is still valid by the time the original thread reads it.
        // Post callbacks are implemented as a contiguous list for fast add and invoke, with minimal memory.
        // Post is also expected to be used more frequently than Send, so we want it to be as efficient as possible, rather than sharing an implementation.

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
        // (see https://stackoverflow.com/questions/24742325/why-does-struct-alignment-depend-on-whether-a-field-type-is-primitive-or-user-de).
        private struct SmallFields
        {
            internal Internal.SpinLocker _executeLock; // Used to prevent Execute(Non)Exhaustive from being called recursively or concurrently.
            internal Internal.SpinLocker _locker;
            internal int _postCount;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private readonly struct PostCallback
        {
            internal readonly SendOrPostCallback _callback;
            internal readonly object _state;

            [MethodImpl(Internal.InlineOption)]
            internal PostCallback(SendOrPostCallback callback, object state)
            {
                _callback = callback;
                _state = state;
            }

            [MethodImpl(Internal.InlineOption)]
            internal void Invoke()
                => _callback.Invoke(_state);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class SendCallback : Internal.HandleablePromiseBase, Internal.ILinked<SendCallback>
        {
            SendCallback Internal.ILinked<SendCallback>.Next
            {
                get => _next.UnsafeAs<SendCallback>();
                set => _next = value;
            }

            private PostCallback _callback;
            private ExceptionDispatchInfo _capturedInfo;
            private readonly AutoResetEvent _callbackCompletedEvent = new AutoResetEvent(false);

            private SendCallback() { }

            [MethodImpl(Internal.InlineOption)]
            private static SendCallback GetOrCreate()
            {
                var obj = Internal.ObjectPool.TryTakeOrInvalid<SendCallback>();
                return obj == Internal.PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new SendCallback()
                    : obj.UnsafeAs<SendCallback>();
            }

            internal static SendCallback GetOrCreate(SendOrPostCallback callback, object state)
            {
                var sc = GetOrCreate();
                sc._next = null;
                sc._callback = new PostCallback(callback, state);
                return sc;
            }

            internal void Invoke()
            {
                try
                {
                    _callback.Invoke();
                }
                catch (Exception e)
                {
                    _capturedInfo = ExceptionDispatchInfo.Capture(e);
                }
                finally
                {
                    _callbackCompletedEvent.Set();
                }
            }

            internal void Send(ref ManualSynchronizationContextCore parent)
            {
                parent._smallFields._locker.Enter();
                parent._sendQueue.Enqueue(this);
                parent._smallFields._locker.Exit();

                _callbackCompletedEvent.WaitOne();
                var capturedInfo = _capturedInfo;

                Dispose(); // Dispose after invoke.
                capturedInfo?.Throw();
            }

            private void Dispose()
            {
                _callback = default;
                _capturedInfo = default;
                Internal.ObjectPool.MaybeRepool(this);
            }
        }

        // We swap these when Execute() is called.
        private PostCallback[] _postQueue;
        private PostCallback[] _executing;
        // These must not be readonly.
        private Internal.ValueLinkedQueue<SendCallback> _sendQueue;
        private SmallFields _smallFields;

        /// <summary>
        /// Create a new <see cref="ManualSynchronizationContextCore"/> with an initial capacity.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the queue of <see cref="Post"/> callbacks. The capacity will grow if more callbacks are added.</param>
        public ManualSynchronizationContextCore(int initialCapacity)
        {
            if (initialCapacity < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(initialCapacity), $"{nameof(initialCapacity)} must be greater than or equal to zero.");
            }
            else if (initialCapacity == 0)
            {
#pragma warning disable IDE0301 // Simplify collection initialization
                _postQueue = Array.Empty<PostCallback>();
                _executing = Array.Empty<PostCallback>();
#pragma warning restore IDE0301 // Simplify collection initialization
            }
            else
            {
                _postQueue = new PostCallback[initialCapacity];
                _executing = new PostCallback[initialCapacity];
            }

            _sendQueue = new Internal.ValueLinkedQueue<SendCallback>();
            _smallFields = default;
        }

        /// <summary>
        /// Schedule the delegate to execute on this context with the given state asynchronously, without waiting for it to complete.
        /// </summary>
        /// <remarks>
        /// This method is thread-safe.
        /// </remarks>
        public void Post(SendOrPostCallback d, object state)
        {
            if (d == null)
            {
                throw new System.ArgumentNullException(nameof(d), "SendOrPostCallback may not be null.");
            }

            _smallFields._locker.Enter();
            int count = _smallFields._postCount;
            if (count >= _postQueue.Length)
            {
                Array.Resize(ref _postQueue, checked((count * 2) + 1));
            }
            _postQueue[count] = new PostCallback(d, state);
            _smallFields._postCount = count + 1;
            _smallFields._locker.Exit();
        }

        /// <summary>
        /// Schedule the delegate to execute on this context with the given state, and wait for it to complete.
        /// </summary>
        /// <remarks>
        /// This method does not check the <see cref="Thread.CurrentThread"/> or <see cref="SynchronizationContext.Current"/>.
        /// Your <see cref="SynchronizationContext"/> should check for the appropriate current thread or context in order to invoke the callback synchronously when required.
        /// <para/>
        /// This method is thread-safe.
        /// </remarks>
        public void Send(SendOrPostCallback d, object state)
        {
            if (d == null)
            {
                throw new System.ArgumentNullException(nameof(d), "SendOrPostCallback may not be null.");
            }

            SendCallback.GetOrCreate(d, state).Send(ref this);
        }

        /// <summary>
        /// Invoke all callbacks that were scheduled to run on this context,
        /// and all callbacks that are scheduled to run on this context while this is executing,
        /// exhaustively, until no more callbacks remain.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">This is called recursively or concurrently.</exception>
        /// <exception cref="AggregateException">One or more callbacks threw an exception.</exception>
        public void ExecuteExhaustive()
        {
            if (!_smallFields._executeLock.TryEnter())
            {
                throw new System.InvalidOperationException($"{nameof(ManualSynchronizationContextCore)}.Execute(Non)Exhaustive was called recursively or concurrently. This is not supported.");
            }

            try
            {
                // Catch all exceptions and continue executing callbacks until all are exhausted, then if there are any, throw all exceptions wrapped in AggregateException.
                List<Exception> exceptions = null;

                while (true)
                {
                    var (sendQueue, postQueue, postCount) = GetSendsAndPosts();
                    if (sendQueue.IsEmpty & postCount == 0)
                    {
                        break;
                    }

                    ExecuteCore(sendQueue, postQueue, postCount, ref exceptions);
                }

                if (exceptions != null)
                {
                    throw new AggregateException(exceptions);
                }
            }
            finally
            {
                _smallFields._executeLock.Exit();
            }
        }

        /// <summary>
        /// Invoke all callbacks that were scheduled to run on this context.
        /// Any callbacks that are scheduled to run on this context while this is executing will not be invoked until the next time this is called.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">This is called recursively or concurrently.</exception>
        /// <exception cref="AggregateException">One or more callbacks threw an exception.</exception>
        public void ExecuteNonExhaustive()
        {
            if (!_smallFields._executeLock.TryEnter())
            {
                throw new System.InvalidOperationException($"{nameof(ManualSynchronizationContextCore)}.Execute(Non)Exhaustive was called recursively or concurrently. This is not supported.");
            }

            try
            {
                // Catch all exceptions and continue executing callbacks until all are exhausted, then if there are any, throw all exceptions wrapped in AggregateException.
                List<Exception> exceptions = null;

                var (sendQueue, postQueue, postCount) = GetSendsAndPosts();
                ExecuteCore(sendQueue, postQueue, postCount, ref exceptions);

                if (exceptions != null)
                {
                    throw new AggregateException(exceptions);
                }
            }
            finally
            {
                _smallFields._executeLock.Exit();
            }
        }

        private (Internal.ValueLinkedQueue<SendCallback> sendQueue, PostCallback[] postQueue, int postCount) GetSendsAndPosts()
        {
            _smallFields._locker.Enter();
            var sendQueue = _sendQueue.TakeElements();
            var postQueue = _postQueue;
            int postCount = _smallFields._postCount;
            _smallFields._postCount = 0;
            // Swap the executing and pending arrays.
            _postQueue = _executing;
            _executing = postQueue;
            _smallFields._locker.Exit();

            return (sendQueue, postQueue, postCount);
        }

        private static void ExecuteCore(Internal.ValueLinkedQueue<SendCallback> sendQueue, PostCallback[] postQueue, int postCount, ref List<Exception> exceptions)
        {
            // Execute Send callbacks first so that their waiting threads may continue sooner.
            // We don't need to catch exceptions here because it's already handled in the SendCallback.Invoke().
            if (sendQueue.IsNotEmpty)
            {
                var stack = sendQueue.MoveElementsToStackUnsafe();
                do
                {
                    stack.Pop().Invoke();
                } while (stack.IsNotEmpty);
            }

            for (int i = 0; i < postCount; ++i)
            {
                ref var callback = ref postQueue[i];
                try
                {
                    callback.Invoke();
                }
                catch (Exception e)
                {
                    Internal.RecordException(e, ref exceptions);
                }
                callback = default;
            }
        }
    }
}