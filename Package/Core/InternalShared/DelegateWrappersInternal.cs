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

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateVoidVoid Create(Action callback)
            => new Internal.DelegateVoidVoid(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateVoidResult<TResult> Create<TResult>(Func<TResult> callback)
            => new Internal.DelegateVoidResult<TResult>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateArgVoid<TArg> Create<TArg>(Action<TArg> callback)
            => new Internal.DelegateArgVoid<TArg>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateArgResult<TArg, TResult> Create<TArg, TResult>(Func<TArg, TResult> callback)
            => new Internal.DelegateArgResult<TArg, TResult>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateCaptureVoidVoid<TCapture> Create<TCapture>(in TCapture capturedValue, Action<TCapture> callback)
            => new Internal.DelegateCaptureVoidVoid<TCapture>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateCaptureVoidResult<TCapture, TResult> Create<TCapture, TResult>(in TCapture capturedValue, Func<TCapture, TResult> callback)
            => new Internal.DelegateCaptureVoidResult<TCapture, TResult>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(in TCapture capturedValue, Action<TCapture, TArg> callback)
            => new Internal.DelegateCaptureArgVoid<TCapture, TArg>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(in TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
            => new Internal.DelegateCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.Delegate2ArgVoid<TArg1, TArg2> Create<TArg1, TArg2>(Action<TArg1, TArg2> callback)
            => new Internal.Delegate2ArgVoid<TArg1, TArg2>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateCapture2ArgVoid<TCapture, TArg1, TArg2> Create<TCapture, TArg1, TArg2>(in TCapture capturedValue, Action<TCapture, TArg1, TArg2> callback)
            => new Internal.DelegateCapture2ArgVoid<TCapture, TArg1, TArg2>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.Delegate2ArgResult<TArg1, TArg2, TResult> Create<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult> callback)
            => new Internal.Delegate2ArgResult<TArg1, TArg2, TResult>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateCapture2ArgResult<TCapture, TArg1, TArg2, TResult> Create<TCapture, TArg1, TArg2, TResult>(in TCapture capturedValue, Func<TCapture, TArg1, TArg2, TResult> callback)
            => new Internal.DelegateCapture2ArgResult<TCapture, TArg1, TArg2, TResult>(capturedValue, callback);

        // Unity IL2CPP has a maximum nested generic depth, so unfortunately we have to create separate struct wrappers,
        // so the generic will only nest <TArg> instead of <Promise<TArg>.ResultContainer>
        #region ResultContainer Delegates

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

        #endregion ResultContainer Delegates
    }

    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateVoidVoid : IAction,
            IFunc<Promise>,
            IFunc<VoidResult, VoidResult>,
            IFunc<VoidResult, Promise>
        {
            private readonly Action _callback;

            [MethodImpl(InlineOption)]
            public DelegateVoidVoid(Action callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public void Invoke()
                => _callback.Invoke();

            [MethodImpl(InlineOption)]
            Promise IFunc<Promise>.Invoke()
            {
                _callback.Invoke();
                return Promise.Resolved();
            }

            [MethodImpl(InlineOption)]
            VoidResult IFunc<VoidResult, VoidResult>.Invoke(in VoidResult arg)
            {
                _callback.Invoke();
                return default;
            }

            [MethodImpl(InlineOption)]
            Promise IFunc<VoidResult, Promise>.Invoke(in VoidResult arg)
            {
                _callback.Invoke();
                return Promise.Resolved();
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateVoidResult<TResult> : IFunc<TResult>,
            IFunc<Promise<TResult>>,
            IFunc<VoidResult, TResult>,
            IFunc<VoidResult, Promise<TResult>>
        {
            private readonly Func<TResult> _callback;

            [MethodImpl(InlineOption)]
            public DelegateVoidResult(Func<TResult> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public TResult Invoke()
                => _callback.Invoke();

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<Promise<TResult>>.Invoke()
                => Promise.Resolved(_callback.Invoke());

            [MethodImpl(InlineOption)]
            TResult IFunc<VoidResult, TResult>.Invoke(in VoidResult arg)
                => _callback.Invoke();

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<VoidResult, Promise<TResult>>.Invoke(in VoidResult arg)
                => Promise.Resolved(_callback.Invoke());
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateArgVoid<TArg> : IAction<TArg>,
            IFunc<TArg, Promise>,
            IFunc<TArg, VoidResult>
        {
            private readonly Action<TArg> _callback;

            [MethodImpl(InlineOption)]
            public DelegateArgVoid(Action<TArg> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public void Invoke(in TArg arg)
                => _callback.Invoke(arg);

            [MethodImpl(InlineOption)]
            Promise IFunc<TArg, Promise>.Invoke(in TArg arg)
            {
                _callback.Invoke(arg);
                return Promise.Resolved();
            }

            [MethodImpl(InlineOption)]
            VoidResult IFunc<TArg, VoidResult>.Invoke(in TArg arg)
            {
                _callback.Invoke(arg);
                return default;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateArgResult<TArg, TResult> : IFunc<TArg, TResult>,
            IFunc<TArg, Promise<TResult>>
        {
            private readonly Func<TArg, TResult> _callback;

            [MethodImpl(InlineOption)]
            public DelegateArgResult(Func<TArg, TResult> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public TResult Invoke(in TArg arg)
                => _callback.Invoke(arg);

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<TArg, Promise<TResult>>.Invoke(in TArg arg)
                => Promise.Resolved(_callback.Invoke(arg));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateCaptureVoidVoid<TCapture> : IAction,
            IFunc<Promise>,
            IFunc<VoidResult, VoidResult>,
            IFunc<VoidResult, Promise>
        {
            private readonly Action<TCapture> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateCaptureVoidVoid(in TCapture capturedValue, Action<TCapture> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public void Invoke()
                => _callback.Invoke(_capturedValue);

            [MethodImpl(InlineOption)]
            Promise IFunc<Promise>.Invoke()
            {
                _callback.Invoke(_capturedValue);
                return Promise.Resolved();
            }

            [MethodImpl(InlineOption)]
            VoidResult IFunc<VoidResult, VoidResult>.Invoke(in VoidResult arg)
            {
                _callback.Invoke(_capturedValue);
                return default;
            }

            [MethodImpl(InlineOption)]
            Promise IFunc<VoidResult, Promise>.Invoke(in VoidResult arg)
            {
                _callback.Invoke(_capturedValue);
                return Promise.Resolved();
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateCaptureVoidResult<TCapture, TResult> : IFunc<TResult>,
            IFunc<Promise<TResult>>,
            IFunc<VoidResult, TResult>,
            IFunc<VoidResult, Promise<TResult>>
        {
            private readonly Func<TCapture, TResult> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateCaptureVoidResult(in TCapture capturedValue, Func<TCapture, TResult> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke()
                => _callback.Invoke(_capturedValue);

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<Promise<TResult>>.Invoke()
                => Promise.Resolved(_callback.Invoke(_capturedValue));

            [MethodImpl(InlineOption)]
            TResult IFunc<VoidResult, TResult>.Invoke(in VoidResult arg)
                => _callback.Invoke(_capturedValue);

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<VoidResult, Promise<TResult>>.Invoke(in VoidResult arg)
                => Promise.Resolved(_callback.Invoke(_capturedValue));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateCaptureArgVoid<TCapture, TArg> : IAction<TArg>,
            IFunc<TArg, Promise>,
            IFunc<TArg, VoidResult>
        {
            private readonly Action<TCapture, TArg> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateCaptureArgVoid(in TCapture capturedValue, Action<TCapture, TArg> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public void Invoke(in TArg arg)
                => _callback.Invoke(_capturedValue, arg);

            [MethodImpl(InlineOption)]
            Promise IFunc<TArg, Promise>.Invoke(in TArg arg)
            {
                _callback.Invoke(_capturedValue, arg);
                return Promise.Resolved();
            }

            [MethodImpl(InlineOption)]
            VoidResult IFunc<TArg, VoidResult>.Invoke(in TArg arg)
            {
                _callback.Invoke(_capturedValue, arg);
                return default;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateCaptureArgResult<TCapture, TArg, TResult> : IFunc<TArg, TResult>,
            IFunc<TArg, Promise<TResult>>
        {
            private readonly Func<TCapture, TArg, TResult> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateCaptureArgResult(in TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke(in TArg arg)
                => _callback.Invoke(_capturedValue, arg);

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<TArg, Promise<TResult>>.Invoke(in TArg arg)
                => Promise.Resolved(_callback.Invoke(_capturedValue, arg));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct Delegate2ArgVoid<TArg1, TArg2> : IAction<TArg1, TArg2>,
            IFunc<TArg1, TArg2, Promise>
        {
            private readonly Action<TArg1, TArg2> _callback;

            [MethodImpl(InlineOption)]
            public Delegate2ArgVoid(Action<TArg1, TArg2> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public void Invoke(in TArg1 arg1, in TArg2 arg2)
                => _callback.Invoke(arg1, arg2);

            [MethodImpl(InlineOption)]
            Promise IFunc<TArg1, TArg2, Promise>.Invoke(in TArg1 arg1, in TArg2 arg2)
            {
                _callback.Invoke(arg1, arg2);
                return Promise.Resolved();
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateCapture2ArgVoid<TCapture, TArg1, TArg2> : IAction<TArg1, TArg2>,
            IFunc<TArg1, TArg2, Promise>
        {
            private readonly Action<TCapture, TArg1, TArg2> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateCapture2ArgVoid(in TCapture capturedValue, Action<TCapture, TArg1, TArg2> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public void Invoke(in TArg1 arg1, in TArg2 arg2)
                => _callback.Invoke(_capturedValue, arg1, arg2);

            [MethodImpl(InlineOption)]
            Promise IFunc<TArg1, TArg2, Promise>.Invoke(in TArg1 arg1, in TArg2 arg2)
            {
                _callback.Invoke(_capturedValue, arg1, arg2);
                return Promise.Resolved();
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct Delegate2ArgResult<TArg1, TArg2, TResult> : IFunc<TArg1, TArg2, TResult>,
            IFunc<TArg1, TArg2, Promise<TResult>>
        {
            private readonly Func<TArg1, TArg2, TResult> _callback;

            [MethodImpl(InlineOption)]
            public Delegate2ArgResult(Func<TArg1, TArg2, TResult> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public TResult Invoke(in TArg1 arg1, in TArg2 arg2)
                => _callback.Invoke(arg1, arg2);

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<TArg1, TArg2, Promise<TResult>>.Invoke(in TArg1 arg1, in TArg2 arg2)
                => Promise.Resolved(_callback.Invoke(arg1, arg2));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateCapture2ArgResult<TCapture, TArg1, TArg2, TResult> : IFunc<TArg1, TArg2, TResult>,
            IFunc<TArg1, TArg2, Promise<TResult>>
        {
            private readonly Func<TCapture, TArg1, TArg2, TResult> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateCapture2ArgResult(in TCapture capturedValue, Func<TCapture, TArg1, TArg2, TResult> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke(in TArg1 arg1, in TArg2 arg2)
                => _callback.Invoke(_capturedValue, arg1, arg2);

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<TArg1, TArg2, Promise<TResult>>.Invoke(in TArg1 arg1, in TArg2 arg2)
                => Promise.Resolved(_callback.Invoke(_capturedValue, arg1, arg2));
        }

        // Unity IL2CPP has a maximum nested generic depth, so unfortunately we have to create separate struct wrappers,
        // so the generic will only nest <TArg> instead of <Promise<TArg>.ResultContainer>
        #region ResultContainer Delegates

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateResultContainerArgVoid<TArg> : IAction<Promise<TArg>.ResultContainer>,
            IFunc<Promise<TArg>.ResultContainer, Promise>,
            IFunc<Promise<TArg>.ResultContainer, VoidResult>
        {
            private readonly Action<Promise<TArg>.ResultContainer> _callback;

            [MethodImpl(InlineOption)]
            public DelegateResultContainerArgVoid(Action<Promise<TArg>.ResultContainer> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public void Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(arg);

            [MethodImpl(InlineOption)]
            Promise IFunc<Promise<TArg>.ResultContainer, Promise>.Invoke(in Promise<TArg>.ResultContainer arg)
            {
                _callback.Invoke(arg);
                return Promise.Resolved();
            }

            [MethodImpl(InlineOption)]
            VoidResult IFunc<Promise<TArg>.ResultContainer, VoidResult>.Invoke(in Promise<TArg>.ResultContainer arg)
            {
                _callback.Invoke(arg);
                return default;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateResultContainerArgResult<TArg, TResult> : IFunc<Promise<TArg>.ResultContainer, TResult>,
            IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
        {
            private readonly Func<Promise<TArg>.ResultContainer, TResult> _callback;

            [MethodImpl(InlineOption)]
            public DelegateResultContainerArgResult(Func<Promise<TArg>.ResultContainer, TResult> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public TResult Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(arg);

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>.Invoke(in Promise<TArg>.ResultContainer arg)
                => Promise.Resolved(_callback.Invoke(arg));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateResultContainerCaptureArgVoid<TCapture, TArg> : IAction<Promise<TArg>.ResultContainer>,
            IFunc<Promise<TArg>.ResultContainer, Promise>,
            IFunc<Promise<TArg>.ResultContainer, VoidResult>
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
            Promise IFunc<Promise<TArg>.ResultContainer, Promise>.Invoke(in Promise<TArg>.ResultContainer arg)
            {
                _callback.Invoke(_capturedValue, arg);
                return Promise.Resolved();
            }

            [MethodImpl(InlineOption)]
            VoidResult IFunc<Promise<TArg>.ResultContainer, VoidResult>.Invoke(in Promise<TArg>.ResultContainer arg)
            {
                _callback.Invoke(_capturedValue, arg);
                return default;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateResultContainerCaptureArgResult<TCapture, TArg, TResult> : IFunc<Promise<TArg>.ResultContainer, TResult>,
            IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
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
            Promise<TResult> IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>.Invoke(in Promise<TArg>.ResultContainer arg)
                => Promise.Resolved(_callback.Invoke(_capturedValue, arg));
        }

        #endregion ResultContainer Delegates
    }
}