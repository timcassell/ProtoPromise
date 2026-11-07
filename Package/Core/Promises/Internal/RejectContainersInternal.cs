#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0028 // Simplify collection initialization
#pragma warning disable IDE0054 // Use compound assignment
#pragma warning disable IDE0090 // Use 'new(...)'

using Proto.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Proto.Promises
{
    partial class Internal
    {
        // captureTrace is true when the rejection site should be captured (e.g. Deferred.Reject, or where an exception was caught after invoking user code,
        // which shows what invoked the code), or false when the traceable's trace already describes it (e.g. an exception that the library created
        // outside of user code, or user code that ran synchronously where the traceable was created).
        internal static IRejectContainer CreateRejectContainer(object reason, bool captureTrace, object traceable)
        {
            if (reason is IRejectionToContainer internalRejection)
            {
                // reason is an internal rejection object, get its container instead of wrapping it.
                return internalRejection.ToContainer(traceable);
            }

            // If reason is null, behave the same way .Net behaves if you throw null.
            reason = reason ?? new NullReferenceException();
            return reason is Exception e
                ? ExceptionRejectionContainer.Create(e, captureTrace, traceable)
                : (IRejectContainer) RejectionContainer.Create(reason, captureTrace, traceable);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class RejectContainer : IRejectContainer
        {
            protected CausalityTrace _causalityTrace;

            // The captured trace is where the promise was rejected: the callback that threw runs in the scope of its trace, so the trace has it as its cause,
            // and the code that rejected a deferred is on the stack.
            protected void SetCausalityTrace(bool captureTrace, object traceable)
                => _causalityTrace = CausalityTrace.IsEnabled & captureTrace
                    ? CausalityTrace.Capture()
                    : GetTrace(traceable);

            public abstract object Value { get; }
            public abstract Exception GetValueAsException();
            public abstract void ReportUnhandled();
            public abstract ExceptionDispatchInfo GetExceptionDispatchInfo();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed partial class RejectionContainer : RejectContainer, IRejectionToContainer, ICantHandleException
        {
            private object _value;
            public override object Value => _value;

            private RejectionContainer() { }

            public override Exception GetValueAsException()
                => ToException();

            internal static RejectionContainer Create(object value, bool captureTrace, object traceable)
            {
                var container = new RejectionContainer
                {
                    _value = value
                };
                container.SetCausalityTrace(captureTrace, traceable);
                return container;
            }

            public override void ReportUnhandled()
                => ReportUnhandledException(ToException());

            private UnhandledException ToException()
                => new UnhandledException(Value, $"A rejected value was not handled, type: {Value.GetType()}, value: {Value}", _causalityTrace, null);

            public override ExceptionDispatchInfo GetExceptionDispatchInfo()
                => ExceptionDispatchInfo.Capture(ToException());

            IRejectContainer IRejectionToContainer.ToContainer(object traceable)
                => this;

            void ICantHandleException.ReportUnhandled(object traceable)
                => ReportUnhandled();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed partial class ExceptionRejectionContainer : RejectContainer, IRejectionToContainer, ICantHandleException
        {
            private ExceptionDispatchInfo _capturedInfo;

            new private void SetCausalityTrace(bool captureTrace, object traceable)
            {
                // The common case (not traced) doesn't access the table.
                if (!CausalityTrace.IsEnabled)
                {
                    return;
                }
                // The exception may have been rethrown from await in an async function, so we use the trace of where it was originally rejected or thrown.
                if (TryGetExceptionTrace(Value, out _causalityTrace))
                {
                    return;
                }
                base.SetCausalityTrace(captureTrace, traceable);
                if (_causalityTrace != null)
                {
                    AddExceptionTrace(Value, _causalityTrace);
                }
            }

            public override object Value => _capturedInfo.SourceException;

            private ExceptionRejectionContainer() { }

            internal static ExceptionRejectionContainer Create(Exception value, bool captureTrace, object traceable)
            {
                var container = new ExceptionRejectionContainer
                {
                    _capturedInfo = ExceptionDispatchInfo.Capture(value)
                };
                container.SetCausalityTrace(captureTrace, traceable);
                return container;
            }

            public override void ReportUnhandled()
                => ReportUnhandledException(ToException());

            private UnhandledException ToException()
                => new UnhandledException(Value, "An exception was not handled.", _causalityTrace, _capturedInfo.SourceException);

            public override ExceptionDispatchInfo GetExceptionDispatchInfo()
                => _capturedInfo;

            IRejectContainer IRejectionToContainer.ToContainer(object traceable)
                => this;

            void ICantHandleException.ReportUnhandled(object traceable)
                => ReportUnhandled();

            public override Exception GetValueAsException()
                => _capturedInfo.SourceException;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed partial class AggregateRejectionContainer : RejectContainer, IRejectionToContainer, ICantHandleException
        {
            private List<Exception> _exceptions = new List<Exception>();
            private ExceptionDispatchInfo _dispatchInfo;

            internal List<Exception> Exceptions => _exceptions;

            private ExceptionDispatchInfo DispatchInfo
            {
                get
                {
                    if (_dispatchInfo == null)
                    {
                        _dispatchInfo = ExceptionDispatchInfo.Capture(new AggregateException(_exceptions));
                        _exceptions = null;
                    }
                    return _dispatchInfo;
                }
            }

            public override object Value => DispatchInfo.SourceException;

            internal AggregateRejectionContainer(object traceable)
            {
                SetCausalityTrace(false, traceable);
            }

            internal void Add(Exception e)
                => _exceptions.Add(e);

            public override void ReportUnhandled()
                => ReportUnhandledException(ToException());

            private UnhandledException ToException()
                => new UnhandledException(Value, "An exception was not handled.", _causalityTrace, null);

            public override ExceptionDispatchInfo GetExceptionDispatchInfo()
                => DispatchInfo;

            IRejectContainer IRejectionToContainer.ToContainer(object traceable)
                => this;

            void ICantHandleException.ReportUnhandled(object traceable)
                => ReportUnhandled();

            public override Exception GetValueAsException()
                => DispatchInfo.SourceException;
        }
    }
}