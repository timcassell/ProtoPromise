#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0074 // Use compound assignment

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        internal interface IDeferredPromise : ITraceable
        {
            int DeferredId { get; }
            bool TryIncrementDeferredId(int deferredId);
            void RejectDirect(IRejectContainer reasonContainer);
            void CancelDirect();
        }

        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class DeferredPromiseBase<TResult> : PromiseSingleAwait<TResult>, IDeferredPromise
            {
                public int DeferredId
                {
                    [MethodImpl(InlineOption)]
                    get => _deferredId;
                }

                protected DeferredPromiseBase() { }

                ~DeferredPromiseBase()
                {
                    if (State == Promise.State.Pending)
                    {
                        // Deferred wasn't handled.
                        ReportRejection(UnhandledDeferredException.instance, this);
                    }
                }

                [MethodImpl(InlineOption)]
                public bool TryIncrementDeferredId(int deferredId)
                {
                    bool success = Interlocked.CompareExchange(ref _deferredId, unchecked(deferredId + 1), deferredId) == deferredId;
                    MaybeThrowIfInPool(this, success);
                    return success;
                }

                public void RejectDirect(IRejectContainer reasonContainer)
                {
                    RejectContainer = reasonContainer;
                    HandleNextInternal(Promise.State.Rejected);
                }

                [MethodImpl(InlineOption)]
                public void CancelDirect()
                {
                    ThrowIfInPool(this);
                    HandleNextInternal(Promise.State.Canceled);
                }
            }

            // The only purpose of this is to cast the ref when converting a DeferredBase to a Deferred(<T>) to avoid extra checks.
            // Otherwise, DeferredPromise<T> would be unnecessary and this would be implemented in the base class.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal partial class DeferredPromise<TResult> : DeferredPromiseBase<TResult>
            {
                protected DeferredPromise() { }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static DeferredPromise<TResult> GetFromPoolOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<DeferredPromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new DeferredPromise<TResult>()
                        : obj.UnsafeAs<DeferredPromise<TResult>>();
                }

                internal static DeferredPromise<TResult> GetOrCreate()
                {
                    var promise = GetFromPoolOrCreate();
                    promise.Reset();
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal static bool TryResolve(DeferredPromise<TResult> _this, int deferredId, in TResult value)
                {
                    if (_this?.TryIncrementDeferredId(deferredId) == true)
                    {
                        _this.ResolveDirect(value);
                        return true;
                    }
                    return false;
                }

                [MethodImpl(InlineOption)]
                internal static bool TryResolveVoid(DeferredPromise<TResult> _this, int deferredId)
                {
                    if (_this?.TryIncrementDeferredId(deferredId) == true)
                    {
                        _this.ResolveDirectVoid();
                        return true;
                    }
                    return false;
                }

                [MethodImpl(InlineOption)]
                internal void ResolveDirect(in TResult value)
                {
                    ThrowIfInPool(this);
                    _result = value;
                    HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                internal void ResolveDirectVoid()
                {
                    ThrowIfInPool(this);
                    HandleNextInternal(Promise.State.Resolved);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class DeferredNewPromise<TResult, TDelegate> : DeferredPromise<TResult>
                where TDelegate : IDelegateNew<TResult>
            {
                private DeferredNewPromise() { }

                internal override void MaybeDispose()
                {
                    // It is theoretically possible for this to be completed from the callback, and then the callback throws,
                    // causing this to attempt to complete again. The thread could be starved while another thread
                    // re-uses this object from the pool. This interlocked operation protects against that.
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _disposeCounter, -1) == 0)
                    {
                        Dispose();
                        ObjectPool.MaybeRepool(this);
                    }
                }

                [MethodImpl(InlineOption)]
                new private static DeferredNewPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<DeferredNewPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new DeferredNewPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<DeferredNewPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static DeferredNewPromise<TResult, TDelegate> GetOrCreate(TDelegate runner)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._disposeCounter = 2;
                    promise._runner = runner;
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal void RunOrScheduleOnContext(ContinuationOptions invokeOptions)
                {
                    if (invokeOptions.GetShouldContinueImmediately(out var context))
                    {
                        Run();
                        return;
                    }

                    ScheduleContextCallback(context, this,
                        obj => obj.UnsafeAs<DeferredNewPromise<TResult, TDelegate>>().Run(),
                        obj => obj.UnsafeAs<DeferredNewPromise<TResult, TDelegate>>().Run()
                    );
                }

                private void Run()
                {
                    ThrowIfInPool(this);

                    var deferredId = DeferredId;
                    var runner = _runner;
                    _runner = default;

                    SetCurrentInvoker(this);
                    try
                    {
                        runner.Invoke(this);
                    }
                    catch (OperationCanceledException)
                    {
                        // Don't do anything if the deferred was already completed.
                        if (TryIncrementDeferredId(deferredId))
                        {
                            CancelDirect();
                        }
                    }
                    catch (Exception e)
                    {
                        if (TryIncrementDeferredId(deferredId))
                        {
                            RejectDirect(CreateRejectContainer(e, int.MinValue, null, this));
                        }
                        else
                        {
                            // If the deferred was already completed, report the exception as unhandled.
                            ReportRejection(e, this);
                        }
                    }
                    ClearCurrentInvoker();
                    MaybeDispose();
                }
            }
        } // class PromiseRef
    } // class Internal
} // namespace Proto.Promises