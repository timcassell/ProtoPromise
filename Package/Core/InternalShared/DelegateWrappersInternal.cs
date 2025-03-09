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
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal static class DelegateWrapper
    {
        // These static functions help with the implementation so we don't need to type the generics in every method.

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateVoidVoid Create(Action callback)
        //    => new Internal.DelegateVoidVoid(callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateVoidResult<TResult> Create<TResult>(Func<TResult> callback)
        //    => new Internal.DelegateVoidResult<TResult>(callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateArgVoid<TArg> Create<TArg>(Action<TArg> callback)
        //    => new Internal.DelegateArgVoid<TArg>(callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateArgResult<TArg, TResult> Create<TArg, TResult>(Func<TArg, TResult> callback)
        //    => new Internal.DelegateArgResult<TArg, TResult>(callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegatePromiseVoidVoid Create(Func<Promise> callback)
        //    => new Internal.DelegatePromiseVoidVoid(callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegatePromiseVoidResult<TResult> Create<TResult>(Func<Promise<TResult>> callback)
        //    => new Internal.DelegatePromiseVoidResult<TResult>(callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegatePromiseArgVoid<TArg> Create<TArg>(Func<TArg, Promise> callback)
        //    => new Internal.DelegatePromiseArgVoid<TArg>(callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegatePromiseArgResult<TArg, TResult> Create<TArg, TResult>(Func<TArg, Promise<TResult>> callback)
        //    => new Internal.DelegatePromiseArgResult<TArg, TResult>(callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateCaptureVoidVoid<TCapture> Create<TCapture>(in TCapture capturedValue, Action<TCapture> callback)
        //    => new Internal.DelegateCaptureVoidVoid<TCapture>(capturedValue, callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateCaptureVoidResult<TCapture, TResult> Create<TCapture, TResult>(in TCapture capturedValue, Func<TCapture, TResult> callback)
        //    => new Internal.DelegateCaptureVoidResult<TCapture, TResult>(capturedValue, callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(in TCapture capturedValue, Action<TCapture, TArg> callback)
        //    => new Internal.DelegateCaptureArgVoid<TCapture, TArg>(capturedValue, callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(in TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
        //    => new Internal.DelegateCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateCapturePromiseVoidVoid<TCapture> Create<TCapture>(in TCapture capturedValue, Func<TCapture, Promise> callback)
        //    => new Internal.DelegateCapturePromiseVoidVoid<TCapture>(capturedValue, callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateCapturePromiseVoidResult<TCapture, TResult> Create<TCapture, TResult>(in TCapture capturedValue, Func<TCapture, Promise<TResult>> callback)
        //    => new Internal.DelegateCapturePromiseVoidResult<TCapture, TResult>(capturedValue, callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateCapturePromiseArgVoid<TCapture, TArg> Create<TCapture, TArg>(in TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
        //    => new Internal.DelegateCapturePromiseArgVoid<TCapture, TArg>(capturedValue, callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateCapturePromiseArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(in TCapture capturedValue, Func<TCapture, TArg, Promise<TResult>> callback)
        //    => new Internal.DelegateCapturePromiseArgResult<TCapture, TArg, TResult>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinueVoidVoid Create(Action<Promise.ResultContainer> callback)
            => new Internal.DelegateContinueVoidVoid(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinueVoidResult<TResult> Create<TResult>(Func<Promise.ResultContainer, TResult> callback)
            => new Internal.DelegateContinueVoidResult<TResult>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinueArgVoid<TArg> Create<TArg>(Action<Promise<TArg>.ResultContainer> callback)
            => new Internal.DelegateContinueArgVoid<TArg>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinueArgResult<TArg, TResult> Create<TArg, TResult>(Func<Promise<TArg>.ResultContainer, TResult> callback)
            => new Internal.DelegateContinueArgResult<TArg, TResult>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinueCaptureVoidVoid<TCapture> Create<TCapture>(in TCapture capturedValue, Action<TCapture, Promise.ResultContainer> callback)
            => new Internal.DelegateContinueCaptureVoidVoid<TCapture>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinueCaptureVoidResult<TCapture, TResult> Create<TCapture, TResult>(in TCapture capturedValue, Func<TCapture, Promise.ResultContainer, TResult> callback)
            => new Internal.DelegateContinueCaptureVoidResult<TCapture, TResult>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinueCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(in TCapture capturedValue, Action<TCapture, Promise<TArg>.ResultContainer> callback)
            => new Internal.DelegateContinueCaptureArgVoid<TCapture, TArg>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinueCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, TResult> callback)
            => new Internal.DelegateContinueCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinuePromiseVoidVoid Create(Func<Promise.ResultContainer, Promise> callback)
            => new Internal.DelegateContinuePromiseVoidVoid(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinuePromiseVoidResult<TResult> Create<TResult>(Func<Promise.ResultContainer, Promise<TResult>> callback)
            => new Internal.DelegateContinuePromiseVoidResult<TResult>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinuePromiseArgVoid<TArg> Create<TArg>(Func<Promise<TArg>.ResultContainer, Promise> callback)
            => new Internal.DelegateContinuePromiseArgVoid<TArg>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinuePromiseArgResult<TArg, TResult> Create<TArg, TResult>(Func<Promise<TArg>.ResultContainer, Promise<TResult>> callback)
            => new Internal.DelegateContinuePromiseArgResult<TArg, TResult>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinuePromiseCaptureVoidVoid<TCapture> Create<TCapture>(in TCapture capturedValue, Func<TCapture, Promise.ResultContainer, Promise> callback)
            => new Internal.DelegateContinuePromiseCaptureVoidVoid<TCapture>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinuePromiseCaptureVoidResult<TCapture, TResult> Create<TCapture, TResult>(in TCapture capturedValue, Func<TCapture, Promise.ResultContainer, Promise<TResult>> callback)
            => new Internal.DelegateContinuePromiseCaptureVoidResult<TCapture, TResult>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinuePromiseCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, Promise> callback)
            => new Internal.DelegateContinuePromiseCaptureArgVoid<TCapture, TArg>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateContinuePromiseCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, Promise<TResult>> callback)
            => new Internal.DelegateContinuePromiseCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateNewPromiseVoid Create(Action<Promise.Deferred> callback)
        //    => new Internal.DelegateNewPromiseVoid(callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateNewPromiseResult<TResult> Create<TResult>(Action<Promise<TResult>.Deferred> callback)
        //    => new Internal.DelegateNewPromiseResult<TResult>(callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateNewPromiseCaptureVoid<TCapture> Create<TCapture>(in TCapture capturedValue, Action<TCapture, Promise.Deferred> callback)
        //    => new Internal.DelegateNewPromiseCaptureVoid<TCapture>(capturedValue, callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.DelegateNewPromiseCaptureResult<TCapture, TResult> Create<TCapture, TResult>(in TCapture capturedValue, Action<TCapture, Promise<TResult>.Deferred> callback)
        //    => new Internal.DelegateNewPromiseCaptureResult<TCapture, TResult>(capturedValue, callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.Func2ArgResult<TArg1, TArg2, TResult> Create<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult> callback)
        //    => new Internal.Func2ArgResult<TArg1, TArg2, TResult>(callback);

        //[MethodImpl(Internal.InlineOption)]
        //internal static Internal.Func2ArgResultCapture<TCapture, TArg1, TArg2, TResult> Create<TCapture, TArg1, TArg2, TResult>(in TCapture capturedValue, Func<TCapture, TArg1, TArg2, TResult> callback)
        //    => new Internal.Func2ArgResultCapture<TCapture, TArg1, TArg2, TResult>(capturedValue, callback);
    }

    partial class Internal
    {
        //    #region Regular Delegates

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateNewPromiseVoid : IDelegateNew<VoidResult>
        //    {
        //        private readonly Action<Promise.Deferred> _callback;

        //        [MethodImpl(Internal.InlineOption)]
        //        public DelegateNewPromiseVoid(Action<Promise.Deferred> callback)
        //        {
        //            _callback = callback;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateNew<VoidResult>.Invoke(DeferredPromise<VoidResult> owner)
        //            => _callback.Invoke(new Promise.Deferred(owner, owner.Id, owner.DeferredId));
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateNewPromiseResult<TResult> : IDelegateNew<TResult>
        //    {
        //        private readonly Action<Promise<TResult>.Deferred> _callback;

        //        [MethodImpl(Internal.InlineOption)]
        //        public DelegateNewPromiseResult(Action<Promise<TResult>.Deferred> callback)
        //        {
        //            _callback = callback;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateNew<TResult>.Invoke(DeferredPromise<TResult> owner)
        //            => _callback.Invoke(new Promise<TResult>.Deferred(owner, owner.Id, owner.DeferredId));
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateVoidVoid : IAction, IFunc<Promise>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise, IDelegateRun
        //    {
        //        private readonly Action _callback;

        //        public bool IsNull
        //        {
        //            [MethodImpl(Internal.InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public DelegateVoidVoid(Action callback)
        //        {
        //            _callback = callback;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public void Invoke()
        //            => _callback.Invoke();

        //        [MethodImpl(Internal.InlineOption)]
        //        Promise IFunc<Promise>.Invoke()
        //        {
        //            Invoke();
        //            return new Promise();
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            Invoke();
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            Invoke();
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            Invoke();
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            Invoke();
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateRun.Invoke(PromiseRefBase owner)
        //        {
        //            Invoke();
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateVoidResult<TResult> : IFunc<TResult>, IFunc<Promise<TResult>>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise, IDelegateRun
        //    {
        //        private readonly Func<TResult> _callback;

        //        public bool IsNull
        //        {
        //            [MethodImpl(Internal.InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public DelegateVoidResult(Func<TResult> callback)
        //        {
        //            _callback = callback;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public TResult Invoke()
        //            => _callback.Invoke();

        //        [MethodImpl(Internal.InlineOption)]
        //        Promise<TResult> IFunc<Promise<TResult>>.Invoke()
        //            => new Promise<TResult>(Invoke());

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            TResult result = Invoke();
        //            owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            TResult result = Invoke();
        //            owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            TResult result = Invoke();
        //            owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            TResult result = Invoke();
        //            owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateRun.Invoke(PromiseRefBase owner)
        //        {
        //            TResult result = Invoke();
        //            owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateArgVoid<TArg> : IAction<TArg>, IFunc<TArg, Promise>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise
        //    {
        //        private readonly Action<TArg> _callback;

        //        public bool IsNull
        //        {
        //            [MethodImpl(Internal.InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public DelegateArgVoid(Action<TArg> callback)
        //        {
        //            _callback = callback;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public void Invoke(TArg arg)
        //            => _callback.Invoke(arg);

        //        [MethodImpl(Internal.InlineOption)]
        //        Promise IFunc<TArg, Promise>.Invoke(TArg arg)
        //        {
        //            Invoke(arg);
        //            return new Promise();
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            TArg arg = handler.GetResult<TArg>();
        //            handler.MaybeDispose();
        //            Invoke(arg);
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            TArg arg = handler.GetResult<TArg>();
        //            handler.MaybeDispose();
        //            Invoke(arg);
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        private void InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            if (rejectContainer.TryGetValue(out TArg arg))
        //            {
        //                Invoke(arg);
        //                owner.HandleNextInternal(Promise.State.Resolved);
        //            }
        //            else
        //            {
        //                owner.RejectContainer = rejectContainer;
        //                owner.HandleNextInternal(Promise.State.Rejected);
        //            }
        //        }

        //        void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
        //            => InvokeRejecter(rejectContainer, owner);

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            InvokeRejecter(rejectContainer, owner);
        //        }
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateArgResult<TArg, TResult> : IFunc<TArg, TResult>, IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise
        //    {
        //        private readonly Func<TArg, TResult> _callback;

        //        public bool IsNull
        //        {
        //            [MethodImpl(Internal.InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public DelegateArgResult(Func<TArg, TResult> callback)
        //        {
        //            _callback = callback;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public TResult Invoke(TArg arg)
        //            => _callback.Invoke(arg);

        //        [MethodImpl(Internal.InlineOption)]
        //        Promise<TResult> IFunc<TArg, Promise<TResult>>.Invoke(TArg arg)
        //            => new Promise<TResult>(Invoke(arg));

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            TArg arg = handler.GetResult<TArg>();
        //            handler.MaybeDispose();
        //            TResult result = Invoke(arg);
        //            owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            TArg arg = handler.GetResult<TArg>();
        //            handler.MaybeDispose();
        //            TResult result = Invoke(arg);
        //            owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        private void InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            if (rejectContainer.TryGetValue(out TArg arg))
        //            {
        //                TResult result = Invoke(arg);
        //                owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //                owner.HandleNextInternal(Promise.State.Resolved);
        //            }
        //            else
        //            {
        //                owner.RejectContainer = rejectContainer;
        //                owner.HandleNextInternal(Promise.State.Rejected);
        //            }
        //        }

        //        void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
        //            => InvokeRejecter(rejectContainer, owner);

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            InvokeRejecter(rejectContainer, owner);
        //        }
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegatePromiseVoidVoid : IFunc<Promise>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise, IDelegateRunPromise
        //    {
        //        private readonly Func<Promise> _callback;

        //        public bool IsNull
        //        {
        //            [MethodImpl(Internal.InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public DelegatePromiseVoidVoid(Func<Promise> callback)
        //        {
        //            _callback = callback;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public Promise Invoke()
        //            => _callback.Invoke();

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            Promise result = Invoke();
        //            owner.WaitFor(result);
        //        }

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            Promise result = Invoke();
        //            owner.WaitFor(result);
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateRunPromise.Invoke(PromiseRefBase owner)
        //        {
        //            Promise result = Invoke();
        //            owner.WaitFor(result);
        //        }
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegatePromiseVoidResult<TResult> : IFunc<Promise<TResult>>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise, IDelegateRunPromise
        //    {
        //        private readonly Func<Promise<TResult>> _callback;

        //        public bool IsNull
        //        {
        //            [MethodImpl(Internal.InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public DelegatePromiseVoidResult(Func<Promise<TResult>> callback)
        //        {
        //            _callback = callback;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public Promise<TResult> Invoke()
        //            => _callback.Invoke();

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            Promise<TResult> result = Invoke();
        //            owner.WaitFor(result);
        //        }

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            Promise<TResult> result = Invoke();
        //            owner.WaitFor(result);
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateRunPromise.Invoke(PromiseRefBase owner)
        //        {
        //            Promise<TResult> result = Invoke();
        //            owner.WaitFor(result);
        //        }
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegatePromiseArgVoid<TArg> : IFunc<TArg, Promise>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise
        //    {
        //        private readonly Func<TArg, Promise> _callback;

        //        public bool IsNull
        //        {
        //            [MethodImpl(Internal.InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public DelegatePromiseArgVoid(Func<TArg, Promise> callback)
        //        {
        //            _callback = callback;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public Promise Invoke(TArg arg)
        //            => _callback.Invoke(arg);

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            TArg arg = handler.GetResult<TArg>();
        //            handler.MaybeDispose();
        //            Promise result = Invoke(arg);
        //            owner.WaitFor(result);
        //        }

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            if (rejectContainer.TryGetValue(out TArg arg))
        //            {
        //                Promise result = Invoke(arg);
        //                owner.WaitFor(result);
        //            }
        //            else
        //            {
        //                owner.RejectContainer = rejectContainer;
        //                owner.HandleNextInternal(Promise.State.Rejected);
        //            }
        //        }
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegatePromiseArgResult<TArg, TResult> : IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise
        //    {
        //        private readonly Func<TArg, Promise<TResult>> _callback;

        //        public bool IsNull
        //        {
        //            [MethodImpl(Internal.InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public DelegatePromiseArgResult(Func<TArg, Promise<TResult>> callback)
        //        {
        //            _callback = callback;
        //        }

        //        [MethodImpl(Internal.InlineOption)]
        //        public Promise<TResult> Invoke(TArg arg)
        //            => _callback.Invoke(arg);

        //        [MethodImpl(Internal.InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            TArg arg = handler.GetResult<TArg>();
        //            handler.MaybeDispose();
        //            Promise<TResult> result = Invoke(arg);
        //            owner.WaitFor(result);
        //        }

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            if (rejectContainer.TryGetValue(out TArg arg))
        //            {
        //                Promise<TResult> result = Invoke(arg);
        //                owner.WaitFor(result);
        //            }
        //            else
        //            {
        //                owner.RejectContainer = rejectContainer;
        //                owner.HandleNextInternal(Promise.State.Rejected);
        //            }
        //        }
        //    }


#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateContinueVoidVoid : IAction, IContinuer<VoidResult, VoidResult>
        {
            private readonly Action<Promise.ResultContainer> _callback;

            [MethodImpl(InlineOption)]
            public DelegateContinueVoidVoid(Action<Promise.ResultContainer> callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public void Invoke()
                => _callback.Invoke(new Promise.ResultContainer(null, Promise.State.Resolved));

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
        internal readonly struct DelegateContinueVoidResult<TResult> : IFunc<TResult>, IContinuer<VoidResult, TResult>
        {
            private readonly Func<Promise.ResultContainer, TResult> _callback;

            [MethodImpl(InlineOption)]
            public DelegateContinueVoidResult(Func<Promise.ResultContainer, TResult> callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke()
                => _callback.Invoke(new Promise.ResultContainer(null, Promise.State.Resolved));

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
        internal readonly struct DelegateContinueArgVoid<TArg> : IAction<TArg>, IContinuer<TArg, VoidResult>
        {
            private readonly Action<Promise<TArg>.ResultContainer> _callback;

            [MethodImpl(InlineOption)]
            public DelegateContinueArgVoid(Action<Promise<TArg>.ResultContainer> callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public void Invoke(TArg arg)
                => Invoke(new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));

            [MethodImpl(InlineOption)]
            private void Invoke(Promise<TArg>.ResultContainer resultContainer)
                => _callback.Invoke(resultContainer);

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
        internal readonly struct DelegateContinueArgResult<TArg, TResult> : IFunc<TArg, TResult>, IContinuer<TArg, TResult>
        {
            private readonly Func<Promise<TArg>.ResultContainer, TResult> _callback;

            [MethodImpl(InlineOption)]
            public DelegateContinueArgResult(Func<Promise<TArg>.ResultContainer, TResult> callback)
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
        internal readonly struct DelegateContinuePromiseVoidVoid : IFunc<Promise>, IContinuer<VoidResult, Promise>
        {
            private readonly Func<Promise.ResultContainer, Promise> _callback;

            [MethodImpl(InlineOption)]
            public DelegateContinuePromiseVoidVoid(Func<Promise.ResultContainer, Promise> callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public Promise Invoke()
                => _callback.Invoke(new Promise.ResultContainer(null, Promise.State.Resolved));

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
        internal readonly struct DelegateContinuePromiseVoidResult<TResult> : IFunc<Promise<TResult>>, IContinuer<VoidResult, Promise<TResult>>
        {
            private readonly Func<Promise.ResultContainer, Promise<TResult>> _callback;

            [MethodImpl(InlineOption)]
            public DelegateContinuePromiseVoidResult(Func<Promise.ResultContainer, Promise<TResult>> callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke()
                => _callback.Invoke(new Promise.ResultContainer(null, Promise.State.Resolved));

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
        internal readonly struct DelegateContinuePromiseArgVoid<TArg> : IFunc<TArg, Promise>, IContinuer<TArg, Promise>
        {
            private readonly Func<Promise<TArg>.ResultContainer, Promise> _callback;

            [MethodImpl(InlineOption)]
            public DelegateContinuePromiseArgVoid(Func<Promise<TArg>.ResultContainer, Promise> callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public Promise Invoke(TArg arg)
                => _callback.Invoke(new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));

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
        internal readonly struct DelegateContinuePromiseArgResult<TArg, TResult> : IFunc<TArg, Promise<TResult>>, IContinuer<TArg, Promise<TResult>>
        {
            private readonly Func<Promise<TArg>.ResultContainer, Promise<TResult>> _callback;

            [MethodImpl(InlineOption)]
            public DelegateContinuePromiseArgResult(Func<Promise<TArg>.ResultContainer, Promise<TResult>> callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke(TArg arg)
                => _callback.Invoke(new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));

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


        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateFinally : IAction
        //    {
        //        private readonly Action _callback;

        //        public bool IsNull
        //        {
        //            [MethodImpl(InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public DelegateFinally(Action callback)
        //        {
        //            _callback = callback;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public void Invoke()
        //            => _callback.Invoke();
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateCancel : IAction
        //    {
        //        private readonly Action _callback;

        //        [MethodImpl(InlineOption)]
        //        public DelegateCancel(Action callback)
        //        {
        //            _callback = callback;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public void Invoke()
        //            => _callback.Invoke();
        //    }
        //    #endregion

        //    #region Delegates with capture value

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateNewPromiseCaptureVoid<TCapture> : IDelegateNew<VoidResult>
        //    {
        //        private readonly Action<TCapture, Promise.Deferred> _callback;
        //        private readonly TCapture _capturedValue;

        //        [MethodImpl(InlineOption)]
        //        public DelegateNewPromiseCaptureVoid(in TCapture capturedValue, Action<TCapture, Promise.Deferred> callback)
        //        {
        //            _callback = callback;
        //            _capturedValue = capturedValue;
        //        }

        //        [MethodImpl(InlineOption)]
        //        void IDelegateNew<VoidResult>.Invoke(DeferredPromise<VoidResult> owner)
        //            => _callback.Invoke(_capturedValue, new Promise.Deferred(owner, owner.Id, owner.DeferredId));
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateNewPromiseCaptureResult<TCapture, TResult> : IDelegateNew<TResult>
        //    {
        //        private readonly Action<TCapture, Promise<TResult>.Deferred> _callback;
        //        private readonly TCapture _capturedValue;

        //        [MethodImpl(InlineOption)]
        //        public DelegateNewPromiseCaptureResult(in TCapture capturedValue, Action<TCapture, Promise<TResult>.Deferred> callback)
        //        {
        //            _callback = callback;
        //            _capturedValue = capturedValue;
        //        }

        //        [MethodImpl(InlineOption)]
        //        void IDelegateNew<TResult>.Invoke(DeferredPromise<TResult> owner)
        //            => _callback.Invoke(_capturedValue, new Promise<TResult>.Deferred(owner, owner.Id, owner.DeferredId));
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateCaptureVoidVoid<TCapture> : IAction, IFunc<Promise>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise, IDelegateRun
        //    {
        //        private readonly Action<TCapture> _callback;
        //        private readonly TCapture _capturedValue;

        //        public bool IsNull
        //        {
        //            [MethodImpl(InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public DelegateCaptureVoidVoid(in TCapture capturedValue, Action<TCapture> callback)
        //        {
        //            _callback = callback;
        //            _capturedValue = capturedValue;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public void Invoke()
        //            => _callback.Invoke(_capturedValue);

        //        [MethodImpl(InlineOption)]
        //        Promise IFunc<Promise>.Invoke()
        //        {
        //            Invoke();
        //            return new Promise();
        //        }

        //        [MethodImpl(InlineOption)]
        //        void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            Invoke();
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        [MethodImpl(InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            Invoke();
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            Invoke();
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            Invoke();
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        [MethodImpl(InlineOption)]
        //        void IDelegateRun.Invoke(PromiseRefBase owner)
        //        {
        //            Invoke();
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateCaptureVoidResult<TCapture, TResult> : IFunc<TResult>, IFunc<Promise<TResult>>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise, IDelegateRun
        //    {
        //        private readonly Func<TCapture, TResult> _callback;
        //        private readonly TCapture _capturedValue;

        //        public bool IsNull
        //        {
        //            [MethodImpl(InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public DelegateCaptureVoidResult(in TCapture capturedValue, Func<TCapture, TResult> callback)
        //        {
        //            _callback = callback;
        //            _capturedValue = capturedValue;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public TResult Invoke()
        //            => _callback.Invoke(_capturedValue);

        //        [MethodImpl(InlineOption)]
        //        Promise<TResult> IFunc<Promise<TResult>>.Invoke()
        //            => new Promise<TResult>(Invoke());

        //        [MethodImpl(InlineOption)]
        //        void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            TResult result = Invoke();
        //            owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        [MethodImpl(InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            TResult result = Invoke();
        //            owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            TResult result = Invoke();
        //            owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            TResult result = Invoke();
        //            owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        [MethodImpl(InlineOption)]
        //        void IDelegateRun.Invoke(PromiseRefBase owner)
        //        {
        //            TResult result = Invoke();
        //            owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateCaptureArgVoid<TCapture, TArg> : IAction<TArg>, IFunc<TArg, Promise>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise
        //    {
        //        private readonly Action<TCapture, TArg> _callback;
        //        private readonly TCapture _capturedValue;

        //        public bool IsNull
        //        {
        //            [MethodImpl(InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public DelegateCaptureArgVoid(in TCapture capturedValue, Action<TCapture, TArg> callback)
        //        {
        //            _callback = callback;
        //            _capturedValue = capturedValue;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public void Invoke(TArg arg)
        //        {
        //            _callback.Invoke(_capturedValue, arg);
        //        }

        //        [MethodImpl(InlineOption)]
        //        Promise IFunc<TArg, Promise>.Invoke(TArg arg)
        //        {
        //            Invoke(arg);
        //            return new Promise();
        //        }

        //        [MethodImpl(InlineOption)]
        //        void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            TArg arg = handler.GetResult<TArg>();
        //            handler.MaybeDispose();
        //            Invoke(arg);
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        [MethodImpl(InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            TArg arg = handler.GetResult<TArg>();
        //            handler.MaybeDispose();
        //            Invoke(arg);
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        private void InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            if (rejectContainer.TryGetValue(out TArg arg))
        //            {
        //                Invoke(arg);
        //                owner.HandleNextInternal(Promise.State.Resolved);
        //            }
        //            else
        //            {
        //                owner.RejectContainer = rejectContainer;
        //                owner.HandleNextInternal(Promise.State.Rejected);
        //            }
        //        }

        //        void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
        //            => InvokeRejecter(rejectContainer, owner);

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            InvokeRejecter(rejectContainer, owner);
        //        }
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateCaptureArgResult<TCapture, TArg, TResult> : IFunc<TArg, TResult>, IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise
        //    {
        //        private readonly Func<TCapture, TArg, TResult> _callback;
        //        private readonly TCapture _capturedValue;

        //        public bool IsNull
        //        {
        //            [MethodImpl(InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public DelegateCaptureArgResult(in TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
        //        {
        //            _callback = callback;
        //            _capturedValue = capturedValue;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public TResult Invoke(TArg arg)
        //            => _callback.Invoke(_capturedValue, arg);

        //        [MethodImpl(InlineOption)]
        //        Promise<TResult> IFunc<TArg, Promise<TResult>>.Invoke(TArg arg)
        //            => new Promise<TResult>(Invoke(arg));

        //        [MethodImpl(InlineOption)]
        //        void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            TArg arg = handler.GetResult<TArg>();
        //            handler.MaybeDispose();
        //            TResult result = Invoke(arg);
        //            owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        [MethodImpl(InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            TArg arg = handler.GetResult<TArg>();
        //            handler.MaybeDispose();
        //            TResult result = Invoke(arg);
        //            owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //            owner.HandleNextInternal(Promise.State.Resolved);
        //        }

        //        private void InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            if (rejectContainer.TryGetValue(out TArg arg))
        //            {
        //                TResult result = Invoke(arg);
        //                owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
        //                owner.HandleNextInternal(Promise.State.Resolved);
        //            }
        //            else
        //            {
        //                owner.RejectContainer = rejectContainer;
        //                owner.HandleNextInternal(Promise.State.Rejected);
        //            }
        //        }

        //        void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
        //            => InvokeRejecter(rejectContainer, owner);

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            InvokeRejecter(rejectContainer, owner);
        //        }
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateCapturePromiseVoidVoid<TCapture> : IFunc<Promise>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise, IDelegateRunPromise
        //    {
        //        private readonly Func<TCapture, Promise> _callback;
        //        private readonly TCapture _capturedValue;

        //        public bool IsNull
        //        {
        //            [MethodImpl(InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public DelegateCapturePromiseVoidVoid(in TCapture capturedValue, Func<TCapture, Promise> callback)
        //        {
        //            _callback = callback;
        //            _capturedValue = capturedValue;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public Promise Invoke()
        //            => _callback.Invoke(_capturedValue);

        //        [MethodImpl(InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            Promise result = Invoke();
        //            owner.WaitFor(result);
        //        }

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            Promise result = Invoke();
        //            owner.WaitFor(result);
        //        }

        //        [MethodImpl(InlineOption)]
        //        void IDelegateRunPromise.Invoke(PromiseRefBase owner)
        //        {
        //            Promise result = Invoke();
        //            owner.WaitFor(result);
        //        }
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateCapturePromiseVoidResult<TCapture, TResult> : IFunc<Promise<TResult>>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise, IDelegateRunPromise
        //    {
        //        private readonly Func<TCapture, Promise<TResult>> _callback;
        //        private readonly TCapture _capturedValue;

        //        public bool IsNull
        //        {
        //            [MethodImpl(InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public DelegateCapturePromiseVoidResult(in TCapture capturedValue, Func<TCapture, Promise<TResult>> callback)
        //        {
        //            _callback = callback;
        //            _capturedValue = capturedValue;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public Promise<TResult> Invoke()
        //            => _callback.Invoke(_capturedValue);

        //        [MethodImpl(InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            Promise<TResult> result = Invoke();
        //            owner.WaitFor(result);
        //        }

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            Promise<TResult> result = Invoke();
        //            owner.WaitFor(result);
        //        }

        //        [MethodImpl(InlineOption)]
        //        void IDelegateRunPromise.Invoke(PromiseRefBase owner)
        //        {
        //            Promise<TResult> result = Invoke();
        //            owner.WaitFor(result);
        //        }
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateCapturePromiseArgVoid<TCapture, TArg> : IFunc<TArg, Promise>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise
        //    {
        //        private readonly Func<TCapture, TArg, Promise> _callback;
        //        private readonly TCapture _capturedValue;

        //        public bool IsNull
        //        {
        //            [MethodImpl(InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public DelegateCapturePromiseArgVoid(in TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
        //        {
        //            _callback = callback;
        //            _capturedValue = capturedValue;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public Promise Invoke(TArg arg)
        //            => _callback.Invoke(_capturedValue, arg);

        //        [MethodImpl(InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            TArg arg = handler.GetResult<TArg>();
        //            handler.MaybeDispose();
        //            Promise result = Invoke(arg);
        //            owner.WaitFor(result);
        //        }

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            if (rejectContainer.TryGetValue(out TArg arg))
        //            {
        //                Promise result = Invoke(arg);
        //                owner.WaitFor(result);
        //            }
        //            else
        //            {
        //                owner.RejectContainer = rejectContainer;
        //                owner.HandleNextInternal(Promise.State.Rejected);
        //            }
        //        }
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateCapturePromiseArgResult<TCapture, TArg, TResult> : IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise
        //    {
        //        private readonly Func<TCapture, TArg, Promise<TResult>> _callback;
        //        private readonly TCapture _capturedValue;

        //        public bool IsNull
        //        {
        //            [MethodImpl(InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public DelegateCapturePromiseArgResult(in TCapture capturedValue, Func<TCapture, TArg, Promise<TResult>> callback)
        //        {
        //            _callback = callback;
        //            _capturedValue = capturedValue;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public Promise<TResult> Invoke(TArg arg)
        //            => _callback.Invoke(_capturedValue, arg);

        //        [MethodImpl(InlineOption)]
        //        void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
        //        {
        //            TArg arg = handler.GetResult<TArg>();
        //            handler.MaybeDispose();
        //            Promise<TResult> result = Invoke(arg);
        //            owner.WaitFor(result);
        //        }

        //        void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
        //        {
        //            handler.MaybeDispose();
        //            if (rejectContainer.TryGetValue(out TArg arg))
        //            {
        //                Promise<TResult> result = Invoke(arg);
        //                owner.WaitFor(result);
        //            }
        //            else
        //            {
        //                owner.RejectContainer = rejectContainer;
        //                owner.HandleNextInternal(Promise.State.Rejected);
        //            }
        //        }
        //    }


#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateContinueCaptureVoidVoid<TCapture> : IAction, IContinuer<VoidResult, VoidResult>
        {
            private readonly Action<TCapture, Promise.ResultContainer> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateContinueCaptureVoidVoid(in TCapture capturedValue, Action<TCapture, Promise.ResultContainer> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public void Invoke()
                => _callback.Invoke(_capturedValue, new Promise.ResultContainer(null, Promise.State.Resolved));

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public VoidResult Invoke(in Promise<VoidResult>.ResultContainer resultContainer)
            {
                _callback.Invoke(_capturedValue, new Promise.ResultContainer(resultContainer));
                return default;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateContinueCaptureVoidResult<TCapture, TResult> : IFunc<TResult>, IContinuer<VoidResult, TResult>
        {
            private readonly Func<TCapture, Promise.ResultContainer, TResult> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateContinueCaptureVoidResult(in TCapture capturedValue, Func<TCapture, Promise.ResultContainer, TResult> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke()
                => _callback.Invoke(_capturedValue, new Promise.ResultContainer(null, Promise.State.Resolved));

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke(in Promise<VoidResult>.ResultContainer resultContainer)
                => _callback.Invoke(_capturedValue, resultContainer);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateContinueCaptureArgVoid<TCapture, TArg> : IAction<TArg>, IContinuer<TArg, VoidResult>
        {
            private readonly Action<TCapture, Promise<TArg>.ResultContainer> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateContinueCaptureArgVoid(in TCapture capturedValue, Action<TCapture, Promise<TArg>.ResultContainer> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public void Invoke(TArg arg)
                => _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public VoidResult Invoke(in Promise<TArg>.ResultContainer resultContainer)
            {
                _callback.Invoke(_capturedValue, resultContainer);
                return default;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateContinueCaptureArgResult<TCapture, TArg, TResult> : IFunc<TArg, TResult>, IContinuer<TArg, TResult>
        {
            private readonly Func<TCapture, Promise<TArg>.ResultContainer, TResult> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateContinueCaptureArgResult(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, TResult> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke(TArg arg)
                => _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke(in Promise<TArg>.ResultContainer resultContainer)
                => _callback.Invoke(_capturedValue, resultContainer);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateContinuePromiseCaptureVoidVoid<TCapture> : IFunc<Promise>, IContinuer<VoidResult, Promise>
        {
            private readonly Func<TCapture, Promise.ResultContainer, Promise> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateContinuePromiseCaptureVoidVoid(in TCapture capturedValue, Func<TCapture, Promise.ResultContainer, Promise> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public Promise Invoke()
                => _callback.Invoke(_capturedValue, new Promise.ResultContainer(null, Promise.State.Resolved));

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public Promise Invoke(in Promise<VoidResult>.ResultContainer resultContainer)
                => _callback.Invoke(_capturedValue, new Promise.ResultContainer(resultContainer));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateContinuePromiseCaptureVoidResult<TCapture, TResult> : IFunc<Promise<TResult>>, IContinuer<VoidResult, Promise<TResult>>
        {
            private readonly Func<TCapture, Promise.ResultContainer, Promise<TResult>> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateContinuePromiseCaptureVoidResult(in TCapture capturedValue, Func<TCapture, Promise.ResultContainer, Promise<TResult>> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke()
                => _callback.Invoke(_capturedValue, new Promise.ResultContainer(null, Promise.State.Resolved));

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke(in Promise<VoidResult>.ResultContainer resultContainer)
                => _callback.Invoke(_capturedValue, new Promise.ResultContainer(resultContainer));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateContinuePromiseCaptureArgVoid<TCapture, TArg> : IFunc<TArg, Promise>, IContinuer<TArg, Promise>
        {
            private readonly Func<TCapture, Promise<TArg>.ResultContainer, Promise> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateContinuePromiseCaptureArgVoid(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, Promise> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public Promise Invoke(TArg arg)
                => _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public Promise Invoke(in Promise<TArg>.ResultContainer resultContainer)
                => _callback.Invoke(_capturedValue, resultContainer);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateContinuePromiseCaptureArgResult<TCapture, TArg, TResult> : IFunc<TArg, Promise<TResult>>, IContinuer<TArg, Promise<TResult>>
        {
            private readonly Func<TCapture, Promise<TArg>.ResultContainer, Promise<TResult>> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateContinuePromiseCaptureArgResult(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, Promise<TResult>> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke(TArg arg)
                => _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));

            [MethodImpl(InlineOption)]
            public bool ShouldInvoke(IRejectContainer rejectContainer, Promise.State state, out bool isCatch)
            {
                isCatch = false;
                return true;
            }

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke(in Promise<TArg>.ResultContainer resultContainer)
                => _callback.Invoke(_capturedValue, resultContainer);
        }


        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateCaptureFinally<TCapture> : IAction
        //    {
        //        private readonly Action<TCapture> _callback;
        //        private readonly TCapture _capturedValue;

        //        public bool IsNull
        //        {
        //            [MethodImpl(InlineOption)]
        //            get => _callback == null;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public DelegateCaptureFinally(in TCapture capturedValue, Action<TCapture> callback)
        //        {
        //            _capturedValue = capturedValue;
        //            _callback = callback;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public void Invoke()
        //            => _callback.Invoke(_capturedValue);
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct DelegateCaptureCancel<TCapture> : IAction
        //    {
        //        private readonly Action<TCapture> _callback;
        //        private readonly TCapture _capturedValue;

        //        [MethodImpl(InlineOption)]
        //        public DelegateCaptureCancel(in TCapture capturedValue, Action<TCapture> callback)
        //        {
        //            _capturedValue = capturedValue;
        //            _callback = callback;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public void Invoke()
        //            => _callback.Invoke(_capturedValue);
        //    }
        //    #endregion



        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct Func2ArgResult<TArg1, TArg2, TResult> : IFunc<TArg1, TArg2, TResult>
        //    {
        //        private readonly Func<TArg1, TArg2, TResult> _callback;

        //        [MethodImpl(InlineOption)]
        //        public Func2ArgResult(Func<TArg1, TArg2, TResult> callback)
        //        {
        //            _callback = callback;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public TResult Invoke(TArg1 arg1, TArg2 arg2)
        //            => _callback.Invoke(arg1, arg2);
        //    }

        //#if !PROTO_PROMISE_DEVELOPER_MODE
        //    [DebuggerNonUserCode, StackTraceHidden]
        //#endif
        //    internal readonly struct Func2ArgResultCapture<TCapture, TArg1, TArg2, TResult> : IFunc<TArg1, TArg2, TResult>
        //    {
        //        private readonly Func<TCapture, TArg1, TArg2, TResult> _callback;
        //        private readonly TCapture _capturedValue;

        //        [MethodImpl(InlineOption)]
        //        public Func2ArgResultCapture(in TCapture capturedValue, Func<TCapture, TArg1, TArg2, TResult> callback)
        //        {
        //            _callback = callback;
        //            _capturedValue = capturedValue;
        //        }

        //        [MethodImpl(InlineOption)]
        //        public TResult Invoke(TArg1 arg1, TArg2 arg2)
        //            => _callback.Invoke(_capturedValue, arg1, arg2);
        //    }
    }
}