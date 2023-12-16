using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0180 // Use tuple to swap values

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class ProgressMultiHandler : ProgressBase
        {
            private ValueList<ProgressToken> _tokens = new ValueList<ProgressToken>(8);
            // TODO: This could use ArrayPool instead of an extra list field.
            // But it's not available in older runtimes.
            private ValueList<ProgressToken> _stillValidTokens = new ValueList<ProgressToken>(8);
            private bool _disposed;

            private ProgressMultiHandler() { }

            ~ProgressMultiHandler()
            {
                if (!_disposed)
                {
                    ReportRejection(new UnreleasedObjectException("A Progress.MultiHandler's resources were garbage collected without it being disposed."), this);
                }
            }

            [MethodImpl(InlineOption)]
            private static ProgressMultiHandler GetOrCreateCore()
            {
                var obj = ObjectPool.TryTakeOrInvalid<ProgressMultiHandler>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new ProgressMultiHandler()
                    : obj.UnsafeAs<ProgressMultiHandler>();
            }

            internal static ProgressMultiHandler GetOrCreate()
            {
                var instance = GetOrCreateCore();
                instance._next = null;
                instance._disposed = false;
                SetCreatedStacktrace(instance, 2);
                return instance;
            }

            // We use Monitor instead of SpinLocker since the lock could be held for a longer period of time when reporting.
            [MethodImpl(InlineOption)]
            private void EnterLock()
            {
                Monitor.Enter(this);
            }

            [MethodImpl(InlineOption)]
            internal override void ExitLock()
            {
                Monitor.Exit(this);
            }

            internal void Add(ProgressToken progressToken, int id)
            {
                if (!progressToken.HasListener)
                {
                    return;
                }
                lock (this)
                {
                    if (id != _smallFields._id)
                    {
                        throw new ObjectDisposedException("Progress.MultiHandler");
                    }
                    _tokens.Add(progressToken);
                }
            }

            internal override void Report(double value, int id)
            {
                EnterLock();
                var reportValues = new NewProgressReportValues(this, null, value, id);
                ReportCore(ref reportValues);
            }

            // ProgressMultiHandlers are reported recursively instead of iteratively, because it would be much more complex and expensive to make it iterative.
            // This type is expected to be used infrequently, so it shouldn't cause a StackOverflowException.
            internal override void Report(ref NewProgressReportValues reportValues)
            {
                // Enter this lock before exiting previous lock.
                // This prevents a race condition where another report on a separate thread could get ahead of this report.
                EnterLock();
                reportValues._reporter.ExitLock();
                ReportCore(ref reportValues);
                // Set the next to null to notify the end of the caller loop.
                reportValues._next = null;
            }

            private void ReportCore(ref NewProgressReportValues reportValues)
            {
                // Lock is already entered from the caller.
                if (reportValues._id != _smallFields._id)
                {
                    ExitLock();
                    return;
                }
                ThrowIfInPool(this);

                var reportedProgress = reportValues._value;
                for (int i = 0, max = _tokens.Count; i < max; ++i)
                {
                    var token = _tokens[i];
                    // If the token still has a listener, we re-add it, otherwise we drop it.
                    // We check the id directly instead of calling HasListener, because we don't need the extra null check since we already checked it when it was added.
                    if (token._impl.Id != token._id)
                    {
                        continue;
                    }
                    _stillValidTokens.Add(token);

                    // We have to hold the lock until all tokens have been reported.
                    // We enter the lock again for each listener, because each one exits the lock indiscriminately.
                    EnterLock();
                    reportValues._reporter = this;
                    reportValues._next = token._impl;
                    reportValues._value = Lerp(token._minValue, token._maxValue, reportedProgress);
                    reportValues._id = token._id;
                    do
                    {
                        reportValues._next.Report(ref reportValues);
                    } while (reportValues._next != null);
                }

                // Clear the old tokens and swap the lists so that tokens that no longer have a listener will be dropped.
                _tokens.Clear();
                var temp = _stillValidTokens;
                _stillValidTokens = _tokens;
                _tokens = temp;
                ExitLock();
            }

            internal void Dispose(int id)
            {
                lock (this)
                {
                    if (id != _smallFields._id)
                    {
                        throw new ObjectDisposedException("Progress.MultiHandler");
                    }

                    ThrowIfInPool(this);
                    _disposed = true;
                    unchecked
                    {
                        ++_smallFields._id;
                    }
                    _tokens.Clear();
                }

                ObjectPool.MaybeRepool(this);
            }
        }
    } // class Internal
} // namespace Proto.Promises