#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#if CSHARP_7_OR_LATER // async/await not available in old runtime.

#pragma warning disable CS0436 // Type conflicts with imported type
#pragma warning disable IDE0060 // Remove unused parameter

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using Proto.Promises.Async.CompilerServices;
using Proto.Utils;

namespace Proto.Promises
{
    namespace Async.CompilerServices
    {
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            partial struct PromiseAwaiter : ICriticalNotifyCompletion
        {
            private readonly Promise _promise;

            public PromiseAwaiter(Promise promise)
            {
                _promise = promise;
                Internal.PromiseRef.MaybeMarkAwaited(promise);
            }

            public bool IsCompleted
            {
                get
                {
                    ValidateOperation(1);

                    return _promise._ref == null || _promise._ref.State != Promise.State.Pending;
                }
            }

            public void GetResult()
            {
                ValidateOperation(1);

                if (_promise._ref != null)
                {
                    _promise._ref.GetResultForAwaiter();
                }
            }

            public void OnCompleted(Action continuation)
            {
                ValidateOperation(1);

                _promise.Finally(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                ValidateOperation(1);

                _promise.Finally(continuation);
            }

            partial void ValidateOperation(int skipFrames);
#if PROMISE_DEBUG
            partial void ValidateOperation(int skipFrames)
            {
                Internal.ValidateOperation(_promise, skipFrames + 1);
            }
#endif
        }

        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            partial struct PromiseAwaiter<T> : ICriticalNotifyCompletion
        {
            private readonly Promise<T> _promise;

            public PromiseAwaiter(Promise<T> promise)
            {
                _promise = promise;
                Internal.PromiseRef.MaybeMarkAwaited(promise);
            }

            public bool IsCompleted
            {
                get
                {
                    ValidateOperation(1);

                    return _promise._ref == null || _promise._ref.State != Promise.State.Pending;
                }
            }

            public T GetResult()
            {
                ValidateOperation(1);

                if (_promise._ref != null)
                {
                    return _promise._ref.GetResultForAwaiter<T>();
                }
                return _promise._result;
            }

            public void OnCompleted(Action continuation)
            {
                ValidateOperation(1);

                _promise.Finally(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                ValidateOperation(1);

                _promise.Finally(continuation);
            }

            partial void ValidateOperation(int skipFrames);
#if PROMISE_DEBUG
            partial void ValidateOperation(int skipFrames)
            {
                Internal.ValidateOperation(_promise, skipFrames + 1);
            }
#endif
        }
    }

    [AsyncMethodBuilder(typeof(PromiseMethodBuilder))]
    partial struct Promise
    {
        /// <summary>
        /// Used to support the await keyword.
        /// </summary>
        public PromiseAwaiter GetAwaiter()
        {
            ValidateOperation(1);

            return new PromiseAwaiter(this);
        }
    }

    [AsyncMethodBuilder(typeof(PromiseMethodBuilder<>))]
    partial struct Promise<T>
    {
        /// <summary>
        /// Used to support the await keyword.
        /// </summary>
        public PromiseAwaiter<T> GetAwaiter()
        {
            ValidateOperation(1);

            return new PromiseAwaiter<T>(this);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal sealed class AsyncMethodBuilderAttribute : Attribute
    {
        public Type BuilderType { get; }

        public AsyncMethodBuilderAttribute(Type builderType)
        {
            BuilderType = builderType;
        }
    }
}

namespace Proto.Promises.Async.CompilerServices
{
    /// <summary>
    /// This type and its members are intended for use by the compiler (async Promise functions).
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    internal partial class AsyncPromiseRef : Internal.PromiseRef.AsyncPromiseBase
    {
        private struct Creator : Internal.ICreator<AsyncPromiseRef>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public AsyncPromiseRef Create()
            {
                return new AsyncPromiseRef();
            }
        }

        internal void SetResult()
        {
            ResolveDirect();
        }

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
                    string stacktrace = Internal.FormatStackTrace(new StackTrace[1] { new StackTrace(exception, true) });
#else
                    string stacktrace = new StackTrace(exception, true).ToString();
#endif
                    exception = new InvalidOperationException("RethrowException is only valid in promise onRejected callbacks.", stacktrace);
                }
                RejectDirect(ref exception, int.MinValue);
            }
        }
    }

#if PROMISE_DEBUG || ENABLE_IL2CPP
    // Fix for IL2CPP compile bug. https://issuetracker.unity3d.com/issues/il2cpp-incorrect-results-when-calling-a-method-from-outside-class-in-a-struct
    // Also use this in DEBUG mode for cleaner causality traces.
    sealed partial class AsyncPromiseRef
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        private abstract class PromiseMethodContinuer : IDisposable
        {
            public abstract Action Continuation { get; }
#if PROMISE_DEBUG
            protected Internal.ITraceable _owner;
#endif

            private PromiseMethodContinuer() { }

            public abstract void Dispose();

            public static PromiseMethodContinuer GetOrCreate<TStateMachine>(ref TStateMachine stateMachine, Internal.ITraceable owner) where TStateMachine : IAsyncStateMachine
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
                private struct Creator : Internal.ICreator<Continuer<TStateMachine>>
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

                public static Continuer<TStateMachine> GetOrCreate(ref TStateMachine stateMachine)
                {
                    var continuer = Internal.ObjectPool<Continuer<TStateMachine>>.GetOrCreate<Continuer<TStateMachine>, Creator>(new Creator());
                    continuer._stateMachine = stateMachine;
                    return continuer;
                }

                public override void Dispose()
                {
#if PROMISE_DEBUG
                    _owner = null;
#endif
                    _stateMachine = default;
                    Internal.ObjectPool<Continuer<TStateMachine>>.MaybeRepool(this);
                }

                private void Continue()
                {
#if PROMISE_DEBUG
                    // Set current trace to the function's promise for execution, then set it back.
                    var current = Internal.CurrentTrace;
                    Internal.CurrentTrace = _owner.Trace;
                    try
                    {
                        _stateMachine.MoveNext();
                    }
                    finally
                    {
                        Internal.CurrentTrace = current;
                    }
#else
                    _stateMachine.MoveNext();
#endif
                }
            }
        }

        private PromiseMethodContinuer _continuer;

        public static AsyncPromiseRef GetOrCreate()
        {
            var promise = Internal.ObjectPool<Internal.ITreeHandleable>.GetOrCreate<AsyncPromiseRef, Creator>(new Creator());
            promise.Reset();
            return promise;
        }

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
            Internal.ObjectPool<Internal.ITreeHandleable>.MaybeRepool(this);
        }
    }

    /// <summary>
    /// This type and its members are intended for use by the compiler.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public struct PromiseMethodBuilder
    {
        private AsyncPromiseRef _promise;

        public Promise Task { get { return new Promise(_promise, _promise.Id); } }

        public static PromiseMethodBuilder Create()
        {
            return new PromiseMethodBuilder()
            {
                _promise = AsyncPromiseRef.GetOrCreate()
            };
        }

        public void SetException(Exception exception)
        {
            _promise.SetException(exception);
        }

        public void SetResult()
        {
            _promise.SetResult();
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(_promise.GetContinuation(ref stateMachine));
        }

        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(_promise.GetContinuation(ref stateMachine));
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

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
        private AsyncPromiseRef _promise;

        [DebuggerHidden]
        public Promise<T> Task { get { return new Promise<T>(_promise, _promise.Id); } }

        [DebuggerHidden]
        public static PromiseMethodBuilder<T> Create()
        {
            return new PromiseMethodBuilder<T>()
            {
                _promise = AsyncPromiseRef.GetOrCreate()
            };
        }

        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            _promise.SetException(exception);
        }

        [DebuggerHidden]
        public void SetResult(T result)
        {
            _promise.SetResult(ref result);
        }

        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(_promise.GetContinuation(ref stateMachine));
        }

        [DebuggerHidden]
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(_promise.GetContinuation(ref stateMachine));
        }

        [DebuggerHidden]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    }
#else
    partial class AsyncPromiseRef
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        private sealed class AsyncPromiseRefMachine<TStateMachine> : AsyncPromiseRef where TStateMachine : IAsyncStateMachine
        {
            new private struct Creator : Internal.ICreator<AsyncPromiseRefMachine<TStateMachine>>
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

            public static void SetStateMachine(ref TStateMachine stateMachine, ref AsyncPromiseRef _ref)
            {
                var promise = Internal.ObjectPool<Internal.ITreeHandleable>.GetOrCreate<AsyncPromiseRefMachine<TStateMachine>, Creator>(new Creator());
                promise.Reset();
                // ORDER VERY IMPORTANT, Task must be set before copying stateMachine.
                _ref = promise;
                promise._stateMachine = stateMachine;
            }

            protected override void Dispose()
            {
                base.Dispose();
                _stateMachine = default;
                Internal.ObjectPool<Internal.ITreeHandleable>.MaybeRepool(this);
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
            var promise = Internal.ObjectPool<Internal.ITreeHandleable>.GetOrCreate<AsyncPromiseRef, Creator>(new Creator());
            promise.Reset();
            return promise;
        }

        internal static void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine, ref AsyncPromiseRef _ref) where TStateMachine : IAsyncStateMachine
        {
            AsyncPromiseRefMachine<TStateMachine>.SetStateMachine(ref stateMachine, ref _ref);
        }
    }

    /// <summary>
    /// This type and its members are intended for use by the compiler.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public struct PromiseMethodBuilder
    {
        private AsyncPromiseRef _ref;
        private ushort _id;

        public Promise Task { get { return new Promise(_ref, _id); } }

        private PromiseMethodBuilder(ushort id)
        {
            _ref = null;
            _id = id;
        }

        public static PromiseMethodBuilder Create()
        {
            return new PromiseMethodBuilder(Internal.ValidPromiseIdFromApi);
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

        public void SetResult()
        {
            if (_ref != null)
            {
                _ref.SetResult();
            }
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(GetContinuation(ref stateMachine));
        }

        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(GetContinuation(ref stateMachine));
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

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

    /// <summary>
    /// This type and its members are intended for use by the compiler.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public struct PromiseMethodBuilder<T>
    {
        private AsyncPromiseRef _ref;
        private ushort _id;

        public Promise<T> Task { get { return new Promise<T>(_ref, _id); } }

        private PromiseMethodBuilder(ushort id)
        {
            _ref = null;
            _id = id;
        }

        public static PromiseMethodBuilder<T> Create()
        {
            return new PromiseMethodBuilder<T>(Internal.ValidPromiseIdFromApi);
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

        public void SetResult(T result)
        {
            if (_ref != null)
            {
                _ref.SetResult(ref result);
            }
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(GetContinuation(ref stateMachine));
        }

        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(GetContinuation(ref stateMachine));
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

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
}
#endif // C#7