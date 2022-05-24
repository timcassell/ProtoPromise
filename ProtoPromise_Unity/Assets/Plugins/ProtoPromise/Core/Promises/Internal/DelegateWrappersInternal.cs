#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal static class DelegateWrapper
            {
                // These static functions help with the implementation so we don't need to type the generics in every method.

                [MethodImpl(InlineOption)]
                public static DelegateResolvePassthrough<TResult> CreatePassthrough<TResult>()
                {
                    return new DelegateResolvePassthrough<TResult>(true);
                }

                [MethodImpl(InlineOption)]
                public static Delegate<VoidResult, VoidResult> Create(Action callback)
                {
                    return new Delegate<VoidResult, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static Delegate<VoidResult, TResult> Create<TResult>(Func<TResult> callback)
                {
                    return new Delegate<VoidResult, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static Delegate<TArg, VoidResult> Create<TArg>(Action<TArg> callback)
                {
                    return new Delegate<TArg, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static Delegate<TArg, TResult> Create<TArg, TResult>(Func<TArg, TResult> callback)
                {
                    return new Delegate<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegatePromise<VoidResult, VoidResult> Create(Func<Promise> callback)
                {
                    return new DelegatePromise<VoidResult, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegatePromise<VoidResult, TResult> Create<TResult>(Func<Promise<TResult>> callback)
                {
                    return new DelegatePromise<VoidResult, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegatePromise<TArg, VoidResult> Create<TArg>(Func<TArg, Promise> callback)
                {
                    return new DelegatePromise<TArg, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegatePromise<TArg, TResult> Create<TArg, TResult>(Func<TArg, Promise<TResult>> callback)
                {
                    return new DelegatePromise<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCapture<TCapture, VoidResult, VoidResult> Create<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture> callback)
                {
                    return new DelegateCapture<TCapture, VoidResult, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCapture<TCapture, VoidResult, TResult> Create<TCapture, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TResult> callback)
                {
                    return new DelegateCapture<TCapture, VoidResult, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCapture<TCapture, TArg, VoidResult> Create<TCapture, TArg>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture, TArg> callback)
                {
                    return new DelegateCapture<TCapture, TArg, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCapture<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
                {
                    return new DelegateCapture<TCapture, TArg, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegatePromiseCapture<TCapture, VoidResult, VoidResult> Create<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, Promise> callback)
                {
                    return new DelegatePromiseCapture<TCapture, VoidResult, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegatePromiseCapture<TCapture, VoidResult, TResult> Create<TCapture, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, Promise<TResult>> callback)
                {
                    return new DelegatePromiseCapture<TCapture, VoidResult, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegatePromiseCapture<TCapture, TArg, VoidResult> Create<TCapture, TArg>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
                {
                    return new DelegatePromiseCapture<TCapture, TArg, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegatePromiseCapture<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TArg, Promise<TResult>> callback)
                {
                    return new DelegatePromiseCapture<TCapture, TArg, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinue<VoidResult, VoidResult> Create(Promise.ContinueAction callback)
                {
                    return new DelegateContinue<VoidResult, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinue<VoidResult, TResult> Create<TResult>(Promise.ContinueFunc<TResult> callback)
                {
                    return new DelegateContinue<VoidResult, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinue<TArg, VoidResult> Create<TArg>(Promise<TArg>.ContinueAction callback)
                {
                    return new DelegateContinue<TArg, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinue<TArg, TResult> Create<TArg, TResult>(Promise<TArg>.ContinueFunc<TResult> callback)
                {
                    return new DelegateContinue<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinueCapture<TCapture, VoidResult, VoidResult> Create<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueAction<TCapture> callback)
                {
                    return new DelegateContinueCapture<TCapture, VoidResult, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinueCapture<TCapture, VoidResult, TResult> Create<TCapture, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueFunc<TCapture, TResult> callback)
                {
                    return new DelegateContinueCapture<TCapture, VoidResult, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinueCapture<TCapture, TArg, VoidResult> Create<TCapture, TArg>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueAction<TCapture> callback)
                {
                    return new DelegateContinueCapture<TCapture, TArg, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinueCapture<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, TResult> callback)
                {
                    return new DelegateContinueCapture<TCapture, TArg, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromise<VoidResult, VoidResult> Create(Promise.ContinueFunc<Promise> callback)
                {
                    return new DelegateContinuePromise<VoidResult, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromise<VoidResult, TResult> Create<TResult>(Promise.ContinueFunc<Promise<TResult>> callback)
                {
                    return new DelegateContinuePromise<VoidResult, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromise<TArg, VoidResult> Create<TArg>(Promise<TArg>.ContinueFunc<Promise> callback)
                {
                    return new DelegateContinuePromise<TArg, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromise<TArg, TResult> Create<TArg, TResult>(Promise<TArg>.ContinueFunc<Promise<TResult>> callback)
                {
                    return new DelegateContinuePromise<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromiseCapture<TCapture, VoidResult, VoidResult> Create<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise> callback)
                {
                    return new DelegateContinuePromiseCapture<TCapture, VoidResult, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromiseCapture<TCapture, VoidResult, TResult> Create<TCapture, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise<TResult>> callback)
                {
                    return new DelegateContinuePromiseCapture<TCapture, VoidResult, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromiseCapture<TCapture, TArg, VoidResult> Create<TCapture, TArg>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise> callback)
                {
                    return new DelegateContinuePromiseCapture<TCapture, TArg, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromiseCapture<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise<TResult>> callback)
                {
                    return new DelegateContinuePromiseCapture<TCapture, TArg, TResult>(capturedValue, callback);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateResolvePassthrough<TResult> : IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise
            {
                private readonly bool _isActive;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return !_isActive; }
                }

                [MethodImpl(InlineOption)]
                internal DelegateResolvePassthrough(bool isActive)
                {
                    _isActive = isActive;
                }

                private void Handle(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    // null check is same as typeof(TValue).IsValueType, but is actually optimized away by the JIT. This prevents the type check when TValue is a reference type.
                    if (null != default(TResult) && typeof(TResult) == typeof(VoidResult))
                    {
                        handler.SuppressRejection = true;
                        owner._rejectContainer = handler._rejectContainer;
                        // Very important, write State must come after write _result and _valueContainer. This is a volatile write, so we don't need a full memory barrier.
                        // State is checked for completion, and if it is read not pending on another thread, _result and _valueContainer must have already been written so the other thread can read them.
                        owner.State = handler.State;
                        handler.MaybeDispose();
                        handler = owner;
                        nextHandler = owner.TakeOrHandleNextWaiter();
                    }
                    else
                    {
                        owner.UnsafeAs<PromiseRef<TResult>>().HandleSelf(ref handler, out nextHandler);
                    }
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    Handle(ref handler, out nextHandler, owner);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    Handle(ref handler, out nextHandler, owner);
                }
            }

            #region Regular Delegates
            // NOTE: IDelegate<TArg, TResult> is cleaner, but doesn't play well with AOT compilers (IL2CPP).

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct Delegate<TArg, TResult> : IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise
            {
                internal readonly Delegate _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public Delegate(Delegate callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(TArg arg)
                {
                    // JIT constant-optimizes these checks away.
                    bool isVoidArg = null != default(TArg) && typeof(TArg) == typeof(VoidResult);
                    bool isVoidResult = null != default(TResult) && typeof(TResult) == typeof(VoidResult);
                    if (isVoidResult)
                    {
                        if (isVoidArg)
                        {
                            _callback.UnsafeAs<Action>().Invoke();
                        }
                        else
                        {
                            _callback.UnsafeAs<Action<TArg>>().Invoke(arg);
                        }
                        return default(TResult);
                    }
                    if (isVoidArg)
                    {
                        return _callback.UnsafeAs<Func<TResult>>().Invoke();
                    }
                    return _callback.UnsafeAs<Func<TArg, TResult>>().Invoke(arg);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    TResult result = Invoke(arg);
                    handler = owner;
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    nextHandler = owner.TakeOrHandleNextWaiter();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    TResult result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(CreateResolved(result, 0), ref handler, out nextHandler);
                }

                void IDelegateReject.InvokeRejecter(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    TArg arg;
                    if (handler.TryGetRejectValue(out arg))
                    {
                        TResult result = Invoke(arg);
                        handler.MaybeDispose();
                        handler = owner;
                        owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                        nextHandler = owner.TakeOrHandleNextWaiter();
                    }
                    else
                    {
                        owner.HandleIncompatibleRejection(ref handler, out nextHandler);
                    }
                }

                void IDelegateRejectPromise.InvokeRejecter(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    TArg arg;
                    if (handler.TryGetRejectValue(out arg))
                    {
                        TResult result = Invoke(arg);
                        MaybeDisposePreviousBeforeSecondWait(handler);
                        owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(CreateResolved(result, 0), ref handler, out nextHandler);
                    }
                    else
                    {
                        owner.HandleIncompatibleRejection(ref handler, out nextHandler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegatePromise<TArg, TResult> : IDelegateResolveOrCancelPromise, IDelegateRejectPromise
            {
                internal readonly Delegate _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegatePromise(Delegate callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public Promise<TResult> Invoke(TArg arg)
                {
                    // JIT constant-optimizes these checks away.
                    bool isVoidArg = null != default(TArg) && typeof(TArg) == typeof(VoidResult);
                    bool isVoidResult = null != default(TResult) && typeof(TResult) == typeof(VoidResult);
                    if (isVoidResult)
                    {
                        Promise promise;
                        if (isVoidArg)
                        {
                            promise = _callback.UnsafeAs<Func<Promise>>().Invoke();
                        }
                        else
                        {
                            promise = _callback.UnsafeAs<Func<TArg, Promise>>().Invoke(arg);
                        }
                        return new Promise<TResult>(promise._target._ref, promise._target.Id, promise._target.Depth);
                    }
                    if (isVoidArg)
                    {
                        return _callback.UnsafeAs<Func<Promise<TResult>>>().Invoke();
                    }
                    return _callback.UnsafeAs<Func<TArg, Promise<TResult>>>().Invoke(arg);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    Promise<TResult> result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, ref handler, out nextHandler);
                }

                void IDelegateRejectPromise.InvokeRejecter(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    TArg arg;
                    if (handler.TryGetRejectValue(out arg))
                    {
                        Promise<TResult> result = Invoke(arg);
                        MaybeDisposePreviousBeforeSecondWait(handler);
                        owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, ref handler, out nextHandler);
                    }
                    else
                    {
                        owner.HandleIncompatibleRejection(ref handler, out nextHandler);
                    }
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinue<TArg, TResult> : IDelegateContinue
            {
                private readonly Delegate _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinue(Delegate callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(TArg arg)
                {
                    // JIT constant-optimizes these checks away.
                    bool isVoidArg = null != default(TArg) && typeof(TArg) == typeof(VoidResult);
                    bool isVoidResult = null != default(TResult) && typeof(TResult) == typeof(VoidResult);
                    if (isVoidResult)
                    {
                        if (isVoidArg)
                        {
                            _callback.UnsafeAs<Promise.ContinueAction>().Invoke(new Promise.ResultContainer(null));
                        }
                        else
                        {
                            _callback.UnsafeAs<Promise<TArg>.ContinueAction>().Invoke(new Promise<TArg>.ResultContainer(arg));
                        }
                        return default(TResult);
                    }
                    if (isVoidArg)
                    {
                        return _callback.UnsafeAs<Promise.ContinueFunc<TResult>>().Invoke(new Promise.ResultContainer(null));
                    }
                    return _callback.UnsafeAs<Promise<TArg>.ContinueFunc<TResult>>().Invoke(new Promise<TArg>.ResultContainer(arg));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    // JIT constant-optimizes these checks away.
                    bool isVoidArg = null != default(TArg) && typeof(TArg) == typeof(VoidResult);
                    bool isVoidResult = null != default(TResult) && typeof(TResult) == typeof(VoidResult);
                    if (isVoidResult)
                    {
                        if (isVoidArg)
                        {
                            _callback.UnsafeAs<Promise.ContinueAction>().Invoke(new Promise.ResultContainer(handler));
                        }
                        else
                        {
                            _callback.UnsafeAs<Promise<TArg>.ContinueAction>().Invoke(new Promise<TArg>.ResultContainer(handler));
                        }
                        owner.State = Promise.State.Resolved;
                    }
                    else
                    {
                        TResult result;
                        if (isVoidArg)
                        {
                            result = _callback.UnsafeAs<Promise.ContinueFunc<TResult>>().Invoke(new Promise.ResultContainer(handler));
                        }
                        else
                        {
                            result = _callback.UnsafeAs<Promise<TArg>.ContinueFunc<TResult>>().Invoke(new Promise<TArg>.ResultContainer(handler));
                        }
                        owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    }
                    handler.MaybeDispose();
                    handler = owner;
                    nextHandler = owner.TakeOrHandleNextWaiter();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinuePromise<TArg, TResult> : IDelegateContinuePromise
            {
                private readonly Delegate _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinuePromise(Delegate callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public Promise<TResult> Invoke(TArg arg)
                {
                    // JIT constant-optimizes these checks away.
                    bool isVoidArg = null != default(TArg) && typeof(TArg) == typeof(VoidResult);
                    bool isVoidResult = null != default(TResult) && typeof(TResult) == typeof(VoidResult);
                    if (isVoidResult)
                    {
                        Promise promise;
                        if (isVoidArg)
                        {
                            promise = _callback.UnsafeAs<Promise.ContinueFunc<Promise>>().Invoke(new Promise.ResultContainer(null));
                        }
                        else
                        {
                            promise = _callback.UnsafeAs<Promise<TArg>.ContinueFunc<Promise>>().Invoke(new Promise<TArg>.ResultContainer(arg));
                        }
                        return new Promise<TResult>(promise._target._ref, promise._target.Id, promise._target.Depth);
                    }
                    if (isVoidArg)
                    {
                        return _callback.UnsafeAs<Promise.ContinueFunc<Promise<TResult>>>().Invoke(new Promise.ResultContainer(null));
                    }
                    return _callback.UnsafeAs<Promise<TArg>.ContinueFunc<Promise<TResult>>>().Invoke(new Promise<TArg>.ResultContainer(arg));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    // JIT constant-optimizes these checks away.
                    bool isVoidArg = null != default(TArg) && typeof(TArg) == typeof(VoidResult);
                    bool isVoidResult = null != default(TResult) && typeof(TResult) == typeof(VoidResult);
                    Promise<TResult> result;
                    if (isVoidResult)
                    {
                        Promise promise;
                        if (isVoidArg)
                        {
                            promise = _callback.UnsafeAs<Promise.ContinueFunc<Promise>>().Invoke(new Promise.ResultContainer(handler));
                        }
                        else
                        {
                            promise = _callback.UnsafeAs<Promise<TArg>.ContinueFunc<Promise>>().Invoke(new Promise<TArg>.ResultContainer(handler));
                        }
                        result = new Promise<TResult>(promise._target._ref, promise._target.Id, promise._target.Depth);
                    }
                    else
                    {
                        if (isVoidArg)
                        {
                            result = _callback.UnsafeAs<Promise.ContinueFunc<Promise<TResult>>>().Invoke(new Promise.ResultContainer(handler));
                        }
                        else
                        {
                            result = _callback.UnsafeAs<Promise<TArg>.ContinueFunc<Promise<TResult>>>().Invoke(new Promise<TArg>.ResultContainer(handler));
                        }
                    }
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, ref handler, out nextHandler);
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateFinally : IDelegateSimple
            {
                private readonly Action _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateFinally(Action callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                {
                    _callback.Invoke();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCancel : IDelegateSimple
            {
                private readonly Action _callback;

                [MethodImpl(InlineOption)]
                public DelegateCancel(Action callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                {
                    _callback.Invoke();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateProgress : IProgress<float>
            {
                private readonly Action<float> _callback;

                [MethodImpl(InlineOption)]
                public DelegateProgress(Action<float> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Report(float value)
                {
                    _callback.Invoke(value);
                }
            }
            #endregion

            #region Delegates with capture value
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCapture<TCapture, TArg, TResult> : IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise
            {
                private readonly Delegate _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCapture(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Delegate callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(TArg arg)
                {
                    // JIT constant-optimizes these checks away.
                    bool isVoidCapture = null != default(TCapture) && typeof(TCapture) == typeof(VoidResult);
                    if (isVoidCapture)
                    {
                        return new Delegate<TArg, TResult>(_callback).Invoke(arg);
                    }

                    bool isVoidArg = null != default(TArg) && typeof(TArg) == typeof(VoidResult);
                    bool isVoidResult = null != default(TResult) && typeof(TResult) == typeof(VoidResult);
                    if (isVoidResult)
                    {
                        if (isVoidArg)
                        {
                            _callback.UnsafeAs<Action<TCapture>>().Invoke(_capturedValue);
                        }
                        else
                        {
                            _callback.UnsafeAs<Action<TCapture, TArg>>().Invoke(_capturedValue, arg);
                        }
                        return default(TResult);
                    }
                    if (isVoidArg)
                    {
                        return _callback.UnsafeAs<Func<TCapture, TResult>>().Invoke(_capturedValue);
                    }
                    return _callback.UnsafeAs<Func<TCapture, TArg, TResult>>().Invoke(_capturedValue, arg);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    handler = owner;
                    TResult result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    nextHandler = owner.TakeOrHandleNextWaiter();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    TResult result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(CreateResolved(result, 0), ref handler, out nextHandler);
                }

                void IDelegateReject.InvokeRejecter(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    TArg arg;
                    if (handler.TryGetRejectValue(out arg))
                    {
                        TResult result = Invoke(arg);
                        handler.MaybeDispose();
                        handler = owner;
                        owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                        nextHandler = owner.TakeOrHandleNextWaiter();
                    }
                    else
                    {
                        owner.HandleIncompatibleRejection(ref handler, out nextHandler);
                    }
                }

                void IDelegateRejectPromise.InvokeRejecter(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    TArg arg;
                    if (handler.TryGetRejectValue(out arg))
                    {
                        TResult result = Invoke(arg);
                        MaybeDisposePreviousBeforeSecondWait(handler);
                        owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(CreateResolved(result, 0), ref handler, out nextHandler);
                    }
                    else
                    {
                        owner.HandleIncompatibleRejection(ref handler, out nextHandler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegatePromiseCapture<TCapture, TArg, TResult> : IDelegateResolveOrCancelPromise, IDelegateRejectPromise
            {
                private readonly Delegate _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegatePromiseCapture(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Delegate callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public Promise<TResult> Invoke(TArg arg)
                {
                    // JIT constant-optimizes these checks away.
                    bool isVoidCapture = null != default(TCapture) && typeof(TCapture) == typeof(VoidResult);
                    if (isVoidCapture)
                    {
                        return new DelegatePromise<TArg, TResult>(_callback).Invoke(arg);
                    }

                    bool isVoidArg = null != default(TArg) && typeof(TArg) == typeof(VoidResult);
                    bool isVoidResult = null != default(TResult) && typeof(TResult) == typeof(VoidResult);
                    if (isVoidResult)
                    {
                        Promise promise;
                        if (isVoidArg)
                        {
                            promise = _callback.UnsafeAs<Func<TCapture, Promise>>().Invoke(_capturedValue);
                        }
                        else
                        {
                            promise = _callback.UnsafeAs<Func<TCapture, TArg, Promise>>().Invoke(_capturedValue, arg);
                        }
                        return new Promise<TResult>(promise._target._ref, promise._target.Id, promise._target.Depth);
                    }
                    if (isVoidArg)
                    {
                        return _callback.UnsafeAs<Func<TCapture, Promise<TResult>>>().Invoke(_capturedValue);
                    }
                    return _callback.UnsafeAs<Func<TCapture, TArg, Promise<TResult>>>().Invoke(_capturedValue, arg);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    Promise<TResult> result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, ref handler, out nextHandler);
                }

                void IDelegateRejectPromise.InvokeRejecter(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    TArg arg;
                    if (handler.TryGetRejectValue(out arg))
                    {
                        Promise<TResult> result = Invoke(arg);
                        MaybeDisposePreviousBeforeSecondWait(handler);
                        owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, ref handler, out nextHandler);
                    }
                    else
                    {
                        owner.HandleIncompatibleRejection(ref handler, out nextHandler);
                    }
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCapture<TCapture, TArg, TResult> : IDelegateContinue
            {
                private readonly Delegate _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueCapture(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Delegate callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(TArg arg)
                {
                    // JIT constant-optimizes these checks away.
                    bool isVoidArg = null != default(TArg) && typeof(TArg) == typeof(VoidResult);
                    bool isVoidResult = null != default(TResult) && typeof(TResult) == typeof(VoidResult);
                    if (isVoidResult)
                    {
                        if (isVoidArg)
                        {
                            _callback.UnsafeAs<Promise.ContinueAction<TCapture>>().Invoke(_capturedValue, new Promise.ResultContainer(null));
                        }
                        else
                        {
                            _callback.UnsafeAs<Promise<TArg>.ContinueAction<TCapture>>().Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg));
                        }
                        return default(TResult);
                    }
                    if (isVoidArg)
                    {
                        return _callback.UnsafeAs<Promise.ContinueFunc<TCapture, TResult>>().Invoke(_capturedValue, new Promise.ResultContainer(null));
                    }
                    return _callback.UnsafeAs<Promise<TArg>.ContinueFunc<TCapture, TResult>>().Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    // JIT constant-optimizes these checks away.
                    bool isVoidArg = null != default(TArg) && typeof(TArg) == typeof(VoidResult);
                    bool isVoidResult = null != default(TResult) && typeof(TResult) == typeof(VoidResult);
                    if (isVoidResult)
                    {
                        if (isVoidArg)
                        {
                            _callback.UnsafeAs<Promise.ContinueAction<TCapture>>().Invoke(_capturedValue, new Promise.ResultContainer(handler));
                        }
                        else
                        {
                            _callback.UnsafeAs<Promise<TArg>.ContinueAction<TCapture>>().Invoke(_capturedValue, new Promise<TArg>.ResultContainer(handler));
                        }
                        owner.State = Promise.State.Resolved;
                    }
                    else
                    {
                        TResult result;
                        if (isVoidArg)
                        {
                            result = _callback.UnsafeAs<Promise.ContinueFunc<TCapture, TResult>>().Invoke(_capturedValue, new Promise.ResultContainer(handler));
                        }
                        else
                        {
                            result = _callback.UnsafeAs<Promise<TArg>.ContinueFunc<TCapture, TResult>>().Invoke(_capturedValue, new Promise<TArg>.ResultContainer(handler));
                        }
                        owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    }
                    handler.MaybeDispose();
                    handler = owner;
                    nextHandler = owner.TakeOrHandleNextWaiter();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinuePromiseCapture<TCapture, TArg, TResult> : IDelegateContinuePromise
            {
                private readonly Delegate _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinuePromiseCapture(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Delegate callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public Promise<TResult> Invoke(TArg arg)
                {
                    // JIT constant-optimizes these checks away.
                    bool isVoidArg = null != default(TArg) && typeof(TArg) == typeof(VoidResult);
                    bool isVoidResult = null != default(TResult) && typeof(TResult) == typeof(VoidResult);
                    if (isVoidResult)
                    {
                        Promise promise;
                        if (isVoidArg)
                        {
                            promise = _callback.UnsafeAs<Promise.ContinueFunc<TCapture, Promise>>().Invoke(_capturedValue, new Promise.ResultContainer(null));
                        }
                        else
                        {
                            promise = _callback.UnsafeAs<Promise<TArg>.ContinueFunc<TCapture, Promise>>().Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg));
                        }
                        return new Promise<TResult>(promise._target._ref, promise._target.Id, promise._target.Depth);
                    }
                    if (isVoidArg)
                    {
                        return _callback.UnsafeAs<Promise.ContinueFunc<TCapture, Promise<TResult>>>().Invoke(_capturedValue, new Promise.ResultContainer(null));
                    }
                    return _callback.UnsafeAs<Promise<TArg>.ContinueFunc<TCapture, Promise<TResult>>>().Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner)
                {
                    // JIT constant-optimizes these checks away.
                    bool isVoidArg = null != default(TArg) && typeof(TArg) == typeof(VoidResult);
                    bool isVoidResult = null != default(TResult) && typeof(TResult) == typeof(VoidResult);
                    Promise<TResult> result;
                    if (isVoidResult)
                    {
                        Promise promise;
                        if (isVoidArg)
                        {
                            promise = _callback.UnsafeAs<Promise.ContinueFunc<TCapture, Promise>>().Invoke(_capturedValue, new Promise.ResultContainer(handler));
                        }
                        else
                        {
                            promise = _callback.UnsafeAs<Promise<TArg>.ContinueFunc<TCapture, Promise>>().Invoke(_capturedValue, new Promise<TArg>.ResultContainer(handler));
                        }
                        result = new Promise<TResult>(promise._target._ref, promise._target.Id, promise._target.Depth);
                    }
                    else
                    {
                        if (isVoidArg)
                        {
                            result = _callback.UnsafeAs<Promise.ContinueFunc<TCapture, Promise<TResult>>>().Invoke(_capturedValue, new Promise.ResultContainer(handler));
                        }
                        else
                        {
                            result = _callback.UnsafeAs<Promise<TArg>.ContinueFunc<TCapture, Promise<TResult>>>().Invoke(_capturedValue, new Promise<TArg>.ResultContainer(handler));
                        }
                    }
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, ref handler, out nextHandler);
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureFinally<TCapture> : IDelegateSimple
            {
                private readonly Action<TCapture> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureFinally(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                {
                    _callback.Invoke(_capturedValue);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureCancel<TCapture> : IDelegateSimple
            {
                private readonly Action<TCapture> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateCaptureCancel(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                {
                    _callback.Invoke(_capturedValue);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureProgress<TCapture> : IProgress<float>
            {
                private readonly Action<TCapture, float> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateCaptureProgress(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture, float> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Report(float value)
                {
                    _callback.Invoke(_capturedValue, value);
                }
            }
            #endregion
        }
    }
}