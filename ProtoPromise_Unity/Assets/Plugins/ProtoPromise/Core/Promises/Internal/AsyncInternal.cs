#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CS0436 // Type conflicts with imported type

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using Proto.Promises.Async.CompilerServices;

namespace System.Runtime.CompilerServices
{
    // I would #ifdef this entire file, but Unity complains about namespaces changed with define symbols.
#if CSHARP_7_3_OR_NEWER // Custom async builders only available after C# 7.2.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false, AllowMultiple = false)]
    internal sealed class AsyncMethodBuilderAttribute : Attribute
    {
        public Type BuilderType { get; }

        public AsyncMethodBuilderAttribute(Type builderType)
        {
            BuilderType = builderType;
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER // Custom async builders only available after C# 7.2.
    [AsyncMethodBuilder(typeof(PromiseMethodBuilder))]
    partial struct Promise { }

    [AsyncMethodBuilder(typeof(PromiseMethodBuilder<>))]
    partial struct Promise<T> { }
#endif // CSHARP_7_3_OR_NEWER

    namespace Async.CompilerServices
    {
#if CSHARP_7_3_OR_NEWER // Custom async builders only available after C# 7.2.
        /// <summary>
        /// This type and its members are intended for use by the compiler.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        public struct PromiseMethodBuilder
        {
            private Internal.PromiseMethodBuilderInternal<Internal.VoidResult> _builder;

            [MethodImpl(Internal.InlineOption)]
            private PromiseMethodBuilder(Internal.PromiseMethodBuilderInternal<Internal.VoidResult> builder)
            {
                _builder = builder;
            }

            public Promise Task
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _builder.Task; }
            }

            [MethodImpl(Internal.InlineOption)]
            public static PromiseMethodBuilder Create()
            {
                return new PromiseMethodBuilder(Internal.PromiseMethodBuilderInternal<Internal.VoidResult>.Create());
            }

            [MethodImpl(Internal.InlineOption)]
            public void SetException(Exception exception)
            {
                _builder.SetException(exception);
            }

            [MethodImpl(Internal.InlineOption)]
            public void SetResult()
            {
                _builder.SetResult(new Internal.VoidResult());
            }

            [MethodImpl(Internal.InlineOption)]
            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                _builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
            }

            [SecuritySafeCritical]
            [MethodImpl(Internal.InlineOption)]
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                _builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
            }

            [MethodImpl(Internal.InlineOption)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                _builder.Start(ref stateMachine);
            }

            [MethodImpl(Internal.InlineOption)]
            public void SetStateMachine(IAsyncStateMachine stateMachine)
            {
                _builder.SetStateMachine(stateMachine);
            }
        }

        /// <summary>
        /// This type and its members are intended for use by the compiler.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        public struct PromiseMethodBuilder<T>
        {
            private Internal.PromiseMethodBuilderInternal<T> _builder;

            [MethodImpl(Internal.InlineOption)]
            private PromiseMethodBuilder(Internal.PromiseMethodBuilderInternal<T> builder)
            {
                _builder = builder;
            }

            public Promise<T> Task
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _builder.Task; }
            }

            [MethodImpl(Internal.InlineOption)]
            public static PromiseMethodBuilder<T> Create()
            {
                return new PromiseMethodBuilder<T>(Internal.PromiseMethodBuilderInternal<T>.Create());
            }

            [MethodImpl(Internal.InlineOption)]
            public void SetException(Exception exception)
            {
                _builder.SetException(exception);
            }

            [MethodImpl(Internal.InlineOption)]
            public void SetResult(T result)
            {
                _builder.SetResult(result);
            }

            [MethodImpl(Internal.InlineOption)]
            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                _builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
            }

            [SecuritySafeCritical]
            [MethodImpl(Internal.InlineOption)]
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                _builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
            }

            [MethodImpl(Internal.InlineOption)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                _builder.Start(ref stateMachine);
            }

            [MethodImpl(Internal.InlineOption)]
            public void SetStateMachine(IAsyncStateMachine stateMachine)
            {
                _builder.SetStateMachine(stateMachine);
            }
        }
#endif // CSHARP_7_3_OR_NEWER
    } // namespace Async.CompilerServices
} // namespace Proto.Promises

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER // Custom async builders only available after C# 7.2.
    partial class Internal
    {
#if PROMISE_DEBUG || ENABLE_IL2CPP
        // Fix for IL2CPP compile bug. https://issuetracker.unity3d.com/issues/il2cpp-incorrect-results-when-calling-a-method-from-outside-class-in-a-struct
        // Also use this in DEBUG mode for causality traces.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        public struct PromiseMethodBuilderInternal<T>
        {
            private AsyncPromiseRef _promise;

            [MethodImpl(InlineOption)]
            private PromiseMethodBuilderInternal(AsyncPromiseRef promise)
            {
                _promise = promise;
            }

            public Promise<T> Task
            {
                [MethodImpl(InlineOption)]
                get { return new Promise<T>(_promise, _promise.Id, 0); }
            }

            [MethodImpl(InlineOption)]
            public static PromiseMethodBuilderInternal<T> Create()
            {
                return new PromiseMethodBuilderInternal<T>(AsyncPromiseRef.GetOrCreate());
            }

            public void SetException(Exception exception)
            {
                _promise.SetException(exception);
            }

            [MethodImpl(InlineOption)]
            public void SetResult(T result)
            {
                _promise.SetResult(result);
            }

            [MethodImpl(InlineOption)]
            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                // TODO: check for circular awaits (promise waiting on itself)
                awaiter.OnCompleted(_promise.GetContinuation(ref stateMachine));
            }

            [SecuritySafeCritical]
            [MethodImpl(InlineOption)]
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                awaiter.UnsafeOnCompleted(_promise.GetContinuation(ref stateMachine));
            }

            [MethodImpl(InlineOption)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                // TODO: to support ExecutionContext for AsyncLocal
                //new AsyncTaskMethodBuilder().Start(ref stateMachine);
                stateMachine.MoveNext();
            }

            [MethodImpl(InlineOption)]
            public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        }

#else // PROMISE_DEBUG || ENABLE_IL2CPP

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        [StructLayout(LayoutKind.Auto)]
        internal struct PromiseMethodBuilderInternal<T>
        {
            private AsyncPromiseRef _ref;
            private short _id;
            private T _result;

            public Promise<T> Task
            {
                [MethodImpl(InlineOption)]
                get { return new Promise<T>(_ref, _id, 0, _result); }
            }

            [MethodImpl(InlineOption)]
            public static PromiseMethodBuilderInternal<T> Create()
            {
                return new PromiseMethodBuilderInternal<T>();
            }

            public void SetException(Exception exception)
            {
                if (_ref is null)
                {
                    _ref = AsyncPromiseRef.GetOrCreate();
                    _id = _ref.Id;
                }
                _ref.SetException(exception);
            }

            public void SetResult(in T result)
            {
                if (_ref is null)
                {
                    _result = result;
                    _id = ValidIdFromApi;
                }
                else
                {
                    _ref.SetResult(result);
                }
            }

            [MethodImpl(InlineOption)]
            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                awaiter.OnCompleted(GetContinuation(ref stateMachine));
            }

            [SecuritySafeCritical]
            [MethodImpl(InlineOption)]
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                awaiter.UnsafeOnCompleted(GetContinuation(ref stateMachine));
            }

            [MethodImpl(InlineOption)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                stateMachine.MoveNext();
            }

            [MethodImpl(InlineOption)]
            public void SetStateMachine(IAsyncStateMachine stateMachine) { }

            [MethodImpl(InlineOption)]
            private Action GetContinuation<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                if (_ref is null)
                {
                    AsyncPromiseRef.SetStateMachine(ref stateMachine, ref _ref);
                    _id = _ref.Id;
                }
                return _ref.continuation;
            }
        }
#endif // DEBUG or IL2CPP

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal partial class AsyncPromiseRef : PromiseRef.AsyncPromiseBase
        {
            [MethodImpl(InlineOption)]
            public static AsyncPromiseRef GetOrCreate()
            {
                var promise = ObjectPool<HandleablePromiseBase>.TryTake<AsyncPromiseRef>()
                    ?? new AsyncPromiseRef();
                promise.Reset();
                return promise;
            }

            [MethodImpl(InlineOption)]
            internal void SetResult<T>(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                T result)
            {
                ResolveDirect(result);
            }

            internal void SetException(Exception exception)
            {
                if (exception is OperationCanceledException)
                {
                    CancelDirect();
                }
                else
                {
                    if (exception is RethrowException)
                    {
#if PROMISE_DEBUG
                        string stacktrace = FormatStackTrace(new StackTrace[1] { new StackTrace(exception, true) });
#else
                        string stacktrace = new StackTrace(exception, true).ToString();
#endif
                        exception = new InvalidOperationException("RethrowException is only valid in promise onRejected callbacks.", stacktrace);
                    }
                    RejectDirect(exception, int.MinValue);
                }
            }
        }

#if PROMISE_DEBUG || ENABLE_IL2CPP
        // Fix for IL2CPP compile bug. https://issuetracker.unity3d.com/issues/il2cpp-incorrect-results-when-calling-a-method-from-outside-class-in-a-struct
        // Also use this in DEBUG mode for causality traces.
        sealed partial class AsyncPromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode]
#endif
            private abstract class PromiseMethodContinuer : IDisposable
            {
                public abstract Action Continuation { get; }
#if PROMISE_DEBUG
                protected ITraceable _owner;
#endif

                private PromiseMethodContinuer() { }

                public abstract void Dispose();

                [MethodImpl(InlineOption)]
                public static PromiseMethodContinuer GetOrCreate<TStateMachine>(ref TStateMachine stateMachine, ITraceable owner) where TStateMachine : IAsyncStateMachine
                {
                    var continuer = Continuer<TStateMachine>.GetOrCreate(ref stateMachine);
#if PROMISE_DEBUG
                    continuer._owner = owner;
#endif
                    return continuer;
                }

                /// <summary>
                /// Generic class to reference the state machine without boxing it.
                /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode]
#endif
                private sealed class Continuer<TStateMachine> : PromiseMethodContinuer, ILinked<Continuer<TStateMachine>> where TStateMachine : IAsyncStateMachine
                {
                    Continuer<TStateMachine> ILinked<Continuer<TStateMachine>>.Next { get; set; }
                    public override Action Continuation { get { return _continuation; } }
                    private readonly Action _continuation;
                    private TStateMachine _stateMachine;

                    private Continuer()
                    {
                        _continuation = Continue;
                    }

                    [MethodImpl(InlineOption)]
                    public static Continuer<TStateMachine> GetOrCreate(ref TStateMachine stateMachine)
                    {
                        var continuer = ObjectPool<Continuer<TStateMachine>>.TryTake<Continuer<TStateMachine>>()
                            ?? new Continuer<TStateMachine>();
                        continuer._stateMachine = stateMachine;
                        return continuer;
                    }

                    public override void Dispose()
                    {
#if PROMISE_DEBUG
                        _owner = null;
#endif
                        _stateMachine = default;
                        ObjectPool<Continuer<TStateMachine>>.MaybeRepool(this);
                    }

                    private void Continue()
                    {
#if PROMISE_DEBUG
                        SetCurrentInvoker(_owner);
                        try
                        {
                            _stateMachine.MoveNext();
                        }
                        finally
                        {
                            ClearCurrentInvoker();
                        }
#else
                        _stateMachine.MoveNext();
#endif
                    }
                }
            }

            private PromiseMethodContinuer _continuer;

            [MethodImpl(InlineOption)]
            public Action GetContinuation<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
            {
                if (_continuer is null)
                {
                    _continuer = PromiseMethodContinuer.GetOrCreate(ref stateMachine, this);
                }
                return _continuer.Continuation;
            }

            protected override void Dispose()
            {
                base.Dispose();
                if (_continuer != null)
                {
                    _continuer.Dispose();
                    _continuer = null;
                }
                ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
            }
        } // class AsyncPromiseRef

#else // PROMISE_DEBUG || ENABLE_IL2CPP

        partial class AsyncPromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode]
#endif
            private sealed class AsyncPromiseRefMachine<TStateMachine> : AsyncPromiseRef where TStateMachine : IAsyncStateMachine
            {
                // Using a promiseref object as its own continuer saves 16 bytes of object overhead (x64).
                private TStateMachine _stateMachine;

                private AsyncPromiseRefMachine() { }

                [MethodImpl(InlineOption)]
                public static void SetStateMachine(ref TStateMachine stateMachine, ref AsyncPromiseRef _ref)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<AsyncPromiseRefMachine<TStateMachine>>()
                        ?? new AsyncPromiseRefMachine<TStateMachine>();
                    promise.Reset();
                    // ORDER VERY IMPORTANT, Task must be set before copying stateMachine.
                    _ref = promise;
                    promise._stateMachine = stateMachine;
                }

                protected override void Dispose()
                {
                    SuperDispose();
                    _stateMachine = default;
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void ContinueMethod()
                {
                    _stateMachine.MoveNext();
                }
            }

            // Cache the delegate to prevent new allocations.
            internal readonly Action continuation;

            protected AsyncPromiseRef()
            {
                continuation = ContinueMethod;
            }

            protected virtual void ContinueMethod() { }

            internal static void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine, ref AsyncPromiseRef _ref) where TStateMachine : IAsyncStateMachine
            {
                AsyncPromiseRefMachine<TStateMachine>.SetStateMachine(ref stateMachine, ref _ref);
            }

            protected override void Dispose()
            {
                base.Dispose();
                ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
            }

            // Used for child to call base dispose without repooling for both types.
            // This is necessary because C# doesn't allow `base.base.Dispose()`.
            [MethodImpl(InlineOption)]
            protected void SuperDispose()
            {
                base.Dispose();
            }
        }
#endif // DEBUG or IL2CPP

    } // class Internal
#endif // CSHARP_7_3_OR_NEWER
    } // namespace Proto.Promises