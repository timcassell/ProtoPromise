using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class ProgressRacer : ProgressBase
        {
            private double _minValue;
            private double _maxValue;
            private double _current;
            private int _targetId;
            private bool _disposed;

            private ProgressRacer() { }

            ~ProgressRacer()
            {
                if (!_disposed)
                {
                    ReportRejection(new UnreleasedObjectException("A Progress.RaceBuilder's resources were garbage collected without it being disposed."), this);
                }
            }

            [MethodImpl(InlineOption)]
            private static ProgressRacer GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<ProgressRacer>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new ProgressRacer()
                    : obj.UnsafeAs<ProgressRacer>();
            }

            internal static ProgressRacer GetOrCreate(ProgressToken target)
            {
                var instance = GetOrCreate();
                // We store the target in _next to save memory.
                instance._next = target._impl;
                instance._minValue = target._minValue;
                instance._maxValue = target._maxValue;
                instance._current = 0d;
                instance._targetId = target._id;
                instance._disposed = false;

                SetCreatedStacktrace(instance, 2);
                return instance;
            }

            internal override void Report(double value, int id)
            {
                _smallFields._locker.Enter();
                // Only report values that are larger than the current.
                if (id != _smallFields._id | _current >= value)
                {
                    _smallFields._locker.Exit();
                    return;
                }
                ThrowIfInPool(this);

                _current = value;
                // We report to the target iteratively instead of recursively to prevent StackOverflowException in the case of very deep call chains.
                // _next.Report() exits this lock, so we don't do it here.
                var reportValues = new NewProgressReportValues(this, _next.UnsafeAs<ProgressBase>(), Lerp(_minValue, _maxValue, value), _targetId);
                do
                {
                    reportValues._next.Report(ref reportValues);
                } while (reportValues._next != null);
            }

            internal override void Report(ref NewProgressReportValues reportValues)
            {
                // Enter this lock before exiting previous lock.
                // This prevents a race condition where another report on a separate thread could get ahead of this report.
                _smallFields._locker.Enter();
                reportValues._reporter.ExitLock();
                // Only report values that are larger than the current.
                if (reportValues._id != _smallFields._id | _current >= reportValues._value)
                {
                    _smallFields._locker.Exit();
                    // Set the next to null to notify the end of the caller loop.
                    reportValues._next = null;
                    return;
                }
                ThrowIfInPool(this);

                _current = reportValues._value;
                reportValues._reporter = this;
                reportValues._next = _next.UnsafeAs<ProgressBase>();
                reportValues._value = Lerp(_minValue, _maxValue, reportValues._value);
                reportValues._id = _targetId;
            }

            internal void Dispose(int id)
            {
                _smallFields._locker.Enter();
                if (id != _smallFields._id)
                {
                    _smallFields._locker.Exit();
                    throw new ObjectDisposedException("Progress.RaceBuilder");
                }

                ThrowIfInPool(this);
                _disposed = true;
                unchecked
                {
                    ++_smallFields._id;
                }
                _smallFields._locker.Exit();

                ObjectPool.MaybeRepool(this);
            }

            internal ProgressToken NewToken(int id)
            {
                _smallFields._locker.Enter();
                if (id != _smallFields._id)
                {
                    _smallFields._locker.Exit();
                    throw new ObjectDisposedException("Progress.RaceBuilder");
                }
                id = _smallFields._id;
                _smallFields._locker.Exit();
                return new ProgressToken(this, id, 0d, 1d);
            }
        }
    } // class Internal
} // namespace Proto.Promises