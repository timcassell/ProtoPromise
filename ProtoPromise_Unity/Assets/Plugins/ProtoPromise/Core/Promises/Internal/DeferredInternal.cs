#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        internal interface IDeferredPromise : ITraceable
        {
            short DeferredId { get; }
            bool TryIncrementDeferredIdAndUnregisterCancelation(short deferredId);
            void RejectDirect(RejectContainer reasonContainer);
            void CancelDirect();
#if PROMISE_PROGRESS
            bool TryReportProgress(short deferredId, float progress);
#endif
        }

        internal static class DeferredPromiseHelper
        {
            internal static bool GetIsValidAndPending(IDeferredPromise _this, short deferredId)
            {
                return _this != null && _this.DeferredId == deferredId;
            }

            internal static bool TryIncrementDeferredIdAndUnregisterCancelation(IDeferredPromise _this, short deferredId)
            {
                return _this != null && _this.TryIncrementDeferredIdAndUnregisterCancelation(deferredId);
            }

            internal static bool TryReportProgress(IDeferredPromise _this, short deferredId, float progress)
            {
                ValidateProgressValue(progress, "progress", 1);
#if !PROMISE_PROGRESS
                return GetIsValidAndPending(_this, deferredId);
#else
                return _this != null && _this.TryReportProgress(deferredId, progress);
#endif
            }
        }

        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class DeferredPromiseBase<TResult> : AsyncPromiseBase<TResult>, IDeferredPromise
            {
                public short DeferredId
                {
                    [MethodImpl(InlineOption)]
                    get { return _smallFields._deferredId; }
                }

                protected DeferredPromiseBase() { }

                ~DeferredPromiseBase()
                {
                    try
                    {
                        if (State == Promise.State.Pending)
                        {
                            // Deferred wasn't handled.
                            ReportRejection(UnhandledDeferredException.instance, this);
                        }
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        ReportRejection(e, this);
                    }
                }


                [MethodImpl(InlineOption)]
                public virtual bool TryIncrementDeferredIdAndUnregisterCancelation(short deferredId)
                {
                    bool success = _smallFields.InterlockedTryIncrementDeferredId(deferredId);
                    ThrowIfInPool(this);
                    return success;
                }

                public void RejectDirect(RejectContainer reasonContainer)
                {
                    SetRejectOrCancel(reasonContainer, Promise.State.Rejected);
                    HandleNextInternal();
                }

                [MethodImpl(InlineOption)]
                public void CancelDirect()
                {
                    ThrowIfInPool(this);
                    SetRejectOrCancel(RejectContainer.s_completionSentinel, Promise.State.Canceled);
                    HandleNextInternal();
                }
            }

            // The only purpose of this is to cast the ref when converting a DeferredBase to a Deferred(<T>) to avoid extra checks.
            // Otherwise, DeferredPromise<T> would be unnecessary and this would be implemented in the base class.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal class DeferredPromise<TResult> : DeferredPromiseBase<TResult>
            {
                protected DeferredPromise() { }

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                internal static DeferredPromise<TResult> GetOrCreate()
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<DeferredPromise<TResult>>()
                        ?? new DeferredPromise<TResult>();
                    promise.Reset();
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal static bool TryResolve(DeferredPromise<TResult> _this, short deferredId,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TResult value)
                {
                    if (_this != null && _this.TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        _this.ResolveDirect(value);
                        return true;
                    }
                    return false;
                }

                [MethodImpl(InlineOption)]
                internal void ResolveDirect(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TResult value)
                {
                    SetResult(value);
                    HandleNextInternal();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class DeferredPromiseCancel<TResult> : DeferredPromise<TResult>, ICancelable
            {
                private DeferredPromiseCancel() { }

                protected override void MaybeDispose()
                {
                    Dispose();
                    _cancelationRegistration = default(CancelationRegistration);
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                internal static DeferredPromiseCancel<TResult> GetOrCreate(CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<DeferredPromiseCancel<TResult>>()
                        ?? new DeferredPromiseCancel<TResult>();
                    promise.Reset();
                    cancelationToken.TryRegister(promise, out promise._cancelationRegistration);
                    return promise;
                }

                private bool TryUnregisterCancelation()
                {
                    return TryUnregisterAndIsNotCanceling(ref _cancelationRegistration);
                }

                public override bool TryIncrementDeferredIdAndUnregisterCancelation(short deferredId)
                {
                    return base.TryIncrementDeferredIdAndUnregisterCancelation(deferredId)
                        && TryUnregisterCancelation(); // If TryUnregisterCancelation returns false, it means the CancelationSource was canceled.
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    // A simple increment is sufficient.
                    // If the CancelationSource was canceled before the Deferred was completed, even if the Deferred was completed before the cancelation was invoked, the cancelation takes precedence.
                    _smallFields.InterlockedIncrementDeferredId();
                    CancelDirect();
                }
            }
        } // class PromiseRef
    } // class Internal
} // namespace Proto.Promises