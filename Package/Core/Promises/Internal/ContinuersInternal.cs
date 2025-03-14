#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0290 // Use primary constructor

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
            private PromiseWrapper(Promise promise)
            {
                _ref = promise._ref;
                _result = default;
                _id = promise._id;
            }

            [MethodImpl(InlineOption)]
            private PromiseWrapper(in Promise<T> promise)
            {
                _ref = promise._ref;
                _result = promise._result;
                _id = promise._id;
            }

            [MethodImpl(InlineOption)]
            private PromiseWrapper(in T result)
            {
                _ref = null;
                _result = result;
                _id = 0;
            }

            [MethodImpl(InlineOption)]
            internal Promise<T> AsPromise()
                => new Promise<T>(_ref.UnsafeAs<PromiseRefBase.PromiseRef<T>>(), _id, _result);

            [MethodImpl(InlineOption)]
            public static implicit operator PromiseWrapper<T>(in Promise promise)
                => new PromiseWrapper<T>(promise);

            [MethodImpl(InlineOption)]
            public static implicit operator PromiseWrapper<T>(in Promise<T> promise)
                => new PromiseWrapper<T>(promise);

            [MethodImpl(InlineOption)]
            public static implicit operator PromiseWrapper<T>(in T result)
                => new PromiseWrapper<T>(result);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct VoidTransformer :
            ITransformer<Promise<VoidResult>.ResultContainer, Promise.ResultContainer>,
            ITransformer<Promise<VoidResult>.ResultContainer, VoidResult>,
            ITransformer<VoidResult, PromiseWrapper<VoidResult>>,
            ITransformer<Promise, PromiseWrapper<VoidResult>>
        {
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
            ITransformer<Promise<T>.ResultContainer, Promise<T>.ResultContainer>,
            ITransformer<Promise<T>.ResultContainer, T>,
            ITransformer<T, PromiseWrapper<T>>,
            ITransformer<Promise<T>, PromiseWrapper<T>>
        {
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
        internal readonly struct ContinueWithContinuer : IContinuer
        {
            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out Promise.State invokeType)
            {
                invokeType = Promise.State.Resolved;
                return true;
            }
        }
    }
}