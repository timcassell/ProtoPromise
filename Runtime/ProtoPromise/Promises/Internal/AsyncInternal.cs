#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#if CSHARP_7_OR_LATER // async not available in old runtime.

#pragma warning disable IDE0060 // Remove unused parameter

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using Proto.Promises.Async.CompilerServices;
using Proto.Utils;

namespace Proto.Promises
{
    [AsyncMethodBuilder(typeof(PromiseMethodBuilder))]
    partial struct Promise { }

    [AsyncMethodBuilder(typeof(PromiseMethodBuilder<>))]
    partial struct Promise<T> { }

    partial class Internal
    {
        /// <summary>
        /// This type and its members are intended for use by the compiler (async Promise functions).
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal partial class AsyncPromiseRef : PromiseRef.AsyncPromiseBase
        {
            private struct Creator : ICreator<AsyncPromiseRef>
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public AsyncPromiseRef Create()
                {
                    return new AsyncPromiseRef();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void SetResult()
            {
                ResolveDirect();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void SetResult<T>(ref T result)
            {
                ResolveDirect(ref result);
            }

            internal void SetException(Exception exception)
            {
                if (exception is OperationCanceledException)
                {
                    CancelDirect(ref exception);
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
                    RejectDirect(ref exception, int.MinValue);
                }
            }
        }

    }
}

namespace System.Runtime.CompilerServices
{
#pragma warning disable CS0436 // Type conflicts with imported type
    internal sealed class AsyncMethodBuilderAttribute : Attribute
    {
        public Type BuilderType { get; }

        public AsyncMethodBuilderAttribute(Type builderType)
        {
            BuilderType = builderType;
        }
    }
#pragma warning restore CS0436 // Type conflicts with imported type
}

#if PROMISE_DEBUG || ENABLE_IL2CPP
// Fix for IL2CPP compile bug. https://issuetracker.unity3d.com/issues/il2cpp-incorrect-results-when-calling-a-method-from-outside-class-in-a-struct
// Also use this in DEBUG mode for cleaner causality traces.
namespace Proto.Promises
{
    partial class Internal
    {
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

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    private struct Creator : ICreator<Continuer<TStateMachine>>
                    {
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        public Continuer<TStateMachine> Create()
                        {
                            return new Continuer<TStateMachine>();
                        }
                    }

                    Continuer<TStateMachine> ILinked<Continuer<TStateMachine>>.Next { get; set; }
                    public override Action Continuation { get { return _continuation; } }
                    private readonly Action _continuation;
                    private TStateMachine _stateMachine;

                    private Continuer()
                    {
                        _continuation = Continue;
                    }

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static Continuer<TStateMachine> GetOrCreate(ref TStateMachine stateMachine)
                    {
                        var continuer = ObjectPool<Continuer<TStateMachine>>.GetOrCreate<Continuer<TStateMachine>, Creator>(new Creator());
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static AsyncPromiseRef GetOrCreate()
            {
                var promise = ObjectPool<ITreeHandleable>.GetOrCreate<AsyncPromiseRef, Creator>(new Creator());
                promise.Reset();
                return promise;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                ObjectPool<ITreeHandleable>.MaybeRepool(this);
            }
        }
    }
}

namespace Proto.Promises.Async.CompilerServices
{
    /// <summary>
    /// This type and its members are intended for use by the compiler.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public struct PromiseMethodBuilder
    {
        private Internal.AsyncPromiseRef _promise;

        public Promise Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Promise(_promise, _promise.Id); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PromiseMethodBuilder Create()
        {
            return new PromiseMethodBuilder()
            {
                _promise = Internal.AsyncPromiseRef.GetOrCreate()
            };
        }

        public void SetException(Exception exception)
        {
            _promise.SetException(exception);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            _promise.SetResult();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            // TODO: check for circular awaits (promise waiting on itself)
            awaiter.OnCompleted(_promise.GetContinuation(ref stateMachine));
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(_promise.GetContinuation(ref stateMachine));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    }

    /// <summary>
    /// This type and its members are intended for use by the compiler.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public struct PromiseMethodBuilder<T>
    {
        private Internal.AsyncPromiseRef _promise;

        public Promise<T> Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Promise<T>(_promise, _promise.Id); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PromiseMethodBuilder<T> Create()
        {
            return new PromiseMethodBuilder<T>()
            {
                _promise = Internal.AsyncPromiseRef.GetOrCreate()
            };
        }

        public void SetException(Exception exception)
        {
            _promise.SetException(exception);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T result)
        {
            _promise.SetResult(ref result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(_promise.GetContinuation(ref stateMachine));
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(_promise.GetContinuation(ref stateMachine));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    }

#else // DEBUG or IL2CPP

namespace Proto.Promises
{
    partial class Internal
    {
        partial class AsyncPromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode]
#endif
            private sealed class AsyncPromiseRefMachine<TStateMachine> : AsyncPromiseRef where TStateMachine : IAsyncStateMachine
            {
                new private struct Creator : ICreator<AsyncPromiseRefMachine<TStateMachine>>
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public AsyncPromiseRefMachine<TStateMachine> Create()
                    {
                        return new AsyncPromiseRefMachine<TStateMachine>();
                    }
                }

                // Using a promiseref object as its own continuer saves 16 bytes of object overhead (x64).
                private TStateMachine _stateMachine;

                private AsyncPromiseRefMachine() { }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void SetStateMachine(ref TStateMachine stateMachine, ref AsyncPromiseRef _ref)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<AsyncPromiseRefMachine<TStateMachine>, Creator>(new Creator());
                    promise.Reset();
                    // ORDER VERY IMPORTANT, Task must be set before copying stateMachine.
                    _ref = promise;
                    promise._stateMachine = stateMachine;
                }

                protected override void Dispose()
                {
                    SuperDispose();
                    _stateMachine = default;
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
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

            // For synchronous exceptions.
            internal static AsyncPromiseRef GetOrCreate()
            {
                var promise = ObjectPool<ITreeHandleable>.GetOrCreate<AsyncPromiseRef, Creator>(new Creator());
                promise.Reset();
                return promise;
            }

            internal static void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine, ref AsyncPromiseRef _ref) where TStateMachine : IAsyncStateMachine
            {
                AsyncPromiseRefMachine<TStateMachine>.SetStateMachine(ref stateMachine, ref _ref);
            }

            protected override void Dispose()
            {
                base.Dispose();
                ObjectPool<ITreeHandleable>.MaybeRepool(this);
            }

            // Used for child to call base dispose without repooling for both types.
            // This is necessary because C# doesn't allow `base.base.Dispose()`.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected void SuperDispose()
            {
                base.Dispose();
            }
        }
    }
}

namespace Proto.Promises.Async.CompilerServices
{
    /// <summary>
    /// This type and its members are intended for use by the compiler.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public struct PromiseMethodBuilder
    {
        private Internal.AsyncPromiseRef _ref;
        private int _id;

        public Promise Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Promise(_ref, _id); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PromiseMethodBuilder Create()
        {
            return new PromiseMethodBuilder();
        }

        public void SetException(Exception exception)
        {
            if (_ref is null)
            {
                _ref = Internal.AsyncPromiseRef.GetOrCreate();
                _id = _ref.Id;
            }
            _ref.SetException(exception);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            if (_ref is null)
            {
                _id = Internal.ValidPromiseIdFromApi;
            }
            else
            {
                _ref.SetResult();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(GetContinuation(ref stateMachine));
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(GetContinuation(ref stateMachine));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Action GetContinuation<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            if (_ref is null)
            {
                Internal.AsyncPromiseRef.SetStateMachine(ref stateMachine, ref _ref);
                _id = _ref.Id;
            }
            return _ref.continuation;
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
        private Internal.AsyncPromiseRef _ref;
        private int _id;
        private T _result;

        public Promise<T> Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Promise<T>(_ref, _id, ref _result); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PromiseMethodBuilder<T> Create()
        {
            return new PromiseMethodBuilder<T>();
        }

        public void SetException(Exception exception)
        {
            if (_ref is null)
            {
                _ref = Internal.AsyncPromiseRef.GetOrCreate();
                _id = _ref.Id;
            }
            _ref.SetException(exception);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T result)
        {
            if (_ref is null)
            {
                _result = result;
                _id = Internal.ValidPromiseIdFromApi;
            }
            else
            {
                _ref.SetResult(ref result);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(GetContinuation(ref stateMachine));
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(GetContinuation(ref stateMachine));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Action GetContinuation<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            if (_ref is null)
            {
                Internal.AsyncPromiseRef.SetStateMachine(ref stateMachine, ref _ref);
                _id = _ref.Id;
            }
            return _ref.continuation;
        }
    }
#endif // DEBUG or IL2CPP
}
#endif // C#7