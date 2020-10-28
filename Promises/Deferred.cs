#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

namespace Proto.Promises
{
    partial class Promise
    {
        /// <summary>
        /// Deferred base. An instance of this can be used to report progress and reject the attached <see cref="Promises.Promise"/>.
        /// <para/>You must use <see cref="Deferred"/> or <see cref="Promise{T}.Deferred"/> to resolve the attached <see cref="Promises.Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public struct DeferredBase : IRetainable
        {
            private readonly Promise _promise;
            private readonly ushort _id;

            /// <summary>
            /// The attached <see cref="Promises.Promise"/> that this controls.
            /// </summary>
            public Promise Promise
            {
                get
                {
                    if (!IsValid)
                    {
                        throw new InvalidOperationException("DeferredBase.Promise: instance is not valid.", Internal.GetFormattedStacktrace(1));
                    }
                    return _promise;
                }
            }

            /// <summary>
            /// The state of the attached <see cref="Promises.Promise"/>
            /// </summary>
            public State State
            {
                get
                {
                    if (!IsValid)
                    {
                        throw new InvalidOperationException("DeferredBase.State: instance is not valid.", Internal.GetFormattedStacktrace(1));
                    }
                    return _promise._state;
                }
            }

            /// <summary>
            /// Get whether or not this instance is valid.
            /// <para/>A <see cref="DeferredBase"/> instance is valid if it was casted from a <see cref="Deferred"/> or <see cref="Promise{T}.Deferred"/> instance, and that instance is still valid.
            /// </summary>
            public bool IsValid
            {
                get
                {
                    ValidateThreadAccess(1);

                    return _promise != null && _id == _promise.Id;
                }
            }

            private DeferredBase(Promise promise)
            {
                _promise = promise;
                _id = promise.Id;
            }

            /// <summary>
            /// Internal use for implicit cast operator.
            /// </summary>
            internal static DeferredBase Create(Deferred deferred)
            {
                return deferred.IsValid ? new DeferredBase(deferred.Promise) : default(DeferredBase);
            }

            /// <summary>
            /// Internal use for implicit cast operator.
            /// </summary>
            internal static DeferredBase Create<T>(Promise<T>.Deferred deferred)
            {
                return deferred.IsValid ? new DeferredBase(deferred.Promise) : default(DeferredBase);
            }

            /// <summary>
            /// Cast this to <see cref="Deferred"/>.
            /// </summary>
            public Deferred ToDeferred()
            {
                return Deferred.Create(this);
            }

            /// <summary>
            /// Cast this to <see cref="Promise{T}.Deferred"/>.
            /// </summary>
            public Promise<T>.Deferred ToDeferred<T>()
            {
                return Promise<T>.Deferred.Create(this);
            }

            /// <summary>
            /// Retain this instance and the linked <see cref="Promises.Promise"/>.
            /// <para/>This should always be paired with a call to <see cref="Release"/>
            /// </summary>
            public void Retain()
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("DeferredBase.Retain: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
                _promise.Retain();
            }

            /// <summary>
            /// Release this instance and the linked <see cref="Promises.Promise"/>.
            /// <para/>This should always be paired with a call to <see cref="Retain"/>
            /// </summary>
            public void Release()
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("DeferredBase.Release: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
                _promise.Release();
            }

            /// <summary>
            /// Reject the linked <see cref="Promises.Promise"/> with <paramref name="reason"/>.
            /// </summary>
            public void Reject<TReject>(TReject reason)
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("DeferredBase.Reject: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }

                var promise = Promise;
                ValidateOperation(promise, 1);

                if (State == State.Pending)
                {
                    Promise.CancelCallbacks();
                    promise.RejectDirect(ref reason, 1);
                }
                else
                {
                    Internal.AddRejectionToUnhandledStack(reason, null);
                    Manager.LogWarning("DeferredBase.Reject - Deferred is not in the pending state.");
                }
            }

            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
#if !PROMISE_PROGRESS
            [System.Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
            public void ReportProgress(float progress)
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("DeferredBase.ReportProgress: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }

                var promise = Promise;
                ValidateProgress(1);
                ValidateOperation(promise, 1);
                ValidateProgress(progress, 1);

                if (State != State.Pending)
                {
                    Manager.LogWarning("DeferredBase.ReportProgress - Deferred is not in the pending state.");
                    return;
                }

                promise.ReportProgress(progress);
            }
        }

        /// <summary>
        /// An instance of this is used to report progress and resolve or reject the attached <see cref="Promises.Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public struct Deferred
        {
            private readonly Promise _promise;
            private readonly ushort _id;

            /// <summary>
            /// The attached <see cref="Promises.Promise"/> that this controls.
            /// </summary>
            public Promise Promise
            {
                get
                {
                    if (!IsValid)
                    {
                        throw new InvalidOperationException("Deferred.Promise: instance is not valid.", Internal.GetFormattedStacktrace(1));
                    }
                    return _promise;
                }
            }

            /// <summary>
            /// The state of the attached <see cref="Promises.Promise"/>
            /// </summary>
            public State State
            {
                get
                {
                    if (!IsValid)
                    {
                        throw new InvalidOperationException("Deferred.State: instance is not valid.", Internal.GetFormattedStacktrace(1));
                    }
                    return _promise._state;
                }
            }

            /// <summary>
            /// Get whether or not this instance is valid.
            /// <para/>A <see cref="Deferred"/> is valid if it was created from <see cref="New"/> and the attached <see cref="Promises.Promise"/> has not been disposed.
            /// </summary>
            public bool IsValid
            {
                get
                {
                    ValidateThreadAccess(1);

                    return _promise != null && _id == _promise.Id;
                }
            }

            private Deferred(CancelationToken cancelationToken)
            {
                if (cancelationToken.CanBeCanceled)
                {
                    _promise = InternalProtected.DeferredPromiseCancelVoid.GetOrCreate(cancelationToken);
                }
                else
                {
                    _promise = InternalProtected.DeferredPromiseVoid.GetOrCreate();
                }
                _id = _promise.Id;
            }

            private Deferred(Promise promise)
            {
                _promise = promise;
                _id = promise.Id;
            }

            /// <summary>
            /// Returns a new <see cref="Deferred"/> instance that is linked to and controls the state of a new <see cref="Promises.Promise"/>.
            /// <para/>If the <paramref name="cancelationToken"/> is canceled while the <see cref="Deferred"/> is pending, it and the <see cref="Promises.Promise"/> will be canceled with its reason.
            /// </summary>
            public static Deferred New(CancelationToken cancelationToken = default(CancelationToken))
            {
                ValidateThreadAccess(1);

                return new Deferred(cancelationToken);
            }

            /// <summary>
            /// Internal use for explicit converter. Use <see cref="DeferredBase.ToDeferred"/>.
            /// </summary>
            internal static Deferred Create(DeferredBase deferredBase)
            {
                if (deferredBase.IsValid)
                {
                    Promise promise = deferredBase.Promise;
                    if (promise.ResultType == null)
                    {
                        return new Deferred(promise);
                    }
                }
                return default(Deferred);
            }

            /// <summary>
            /// Retain this instance and the linked <see cref="Promises.Promise"/>.
            /// <para/>This should always be paired with a call to <see cref="Release"/>
            /// </summary>
            public void Retain()
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Deferred.Retain: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
                _promise.Retain();
            }

            /// <summary>
            /// Release this instance and the linked <see cref="Promises.Promise"/>.
            /// <para/>This should always be paired with a call to <see cref="Retain"/>
            /// </summary>
            public void Release()
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Deferred.Release: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
                _promise.Release();
            }

            /// <summary>
            /// Reject the linked <see cref="Promises.Promise"/> with <paramref name="reason"/>.
            /// </summary>
            public void Reject<TReject>(TReject reason)
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }

                var promise = Promise;
                ValidateOperation(promise, 1);

                if (State == State.Pending)
                {
                    Promise.CancelCallbacks();
                    promise.RejectDirect(ref reason, 1);
                }
                else
                {
                    Internal.AddRejectionToUnhandledStack(reason, null);
                    Manager.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                }
            }

            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
#if !PROMISE_PROGRESS
            [System.Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
            public void ReportProgress(float progress)
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Deferred.ReportProgress: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }

                var promise = Promise;
                ValidateProgress(1);
                ValidateOperation(promise, 1);
                ValidateProgress(progress, 1);

                if (State != State.Pending)
                {
                    Manager.LogWarning("Deferred.ReportProgress - Deferred is not in the pending state.");
                    return;
                }

                promise.ReportProgress(progress);
            }

            /// <summary>
            /// Resolve the linked <see cref="Promises.Promise"/>.
            /// </summary>
            public void Resolve()
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Deferred.Resolve: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }

                var promise = Promise;
                ValidateOperation(promise, 1);

                if (State == State.Pending)
                {
                    Promise.CancelCallbacks();
                    promise.ResolveDirect();
                }
                else
                {
                    Manager.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                    return;
                }
            }

            public static implicit operator DeferredBase(Deferred deferred)
            {
                return DeferredBase.Create(deferred);
            }
        }
    }

    public partial class Promise<T>
    {
        /// <summary>
        /// An instance of this is used to handle the state of the <see cref="Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public new struct Deferred
        {
            private readonly Promise<T> _promise;
            private readonly ushort _id;

            /// <summary>
            /// The attached <see cref="Promise{T}"/> that this controls.
            /// </summary>
            public Promise<T> Promise
            {
                get
                {
                    if (!IsValid)
                    {
                        throw new InvalidOperationException("Deferred.Promise: instance is not valid.", Internal.GetFormattedStacktrace(1));
                    }
                    return _promise;
                }
            }

            /// <summary>
            /// The state of the attached <see cref="Promises.Promise"/>
            /// </summary>
            public State State
            {
                get
                {
                    if (!IsValid)
                    {
                        throw new InvalidOperationException("Deferred.State: instance is not valid.", Internal.GetFormattedStacktrace(1));
                    }
                    return _promise._state;
                }
            }

            /// <summary>
            /// Get whether or not this instance is valid.
            /// <para/>A <see cref="Deferred"/> is valid if it was created from <see cref="New"/> and the attached <see cref="Promises.Promise"/> has not been disposed.
            /// </summary>
            public bool IsValid
            {
                get
                {
                    ValidateThreadAccess(1);

                    return _promise != null && _id == _promise.Id;
                }
            }

            private Deferred(CancelationToken cancelationToken)
            {
                if (cancelationToken.CanBeCanceled)
                {
                    _promise = InternalProtected.DeferredPromiseCancel<T>.GetOrCreate(cancelationToken);
                }
                else
                {
                    _promise = InternalProtected.DeferredPromise<T>.GetOrCreate();
                }
                _id = _promise.Id;
            }

            private Deferred(Promise<T> promise)
            {
                _promise = promise;
                _id = promise.Id;
            }

            /// <summary>
            /// Returns a new <see cref="Deferred"/> instance that is linked to and controls the state of a new <see cref="Promises.Promise"/>.
            /// <para/>If the <paramref name="cancelationToken"/> is canceled while the <see cref="Deferred"/> is pending, it and the <see cref="Promises.Promise"/> will be canceled with its reason.
            /// </summary>
            public static Deferred New(CancelationToken cancelationToken = default(CancelationToken))
            {
                ValidateThreadAccess(1);

                return new Deferred(cancelationToken);
            }

            /// <summary>
            /// Internal use for explicit converter. Use <see cref="DeferredBase.ToDeferred"/>.
            /// </summary>
            internal static Deferred Create(DeferredBase deferredBase)
            {
                if (deferredBase.IsValid)
                {
                    Promise promise = deferredBase.Promise;
                    if (promise.ResultType == typeof(T))
                    {
                        return new Deferred((Promise<T>) promise);
                    }
                }
                return default(Deferred);
            }

            /// <summary>
            /// Retain this instance and the linked <see cref="Promises.Promise"/>.
            /// <para/>This should always be paired with a call to <see cref="Release"/>
            /// </summary>
            public void Retain()
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Deferred.Retain: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
                _promise.Retain();
            }

            /// <summary>
            /// Release this instance and the linked <see cref="Promises.Promise"/>.
            /// <para/>This should always be paired with a call to <see cref="Retain"/>
            /// </summary>
            public void Release()
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Deferred.Release: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
                _promise.Release();
            }

            /// <summary>
            /// Reject the linked <see cref="Promises.Promise"/> with <paramref name="reason"/>.
            /// </summary>
            public void Reject<TReject>(TReject reason)
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }

                var promise = Promise;
                ValidateOperation(promise, 1);

                if (State == State.Pending)
                {
                    Promise.CancelCallbacks();
                    promise.RejectDirect(ref reason, 1);
                }
                else
                {
                    Internal.AddRejectionToUnhandledStack(reason, null);
                    Manager.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                }
            }

            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
#if !PROMISE_PROGRESS
            [System.Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
            public void ReportProgress(float progress)
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Deferred.ReportProgress: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }

                var promise = Promise;
                ValidateProgress(1);
                ValidateOperation(promise, 1);
                ValidateProgress(progress, 1);

                if (State != State.Pending)
                {
                    Manager.LogWarning("Deferred.ReportProgress - Deferred is not in the pending state.");
                    return;
                }

                promise.ReportProgress(progress);
            }

            /// <summary>
            /// Resolve the linked <see cref="Promise{T}"/> with <paramref name="value"/>.
            /// </summary>
            public void Resolve(T value)
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Deferred.Resolve: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }

                var promise = Promise;
                ValidateOperation(promise, 1);

                if (State == State.Pending)
                {
                    Promise.CancelCallbacks();
                    promise.ResolveDirect(ref value);
                }
                else
                {
                    Manager.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                    return;
                }
            }

            public static implicit operator DeferredBase(Deferred deferred)
            {
                return DeferredBase.Create(deferred);
            }
        }
    }
}