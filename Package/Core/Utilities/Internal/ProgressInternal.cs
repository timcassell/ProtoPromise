#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0031 // Use null propagation
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0250 // Make struct 'readonly'
#pragma warning disable IDE0251 // Make member 'readonly'

namespace Proto.Promises
{
    partial class Internal
    {
        [MethodImpl(InlineOption)]
        internal static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal struct DelegateProgress : IProgress<double>
        {
            private readonly Action<double> _callback;

            [MethodImpl(InlineOption)]
            public DelegateProgress(Action<double> callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public void Report(double value)
            {
                _callback.Invoke(value);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal struct DelegateCaptureProgress<TCapture> : IProgress<double>
        {
            private readonly Action<TCapture, double> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateCaptureProgress(in TCapture capturedValue, Action<TCapture, double> callback)
            {
                _capturedValue = capturedValue;
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public void Report(double value)
            {
                _callback.Invoke(_capturedValue, value);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal ref struct NewProgressReportValues
        {
            internal ProgressBase _reporter;
            internal ProgressBase _next;
            internal double _value;
            internal int _id;

            [MethodImpl(InlineOption)]
            internal NewProgressReportValues(ProgressBase reporter, ProgressBase next, double value, int id)
            {
                _reporter = reporter;
                _next = next;
                _value = value;
                _id = id;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
        // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
        internal struct ProgressSmallFields
        {
            // Must not be readonly.
            internal SpinLocker _locker;
            internal int _id;

            internal ProgressSmallFields(int instanceId)
            {
                _locker = new SpinLocker();
                _id = instanceId;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class ProgressBase : HandleablePromiseBase, ITraceable
        {
#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif

            // Start with Id 1 instead of 0 to reduce risk of false positives.
            // Must not be readonly.
            internal ProgressSmallFields _smallFields = new ProgressSmallFields(1);

            internal int Id
            {
                [MethodImpl(InlineOption)]
                get { return _smallFields._id; }
            }

            internal virtual void ExitLock()
            {
                _smallFields._locker.Exit();
            }

            internal abstract void Report(double value, int id);
            internal abstract void Report(ref NewProgressReportValues reportValues);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class ProgressListener : ProgressBase
        {
            internal abstract Promise DisposeAsync(int id);
        }

        // Helper method to avoid typing out the TProgress.
        [MethodImpl(InlineOption)]
        internal static ProgressListener GetOrCreateProgress<TProgress>(TProgress progress, SynchronizationContext invokeContext, bool forceAsync, CancelationToken cancelationToken)
            where TProgress : IProgress<double>
        {
            return Progress<TProgress>.GetOrCreate(progress, invokeContext, forceAsync, cancelationToken);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class Progress<TProgress> : ProgressListener, ICancelable
            where TProgress : IProgress<double>
        {
            private TProgress _progress;
            private SynchronizationContext _invokeContext;
            private CancelationRegistration _cancelationRegistration;
            private double _current;
            private uint _retainCounter;
            private bool _isProgressScheduled;
            private bool _forceAsync;
            private bool _canceled;
            private bool _disposed;

            private Progress() { }

            ~Progress()
            {
                if (!_disposed)
                {
                    ReportRejection(new UnreleasedObjectException("A Progress's resources were garbage collected without it being disposed."), this);
                }
            }

            [MethodImpl(InlineOption)]
            private static Progress<TProgress> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<Progress<TProgress>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new Progress<TProgress>()
                    : obj.UnsafeAs<Progress<TProgress>>();
            }

            internal static Progress<TProgress> GetOrCreate(TProgress progress, SynchronizationContext invokeContext, bool forceAsync, CancelationToken cancelationToken)
            {
                var instance = GetOrCreate();
                instance._next = null;
                instance._progress = progress;
                instance._invokeContext = invokeContext;
                instance._forceAsync = forceAsync;
                // Set to nan so the first Report(0) will invoke.
                instance._current = float.NaN;
                instance._retainCounter = 1;
                instance._canceled = false;
                instance._disposed = false;

                SetCreatedStacktrace(instance, 2);
                // Hook up the cancelation last.
                cancelationToken.TryRegister(instance, out instance._cancelationRegistration);
                return instance;
            }

            private void DisposeAndRepool()
            {
                _progress = default(TProgress);
                _invokeContext = null;
                ObjectPool.MaybeRepool(this);
            }

            [MethodImpl(InlineOption)]
            private bool ShouldInvokeSynchronous()
            {
                return _invokeContext == null | (!_forceAsync & _invokeContext == Promise.Manager.ThreadStaticSynchronizationContext);
            }

            [MethodImpl(InlineOption)]
            private void Retain()
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                checked
#else
                unchecked
#endif
                {
                    ++_retainCounter;
                }
            }

            internal override void Report(double value, int id)
            {
                _smallFields._locker.Enter();
                ReportCore(value, id);
            }

            internal override void Report(ref NewProgressReportValues reportValues)
            {
                // Enter this lock before exiting previous lock.
                // This prevents a race condition where another report on a separate thread could get ahead of this report.
                _smallFields._locker.Enter();
                reportValues._reporter.ExitLock();
                ReportCore(reportValues._value, reportValues._id);
                // Set the next to null to notify the end of the caller loop.
                reportValues._next = null;
            }

            private void ReportCore(double value, int id)
            {
                // Lock is already entered from the caller.
                if (_canceled | id != _smallFields._id | _current == value)
                {
                    _smallFields._locker.Exit();
                    return;
                }
                ThrowIfInPool(this);

                _current = value;

                if (ShouldInvokeSynchronous())
                {
                    Retain();
                    // Exit the lock before invoking so we're not holding the lock while user code runs.
                    _smallFields._locker.Exit();
                    InvokeAndCatch(value);
                    AfterInvoke();
                    return;
                }

                if (_isProgressScheduled)
                {
                    _smallFields._locker.Exit();
                    return;
                }

                _isProgressScheduled = true;
                Retain();
                // Exit the lock before scheduling on the context.
                _smallFields._locker.Exit();

                ScheduleContextCallback(_invokeContext, this,
                    obj => obj.UnsafeAs<Progress<TProgress>>().ReportOnContext(),
                    obj => obj.UnsafeAs<Progress<TProgress>>().ReportOnContext()
                );
            }

            private void InvokeAndCatch(double value)
            {
                SetCurrentInvoker(this);
                try
                {
                    _progress.Report(value);
                }
                catch (Exception e)
                {
                    ReportRejection(e, this);
                }
                ClearCurrentInvoker();
            }

            private void AfterInvoke()
            {
                unchecked
                {
                    _smallFields._locker.Enter();
                    uint retains = --_retainCounter;
                    var next = _next;
                    _smallFields._locker.Exit();
                    if (retains == 0)
                    {
                        DisposeAndRepool();
                        // _next is used to store the deferred promise used for DisposeAsync.
                        if (next != null)
                        {
                            next.UnsafeAs<PromiseRefBase.DeferredPromise<VoidResult>>().ResolveDirectVoid();
                        }
                    }
                }
            }

            private void ReportOnContext()
            {
                ThrowIfInPool(this);

                _smallFields._locker.Enter();
                double progress = _current;
                _isProgressScheduled = false;
                var cancelationToken = _cancelationRegistration.Token;
                // Exit the lock before invoking so we're not holding the lock while user code runs.
                _smallFields._locker.Exit();

                if (!_canceled & !cancelationToken.IsCancelationRequested)
                {
                    InvokeAndCatch(progress);
                }

                AfterInvoke();
            }

            void ICancelable.Cancel()
            {
                ThrowIfInPool(this);
                _canceled = true;
            }

            internal override Promise DisposeAsync(int id)
            {
                _smallFields._locker.Enter();
                if (id != _smallFields._id)
                {
                    _smallFields._locker.Exit();
                    // IAsyncDisposable.DisposeAsync must not throw if it's called multiple times, according to MSDN documentation.
                    return Promise.Resolved();
                }

                ThrowIfInPool(this);
                _disposed = true;
                unchecked
                {
                    ++_smallFields._id;

                    var registration = _cancelationRegistration;
                    _cancelationRegistration = default(CancelationRegistration);

                    if (--_retainCounter == 0)
                    {
                        _smallFields._locker.Exit();
                        registration.Dispose();
                        DisposeAndRepool();
                        return Promise.Resolved();
                    }

                    // Not all invokes are complete yet, create a deferred promise that will be resolved when all invokes are complete.
                    var deferredPromise = PromiseRefBase.DeferredPromise<VoidResult>.GetOrCreate();
                    // We store the deferred promise in _next to save memory.
                    _next = deferredPromise;
                    registration.Dispose();
                    _smallFields._locker.Exit();
                    return new Promise(deferredPromise, deferredPromise.Id);
                }
            }
        }
    } // class Internal
} // namespace Proto.Promises