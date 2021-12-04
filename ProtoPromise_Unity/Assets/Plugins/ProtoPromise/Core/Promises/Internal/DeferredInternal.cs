#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

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
                    get { return _idsAndRetains._deferredId; }
                }

                protected DeferredPromiseBase() { }

                ~DeferredPromiseBase()
                {
                    if (State == Promise.State.Pending)
                    {
                        // Deferred wasn't handled.
                        AddRejectionToUnhandledStack(UnhandledDeferredException.instance, this);
                    }
                }

                internal static bool GetIsValidAndPending(DeferredPromiseBase _this, short deferredId)
                {
                    return _this != null && _this.DeferredId == deferredId;
                }

                protected virtual bool TryUnregisterCancelation() { return true; }

                protected bool TryIncrementDeferredIdAndUnregisterCancelation(short deferredId)
                {
                    return _idsAndRetains.InterlockedTryIncrementDeferredId(deferredId)
                        && TryUnregisterCancelation(); // If TryUnregisterCancelation returns false, it means the CancelationSource was canceled.
                }

                internal static bool TryReject<TReject>(DeferredPromiseBase _this, short deferredId,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TReject reason, int rejectSkipFrames)
                {
                    return _this != null && _this.TryReject(deferredId, reason, 1);
                }

                [MethodImpl(InlineOption)]
                private bool TryReject<TReject>(short deferredId,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TReject reason, int rejectSkipFrames)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        RejectDirect(reason, rejectSkipFrames + 1);
                        return true;
                    }
                    return false;
                }

                internal static bool TryCancel<TCancel>(DeferredPromiseBase _this, short deferredId,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCancel reason)
                {
                    return _this != null && _this.TryCancel(deferredId, reason);
                }

                [MethodImpl(InlineOption)]
                private bool TryCancel<TCancel>(short deferredId,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCancel reason)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        CancelDirect(reason);
                        return true;
                    }
                    return false;
                }

                internal static bool TryCancelVoid(DeferredPromiseBase _this, short deferredId)
                {
                    return _this != null && _this.TryCancelVoid(deferredId);
                }

                [MethodImpl(InlineOption)]
                private bool TryCancelVoid(short deferredId)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        CancelDirect();
                        return true;
                    }
                    return false;
                }

                protected void CancelFromToken(ICancelValueContainer valueContainer)
                {
                    ThrowIfInPool(this);
                    // A simple increment is sufficient.
                    // If the CancelationSource was canceled before the Deferred was completed, even if the Deferred was completed before the cancelation was invoked, the cancelation takes precedence.
                    _idsAndRetains.InterlockedIncrementDeferredId();
                    RejectOrCancelInternal(valueContainer);
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

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                // Used for child to call base dispose without repooling for both types.
                // This is necessary because C# doesn't allow `base.base.Dispose()`.
                [MethodImpl(InlineOption)]
                protected void SuperDispose()
                {
                    base.Dispose();
                }

                internal static DeferredPromise<T> GetOrCreate()
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<DeferredPromise<T>>()
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
                    return _this != null && _this.TryResolve(deferredId, value);
                }

                [MethodImpl(InlineOption)]
                private bool TryResolve(short deferredId,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    T value)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        ResolveDirect(value);
                        return true;
                    }
                    return false;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class DeferredPromiseCancel<T> : DeferredPromise<T>, ICancelDelegate
            {
                private DeferredPromiseCancel() { }

                protected override void Dispose()
                {
                    SuperDispose();
                    _cancelationRegistration = default(CancelationRegistration);
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                internal static DeferredPromiseCancel<T> GetOrCreate(CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<DeferredPromiseCancel<T>>()
                        ?? new DeferredPromiseCancel<T>();
                    promise.Reset();
                    cancelationToken.TryRegisterInternal(promise, out promise._cancelationRegistration);
                    return promise;
                }

                protected override bool TryUnregisterCancelation()
                {
                    ThrowIfInPool(this);
                    return TryUnregisterAndIsNotCanceling(ref _cancelationRegistration);
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    CancelFromToken(valueContainer);
                }

                void ICancelDelegate.Dispose() { ThrowIfInPool(this); }
            }
        } // class PromiseRef
    } // class Internal
} // namespace Proto.Promises