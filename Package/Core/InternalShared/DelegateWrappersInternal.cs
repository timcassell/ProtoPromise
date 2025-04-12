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
    internal static partial class DelegateWrapper
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
        internal static Internal.AsyncDelegateVoidVoid Create(Func<Promise> callback)
            => new Internal.AsyncDelegateVoidVoid(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateVoidResult<TResult> Create<TResult>(Func<Promise<TResult>> callback)
            => new Internal.AsyncDelegateVoidResult<TResult>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateArgVoid<TArg> Create<TArg>(Func<TArg, Promise> callback)
            => new Internal.AsyncDelegateArgVoid<TArg>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateArgResult<TArg, TResult> Create<TArg, TResult>(Func<TArg, Promise<TResult>> callback)
            => new Internal.AsyncDelegateArgResult<TArg, TResult>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateCaptureVoidVoid<TCapture> Create<TCapture>(in TCapture capturedValue, Func<TCapture, Promise> callback)
            => new Internal.AsyncDelegateCaptureVoidVoid<TCapture>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateCaptureVoidResult<TCapture, TResult> Create<TCapture, TResult>(in TCapture capturedValue, Func<TCapture, Promise<TResult>> callback)
            => new Internal.AsyncDelegateCaptureVoidResult<TCapture, TResult>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(in TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
            => new Internal.AsyncDelegateCaptureArgVoid<TCapture, TArg>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(in TCapture capturedValue, Func<TCapture, TArg, Promise<TResult>> callback)
            => new Internal.AsyncDelegateCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);


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

        // Unfortunately, some APIs like Promise.ParallelForEach swapped the order of the capture value.
        // TODO: Add consistent APIs, and remove the inconsistent APIs in next major version.
        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateArgCaptureArgResult<TArg1, TCapture, TArg2, TResult> Create<TArg1, TCapture, TArg2, TResult>(in TCapture capturedValue, Func<TArg1, TCapture, TArg2, TResult> callback)
            => new Internal.DelegateArgCaptureArgResult<TArg1, TCapture, TArg2, TResult>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.Delegate3ArgResult<TArg1, TArg2, TArg3, TResult> Create<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, TResult> callback)
            => new Internal.Delegate3ArgResult<TArg1, TArg2, TArg3, TResult>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateCapture3ArgResult<TCapture, TArg1, TArg2, TArg3, TResult> Create<TCapture, TArg1, TArg2, TArg3, TResult>(in TCapture capturedValue, Func<TCapture, TArg1, TArg2, TArg3, TResult> callback)
            => new Internal.DelegateCapture3ArgResult<TCapture, TArg1, TArg2, TArg3, TResult>(capturedValue, callback);
    }

    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateVoidVoid : IAction,
            IAction<VoidResult>,
            IFunc<VoidResult, VoidResult>,
            IFunc<Promise>,
            IFunc<VoidResult, Promise>,
            ICancelable
        {
            private readonly Action _callback;

            [MethodImpl(InlineOption)]
            public DelegateVoidVoid(Action callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public void Invoke()
                => _callback.Invoke();

            [MethodImpl(InlineOption)]
            void IAction<VoidResult>.Invoke(in VoidResult arg)
                => _callback.Invoke();

            [MethodImpl(InlineOption)]
            VoidResult IFunc<VoidResult, VoidResult>.Invoke(in VoidResult arg)
            {
                _callback.Invoke();
                return default;
            }

            [MethodImpl(InlineOption)]
            Promise IFunc<Promise>.Invoke()
            {
                _callback.Invoke();
                return Promise.Resolved();
            }

            [MethodImpl(InlineOption)]
            Promise IFunc<VoidResult, Promise>.Invoke(in VoidResult arg)
            {
                _callback.Invoke();
                return Promise.Resolved();
            }

            [MethodImpl(InlineOption)]
            void ICancelable.Cancel()
                => _callback.Invoke();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateVoidResult<TResult> : IFunc<TResult>,
            IFunc<VoidResult, TResult>,
            IFunc<Promise<TResult>>,
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
            TResult IFunc<VoidResult, TResult>.Invoke(in VoidResult arg)
                => _callback.Invoke();

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<Promise<TResult>>.Invoke()
                => Promise.Resolved(_callback.Invoke());

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<VoidResult, Promise<TResult>>.Invoke(in VoidResult arg)
                => Promise.Resolved(_callback.Invoke());
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateArgVoid<TArg> : IAction<TArg>,
            IFunc<TArg, VoidResult>,
            IFunc<TArg, Promise>,
            IFunc<TArg, CancelationToken, Promise>,
            IProgress<TArg>
        {
            private readonly Action<TArg> _callback;

            [MethodImpl(InlineOption)]
            public DelegateArgVoid(Action<TArg> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public void Invoke(in TArg arg)
                => _callback.Invoke(arg);

            [MethodImpl(InlineOption)]
            VoidResult IFunc<TArg, VoidResult>.Invoke(in TArg arg)
            {
                _callback.Invoke(arg);
                return default;
            }

            [MethodImpl(InlineOption)]
            Promise IFunc<TArg, Promise>.Invoke(in TArg arg)
            {
                _callback.Invoke(arg);
                return Promise.Resolved();
            }

            [MethodImpl(InlineOption)]
            Promise IFunc<TArg, CancelationToken, Promise>.Invoke(in TArg arg1, in CancelationToken arg2)
            {
                _callback.Invoke(arg1);
                return Promise.Resolved();
            }

            [MethodImpl(InlineOption)]
            void IProgress<TArg>.Report(TArg value)
                => _callback.Invoke(value);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateArgResult<TArg, TResult> : IFunc<TArg, TResult>,
            IFunc<TArg, Promise<TResult>>,
            IFunc<TArg, CancelationToken, Promise<TResult>>
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

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<TArg, CancelationToken, Promise<TResult>>.Invoke(in TArg arg1, in CancelationToken arg2)
                => Promise.Resolved(_callback.Invoke(arg1));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateCaptureVoidVoid<TCapture> : IAction,
            IAction<VoidResult>,
            IFunc<VoidResult, VoidResult>,
            IFunc<Promise>,
            IFunc<VoidResult, Promise>,
            ICancelable
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
            void IAction<VoidResult>.Invoke(in VoidResult arg)
                => _callback.Invoke(_capturedValue);

            [MethodImpl(InlineOption)]
            VoidResult IFunc<VoidResult, VoidResult>.Invoke(in VoidResult arg)
            {
                _callback.Invoke(_capturedValue);
                return default;
            }

            [MethodImpl(InlineOption)]
            Promise IFunc<Promise>.Invoke()
            {
                _callback.Invoke(_capturedValue);
                return Promise.Resolved();
            }

            [MethodImpl(InlineOption)]
            Promise IFunc<VoidResult, Promise>.Invoke(in VoidResult arg)
            {
                _callback.Invoke(_capturedValue);
                return Promise.Resolved();
            }

            [MethodImpl(InlineOption)]
            void ICancelable.Cancel()
                => _callback.Invoke(_capturedValue);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateCaptureVoidResult<TCapture, TResult> : IFunc<TResult>,
            IFunc<VoidResult, TResult>,
            IFunc<Promise<TResult>>,
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
            TResult IFunc<VoidResult, TResult>.Invoke(in VoidResult arg)
                => _callback.Invoke(_capturedValue);

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<Promise<TResult>>.Invoke()
                => Promise.Resolved(_callback.Invoke(_capturedValue));

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<VoidResult, Promise<TResult>>.Invoke(in VoidResult arg)
                => Promise.Resolved(_callback.Invoke(_capturedValue));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateCaptureArgVoid<TCapture, TArg> : IAction<TArg>,
            IFunc<TArg, VoidResult>,
            IFunc<TArg, Promise>,
            IFunc<TArg, CancelationToken, Promise>,
            IProgress<TArg>
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
            VoidResult IFunc<TArg, VoidResult>.Invoke(in TArg arg)
            {
                _callback.Invoke(_capturedValue, arg);
                return default;
            }

            [MethodImpl(InlineOption)]
            Promise IFunc<TArg, Promise>.Invoke(in TArg arg)
            {
                _callback.Invoke(_capturedValue, arg);
                return Promise.Resolved();
            }

            [MethodImpl(InlineOption)]
            Promise IFunc<TArg, CancelationToken, Promise>.Invoke(in TArg arg1, in CancelationToken arg2)
            {
                _callback.Invoke(_capturedValue, arg1);
                return Promise.Resolved();
            }

            [MethodImpl(InlineOption)]
            void IProgress<TArg>.Report(TArg value)
                => _callback.Invoke(_capturedValue, value);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateCaptureArgResult<TCapture, TArg, TResult> : IFunc<TArg, TResult>,
            IFunc<TArg, Promise<TResult>>,
            IFunc<TArg, CancelationToken, Promise<TResult>>
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

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<TArg, CancelationToken, Promise<TResult>>.Invoke(in TArg arg1, in CancelationToken arg2)
                => Promise.Resolved(_callback.Invoke(_capturedValue, arg1));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateVoidVoid : IFunc<Promise>,
            IFunc<VoidResult, Promise>
        {
            private readonly Func<Promise> _callback;

            [MethodImpl(InlineOption)]
            public AsyncDelegateVoidVoid(Func<Promise> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public Promise Invoke()
                => _callback.Invoke();

            [MethodImpl(InlineOption)]
            Promise IFunc<VoidResult, Promise>.Invoke(in VoidResult arg)
                => _callback.Invoke();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateVoidResult<TResult> : IFunc<Promise<TResult>>,
            IFunc<VoidResult, Promise<TResult>>
        {
            private readonly Func<Promise<TResult>> _callback;

            [MethodImpl(InlineOption)]
            public AsyncDelegateVoidResult(Func<Promise<TResult>> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke()
                => _callback.Invoke();

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<VoidResult, Promise<TResult>>.Invoke(in VoidResult arg)
                => _callback.Invoke();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateArgVoid<TArg> : IFunc<TArg, Promise>
        {
            private readonly Func<TArg, Promise> _callback;

            [MethodImpl(InlineOption)]
            public AsyncDelegateArgVoid(Func<TArg, Promise> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public Promise Invoke(in TArg arg)
                => _callback.Invoke(arg);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateArgResult<TArg, TResult> : IFunc<TArg, Promise<TResult>>
        {
            private readonly Func<TArg, Promise<TResult>> _callback;

            [MethodImpl(InlineOption)]
            public AsyncDelegateArgResult(Func<TArg, Promise<TResult>> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke(in TArg arg)
                => _callback.Invoke(arg);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateCaptureVoidVoid<TCapture> : IFunc<Promise>,
            IFunc<VoidResult, Promise>
        {
            private readonly Func<TCapture, Promise> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public AsyncDelegateCaptureVoidVoid(in TCapture capturedValue, Func<TCapture, Promise> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public Promise Invoke()
                => _callback.Invoke(_capturedValue);

            [MethodImpl(InlineOption)]
            Promise IFunc<VoidResult, Promise>.Invoke(in VoidResult arg)
                => _callback.Invoke(_capturedValue);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateCaptureVoidResult<TCapture, TResult> : IFunc<Promise<TResult>>,
            IFunc<VoidResult, Promise<TResult>>
        {
            private readonly Func<TCapture, Promise<TResult>> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public AsyncDelegateCaptureVoidResult(in TCapture capturedValue, Func<TCapture, Promise<TResult>> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke()
                => _callback.Invoke(_capturedValue);

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<VoidResult, Promise<TResult>>.Invoke(in VoidResult arg)
                => _callback.Invoke(_capturedValue);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateCaptureArgVoid<TCapture, TArg> : IFunc<TArg, Promise>
        {
            private readonly Func<TCapture, TArg, Promise> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public AsyncDelegateCaptureArgVoid(in TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public Promise Invoke(in TArg arg)
                => _callback.Invoke(_capturedValue, arg);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateCaptureArgResult<TCapture, TArg, TResult> : IFunc<TArg, Promise<TResult>>
        {
            private readonly Func<TCapture, TArg, Promise<TResult>> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public AsyncDelegateCaptureArgResult(in TCapture capturedValue, Func<TCapture, TArg, Promise<TResult>> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke(in TArg arg)
                => _callback.Invoke(_capturedValue, arg);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct Delegate2ArgVoid<TArg1, TArg2> : IAction<TArg1, TArg2>,
            IFunc<TArg1, TArg2, Promise>,
            IFunc<TArg1, TArg2, CancelationToken, Promise>
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

            [MethodImpl(InlineOption)]
            Promise IFunc<TArg1, TArg2, CancelationToken, Promise>.Invoke(in TArg1 arg1, in TArg2 arg2, in CancelationToken arg3)
            {
                _callback.Invoke(arg1, arg2);
                return Promise.Resolved();
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateCapture2ArgVoid<TCapture, TArg1, TArg2> : IAction<TArg1, TArg2>,
            IFunc<TArg1, TArg2, Promise>,
            IFunc<TArg1, TArg2, CancelationToken, Promise>
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

            [MethodImpl(InlineOption)]
            Promise IFunc<TArg1, TArg2, CancelationToken, Promise>.Invoke(in TArg1 arg1, in TArg2 arg2, in CancelationToken arg3)
            {
                _callback.Invoke(_capturedValue, arg1, arg2);
                return Promise.Resolved();
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct Delegate2ArgResult<TArg1, TArg2, TResult> : IFunc<TArg1, TArg2, TResult>,
            IFunc<TArg1, TArg2, Promise<TResult>>,
            IFunc<TArg1, TArg2, CancelationToken, Promise<TResult>>
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

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<TArg1, TArg2, CancelationToken, Promise<TResult>>.Invoke(in TArg1 arg1, in TArg2 arg2, in CancelationToken arg3)
                => Promise.Resolved(_callback.Invoke(arg1, arg2));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateCapture2ArgResult<TCapture, TArg1, TArg2, TResult> : IFunc<TArg1, TArg2, TResult>,
            IFunc<TArg1, TArg2, Promise<TResult>>,
            IFunc<TArg1, TArg2, CancelationToken, Promise<TResult>>
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

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<TArg1, TArg2, CancelationToken, Promise<TResult>>.Invoke(in TArg1 arg1, in TArg2 arg2, in CancelationToken arg3)
                => Promise.Resolved(_callback.Invoke(_capturedValue, arg1, arg2));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateArgCaptureArgResult<TArg1, TCapture, TArg2, TResult> : IFunc<TArg1, TArg2, TResult>,
            IFunc<TArg1, TArg2, Promise<TResult>>
        {
            private readonly Func<TArg1, TCapture, TArg2, TResult> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateArgCaptureArgResult(in TCapture capturedValue, Func<TArg1, TCapture, TArg2, TResult> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke(in TArg1 arg1, in TArg2 arg2)
                => _callback.Invoke(arg1, _capturedValue, arg2);

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<TArg1, TArg2, Promise<TResult>>.Invoke(in TArg1 arg1, in TArg2 arg2)
                => Promise.Resolved(_callback.Invoke(arg1, _capturedValue, arg2));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct Delegate3ArgResult<TArg1, TArg2, TArg3, TResult> : IFunc<TArg1, TArg2, TArg3, TResult>,
            IFunc<TArg1, TArg2, TArg3, Promise<TResult>>
        {
            private readonly Func<TArg1, TArg2, TArg3, TResult> _callback;

            [MethodImpl(InlineOption)]
            public Delegate3ArgResult(Func<TArg1, TArg2, TArg3, TResult> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public TResult Invoke(in TArg1 arg1, in TArg2 arg2, in TArg3 arg3)
                => _callback.Invoke(arg1, arg2, arg3);

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<TArg1, TArg2, TArg3, Promise<TResult>>.Invoke(in TArg1 arg1, in TArg2 arg2, in TArg3 arg3)
                => Promise.Resolved(_callback.Invoke(arg1, arg2, arg3));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateCapture3ArgResult<TCapture, TArg1, TArg2, TArg3, TResult> : IFunc<TArg1, TArg2, TArg3, TResult>,
            IFunc<TArg1, TArg2, TArg3, Promise<TResult>>
        {
            private readonly Func<TCapture, TArg1, TArg2, TArg3, TResult> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateCapture3ArgResult(in TCapture capturedValue, Func<TCapture, TArg1, TArg2, TArg3, TResult> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke(in TArg1 arg1, in TArg2 arg2, in TArg3 arg3)
                => _callback.Invoke(_capturedValue, arg1, arg2, arg3);

            [MethodImpl(InlineOption)]
            Promise<TResult> IFunc<TArg1, TArg2, TArg3, Promise<TResult>>.Invoke(in TArg1 arg1, in TArg2 arg2, in TArg3 arg3)
                => Promise.Resolved(_callback.Invoke(_capturedValue, arg1, arg2, arg3));
        }
    }
}