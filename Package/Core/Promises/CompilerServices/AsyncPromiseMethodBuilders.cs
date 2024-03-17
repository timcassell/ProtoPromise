#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

// Fix for IL2CPP compile bug. https://issuetracker.unity3d.com/issues/il2cpp-incorrect-results-when-calling-a-method-from-outside-class-in-a-struct
// Unity fixed in 2020.3.20f1 and 2021.1.24f1, but it's simpler to just check for 2021.2 or newer.
// Don't use optimized mode in DEBUG mode for causality traces.
#if (ENABLE_IL2CPP && !UNITY_2021_2_OR_NEWER) || PROMISE_DEBUG
#undef OPTIMIZED_ASYNC_MODE
#else
#define OPTIMIZED_ASYNC_MODE
#endif

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0074 // Use compound assignment
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0251 // Make member 'readonly'
// Other async libraries could be doing the same AsyncMethodBuilderAttribute trick, but left theirs public, so we suppress the warning just in case.
#pragma warning disable CS0436 // Type conflicts with imported type

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.CompilerServices
{
#if !(NETCOREAPP || NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER)
    // This attribute is required for the C# compiler to compile custom async methods. It is only included publicly in .Net Standard 2.1 and .Net Core.
    // We don't need to make it public, as the C# compiler can work with it being internal.

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false, AllowMultiple = false)]
    internal sealed class AsyncMethodBuilderAttribute : Attribute
    {
        public Type BuilderType { get; private set; }

        public AsyncMethodBuilderAttribute(Type builderType)
        {
            BuilderType = builderType;
        }
    }
#endif
}

namespace Proto.Promises
{
    [AsyncMethodBuilder(typeof(CompilerServices.AsyncPromiseMethodBuilder))]
    partial struct Promise { }

    [AsyncMethodBuilder(typeof(CompilerServices.AsyncPromiseMethodBuilder<>))]
    partial struct Promise<T> { }

    namespace CompilerServices
    {
        /// <summary>
        /// Provides a builder for asynchronous methods that return <see cref="Promise"/>.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public partial struct AsyncPromiseMethodBuilder
        {
            /// <summary>Gets the <see cref="Promise"/> for this builder.</summary>
            /// <returns>The <see cref="Promise"/> representing the builder's asynchronous operation.</returns>
            public Promise Task
            {
                [MethodImpl(Internal.InlineOption)]
                get => new Promise(_ref, _id);
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
                => Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref _ref, ref _id);

            /// <summary>
            /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
            /// </summary>
            /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
            /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
            /// <param name="awaiter">The awaiter.</param>
            /// <param name="stateMachine">The state machine.</param>
            [SecuritySafeCritical]
            [MethodImpl(Internal.InlineOption)]
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
                => Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref _ref, ref _id);

            /// <summary>Initiates the builder's execution with the associated state machine.</summary>
            /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
            /// <param name="stateMachine">The state machine instance, passed by reference.</param>
            [MethodImpl(Internal.InlineOption)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                if (Promise.Config.AsyncFlowExecutionContextEnabled)
                {
                    // To support ExecutionContext for AsyncLocal<T>.
                    // We can use AsyncTaskMethodBuilder to run the state machine on the execution context without creating an object. https://github.com/dotnet/runtime/discussions/56202#discussioncomment-1042195
                    new AsyncTaskMethodBuilder().Start(ref stateMachine);
                }
                else
                {
                    stateMachine.MoveNext();
                }
            }

            /// <summary>Does nothing.</summary>
            /// <param name="stateMachine">The heap-allocated state machine object.</param>
            [MethodImpl(Internal.InlineOption)]
            public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        }

        /// <summary>
        /// Provides a builder for asynchronous methods that return <see cref="Promise{T}"/>.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public partial struct AsyncPromiseMethodBuilder<T>
        {
            /// <summary>Gets the <see cref="Promise{T}"/> for this builder.</summary>
            /// <returns>The <see cref="Promise{T}"/> representing the builder's asynchronous operation.</returns>
            public Promise<T> Task
            {
                [MethodImpl(Internal.InlineOption)]
                get => new Promise<T>(_ref, _id, _result);
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
                => Internal.PromiseRefBase.AsyncPromiseRef<T>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref _ref, ref _id);

            /// <summary>
            /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
            /// </summary>
            /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
            /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
            /// <param name="awaiter">The awaiter.</param>
            /// <param name="stateMachine">The state machine.</param>
            [SecuritySafeCritical]
            [MethodImpl(Internal.InlineOption)]
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
                => Internal.PromiseRefBase.AsyncPromiseRef<T>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref _ref, ref _id);

            /// <summary>Initiates the builder's execution with the associated state machine.</summary>
            /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
            /// <param name="stateMachine">The state machine instance, passed by reference.</param>
            [MethodImpl(Internal.InlineOption)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
                => new AsyncPromiseMethodBuilder().Start(ref stateMachine);

            /// <summary>Does nothing.</summary>
            /// <param name="stateMachine">The heap-allocated state machine object.</param>
            [MethodImpl(Internal.InlineOption)]
            public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        }

#if !OPTIMIZED_ASYNC_MODE

        partial struct AsyncPromiseMethodBuilder
        {
            [MethodImpl(Internal.InlineOption)]
            private AsyncPromiseMethodBuilder(Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult> promise)
            {
                _ref = promise;
                _id = promise.Id;
            }

            /// <summary>Initializes a new <see cref="AsyncPromiseMethodBuilder"/>.</summary>
            /// <returns>The initialized <see cref="AsyncPromiseMethodBuilder"/>.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static AsyncPromiseMethodBuilder Create()
                => new AsyncPromiseMethodBuilder(Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult>.GetOrCreate());

            /// <summary>
            /// Completes the <see cref="Promise"/> in the <see cref="Promise.State">Rejected</see> state with the specified exception.
            /// </summary>
            /// <param name="exception">The <see cref="Exception"/> to use to reject the promise.</param>
            public void SetException(Exception exception)
                => _ref.SetException(exception);

            /// <summary>
            /// Completes the <see cref="Promise"/> in the <see cref="Promise.State">Resolved</see> state.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void SetResult()
                => _ref.SetAsyncResultVoid();
        }

        partial struct AsyncPromiseMethodBuilder<T>
        {
            [MethodImpl(Internal.InlineOption)]
            private AsyncPromiseMethodBuilder(Internal.PromiseRefBase.AsyncPromiseRef<T> promise)
            {
                _ref = promise;
                _id = promise.Id;
            }

            /// <summary>Initializes a new <see cref="AsyncPromiseMethodBuilder{T}"/>.</summary>
            /// <returns>The initialized <see cref="AsyncPromiseMethodBuilder{T}"/>.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static AsyncPromiseMethodBuilder<T> Create()
                => new AsyncPromiseMethodBuilder<T>(Internal.PromiseRefBase.AsyncPromiseRef<T>.GetOrCreate());

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Rejected</see> state with the specified exception.
            /// </summary>
            /// <param name="exception">The <see cref="Exception"/> to use to reject the promise.</param>
            public void SetException(Exception exception)
                => _ref.SetException(exception);

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Resolved</see> state with the specified result.
            /// </summary>
            /// <param name="result">The result to use to complete the task.</param>
            [MethodImpl(Internal.InlineOption)]
            public void SetResult(T result)
                => _ref.SetAsyncResult(result);
        }

#else // !OPTIMIZED_ASYNC_MODE

        // This code could be used for DEBUG mode, but IL2CPP requires the non-optimized code even in RELEASE mode, and I don't want to add extra unnecessary null checks there.
        partial struct AsyncPromiseMethodBuilder
        {
            /// <summary>Initializes a new <see cref="AsyncPromiseMethodBuilder"/>.</summary>
            /// <returns>The initialized <see cref="AsyncPromiseMethodBuilder"/>.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static AsyncPromiseMethodBuilder Create()
                => default;

            /// <summary>
            /// Completes the <see cref="Promise"/> in the <see cref="Promise.State">Rejected</see> state with the specified exception.
            /// </summary>
            /// <param name="exception">The <see cref="Exception"/> to use to reject the promise.</param>
            public void SetException(Exception exception)
            {
                if (_ref == null)
                {
                    _ref = Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult>.GetOrCreate();
                    _id = _ref.Id;
                }
                _ref.SetException(exception);
            }

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Resolved</see> state.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void SetResult()
                => _ref?.SetAsyncResultVoid();
        }

        partial struct AsyncPromiseMethodBuilder<T>
        {
            /// <summary>Initializes a new <see cref="AsyncPromiseMethodBuilder{T}"/>.</summary>
            /// <returns>The initialized <see cref="AsyncPromiseMethodBuilder{T}"/>.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static AsyncPromiseMethodBuilder<T> Create()
                => default;

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Rejected</see> state with the specified exception.
            /// </summary>
            /// <param name="exception">The <see cref="Exception"/> to use to reject the promise.</param>
            public void SetException(Exception exception)
            {
                if (_ref == null)
                {
                    _ref = Internal.PromiseRefBase.AsyncPromiseRef<T>.GetOrCreate();
                    _id = _ref.Id;
                }
                _ref.SetException(exception);
            }

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Resolved</see> state with the specified result.
            /// </summary>
            /// <param name="result">The result to use to complete the task.</param>
            [MethodImpl(Internal.InlineOption)]
            public void SetResult(T result)
            {
                if (_ref == null)
                {
                    _result = result;
                }
                else
                {
                    _ref.SetAsyncResult(result);
                }
            }
        }
#endif // !OPTIMIZED_ASYNC_MODE
    } // namespace CompilerServices
} // namespace Proto.Promises