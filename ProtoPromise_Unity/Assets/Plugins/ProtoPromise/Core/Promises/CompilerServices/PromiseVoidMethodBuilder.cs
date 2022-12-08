#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#pragma warning disable IDE0044 // Add readonly modifier

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises.Async.CompilerServices
{
    /// <summary>
    /// Provides a builder for asynchronous methods that return void, using the Promise system instead of the Task system.
    /// This type is intended to be used to override the default builder used on `async void` methods.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public struct PromiseVoidMethodBuilder
    {
        // This must not be readonly.
        private PromiseMethodBuilder _target;

        [MethodImpl(Internal.InlineOption)]
        private PromiseVoidMethodBuilder(PromiseMethodBuilder target)
        {
            _target = target;
        }

        /// <summary>Initializes a new <see cref="PromiseVoidMethodBuilder"/>.</summary>
        /// <returns>The initialized <see cref="PromiseVoidMethodBuilder"/>.</returns>
        [MethodImpl(Internal.InlineOption)]
        public static PromiseVoidMethodBuilder Create()
        {
            return new PromiseVoidMethodBuilder(PromiseMethodBuilder.Create());
        }

        /// <summary>Initiates the builder's execution with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [MethodImpl(Internal.InlineOption)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            _target.Start(ref stateMachine);
            _target.Task.Forget();
        }

        /// <summary>Does nothing.</summary>
        /// <param name="stateMachine">The heap-allocated state machine object.</param>
        [MethodImpl(Internal.InlineOption)]
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            _target.SetStateMachine(stateMachine);
        }

        /// <summary>
        /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
        /// </summary>
        /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        [MethodImpl(Internal.InlineOption)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            _target.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        /// <summary>
        /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
        /// </summary>
        /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        [MethodImpl(Internal.InlineOption)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            _target.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }

        /// <summary>
        /// Completes the method builder successfully.
        /// </summary>
        public void SetResult()
        {
            _target.SetResult();
        }

        /// <summary>
        /// Faults the method builder with an exception.
        /// </summary>
        /// <param name="exception">The exception that is the cause of this fault.</param>
        public void SetException(Exception exception)
        {
            _target.SetException(exception);
        }
    }
}