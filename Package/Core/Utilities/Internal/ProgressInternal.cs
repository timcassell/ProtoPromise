#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Diagnostics;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0290 // Use primary constructor

namespace Proto.Promises
{
    partial class Internal
    {
        [MethodImpl(InlineOption)]
        internal static double Lerp(double a, double b, double t)
            => a + (b - a) * t;

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal ref struct ProgressReportValues
        {
            internal ProgressBase _reporter;
            internal ProgressBase _next;
            internal double _value;
            internal int _id;

            [MethodImpl(InlineOption)]
            internal ProgressReportValues(ProgressBase reporter, ProgressBase next, double value, int id)
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
        internal abstract class ProgressBase : HandleablePromiseBase
        {
            // Start with Id 1 instead of 0 to reduce risk of false positives.
            // Must not be readonly.
            internal ProgressSmallFields _smallFields = new ProgressSmallFields(1);

            internal int Id
            {
                [MethodImpl(InlineOption)]
                get => _smallFields._id;
            }

            internal virtual void ExitLock()
                => _smallFields._locker.Exit();

            internal abstract void Report(double value, int id);
            internal abstract void Report(ref ProgressReportValues reportValues);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class ProgressListener : ProgressBase
        {
            internal abstract Promise DisposeAsync(int id);

            // Invoke, but the handler runs in the scope of this progress's trace.
            // It's not generic over the handler, so that AOT compilers (e.g. IL2CPP) don't generate it for every handler type.
            // NoInlining to keep the common case small.
            [MethodImpl(MethodImplOptions.NoInlining)]
            protected void InvokeInScope(double value)
            {
                // A progress that wasn't traced doesn't enter a scope, the same as the common case.
                var scope = CausalityTrace.EnterScopeInternal(GetTrace(this));
                try
                {
                    InvokeHandler(value);
                }
                catch (Exception e)
                {
                    // Reported inside the scope, the same as other callbacks (see ReportCaughtException).
                    ReportCaughtException(e, this);
                }
                finally
                {
                    scope.Dispose();
                }
            }

            protected abstract void InvokeHandler(double value);
        }

        // Helper method to avoid typing out the TProgress.
        [MethodImpl(InlineOption)]
        internal static Progress NewProgress<TProgress>(TProgress progress, ContinuationOptions invokeOptions, CancelationToken cancelationToken)
            where TProgress : IProgress<double>
            => new Progress(Progress<TProgress>.GetOrCreate(progress, invokeOptions, cancelationToken));

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

            internal static Progress<TProgress> GetOrCreate(TProgress progress, ContinuationOptions invokeOptions, CancelationToken cancelationToken)
            {
                var instance = GetOrCreate();
                instance._next = null;
                instance._progress = progress;
                instance._invokeContext = invokeOptions.GetContinuationContext();
                instance._forceAsync = invokeOptions.CompletedBehavior == CompletedContinuationBehavior.Asynchronous;
                // Set to nan so the first Report(0) will invoke.
                instance._current = float.NaN;
                instance._retainCounter = 1;
                instance._canceled = false;
                instance._disposed = false;

                SetCreatedStacktrace(instance);
                // Hook up the cancelation last.
                instance._cancelationRegistration = cancelationToken.Register<ICancelable>(instance);
                return instance;
            }

            private void DisposeAndRepool()
            {
                ClearReferences(ref _progress);
                _invokeContext = null;
                ObjectPool.MaybeRepool(this);
            }

            [MethodImpl(InlineOption)]
            private bool GetShouldInvokeSynchronously()
            {
                var context = _invokeContext;
                if (context == null)
                {
                    return true;
                }
                if (_forceAsync)
                {
                    return false;
                }
                return context == BackgroundSynchronizationContextSentinel.s_instance
                    ? Thread.CurrentThread.IsThreadPoolThread
                    : context == Promise.Manager.ThreadStaticSynchronizationContext;
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

            internal override void Report(ref ProgressReportValues reportValues)
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

                if (GetShouldInvokeSynchronously())
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
                if (!CausalityTrace.IsEnabled)
                {
                    // Common case: no causality trace.
                    try
                    {
                        _progress.Report(value);
                    }
                    catch (Exception e)
                    {
                        ReportRejection(e, this);
                    }
                    return;
                }
                InvokeInScope(value);
            }

            protected override void InvokeHandler(double value)
                => _progress.Report(value);

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
                        next?.UnsafeAs<PromiseRefBase.DeferredPromise<VoidResult>>().ResolveDirectVoid();
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
                    _cancelationRegistration = default;

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