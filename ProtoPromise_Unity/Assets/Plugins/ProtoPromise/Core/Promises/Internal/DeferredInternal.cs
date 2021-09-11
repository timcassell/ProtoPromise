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
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal partial struct DeferredInternal<TDeferredRef> where TDeferredRef : PromiseRef.DeferredPromiseBase
        {
            internal readonly TDeferredRef _ref;
            internal readonly short _promiseId;
            internal readonly short _deferredId;

            [MethodImpl(InlineOption)]
            public Promise GetPromiseVoid()
            {
                return new Promise(_ref, _promiseId);
            }

            [MethodImpl(InlineOption)]
            public Promise<T> GetPromise<T>()
            {
                return new Promise<T>(_ref, _promiseId);
            }

            public bool IsValidAndPending
            {
                get
                {
                    DeferredInternal<TDeferredRef> _this = this;
                    return _this._ref != null && _this._deferredId == _this._ref.DeferredId;
                }
            }

            [MethodImpl(InlineOption)]
            internal DeferredInternal(TDeferredRef deferredRef, short promiseId, short deferredId)
            {
                _ref = deferredRef;
                _promiseId = promiseId;
                _deferredId = deferredId;
            }

            public void ResolveVoid()
            {
                if (!TryResolveVoid())
                {
                    throw new InvalidOperationException("Deferred.Resolve: instance is not valid.", GetFormattedStacktrace(1));
                }
            }

            public bool TryResolveVoid()
            {
                DeferredInternal<TDeferredRef> _this = this;
                return _this._ref != null && _this._ref.TryResolveVoid(_this._deferredId);
            }

            public void Resolve<T>(ref T value)
            {
                if (!TryResolve(ref value))
                {
                    throw new InvalidOperationException("Deferred.Resolve: instance is not valid.", GetFormattedStacktrace(1));
                }
            }

            public bool TryResolve<T>(ref T value)
            {
                DeferredInternal<TDeferredRef> _this = this;
                return _this._ref != null && _this._ref.TryResolve(ref value, _this._deferredId);
            }

            public void Reject<TReject>(ref TReject reason)
            {
                if (!TryReject(ref reason))
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid.", GetFormattedStacktrace(1));
                }
            }

            public bool TryReject<TReject>(ref TReject reason)
            {
                DeferredInternal<TDeferredRef> _this = this;
                return _this._ref != null && _this._ref.TryReject(ref reason, _this._deferredId, 1);
            }

            public void Cancel<TCancel>(ref TCancel reason)
            {
                if (!TryCancel(ref reason))
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid.", GetFormattedStacktrace(1));
                }
            }

            public bool TryCancel<TCancel>(ref TCancel reason)
            {
                DeferredInternal<TDeferredRef> _this = this;
                return _this._ref != null && _this._ref.TryCancel(ref reason, _this._deferredId);
            }

            public void Cancel()
            {
                if (!TryCancel())
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid.", GetFormattedStacktrace(1));
                }
            }

            public bool TryCancel()
            {
                DeferredInternal<TDeferredRef> _this = this;
                return _this._ref != null && _this._ref.TryCancelVoid(_this._deferredId);
            }

            public void ReportProgress(float progress)
            {
                if (!TryReportProgress(progress))
                {
                    throw new InvalidOperationException("Deferred.ReportProgress: instance is not valid.", GetFormattedStacktrace(1));
                }
            }

            public bool TryReportProgress(float progress)
            {
                ValidateProgress(progress, 1);
#if !PROMISE_PROGRESS
                return IsValidAndPending;
#else
                DeferredInternal<TDeferredRef> _this = this;
                return _this._ref != null && _this._ref.TryReportProgress(progress, _this._deferredId);
#endif
            }

            public override int GetHashCode()
            {
                DeferredInternal<TDeferredRef> _this = this;
                if (_this._ref == null)
                {
                    return 0;
                }
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + _this._deferredId.GetHashCode();
                    hash = hash * 31 + _this._promiseId.GetHashCode();
                    hash = hash * 31 + _this._ref.GetHashCode();
                    return hash;
                }
            }

            public override bool Equals(object obj)
            {
#if CSHARP_7_3_OR_NEWER
                return obj is DeferredInternal<TDeferredRef> deferred && this == deferred;
#else
                return obj is DeferredInternal<TDeferredRef> && this == (DeferredInternal<TDeferredRef>) obj;
#endif
            }

            public static bool operator ==(DeferredInternal<TDeferredRef> lhs, DeferredInternal<TDeferredRef> rhs)
            {
                return lhs._ref == rhs._ref
                    & lhs._deferredId == rhs._deferredId
                    & lhs._promiseId == rhs._promiseId;
            }

            [MethodImpl(InlineOption)]
            public static bool operator !=(DeferredInternal<TDeferredRef> lhs, DeferredInternal<TDeferredRef> rhs)
            {
                return !(lhs == rhs);
            }
        }

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

                protected virtual bool TryUnregisterCancelation() { return true; }

                protected bool TryIncrementDeferredIdAndUnregisterCancelation(short deferredId)
                {
                    return _idsAndRetains.InterlockedTryIncrementDeferredId(deferredId)
                        && TryUnregisterCancelation(); // If TryUnregisterCancelation returns false, it means the CancelationSource was canceled.
                }

                [MethodImpl(InlineOption)]
                internal bool TryResolveVoid(short deferredId)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        ResolveDirect();
                        return true;
                    }
                    return false;
                }

                [MethodImpl(InlineOption)]
                internal bool TryResolve<T>(ref T value, short deferredId)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        ResolveDirect(ref value);
                        return true;
                    }
                    return false;
                }

                internal bool TryReject<TReject>(ref TReject reason, short deferredId, int rejectSkipFrames)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        RejectDirect(ref reason, rejectSkipFrames + 1);
                        return true;
                    }
                    return false;
                }

                internal bool TryCancel<TCancel>(ref TCancel reason, short deferredId)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        CancelDirect(ref reason);
                        return true;
                    }
                    return false;
                }

                internal bool TryCancelVoid(short deferredId)
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
            }

            // The only purpose of this is to cast the ref when converting a DeferredBase to a Deferred(<T>) to avoid extra checks.
            // Otherwise, DeferredPromise<T> would be unnecessary and this would be implemented in the base class.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal class DeferredPromiseVoid : DeferredPromiseBase
            {
                protected DeferredPromiseVoid() { }

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

                internal static DeferredPromiseVoid GetOrCreate()
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<DeferredPromiseVoid>()
                        ?? new DeferredPromiseVoid();
                    promise.Reset();
                    promise.ResetDepth();
                    return promise;
                }
            }

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
                    promise.ResetDepth();
                    return promise;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class DeferredPromiseVoidCancel : DeferredPromiseVoid, ICancelDelegate
            {
                private DeferredPromiseVoidCancel() { }

                protected override void Dispose()
                {
                    SuperDispose();
                    _cancelationRegistration = default(CancelationRegistration);
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                internal static DeferredPromiseVoidCancel GetOrCreate(CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<DeferredPromiseVoidCancel>()
                        ?? new DeferredPromiseVoidCancel();
                    promise.Reset();
                    promise.ResetDepth();
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
                    promise.ResetDepth();
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