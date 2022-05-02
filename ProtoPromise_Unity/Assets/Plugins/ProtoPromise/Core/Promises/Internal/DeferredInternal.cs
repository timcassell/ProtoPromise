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
        partial class PromiseRef
        {

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class DeferredPromiseBase : AsyncPromiseBase
            {
                internal short DeferredId
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
                            AddRejectionToUnhandledStack(UnhandledDeferredException.instance, this);
                        }
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        AddRejectionToUnhandledStack(e, this);
                    }
                }

                internal static bool GetIsValidAndPending(DeferredPromiseBase _this, short deferredId)
                {
                    return _this != null && _this.DeferredId == deferredId;
                }

                protected virtual bool TryUnregisterCancelation() { return true; }

                protected bool TryIncrementDeferredIdAndUnregisterCancelation(short deferredId)
                {
                    return _smallFields.InterlockedTryIncrementDeferredId(deferredId)
                        && TryUnregisterCancelation(); // If TryUnregisterCancelation returns false, it means the CancelationSource was canceled.
                }

                internal static bool TryReject<TReject>(DeferredPromiseBase _this, short deferredId,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TReject reason, int rejectSkipFrames)
                {
                    if (_this != null && _this.TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        _this.RejectDirect(reason, rejectSkipFrames + 1);
                        return true;
                    }
                    return false;
                }

                internal static bool TryCancel(DeferredPromiseBase _this, short deferredId)
                {
                    if (_this != null && _this.TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        _this.CancelDirect();
                        return true;
                    }
                    return false;
                }

                protected void CancelFromToken()
                {
                    ThrowIfInPool(this);
                    // A simple increment is sufficient.
                    // If the CancelationSource was canceled before the Deferred was completed, even if the Deferred was completed before the cancelation was invoked, the cancelation takes precedence.
                    _smallFields.InterlockedIncrementDeferredId();
                    CancelDirect();
                }

                internal static bool TryReportProgress(DeferredPromiseBase _this, short deferredId, float progress)
                {
                    ValidateProgress(progress, 1);
#if !PROMISE_PROGRESS
                    return GetIsValidAndPending(_this, deferredId);
#else
                    return _this != null && _this.TryReportProgress(deferredId, progress);
#endif
                }
            }

            // The only purpose of this is to cast the ref when converting a DeferredBase to a Deferred(<T>) to avoid extra checks.
            // Otherwise, DeferredPromise<T> would be unnecessary and this would be implemented in the base class.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal class DeferredPromise<T> : DeferredPromiseBase
            {
                protected DeferredPromise() { }

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                internal static DeferredPromise<T> GetOrCreate()
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<DeferredPromise<T>>()
                        ?? new DeferredPromise<T>();
                    promise.Reset();
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal static bool TryResolve(DeferredPromise<T> _this, short deferredId,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    T value)
                {
                    if (_this != null && _this.TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        _this.ResolveDirect(value);
                        return true;
                    }
                    return false;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class DeferredPromiseCancel<T> : DeferredPromise<T>, ICancelable
            {
                private DeferredPromiseCancel() { }

                protected override void MaybeDispose()
                {
                    Dispose();
                    _cancelationRegistration = default(CancelationRegistration);
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                internal static DeferredPromiseCancel<T> GetOrCreate(CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<DeferredPromiseCancel<T>>()
                        ?? new DeferredPromiseCancel<T>();
                    promise.Reset();
                    cancelationToken.TryRegister(promise, out promise._cancelationRegistration);
                    return promise;
                }

                protected override bool TryUnregisterCancelation()
                {
                    ThrowIfInPool(this);
                    return TryUnregisterAndIsNotCanceling(ref _cancelationRegistration);
                }

                void ICancelable.Cancel()
                {
                    CancelFromToken();
                }
            }
        } // class PromiseRef
    } // class Internal
} // namespace Proto.Promises