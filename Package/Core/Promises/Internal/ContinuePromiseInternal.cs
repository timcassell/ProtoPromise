#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0016 // Use 'throw' expression
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0290 // Use primary constructor

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    // This type allows us to treat `Promise` as `Promise<VoidResult>` to reduce code duplication.
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal readonly struct PromiseWrapper<T>
    {
        internal readonly Internal.PromiseRefBase _ref;
        internal readonly T _result;
        internal readonly short _id;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseWrapper(Internal.PromiseRefBase promise, short promiseId, in T result)
        {
            _ref = promise;
            _result = result;
            _id = promiseId;
        }

        [MethodImpl(Internal.InlineOption)]
        internal PromiseWrapper<T> Duplicate()
        {
            if (_ref == null)
            {
                return this;
            }
            var duplicate = _ref.GetDuplicate(_id);
            return new PromiseWrapper<T>(duplicate, duplicate.Id, default);
        }

        [MethodImpl(Internal.InlineOption)]
        public static implicit operator Promise<T>(in PromiseWrapper<T> promiseWrapper)
            => new Promise<T>(promiseWrapper._ref.UnsafeAs<Internal.PromiseRefBase.PromiseRef<T>>(), promiseWrapper._id, promiseWrapper._result);

        [MethodImpl(Internal.InlineOption)]
        public static implicit operator Promise(in PromiseWrapper<T> promiseWrapper)
            => new Promise(promiseWrapper._ref, promiseWrapper._id);

        [MethodImpl(Internal.InlineOption)]
        public static implicit operator PromiseWrapper<T>(in Promise promise)
            => new PromiseWrapper<T>(promise._ref, promise._id, default);

        [MethodImpl(Internal.InlineOption)]
        public static implicit operator PromiseWrapper<T>(in Promise<T> promise)
            => new PromiseWrapper<T>(promise._ref, promise._id, promise._result);

        [MethodImpl(Internal.InlineOption)]
        public static implicit operator PromiseWrapper<T>(in T result)
            => new PromiseWrapper<T>(null, 0, result);
    }

    // Unity IL2CPP has a maximum nested generic depth, so unfortunately we have to create separate delegate wrappers,
    // so the generic will only nest <TArg> instead of <Promise<TArg>.ResultContainer>
    partial class DelegateWrapper
    {
        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateResultContainerArgVoid<TArg> Create<TArg>(Action<Promise<TArg>.ResultContainer> callback)
            => new Internal.DelegateResultContainerArgVoid<TArg>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateResultContainerArgResult<TArg, TResult> Create<TArg, TResult>(Func<Promise<TArg>.ResultContainer, TResult> callback)
            => new Internal.DelegateResultContainerArgResult<TArg, TResult>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateResultContainerCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(in TCapture capturedValue, Action<TCapture, Promise<TArg>.ResultContainer> callback)
            => new Internal.DelegateResultContainerCaptureArgVoid<TCapture, TArg>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateResultContainerCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, TResult> callback)
            => new Internal.DelegateResultContainerCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateResultContainerArgVoid<TArg> Create<TArg>(Func<Promise<TArg>.ResultContainer, Promise> callback)
            => new Internal.AsyncDelegateResultContainerArgVoid<TArg>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateResultContainerArgResult<TArg, TResult> Create<TArg, TResult>(Func<Promise<TArg>.ResultContainer, Promise<TResult>> callback)
            => new Internal.AsyncDelegateResultContainerArgResult<TArg, TResult>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateResultContainerCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, Promise> callback)
            => new Internal.AsyncDelegateResultContainerCaptureArgVoid<TCapture, TArg>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateResultContainerCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, Promise<TResult>> callback)
            => new Internal.AsyncDelegateResultContainerCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);
    }

    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateResultContainerArgVoid<TArg> : IAction<Promise<TArg>.ResultContainer>,
            IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<VoidResult>>
        {
            private readonly Action<Promise<TArg>.ResultContainer> _callback;

            [MethodImpl(InlineOption)]
            public DelegateResultContainerArgVoid(Action<Promise<TArg>.ResultContainer> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public void Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(arg);

            [MethodImpl(InlineOption)]
            PromiseWrapper<VoidResult> IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<VoidResult>>.Invoke(in Promise<TArg>.ResultContainer arg)
            {
                _callback.Invoke(arg);
                return default;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateResultContainerArgResult<TArg, TResult> : IFunc<Promise<TArg>.ResultContainer, TResult>,
            IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
        {
            private readonly Func<Promise<TArg>.ResultContainer, TResult> _callback;

            [MethodImpl(InlineOption)]
            public DelegateResultContainerArgResult(Func<Promise<TArg>.ResultContainer, TResult> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public TResult Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(arg);

            [MethodImpl(InlineOption)]
            PromiseWrapper<TResult> IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>.Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(arg);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateResultContainerCaptureArgVoid<TCapture, TArg> : IAction<Promise<TArg>.ResultContainer>,
            IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<VoidResult>>
        {
            private readonly Action<TCapture, Promise<TArg>.ResultContainer> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateResultContainerCaptureArgVoid(in TCapture capturedValue, Action<TCapture, Promise<TArg>.ResultContainer> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public void Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(_capturedValue, arg);

            [MethodImpl(InlineOption)]
            PromiseWrapper<VoidResult> IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<VoidResult>>.Invoke(in Promise<TArg>.ResultContainer arg)
            {
                _callback.Invoke(_capturedValue, arg);
                return default;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateResultContainerCaptureArgResult<TCapture, TArg, TResult> : IFunc<Promise<TArg>.ResultContainer, TResult>,
            IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
        {
            private readonly Func<TCapture, Promise<TArg>.ResultContainer, TResult> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateResultContainerCaptureArgResult(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, TResult> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(_capturedValue, arg);

            [MethodImpl(InlineOption)]
            PromiseWrapper<TResult> IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>.Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(_capturedValue, arg);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateResultContainerArgVoid<TArg> : IFunc<Promise<TArg>.ResultContainer, Promise>,
            IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<VoidResult>>
        {
            private readonly Func<Promise<TArg>.ResultContainer, Promise> _callback;

            [MethodImpl(InlineOption)]
            public AsyncDelegateResultContainerArgVoid(Func<Promise<TArg>.ResultContainer, Promise> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public Promise Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(arg);

            [MethodImpl(InlineOption)]
            PromiseWrapper<VoidResult> IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<VoidResult>>.Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(arg);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateResultContainerArgResult<TArg, TResult> : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>,
            IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
        {
            private readonly Func<Promise<TArg>.ResultContainer, Promise<TResult>> _callback;

            [MethodImpl(InlineOption)]
            public AsyncDelegateResultContainerArgResult(Func<Promise<TArg>.ResultContainer, Promise<TResult>> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(arg);

            [MethodImpl(InlineOption)]
            PromiseWrapper<TResult> IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>.Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(arg);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateResultContainerCaptureArgVoid<TCapture, TArg> : IFunc<Promise<TArg>.ResultContainer, Promise>,
            IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<VoidResult>>
        {
            private readonly Func<TCapture, Promise<TArg>.ResultContainer, Promise> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public AsyncDelegateResultContainerCaptureArgVoid(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, Promise> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public Promise Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(_capturedValue, arg);

            [MethodImpl(InlineOption)]
            PromiseWrapper<VoidResult> IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<VoidResult>>.Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(_capturedValue, arg);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateResultContainerCaptureArgResult<TCapture, TArg, TResult> : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>,
            IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
        {
            private readonly Func<TCapture, Promise<TArg>.ResultContainer, Promise<TResult>> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public AsyncDelegateResultContainerCaptureArgResult(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, Promise<TResult>> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(_capturedValue, arg);

            [MethodImpl(InlineOption)]
            PromiseWrapper<TResult> IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>.Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(_capturedValue, arg);
        }

        partial class PromiseRefBase
        {
            partial class PromiseWaitPromise<TResult>
            {
                [MethodImpl(InlineOption)]
                protected void InvokeAndAdopt<TArg, TDelegate>(in TArg arg, TDelegate callback, IRejectContainer rejectContainer)
                    where TDelegate : IFunc<TArg, PromiseWrapper<TResult>>
                {
                    Promise.State state;
                    SetCurrentInvoker(this);
                    try
                    {
                        var result = callback.Invoke(arg);
                        ValidateReturn(result._ref, result._id);

                        this.SetPrevious(result._ref);
                        if (result._ref == null)
                        {
                            _result = result._result;
                            state = Promise.State.Resolved;
                        }
                        else
                        {
                            PromiseRefBase promiseSingleAwait = result._ref.AddWaiter(result._id, this, out var previousWaiter);
                            if (previousWaiter == PendingAwaitSentinel.s_instance)
                            {
                                return;
                            }
                            state = VerifyAndGetResultFromComplete(result._ref, promiseSingleAwait);
                        }
                    }
                    catch (RethrowException e)
                    {
                        // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                        if (rejectContainer != null)
                        {
                            RejectContainer = rejectContainer;
                        }
                        else
                        {
                            RejectContainer = CreateRejectContainer(e, int.MinValue, null, this);
                        }
                        state = Promise.State.Rejected;
                    }
                    catch (OperationCanceledException)
                    {
                        state = Promise.State.Canceled;
                    }
                    catch (Exception e)
                    {
                        RejectContainer = CreateRejectContainer(e, int.MinValue, null, this);
                        state = Promise.State.Rejected;
                    }
                    finally
                    {
                        ClearCurrentInvoker();
                    }

                    // We handle next last, so that if the runtime wants to, it can tail-call optimize.
                    // Unfortunately, C# currently doesn't have a way to add the .tail prefix directly. https://github.com/dotnet/csharplang/discussions/8990
                    HandleNextInternal(state);
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private Promise.State VerifyAndGetResultFromComplete(PromiseRefBase completePromise, PromiseRefBase promiseSingleAwait)
                {
                    if (VerifyWaiter(promiseSingleAwait))
                    {
                        completePromise.WaitUntilStateIsNotPending();
                        RejectContainer = completePromise.RejectContainer;
                        completePromise.SuppressRejection = true;
                        _result = completePromise.GetResult<TResult>();
                        var state = completePromise.State;
                        completePromise.MaybeDispose();
                        return state;
                    }

                    var exception = new InvalidReturnException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", string.Empty);
                    RejectContainer = CreateRejectContainer(exception, int.MinValue, null, this);
                    return Promise.State.Rejected;
                }
            }

            // Ideally all of these would simply be `ContinuePromise<TArg, TResult, TDelegate>`, but `Promise.ResultContainer` is not the same type as `Promise<VoidResult>.ResultContainer`.
            // There would be much less code duplication if the runtime supported void in generics, but we work with what's available.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ContinuePromise<TResult, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IFunc<Promise.ResultContainer, PromiseWrapper<TResult>>
            {
                private ContinuePromise() { }

                [MethodImpl(InlineOption)]
                private static ContinuePromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ContinuePromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ContinuePromise<TResult, TDelegate>()
                        : obj.UnsafeAs<ContinuePromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static ContinuePromise<TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);
                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdopt(new Promise.ResultContainer(rejectContainer, state), callback, null);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ContinueWaitPromise<TResult, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IFunc<Promise.ResultContainer, PromiseWrapper<TResult>>
            {
                private ContinueWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static ContinueWaitPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ContinueWaitPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ContinueWaitPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<ContinueWaitPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static ContinueWaitPromise<TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    if (!_firstContinue)
                    {
                        HandleSelf(handler, state);
                        return;
                    }
                    _firstContinue = false;

                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdopt(new Promise.ResultContainer(rejectContainer, state), callback, null);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ContinuePromise<TArg, TResult, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
            {
                private ContinuePromise() { }

                [MethodImpl(InlineOption)]
                private static ContinuePromise<TArg, TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ContinuePromise<TArg, TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ContinuePromise<TArg, TResult, TDelegate>()
                        : obj.UnsafeAs<ContinuePromise<TArg, TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static ContinuePromise<TArg, TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);
                    var rejectContainer = handler.RejectContainer;
                    var arg = handler.GetResult<TArg>();
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdopt(new Promise<TArg>.ResultContainer(arg, rejectContainer, state), callback, null);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ContinueWaitPromise<TArg, TResult, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
            {
                private ContinueWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static ContinueWaitPromise<TArg, TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ContinueWaitPromise<TArg, TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ContinueWaitPromise<TArg, TResult, TDelegate>()
                        : obj.UnsafeAs<ContinueWaitPromise<TArg, TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static ContinueWaitPromise<TArg, TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    if (!_firstContinue)
                    {
                        HandleSelf(handler, state);
                        return;
                    }
                    _firstContinue = false;

                    var rejectContainer = handler.RejectContainer;
                    var arg = handler.GetResult<TArg>();
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdopt(new Promise<TArg>.ResultContainer(arg, rejectContainer, state), callback, null);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelableContinuePromise<TResult, TDelegate> : PromiseWaitPromise<TResult>, ICancelable
                where TDelegate : IFunc<Promise.ResultContainer, PromiseWrapper<TResult>>
            {
                private CancelableContinuePromise() { }

                [MethodImpl(InlineOption)]
                private static CancelableContinuePromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelableContinuePromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelableContinuePromise<TResult, TDelegate>()
                        : obj.UnsafeAs<CancelableContinuePromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelableContinuePromise<TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    promise._cancelationHelper.Reset();
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default;
                    _callback = default;
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    if (!_cancelationHelper.TrySetCompleted())
                    {
                        handler.MaybeReportUnhandledAndDispose(state);
                        MaybeDispose();
                        return;
                    }

                    _cancelationHelper.UnregisterAndWait();
                    _cancelationHelper.ReleaseOne();

                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdopt(new Promise.ResultContainer(rejectContainer, state), callback, null);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelableContinueWaitPromise<TResult, TDelegate> : PromiseWaitPromise<TResult>, ICancelable
                where TDelegate : IFunc<Promise.ResultContainer, PromiseWrapper<TResult>>
            {
                private CancelableContinueWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static CancelableContinueWaitPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelableContinueWaitPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelableContinueWaitPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<CancelableContinueWaitPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelableContinueWaitPromise<TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    promise._cancelationHelper.Reset();
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default;
                    _callback = default;
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    if (!_firstContinue)
                    {
                        HandleSelf(handler, state);
                        return;
                    }
                    _firstContinue = false;

                    if (!_cancelationHelper.TrySetCompleted())
                    {
                        handler.MaybeReportUnhandledAndDispose(state);
                        MaybeDispose();
                        return;
                    }

                    _cancelationHelper.UnregisterAndWait();
                    _cancelationHelper.ReleaseOne();

                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdopt(new Promise.ResultContainer(rejectContainer, state), callback, null);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelableContinuePromise<TArg, TResult, TDelegate> : PromiseWaitPromise<TResult>, ICancelable
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
            {
                private CancelableContinuePromise() { }

                [MethodImpl(InlineOption)]
                private static CancelableContinuePromise<TArg, TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelableContinuePromise<TArg, TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelableContinuePromise<TArg, TResult, TDelegate>()
                        : obj.UnsafeAs<CancelableContinuePromise<TArg, TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelableContinuePromise<TArg, TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    promise._cancelationHelper.Reset();
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default;
                    _callback = default;
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    if (!_cancelationHelper.TrySetCompleted())
                    {
                        handler.MaybeReportUnhandledAndDispose(state);
                        MaybeDispose();
                        return;
                    }

                    _cancelationHelper.UnregisterAndWait();
                    _cancelationHelper.ReleaseOne();

                    var rejectContainer = handler.RejectContainer;
                    var arg = handler.GetResult<TArg>();
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdopt(new Promise<TArg>.ResultContainer(arg, rejectContainer, state), callback, null);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelableContinueWaitPromise<TArg, TResult, TDelegate> : PromiseWaitPromise<TResult>, ICancelable
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
            {
                private CancelableContinueWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static CancelableContinueWaitPromise<TArg, TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelableContinueWaitPromise<TArg, TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelableContinueWaitPromise<TArg, TResult, TDelegate>()
                        : obj.UnsafeAs<CancelableContinueWaitPromise<TArg, TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelableContinueWaitPromise<TArg, TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    promise._cancelationHelper.Reset();
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default;
                    _callback = default;
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    if (!_firstContinue)
                    {
                        HandleSelf(handler, state);
                        return;
                    }
                    _firstContinue = false;

                    if (!_cancelationHelper.TrySetCompleted())
                    {
                        handler.MaybeReportUnhandledAndDispose(state);
                        MaybeDispose();
                        return;
                    }

                    _cancelationHelper.UnregisterAndWait();
                    _cancelationHelper.ReleaseOne();

                    var rejectContainer = handler.RejectContainer;
                    var arg = handler.GetResult<TArg>();
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdopt(new Promise<TArg>.ResultContainer(arg, rejectContainer, state), callback, null);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                }
            }
        } // class PromiseRefBase
    } // class Internal
} // namespace Proto.Promises