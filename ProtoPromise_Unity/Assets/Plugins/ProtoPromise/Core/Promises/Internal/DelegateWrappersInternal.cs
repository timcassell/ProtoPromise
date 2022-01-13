﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
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
        partial class PromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal static class DelegateWrapper
            {
                // These static functions help with the implementation so we don't need to type the generics in every method.

                [MethodImpl(InlineOption)]
                internal static DelegateResolvePassthrough CreatePassthrough()
                {
                    return new DelegateResolvePassthrough(true);
                }

                [MethodImpl(InlineOption)]
                internal static Delegate<VoidResult, VoidResult> Create(Action callback)
                {
                    return new Delegate<VoidResult, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static Delegate<VoidResult, TResult> Create<TResult>(Func<TResult> callback)
                {
                    return new Delegate<VoidResult, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static Delegate<TArg, VoidResult> Create<TArg>(Action<TArg> callback)
                {
                    return new Delegate<TArg, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static Delegate<TArg, TResult> Create<TArg, TResult>(Func<TArg, TResult> callback)
                {
                    return new Delegate<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegatePromise<VoidResult, VoidResult> Create(Func<Promise> callback)
                {
                    return new DelegatePromise<VoidResult, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegatePromise<VoidResult, TResult> Create<TResult>(Func<Promise<TResult>> callback)
                {
                    return new DelegatePromise<VoidResult, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegatePromise<TArg, VoidResult> Create<TArg>(Func<TArg, Promise> callback)
                {
                    return new DelegatePromise<TArg, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegatePromise<TArg, TResult> Create<TArg, TResult>(Func<TArg, Promise<TResult>> callback)
                {
                    return new DelegatePromise<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateCapture<TCapture, VoidResult, VoidResult> Create<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture> callback)
                {
                    return new DelegateCapture<TCapture, VoidResult, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateCapture<TCapture, VoidResult, TResult> Create<TCapture, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TResult> callback)
                {
                    return new DelegateCapture<TCapture, VoidResult, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateCapture<TCapture, TArg, VoidResult> Create<TCapture, TArg>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture, TArg> callback)
                {
                    return new DelegateCapture<TCapture, TArg, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateCapture<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
                {
                    return new DelegateCapture<TCapture, TArg, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegatePromiseCapture<TCapture, VoidResult, VoidResult> Create<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, Promise> callback)
                {
                    return new DelegatePromiseCapture<TCapture, VoidResult, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegatePromiseCapture<TCapture, VoidResult, TResult> Create<TCapture, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, Promise<TResult>> callback)
                {
                    return new DelegatePromiseCapture<TCapture, VoidResult, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegatePromiseCapture<TCapture, TArg, VoidResult> Create<TCapture, TArg>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
                {
                    return new DelegatePromiseCapture<TCapture, TArg, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegatePromiseCapture<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TArg, Promise<TResult>> callback)
                {
                    return new DelegatePromiseCapture<TCapture, TArg, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinue<VoidResult, VoidResult> Create(Promise.ContinueAction callback)
                {
                    return new DelegateContinue<VoidResult, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinue<VoidResult, TResult> Create<TResult>(Promise.ContinueFunc<TResult> callback)
                {
                    return new DelegateContinue<VoidResult, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinue<TArg, VoidResult> Create<TArg>(Promise<TArg>.ContinueAction callback)
                {
                    return new DelegateContinue<TArg, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinue<TArg, TResult> Create<TArg, TResult>(Promise<TArg>.ContinueFunc<TResult> callback)
                {
                    return new DelegateContinue<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinueCapture<TCapture, VoidResult, VoidResult> Create<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueAction<TCapture> callback)
                {
                    return new DelegateContinueCapture<TCapture, VoidResult, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinueCapture<TCapture, VoidResult, TResult> Create<TCapture, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueFunc<TCapture, TResult> callback)
                {
                    return new DelegateContinueCapture<TCapture, VoidResult, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinueCapture<TCapture, TArg, VoidResult> Create<TCapture, TArg>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueAction<TCapture> callback)
                {
                    return new DelegateContinueCapture<TCapture, TArg, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinueCapture<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, TResult> callback)
                {
                    return new DelegateContinueCapture<TCapture, TArg, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromise<VoidResult, VoidResult> Create(Promise.ContinueFunc<Promise> callback)
                {
                    return new DelegateContinuePromise<VoidResult, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromise<VoidResult, TResult> Create<TResult>(Promise.ContinueFunc<Promise<TResult>> callback)
                {
                    return new DelegateContinuePromise<VoidResult, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromise<TArg, VoidResult> Create<TArg>(Promise<TArg>.ContinueFunc<Promise> callback)
                {
                    return new DelegateContinuePromise<TArg, VoidResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromise<TArg, TResult> Create<TArg, TResult>(Promise<TArg>.ContinueFunc<Promise<TResult>> callback)
                {
                    return new DelegateContinuePromise<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromiseCapture<TCapture, VoidResult, VoidResult> Create<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise> callback)
                {
                    return new DelegateContinuePromiseCapture<TCapture, VoidResult, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromiseCapture<TCapture, VoidResult, TResult> Create<TCapture, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise<TResult>> callback)
                {
                    return new DelegateContinuePromiseCapture<TCapture, VoidResult, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromiseCapture<TCapture, TArg, VoidResult> Create<TCapture, TArg>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise> callback)
                {
                    return new DelegateContinuePromiseCapture<TCapture, TArg, VoidResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromiseCapture<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise<TResult>> callback)
                {
                    return new DelegateContinuePromiseCapture<TCapture, TArg, TResult>(capturedValue, callback);
                }
            }

            partial class PromiseSingleAwait
            {
                [MethodImpl(InlineOption)]
                protected static DelegateHandleResolve<TDelegate> CreateResolveWrapper<TDelegate>(PromiseSingleAwait owner, TDelegate del) where TDelegate : IDelegateResolveOrCancel
                {
                    return new DelegateHandleResolve<TDelegate>(owner, del);
                }

                [MethodImpl(InlineOption)]
                protected static DelegateHandleReject<TDelegate> CreateRejectWrapper<TDelegate>(PromiseSingleAwait owner, TDelegate del) where TDelegate : IDelegateReject
                {
                    return new DelegateHandleReject<TDelegate>(owner, del);
                }

                [MethodImpl(InlineOption)]
                protected static DelegateHandleContinue<TDelegate> CreateContinueWrapper<TDelegate>(PromiseSingleAwait owner, TDelegate del) where TDelegate : IDelegateContinue
                {
                    return new DelegateHandleContinue<TDelegate>(owner, del);
                }

                protected struct DelegateHandleResolve<TDelegate> : IDelegateHandle where TDelegate : IDelegateResolveOrCancel
                {
                    private readonly PromiseSingleAwait _owner;
                    private readonly TDelegate _del;

                    [MethodImpl(InlineOption)]
                    internal DelegateHandleResolve(PromiseSingleAwait owner, TDelegate del)
                    {
                        _owner = owner;
                        _del = del;
                    }

                    [MethodImpl(InlineOption)]
                    void IDelegateHandle.InvokeAndHandle(ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                    {
                        _del.InvokeResolver(valueContainer, _owner, ref executionScheduler);
                    }

                    [MethodImpl(InlineOption)]
                    void IDelegateHandle.InvokeAndHandle(ValueContainer valueContainer, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                    {
                        _del.InvokeResolver(valueContainer, _owner, ref cancelationHelper, ref executionScheduler);
                    }
                }

                protected struct DelegateHandleReject<TDelegate> : IDelegateHandle where TDelegate : IDelegateReject
                {
                    private readonly PromiseSingleAwait _owner;
                    private readonly TDelegate _del;

                    [MethodImpl(InlineOption)]
                    internal DelegateHandleReject(PromiseSingleAwait owner, TDelegate del)
                    {
                        _owner = owner;
                        _del = del;
                    }

                    [MethodImpl(InlineOption)]
                    void IDelegateHandle.InvokeAndHandle(ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                    {
                        _del.InvokeRejecter(valueContainer, _owner, ref executionScheduler);
                    }

                    [MethodImpl(InlineOption)]
                    void IDelegateHandle.InvokeAndHandle(ValueContainer valueContainer, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                    {
                        _del.InvokeRejecter(valueContainer, _owner, ref cancelationHelper, ref executionScheduler);
                    }
                }

                protected struct DelegateHandleContinue<TDelegate> : IDelegateHandle where TDelegate : IDelegateContinue
                {
                    private readonly PromiseSingleAwait _owner;
                    private readonly TDelegate _del;

                    [MethodImpl(InlineOption)]
                    internal DelegateHandleContinue(PromiseSingleAwait owner, TDelegate del)
                    {
                        _owner = owner;
                        _del = del;
                    }

                    [MethodImpl(InlineOption)]
                    void IDelegateHandle.InvokeAndHandle(ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                    {
                        _del.Invoke(valueContainer, _owner, ref executionScheduler);
                    }

                    [MethodImpl(InlineOption)]
                    void IDelegateHandle.InvokeAndHandle(ValueContainer valueContainer, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                    {
                        _del.Invoke(valueContainer, _owner, ref cancelationHelper, ref executionScheduler);
                    }
                }

                protected struct DelegateHandleFinally<TDelegate> : IDelegateHandle where TDelegate : IDelegateSimple
                {
                    private readonly PromiseSingleAwait _owner;
                    private readonly TDelegate _del;

                    [MethodImpl(InlineOption)]
                    internal DelegateHandleFinally(PromiseSingleAwait owner, TDelegate del)
                    {
                        _owner = owner;
                        _del = del;
                    }

                    [MethodImpl(InlineOption)]
                    void IDelegateHandle.InvokeAndHandle(ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                    {
                        _del.Invoke();
                        _owner.HandleSelf(valueContainer, ref executionScheduler);
                    }

                    [MethodImpl(InlineOption)]
                    void IDelegateHandle.InvokeAndHandle(ValueContainer valueContainer, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                    {
                        _del.Invoke();
                        _owner.HandleSelf(valueContainer, ref executionScheduler);
                    }
                }
            }

            partial class PromiseWaitPromise
            {
                [MethodImpl(InlineOption)]
                protected static DelegateHandleResolvePromise<TDelegate> CreateResolveWrapper<TDelegate>(PromiseWaitPromise owner, TDelegate del) where TDelegate : IDelegateResolveOrCancelPromise
                {
                    return new DelegateHandleResolvePromise<TDelegate>(owner, del);
                }

                [MethodImpl(InlineOption)]
                protected static DelegateHandleRejectPromise<TDelegate> CreateRejectWrapper<TDelegate>(PromiseWaitPromise owner, TDelegate del) where TDelegate : IDelegateRejectPromise
                {
                    return new DelegateHandleRejectPromise<TDelegate>(owner, del);
                }

                [MethodImpl(InlineOption)]
                protected static DelegateHandleContinuePromise<TDelegate> CreateContinueWrapper<TDelegate>(PromiseWaitPromise owner, TDelegate del) where TDelegate : IDelegateContinuePromise
                {
                    return new DelegateHandleContinuePromise<TDelegate>(owner, del);
                }

                protected struct DelegateHandleResolvePromise<TDelegate> : IDelegateHandle where TDelegate : IDelegateResolveOrCancelPromise
                {
                    private readonly PromiseWaitPromise _owner;
                    private readonly TDelegate _del;

                    [MethodImpl(InlineOption)]
                    internal DelegateHandleResolvePromise(PromiseWaitPromise owner, TDelegate del)
                    {
                        _owner = owner;
                        _del = del;
                    }

                    [MethodImpl(InlineOption)]
                    void IDelegateHandle.InvokeAndHandle(ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                    {
                        _del.InvokeResolver(valueContainer, _owner, ref executionScheduler);
                    }

                    [MethodImpl(InlineOption)]
                    void IDelegateHandle.InvokeAndHandle(ValueContainer valueContainer, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                    {
                        _del.InvokeResolver(valueContainer, _owner, ref cancelationHelper, ref executionScheduler);
                    }
                }

                protected struct DelegateHandleRejectPromise<TDelegate> : IDelegateHandle where TDelegate : IDelegateRejectPromise
                {
                    private readonly PromiseWaitPromise _owner;
                    private readonly TDelegate _del;

                    [MethodImpl(InlineOption)]
                    internal DelegateHandleRejectPromise(PromiseWaitPromise owner, TDelegate del)
                    {
                        _owner = owner;
                        _del = del;
                    }

                    [MethodImpl(InlineOption)]
                    void IDelegateHandle.InvokeAndHandle(ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                    {
                        _del.InvokeRejecter(valueContainer, _owner, ref executionScheduler);
                    }

                    [MethodImpl(InlineOption)]
                    void IDelegateHandle.InvokeAndHandle(ValueContainer valueContainer, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                    {
                        _del.InvokeRejecter(valueContainer, _owner, ref cancelationHelper, ref executionScheduler);
                    }
                }

                protected struct DelegateHandleContinuePromise<TDelegate> : IDelegateHandle where TDelegate : IDelegateContinuePromise
                {
                    private readonly PromiseWaitPromise _owner;
                    private readonly TDelegate _del;

                    [MethodImpl(InlineOption)]
                    internal DelegateHandleContinuePromise(PromiseWaitPromise owner, TDelegate del)
                    {
                        _owner = owner;
                        _del = del;
                    }

                    [MethodImpl(InlineOption)]
                    void IDelegateHandle.InvokeAndHandle(ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                    {
                        _del.Invoke(valueContainer, _owner, ref executionScheduler);
                    }

                    [MethodImpl(InlineOption)]
                    void IDelegateHandle.InvokeAndHandle(ValueContainer valueContainer, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                    {
                        _del.Invoke(valueContainer, _owner, ref cancelationHelper, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateResolvePassthrough : IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise
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

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(ValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    owner.ResolveInternal(valueContainer, ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(ValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.ResolveInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void IDelegateResolveOrCancelPromise.InvokeResolver(ValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler)
                {
                    owner.ResolveInternal(valueContainer, ref executionScheduler);
                    valueContainer.Release();
                }

                void IDelegateResolveOrCancelPromise.InvokeResolver(ValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.ResolveInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
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
                            ((Action) _callback).Invoke();
                        }
                        else
                        {
                            ((Action<TArg>) _callback).Invoke(arg);
                        }
                        return default(TResult);
                    }
                    if (isVoidArg)
                    {
                        return ((Func<TResult>) _callback).Invoke();
                    }
                    return ((Func<TArg, TResult>) _callback).Invoke(arg);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(ValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    TResult result = Invoke(valueContainer.GetValue<TArg>());
                    valueContainer.Release();
                    owner.ResolveInternal(CreateResolveContainer(result, 0), ref executionScheduler);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(ValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg = valueContainer.GetValue<TArg>();
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        TResult result = Invoke(arg);
                        valueContainer.Release();
                        owner.ResolveInternal(CreateResolveContainer(result, 0), ref executionScheduler);
                    }
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(ValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler)
                {
                    TResult result = Invoke(valueContainer.GetValue<TArg>());
                    owner.WaitFor(CreateResolved(result), ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(ValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg = valueContainer.GetValue<TArg>();
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        TResult result = Invoke(arg);
                        owner.WaitFor(CreateResolved(result), ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void IDelegateReject.InvokeRejecter(ValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg;
                    if (valueContainer.TryGetValue(out arg))
                    {
                        TResult result = Invoke(arg);
                        valueContainer.Release();
                        owner.ResolveInternal(CreateResolveContainer(result, 0), ref executionScheduler);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void IDelegateReject.InvokeRejecter(ValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg;
                    if (valueContainer.TryGetValue(out arg))
                    {
                        if (cancelationHelper.TryUnregister(owner))
                        {
                            TResult result = Invoke(arg);
                            valueContainer.Release();
                            owner.ResolveInternal(CreateResolveContainer(result, 0), ref executionScheduler);
                        }
                    }
                    else if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void IDelegateRejectPromise.InvokeRejecter(ValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg;
                    if (valueContainer.TryGetValue(out arg))
                    {
                        TResult result = Invoke(arg);
                        owner.WaitFor(CreateResolved(result), ref executionScheduler);
                        valueContainer.Release();
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void IDelegateRejectPromise.InvokeRejecter(ValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg;
                    if (valueContainer.TryGetValue(out arg))
                    {
                        if (cancelationHelper.TryUnregister(owner))
                        {
                            TResult result = Invoke(arg);
                            owner.WaitFor(CreateResolved(result), ref executionScheduler);
                            valueContainer.Release();
                        }
                    }
                    else if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
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
                            promise = ((Func<Promise>) _callback).Invoke();
                        }
                        else
                        {
                            promise = ((Func<TArg, Promise>) _callback).Invoke(arg);
                        }
                        return new Promise<TResult>(promise._target._ref, promise._target.Id, promise._target.Depth);
                    }
                    if (isVoidArg)
                    {
                        return ((Func<Promise<TResult>>) _callback).Invoke();
                    }
                    return ((Func<TArg, Promise<TResult>>) _callback).Invoke(arg);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(ValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler)
                {
                    Promise<TResult> result = Invoke(valueContainer.GetValue<TArg>());
                    owner.WaitFor(result, ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(ValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg = valueContainer.GetValue<TArg>();
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Promise<TResult> result = Invoke(arg);
                        owner.WaitFor(result, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void IDelegateRejectPromise.InvokeRejecter(ValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg;
                    if (valueContainer.TryGetValue(out arg))
                    {
                        Promise<TResult> result = Invoke(arg);
                        owner.WaitFor(result, ref executionScheduler);
                        valueContainer.Release();
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void IDelegateRejectPromise.InvokeRejecter(ValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg;
                    if (valueContainer.TryGetValue(out arg))
                    {
                        if (cancelationHelper.TryUnregister(owner))
                        {
                            Promise<TResult> result = Invoke(arg);
                            owner.WaitFor(result, ref executionScheduler);
                            valueContainer.Release();
                        }
                    }
                    else if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
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
                            ((Promise.ContinueAction) _callback).Invoke(new Promise.ResultContainer(null));
                        }
                        else
                        {
                            ((Promise<TArg>.ContinueAction) _callback).Invoke(new Promise<TArg>.ResultContainer(arg));
                        }
                        return default(TResult);
                    }
                    if (isVoidArg)
                    {
                        return ((Promise.ContinueFunc<TResult>) _callback).Invoke(new Promise.ResultContainer(null));
                    }
                    return ((Promise<TArg>.ContinueFunc<TResult>) _callback).Invoke(new Promise<TArg>.ResultContainer(arg));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(ValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    // JIT constant-optimizes these checks away.
                    bool isVoidArg = null != default(TArg) && typeof(TArg) == typeof(VoidResult);
                    bool isVoidResult = null != default(TResult) && typeof(TResult) == typeof(VoidResult);
                    ValueContainer resolveContainer;
                    if (isVoidResult)
                    {
                        if (isVoidArg)
                        {
                            ((Promise.ContinueAction) _callback).Invoke(new Promise.ResultContainer(valueContainer));
                        }
                        else
                        {
                            ((Promise<TArg>.ContinueAction) _callback).Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                        }
                        resolveContainer = ResolveContainerVoid.GetOrCreate(0);
                    }
                    else
                    {
                        TResult result;
                        if (isVoidArg)
                        {
                            result = ((Promise.ContinueFunc<TResult>) _callback).Invoke(new Promise.ResultContainer(valueContainer));
                        }
                        else
                        {
                            result = ((Promise<TArg>.ContinueFunc<TResult>) _callback).Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                        }
                        resolveContainer = CreateResolveContainer(result, 0);
                    }
                    valueContainer.Release();
                    owner.ResolveInternal(resolveContainer, ref executionScheduler);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(ValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
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
                            promise = ((Promise.ContinueFunc<Promise>) _callback).Invoke(new Promise.ResultContainer(null));
                        }
                        else
                        {
                            promise = ((Promise<TArg>.ContinueFunc<Promise>) _callback).Invoke(new Promise<TArg>.ResultContainer(arg));
                        }
                        return new Promise<TResult>(promise._target._ref, promise._target.Id, promise._target.Depth);
                    }
                    if (isVoidArg)
                    {
                        return ((Promise.ContinueFunc<Promise<TResult>>) _callback).Invoke(new Promise.ResultContainer(null));
                    }
                    return ((Promise<TArg>.ContinueFunc<Promise<TResult>>) _callback).Invoke(new Promise<TArg>.ResultContainer(arg));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(ValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler)
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
                            promise = ((Promise.ContinueFunc<Promise>) _callback).Invoke(new Promise.ResultContainer(valueContainer));
                        }
                        else
                        {
                            promise = ((Promise<TArg>.ContinueFunc<Promise>) _callback).Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                        }
                        result = new Promise<TResult>(promise._target._ref, promise._target.Id, promise._target.Depth);
                    }
                    else
                    {
                        if (isVoidArg)
                        {
                            result = ((Promise.ContinueFunc<Promise<TResult>>) _callback).Invoke(new Promise.ResultContainer(valueContainer));
                        }
                        else
                        {
                            result = ((Promise<TArg>.ContinueFunc<Promise<TResult>>) _callback).Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                        }
                    }
                    owner.WaitFor(result, ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(ValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
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
                            ((Action<TCapture>) _callback).Invoke(_capturedValue);
                        }
                        else
                        {
                            ((Action<TCapture, TArg>) _callback).Invoke(_capturedValue, arg);
                        }
                        return default(TResult);
                    }
                    if (isVoidArg)
                    {
                        return ((Func<TCapture, TResult>) _callback).Invoke(_capturedValue);
                    }
                    return ((Func<TCapture, TArg, TResult>) _callback).Invoke(_capturedValue, arg);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(ValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    TResult result = Invoke(valueContainer.GetValue<TArg>());
                    valueContainer.Release();
                    owner.ResolveInternal(CreateResolveContainer(result, 0), ref executionScheduler);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(ValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg = valueContainer.GetValue<TArg>();
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        TResult result = Invoke(arg);
                        valueContainer.Release();
                        owner.ResolveInternal(CreateResolveContainer(result, 0), ref executionScheduler);
                    }
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(ValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler)
                {
                    TResult result = Invoke(valueContainer.GetValue<TArg>());
                    owner.WaitFor(CreateResolved(result), ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(ValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg = valueContainer.GetValue<TArg>();
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        TResult result = Invoke(arg);
                        owner.WaitFor(CreateResolved(result), ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void IDelegateReject.InvokeRejecter(ValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg;
                    if (valueContainer.TryGetValue(out arg))
                    {
                        TResult result = Invoke(arg);
                        valueContainer.Release();
                        owner.ResolveInternal(CreateResolveContainer(result, 0), ref executionScheduler);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void IDelegateReject.InvokeRejecter(ValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg;
                    if (valueContainer.TryGetValue(out arg))
                    {
                        if (cancelationHelper.TryUnregister(owner))
                        {
                            TResult result = Invoke(arg);
                            valueContainer.Release();
                            owner.ResolveInternal(CreateResolveContainer(result, 0), ref executionScheduler);
                        }
                    }
                    else if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void IDelegateRejectPromise.InvokeRejecter(ValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg;
                    if (valueContainer.TryGetValue(out arg))
                    {
                        TResult result = Invoke(arg);
                        owner.WaitFor(CreateResolved(result), ref executionScheduler);
                        valueContainer.Release();
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void IDelegateRejectPromise.InvokeRejecter(ValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg;
                    if (valueContainer.TryGetValue(out arg))
                    {
                        if (cancelationHelper.TryUnregister(owner))
                        {
                            TResult result = Invoke(arg);
                            owner.WaitFor(CreateResolved(result), ref executionScheduler);
                            valueContainer.Release();
                        }
                    }
                    else if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
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
                            promise = ((Func<TCapture, Promise>) _callback).Invoke(_capturedValue);
                        }
                        else
                        {
                            promise = ((Func<TCapture, TArg, Promise>) _callback).Invoke(_capturedValue, arg);
                        }
                        return new Promise<TResult>(promise._target._ref, promise._target.Id, promise._target.Depth);
                    }
                    if (isVoidArg)
                    {
                        return ((Func<TCapture, Promise<TResult>>) _callback).Invoke(_capturedValue);
                    }
                    return ((Func<TCapture, TArg, Promise<TResult>>) _callback).Invoke(_capturedValue, arg);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(ValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler)
                {
                    Promise<TResult> result = Invoke(valueContainer.GetValue<TArg>());
                    owner.WaitFor(result, ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(ValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg = valueContainer.GetValue<TArg>();
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Promise<TResult> result = Invoke(arg);
                        owner.WaitFor(result, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void IDelegateRejectPromise.InvokeRejecter(ValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg;
                    if (valueContainer.TryGetValue(out arg))
                    {
                        Promise<TResult> result = Invoke(arg);
                        owner.WaitFor(result, ref executionScheduler);
                        valueContainer.Release();
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void IDelegateRejectPromise.InvokeRejecter(ValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    TArg arg;
                    if (valueContainer.TryGetValue(out arg))
                    {
                        if (cancelationHelper.TryUnregister(owner))
                        {
                            Promise<TResult> result = Invoke(arg);
                            owner.WaitFor(result, ref executionScheduler);
                            valueContainer.Release();
                        }
                    }
                    else if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
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
                            ((Promise.ContinueAction<TCapture>) _callback).Invoke(_capturedValue, new Promise.ResultContainer(null));
                        }
                        else
                        {
                            ((Promise<TArg>.ContinueAction<TCapture>) _callback).Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg));
                        }
                        return default(TResult);
                    }
                    if (isVoidArg)
                    {
                        return ((Promise.ContinueFunc<TCapture, TResult>) _callback).Invoke(_capturedValue, new Promise.ResultContainer(null));
                    }
                    return ((Promise<TArg>.ContinueFunc<TCapture, TResult>) _callback).Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(ValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    // JIT constant-optimizes these checks away.
                    bool isVoidArg = null != default(TArg) && typeof(TArg) == typeof(VoidResult);
                    bool isVoidResult = null != default(TResult) && typeof(TResult) == typeof(VoidResult);
                    ValueContainer resolveContainer;
                    if (isVoidResult)
                    {
                        if (isVoidArg)
                        {
                            ((Promise.ContinueAction<TCapture>) _callback).Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                        }
                        else
                        {
                            ((Promise<TArg>.ContinueAction<TCapture>) _callback).Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                        }
                        resolveContainer = ResolveContainerVoid.GetOrCreate(0);
                    }
                    else
                    {
                        TResult result;
                        if (isVoidArg)
                        {
                            result = ((Promise.ContinueFunc<TCapture, TResult>) _callback).Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                        }
                        else
                        {
                            result = ((Promise<TArg>.ContinueFunc<TCapture, TResult>) _callback).Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                        }
                        resolveContainer = CreateResolveContainer(result, 0);
                    }
                    valueContainer.Release();
                    owner.ResolveInternal(resolveContainer, ref executionScheduler);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(ValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
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
                            promise = ((Promise.ContinueFunc<TCapture, Promise>) _callback).Invoke(_capturedValue, new Promise.ResultContainer(null));
                        }
                        else
                        {
                            promise = ((Promise<TArg>.ContinueFunc<TCapture, Promise>) _callback).Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg));
                        }
                        return new Promise<TResult>(promise._target._ref, promise._target.Id, promise._target.Depth);
                    }
                    if (isVoidArg)
                    {
                        return ((Promise.ContinueFunc<TCapture, Promise<TResult>>) _callback).Invoke(_capturedValue, new Promise.ResultContainer(null));
                    }
                    return ((Promise<TArg>.ContinueFunc<TCapture, Promise<TResult>>) _callback).Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(ValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler)
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
                            promise = ((Promise.ContinueFunc<TCapture, Promise>) _callback).Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                        }
                        else
                        {
                            promise = ((Promise<TArg>.ContinueFunc<TCapture, Promise>) _callback).Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                        }
                        result = new Promise<TResult>(promise._target._ref, promise._target.Id, promise._target.Depth);
                    }
                    else
                    {
                        if (isVoidArg)
                        {
                            result = ((Promise.ContinueFunc<TCapture, Promise<TResult>>) _callback).Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                        }
                        else
                        {
                            result = ((Promise<TArg>.ContinueFunc<TCapture, Promise<TResult>>) _callback).Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                        }
                    }
                    owner.WaitFor(result, ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(ValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
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