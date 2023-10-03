#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

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

// Fix for IL2CPP compile bug. https://issuetracker.unity3d.com/issues/il2cpp-incorrect-results-when-calling-a-method-from-outside-class-in-a-struct
// Unity fixed in 2020.3.20f1 and 2021.1.24f1, but it's simpler to just check for 2021.2 or newer.
// Don't use optimized mode in DEBUG mode for causality traces.
#if (ENABLE_IL2CPP && !UNITY_2021_2_OR_NEWER) || PROMISE_DEBUG
#undef OPTIMIZED_ASYNC_MODE
#else
#define OPTIMIZED_ASYNC_MODE
#endif

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0031 // Use null propagation
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0074 // Use compound assignment
// Other async libraries could be doing the same AsyncMethodBuilderAttribute trick, but left theirs public, so we suppress the warning just in case.
#pragma warning disable 0436 // Type conflicts with imported type

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.CompilerServices
{
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP
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

#if NET_LEGACY
    // The C# compiler does not support custom async methods for runtimes older than .Net 4.5,
    // but we include these interfaces to make it so we don't have to conditionally compile out the builders.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface INotifyCompletion
    {
        void OnCompleted(Action continuation);
    }

    public interface ICriticalNotifyCompletion : INotifyCompletion
    {
        void UnsafeOnCompleted(Action continuation);
    }

    public interface IAsyncStateMachine
    {
        void MoveNext();
        void SetStateMachine(IAsyncStateMachine stateMachine);
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#endif // NET_LEGACY
}

namespace Proto.Promises
{
    [AsyncMethodBuilder(typeof(Async.CompilerServices.PromiseMethodBuilder))]
    partial struct Promise { }

    [AsyncMethodBuilder(typeof(Async.CompilerServices.PromiseMethodBuilder<>))]
    partial struct Promise<T> { }

    namespace Async.CompilerServices
    {
        /// <summary>
        /// Provides a builder for asynchronous methods that return <see cref="Promise"/>.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public partial struct PromiseMethodBuilder
        {
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
                Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref _ref);
            }

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
            {
                Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref _ref);
            }

            /// <summary>Initiates the builder's execution with the associated state machine.</summary>
            /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
            /// <param name="stateMachine">The state machine instance, passed by reference.</param>
            [MethodImpl(Internal.InlineOption)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult>.Start(ref stateMachine, ref _ref);
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
        public partial struct PromiseMethodBuilder<T>
        {
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
                Internal.PromiseRefBase.AsyncPromiseRef<T>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref _ref);
            }

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
            {
                Internal.PromiseRefBase.AsyncPromiseRef<T>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref _ref);
            }

            /// <summary>Initiates the builder's execution with the associated state machine.</summary>
            /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
            /// <param name="stateMachine">The state machine instance, passed by reference.</param>
            [MethodImpl(Internal.InlineOption)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                Internal.PromiseRefBase.AsyncPromiseRef<T>.Start(ref stateMachine, ref _ref);
            }

            /// <summary>Does nothing.</summary>
            /// <param name="stateMachine">The heap-allocated state machine object.</param>
            [MethodImpl(Internal.InlineOption)]
            public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        }

#if !OPTIMIZED_ASYNC_MODE

        partial struct PromiseMethodBuilder
        {
            [MethodImpl(Internal.InlineOption)]
            private PromiseMethodBuilder(Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult> promise)
            {
                _ref = promise;
            }

            /// <summary>Gets the <see cref="Promise"/> for this builder.</summary>
            /// <returns>The <see cref="Promise"/> representing the builder's asynchronous operation.</returns>
            public Promise Task
            {
                [MethodImpl(Internal.InlineOption)]
                get { return new Promise(_ref, _ref.Id, 0); }
            }

            /// <summary>Initializes a new <see cref="PromiseMethodBuilder"/>.</summary>
            /// <returns>The initialized <see cref="PromiseMethodBuilder"/>.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static PromiseMethodBuilder Create()
            {
                return new PromiseMethodBuilder(Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult>.GetOrCreate());
            }

            /// <summary>
            /// Completes the <see cref="Promise"/> in the <see cref="Promise.State">Rejected</see> state with the specified exception.
            /// </summary>
            /// <param name="exception">The <see cref="Exception"/> to use to reject the promise.</param>
            public void SetException(Exception exception)
            {
                _ref.SetException(exception);
            }

            /// <summary>
            /// Completes the <see cref="Promise"/> in the <see cref="Promise.State">Resolved</see> state.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void SetResult()
            {
                _ref.SetAsyncResultVoid();
            }
        }

        partial struct PromiseMethodBuilder<T>
        {
            [MethodImpl(Internal.InlineOption)]
            private PromiseMethodBuilder(Internal.PromiseRefBase.AsyncPromiseRef<T> promise)
            {
                _ref = promise;
            }

            /// <summary>Gets the <see cref="Promise{T}"/> for this builder.</summary>
            /// <returns>The <see cref="Promise{T}"/> representing the builder's asynchronous operation.</returns>
            public Promise<T> Task
            {
                [MethodImpl(Internal.InlineOption)]
                get { return new Promise<T>(_ref, _ref.Id, 0); }
            }

            /// <summary>Initializes a new <see cref="PromiseMethodBuilder{T}"/>.</summary>
            /// <returns>The initialized <see cref="PromiseMethodBuilder{T}"/>.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static PromiseMethodBuilder<T> Create()
            {
                return new PromiseMethodBuilder<T>(Internal.PromiseRefBase.AsyncPromiseRef<T>.GetOrCreate());
            }

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Rejected</see> state with the specified exception.
            /// </summary>
            /// <param name="exception">The <see cref="Exception"/> to use to reject the promise.</param>
            public void SetException(Exception exception)
            {
                _ref.SetException(exception);
            }

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Resolved</see> state with the specified result.
            /// </summary>
            /// <param name="result">The result to use to complete the task.</param>
            [MethodImpl(Internal.InlineOption)]
            public void SetResult(T result)
            {
                _ref.SetAsyncResult(result);
            }
        }

#else // !OPTIMIZED_ASYNC_MODE

        // This code could be used for DEBUG mode, but IL2CPP requires the non-optimized code even in RELEASE mode, and I don't want to add extra unnecessary null checks there.
        partial struct PromiseMethodBuilder
        {
            /// <summary>Gets the <see cref="Promise"/> for this builder.</summary>
            /// <returns>The <see cref="Promise"/> representing the builder's asynchronous operation.</returns>
            public Promise Task
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return _ref == null ? Promise.Resolved() : new Promise(_ref, _ref.Id, 0);
                }
            }
            
            /// <summary>Initializes a new <see cref="PromiseMethodBuilder"/>.</summary>
            /// <returns>The initialized <see cref="PromiseMethodBuilder"/>.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static PromiseMethodBuilder Create()
            {
                return default(PromiseMethodBuilder);
            }

            /// <summary>
            /// Completes the <see cref="Promise"/> in the <see cref="Promise.State">Rejected</see> state with the specified exception.
            /// </summary>
            /// <param name="exception">The <see cref="Exception"/> to use to reject the promise.</param>
            public void SetException(Exception exception)
            {
                if (_ref == null)
                {
                    _ref = Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult>.GetOrCreate();
                }
                _ref.SetException(exception);
            }

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Resolved</see> state.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void SetResult()
            {
                if (_ref != null)
                {
                    _ref.SetAsyncResultVoid();
                }
            }
        }

        partial struct PromiseMethodBuilder<T>
        {
            /// <summary>Gets the <see cref="Promise{T}"/> for this builder.</summary>
            /// <returns>The <see cref="Promise{T}"/> representing the builder's asynchronous operation.</returns>
            public Promise<T> Task
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return _ref == null ? new Promise<T>(_result) : new Promise<T>(_ref, _ref.Id, 0);
                }
            }

            /// <summary>Initializes a new <see cref="PromiseMethodBuilder{T}"/>.</summary>
            /// <returns>The initialized <see cref="PromiseMethodBuilder{T}"/>.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static PromiseMethodBuilder<T> Create()
            {
                return default(PromiseMethodBuilder<T>);
            }

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Rejected</see> state with the specified exception.
            /// </summary>
            /// <param name="exception">The <see cref="Exception"/> to use to reject the promise.</param>
            public void SetException(Exception exception)
            {
                if (_ref == null)
                {
                    _ref = Internal.PromiseRefBase.AsyncPromiseRef<T>.GetOrCreate();
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
    } // namespace Async.CompilerServices
} // namespace Proto.Promises