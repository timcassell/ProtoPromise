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
    // These help reduce typed out generics.

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal static class Continuer
    {
        [MethodImpl(Internal.InlineOption)]
        internal static Internal.ContinueWithContinuer<TDelegate> ContinueWith<TDelegate>(in TDelegate callback)
            where TDelegate : IAction<Promise.ResultContainer>
            => new Internal.ContinueWithContinuer<TDelegate>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.ContinueWithWaitContinuer<TDelegate> ContinueWithWait<TDelegate>(in TDelegate callback)
            where TDelegate : IFunc<Promise.ResultContainer, Promise>
            => new Internal.ContinueWithWaitContinuer<TDelegate>(callback);
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal static class ContinuerArg<TArg>
    {
        [MethodImpl(Internal.InlineOption)]
        internal static Internal.ContinueWithContinuerArg<TArg, TDelegate> ContinueWith<TDelegate>(in TDelegate callback)
            where TDelegate : IAction<Promise<TArg>.ResultContainer>
            => new Internal.ContinueWithContinuerArg<TArg, TDelegate>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.ContinueWithWaitContinuerArg<TArg, TDelegate> ContinueWithWait<TDelegate>(in TDelegate callback)
            where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>
            => new Internal.ContinueWithWaitContinuerArg<TArg, TDelegate>(callback);
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal static class ContinuerResult<TResult>
    {
        [MethodImpl(Internal.InlineOption)]
        internal static Internal.ContinueWithContinuerResult<TResult, TDelegate> ContinueWith<TDelegate>(in TDelegate callback)
            where TDelegate : IFunc<Promise.ResultContainer, TResult>
            => new Internal.ContinueWithContinuerResult<TResult, TDelegate>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.ContinueWithWaitContinuerResult<TResult, TDelegate> ContinueWithWait<TDelegate>(in TDelegate callback)
            where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>
            => new Internal.ContinueWithWaitContinuerResult<TResult, TDelegate>(callback);
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal static class Continuer<TArg, TResult>
    {
        [MethodImpl(Internal.InlineOption)]
        internal static Internal.ContinueWithContinuer<TArg, TResult, TDelegate> ContinueWith<TDelegate>(in TDelegate callback)
            where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>
            => new Internal.ContinueWithContinuer<TArg, TResult, TDelegate>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.ContinueWithWaitContinuer<TArg, TResult, TDelegate> ContinueWithWait<TDelegate>(in TDelegate callback)
            where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
            => new Internal.ContinueWithWaitContinuer<TArg, TResult, TDelegate>(callback);
    }

    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct ContinueWithContinuer<TDelegate> : IContinuer<VoidResult, VoidResult>
            where TDelegate : IAction<Promise.ResultContainer>
        {
            private readonly TDelegate _callback;

            [MethodImpl(InlineOption)]
            public ContinueWithContinuer(TDelegate callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public VoidResult Invoke(in Promise<VoidResult>.ResultContainer resultContainer)
            {
                _callback.Invoke(new Promise.ResultContainer(resultContainer));
                return default;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct ContinueWithContinuerResult<TResult, TDelegate> : IContinuer<VoidResult, TResult>
            where TDelegate : IFunc<Promise.ResultContainer, TResult>
        {
            private readonly TDelegate _callback;

            [MethodImpl(InlineOption)]
            public ContinueWithContinuerResult(TDelegate callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke(in Promise<VoidResult>.ResultContainer resultContainer)
                => _callback.Invoke(new Promise.ResultContainer(resultContainer));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct ContinueWithContinuerArg<TArg, TDelegate> : IContinuer<TArg, VoidResult>
            where TDelegate : IAction<Promise<TArg>.ResultContainer>
        {
            private readonly TDelegate _callback;

            [MethodImpl(InlineOption)]
            public ContinueWithContinuerArg(TDelegate callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public VoidResult Invoke(in Promise<TArg>.ResultContainer resultContainer)
            {
                _callback.Invoke(resultContainer);
                return default;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct ContinueWithContinuer<TArg, TResult, TDelegate> : IContinuer<TArg, TResult>
            where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>
        {
            private readonly TDelegate _callback;

            [MethodImpl(InlineOption)]
            public ContinueWithContinuer(TDelegate callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke(TArg arg)
                => Invoke(new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));

            [MethodImpl(InlineOption)]
            private TResult Invoke(Promise<TArg>.ResultContainer resultContainer)
                => _callback.Invoke(resultContainer);

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke(in Promise<TArg>.ResultContainer resultContainer)
                => _callback.Invoke(resultContainer);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct ContinueWithWaitContinuer<TDelegate> : IContinuer<VoidResult, Promise>
            where TDelegate : IFunc<Promise.ResultContainer, Promise>
        {
            private readonly TDelegate _callback;

            [MethodImpl(InlineOption)]
            public ContinueWithWaitContinuer(TDelegate callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public Promise Invoke(in Promise<VoidResult>.ResultContainer resultContainer)
                => _callback.Invoke(new Promise.ResultContainer(resultContainer));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct ContinueWithWaitContinuerResult<TResult, TDelegate> : IContinuer<VoidResult, Promise<TResult>>
            where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>
        {
            private readonly TDelegate _callback;

            [MethodImpl(InlineOption)]
            public ContinueWithWaitContinuerResult(TDelegate callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke(in Promise<VoidResult>.ResultContainer resultContainer)
                => _callback.Invoke(new Promise.ResultContainer(resultContainer));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct ContinueWithWaitContinuerArg<TArg, TDelegate> : IContinuer<TArg, Promise>
            where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>
        {
            private readonly TDelegate _callback;

            [MethodImpl(InlineOption)]
            public ContinueWithWaitContinuerArg(TDelegate callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
            {
                var resultContainer = new Promise<TArg>.ResultContainer(handler.GetResult<TArg>(), rejectContainer, state);
                handler.MaybeDispose();
                Promise result = Invoke(resultContainer);
                owner.WaitFor(result);
            }

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public Promise Invoke(in Promise<TArg>.ResultContainer resultContainer)
                => _callback.Invoke(resultContainer);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct ContinueWithWaitContinuer<TArg, TResult, TDelegate> : IContinuer<TArg, Promise<TResult>>
            where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
        {
            private readonly TDelegate _callback;

            [MethodImpl(InlineOption)]
            public ContinueWithWaitContinuer(TDelegate callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke(in Promise<TArg>.ResultContainer resultContainer)
                => _callback.Invoke(resultContainer);
        }
    }
}