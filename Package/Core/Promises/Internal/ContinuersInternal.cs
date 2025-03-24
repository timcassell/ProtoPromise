#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0290 // Use primary constructor

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        // This type allows us to treat `Promise` as `Promise<VoidResult>` to reduce code duplication.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct PromiseWrapper<T>
        {
            internal readonly PromiseRefBase _ref;
            internal readonly T _result;
            internal readonly short _id;

            [MethodImpl(InlineOption)]
            internal PromiseWrapper(PromiseRefBase promise, short promiseId, T result)
            {
                _ref = promise;
                _result = result;
                _id = promiseId;
            }

            [MethodImpl(InlineOption)]
            internal PromiseWrapper<T> Duplicate()
            {
                if (_ref == null)
                {
                    return this;
                }
                var duplicate = _ref.GetDuplicate(_id);
                return new PromiseWrapper<T>(duplicate, duplicate.Id, default);
            }

            [MethodImpl(InlineOption)]
            public static implicit operator Promise<T>(in PromiseWrapper<T> promiseWrapper)
                => new Promise<T>(promiseWrapper._ref.UnsafeAs<PromiseRefBase.PromiseRef<T>>(), promiseWrapper._id, promiseWrapper._result);

            [MethodImpl(InlineOption)]
            public static implicit operator Promise(in PromiseWrapper<T> promiseWrapper)
                => new Promise(promiseWrapper._ref, promiseWrapper._id);

            [MethodImpl(InlineOption)]
            public static implicit operator PromiseWrapper<T>(in Promise promise)
                => new PromiseWrapper<T>(promise._ref, promise._id, default);

            [MethodImpl(InlineOption)]
            public static implicit operator PromiseWrapper<T>(in Promise<T> promise)
                => new PromiseWrapper<T>(promise._ref, promise._id, promise._result);

            [MethodImpl(InlineOption)]
            public static implicit operator PromiseWrapper<T>(in T result)
                => new PromiseWrapper<T>(null, 0, result);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct VoidTransformer :
            ITransformer<VoidResult, VoidResult>,
            ITransformer<Promise<VoidResult>.ResultContainer, Promise.ResultContainer>,
            ITransformer<Promise<VoidResult>.ResultContainer, VoidResult>,
            ITransformer<VoidResult, PromiseWrapper<VoidResult>>,
            ITransformer<Promise, PromiseWrapper<VoidResult>>
        {
            [MethodImpl(InlineOption)]
            VoidResult ITransformer<VoidResult, VoidResult>.Transform(in VoidResult input)
                => default;

            [MethodImpl(InlineOption)]
            Promise.ResultContainer ITransformer<Promise<VoidResult>.ResultContainer, Promise.ResultContainer>.Transform(in Promise<VoidResult>.ResultContainer input)
                => new Promise.ResultContainer(input);

            [MethodImpl(InlineOption)]
            public VoidResult Transform(in Promise<VoidResult>.ResultContainer input)
                => default;

            [MethodImpl(InlineOption)]
            public PromiseWrapper<VoidResult> Transform(in VoidResult input)
                => default;

            [MethodImpl(InlineOption)]
            public PromiseWrapper<VoidResult> Transform(in Promise input)
                => input;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct TTransformer<T> :
            ITransformer<T, T>,
            ITransformer<Promise<T>.ResultContainer, Promise<T>.ResultContainer>,
            ITransformer<Promise<T>.ResultContainer, T>,
            ITransformer<T, PromiseWrapper<T>>,
            ITransformer<Promise<T>, PromiseWrapper<T>>
        {
            [MethodImpl(InlineOption)]
            T ITransformer<T, T>.Transform(in T input)
                => input;

            [MethodImpl(InlineOption)]
            Promise<T>.ResultContainer ITransformer<Promise<T>.ResultContainer, Promise<T>.ResultContainer>.Transform(in Promise<T>.ResultContainer input)
                => input;

            [MethodImpl(InlineOption)]
            public T Transform(in Promise<T>.ResultContainer input)
                => input.Value;

            [MethodImpl(InlineOption)]
            public PromiseWrapper<T> Transform(in T input)
                => input;

            [MethodImpl(InlineOption)]
            public PromiseWrapper<T> Transform(in Promise<T> input)
                => input;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct CatchTransformer : ITransformer<IRejectContainer, VoidResult>
        {
            [MethodImpl(InlineOption)]
            public VoidResult Transform(in IRejectContainer input)
                => default;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct CatchTransformer<T> :
            ITransformer<Promise<T>.ResultContainer, VoidResult>,
            ITransformer<IRejectContainer, T>
        {
            [MethodImpl(InlineOption)]
            public VoidResult Transform(in Promise<T>.ResultContainer input)
                => default;

            [MethodImpl(InlineOption)]
            public T Transform(in IRejectContainer input)
                => (T) input.Value;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct CatchTransformer<T, TReject> : ITransformer<Promise<T>.ResultContainer, TReject>
        {
            [MethodImpl(InlineOption)]
            public TReject Transform(in Promise<T>.ResultContainer input) 
                => (TReject) input._rejectContainer.Value;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct CatchCancelationTransformer<T> : ITransformer<Promise<T>.ResultContainer, VoidResult>
        {
            [MethodImpl(InlineOption)]
            public VoidResult Transform(in Promise<T>.ResultContainer input)
                => default;
        }

        [Flags]
        internal enum InvokeTypes : byte
        {
            Resolved = 1 << 0,
            Rejected = 1 << 1,
            Canceled = 1 << 2
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct ContinueWithContinuer : IContinuer
        {
            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out InvokeTypes invokeTypes)
            {
                invokeTypes = InvokeTypes.Resolved | InvokeTypes.Rejected | InvokeTypes.Canceled;
                return true;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct ThenResolveContinuer : IContinuer
        {
            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out InvokeTypes invokeTypes)
            {
                invokeTypes = InvokeTypes.Resolved;
                return state == Promise.State.Resolved;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct CatchContinuer : IContinuer
        {
            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out InvokeTypes invokeTypes)
            {
                invokeTypes = InvokeTypes.Rejected;
                return state == Promise.State.Rejected;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct CatchFilteredContinuer<TReject> : IContinuer
        {
            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out InvokeTypes invokeTypes)
            {
                invokeTypes = InvokeTypes.Rejected;
                return state == Promise.State.Rejected && rejectContainer.Value is TReject;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct CatchCancelationContinuer : IContinuer
        {
            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out InvokeTypes invokeTypes)
            {
                invokeTypes = InvokeTypes.Canceled;
                return state == Promise.State.Canceled;
            }
        }
    }
}