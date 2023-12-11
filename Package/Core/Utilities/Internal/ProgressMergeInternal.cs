#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

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
        internal sealed class ProgressMerger : ProgressBase
        {
            private ProgressBase _target;
            private double _minValue;
            private double _maxValue;
            private double _numerator;
            private double _denominator;
            private int _targetId;
            private bool _disposed;

            private ProgressMerger() { }

            ~ProgressMerger()
            {
                if (!_disposed)
                {
                    ReportRejection(new UnreleasedObjectException("A Progress.MergeBuilder's resources were garbage collected without it being disposed."), this);
                }
            }

            [MethodImpl(InlineOption)]
            private static ProgressMerger GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<ProgressMerger>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new ProgressMerger()
                    : obj.UnsafeAs<ProgressMerger>();
            }

            internal static ProgressMerger GetOrCreate(ProgressToken target)
            {
                var instance = GetOrCreate();
                instance._next = null;
                instance._target = target._impl;
                instance._minValue = target._minValue;
                instance._maxValue = target._maxValue;
                instance._numerator = 0d;
                instance._denominator = 0d;
                instance._targetId = target._id;
                instance._disposed = false;

                SetCreatedStacktrace(instance, 2);
                return instance;
            }

            internal override void Report(ref NewProgressReportValues reportValues)
            {
                // Enter this lock before exiting previous lock.
                // This prevents a race condition where another report on a separate thread could get ahead of this report.
                _smallFields._locker.Enter();
                reportValues._reporter._smallFields._locker.Exit();
                if (reportValues._id != _smallFields._id)
                {
                    _smallFields._locker.Exit();
                    // Set the next to null to notify the end of the caller loop.
                    reportValues._next = null;
                    return;
                }
                ThrowIfInPool(this);

                _numerator += reportValues._value;
                reportValues._reporter = this;
                reportValues._next = _target;
                reportValues._value = Lerp(_minValue, _maxValue, _numerator / _denominator);
                reportValues._id = _targetId;
            }

            internal override void Dispose(int id)
            {
                _smallFields._locker.Enter();
                if (id != _smallFields._id)
                {
                    _smallFields._locker.Exit();
                    throw new ObjectDisposedException("Progress.MergeBuilder");
                }

                ThrowIfInPool(this);
                _disposed = true;
                unchecked
                {
                    ++_smallFields._id;
                }
                var tokenHead = _next.UnsafeAs<MergeToken>();
                _next = null;
                _smallFields._locker.Exit();

                ObjectPool.MaybeRepool(this);

                // Dispose all the tokens that were created from this.
                while (tokenHead != null)
                {
                    var temp = tokenHead;
                    tokenHead = temp._next.UnsafeAs<MergeToken>();
                    temp.Dispose(0);
                }
            }

            internal ProgressToken NewToken(double weight, int id)
            {
                _smallFields._locker.Enter();
                // If the weight is very small, or the current denominator is very large, there could be not enough precision to make a difference.
                // Or, if weight or current denominator are very large, adding them could result in infinite.
                // In either case, the merge calculation won't work.
                var newDenominator = _denominator + weight;
                bool idsDontMatch = id != _smallFields._id;
                bool unchangedDenominator = newDenominator == _denominator;
                if (idsDontMatch | unchangedDenominator | double.IsPositiveInfinity(newDenominator))
                {
                    _smallFields._locker.Exit();
                    NewTokenThrow(weight, unchangedDenominator, idsDontMatch);
                }
                ThrowIfInPool(this);

                _denominator = newDenominator;
                var tokenImpl = MergeToken.GetOrCreate(this);
                // We store the merge tokens as a linked-list in _next to save memory.
                tokenImpl._next = _next;
                _next = tokenImpl;
                _smallFields._locker.Exit();

                return new ProgressToken(tokenImpl, tokenImpl.Id, 0d, weight);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void NewTokenThrow(double weight, bool unchangedDenominator, bool idsDontMatch)
            {
                if (idsDontMatch)
                {
                    throw new ObjectDisposedException("Progress.MergeBuilder");
                }
                if (unchangedDenominator)
                {
                    throw new ArithmeticException("The addition of weight (" + weight + ") is too small to calculate.");
                }
                throw new ArithmeticException("The addition of weight (" + weight + ") resulted in infinite.");
            }

            // MergeToken only calls the other Report overload.
            internal override void Report(double value, int id) { throw new System.InvalidOperationException(); }
            internal override Promise DisposeAsync(int id) { throw new System.InvalidOperationException(); }

            private sealed class MergeToken : ProgressBase
            {
                private ProgressMerger _target;
                private double _current;
                private int _targetId;

                private MergeToken() { }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                private bool _disposed;

                ~MergeToken()
                {
                    if (!_disposed)
                    {
                        // For debugging. This should never happen.
                        ReportRejection(new UnreleasedObjectException("A MergeToken was garbage collected without it being disposed."), this);
                    }
                }
#endif // PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE

                [MethodImpl(InlineOption)]
                private static MergeToken GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<MergeToken>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new MergeToken()
                        : obj.UnsafeAs<MergeToken>();
                }

                internal static MergeToken GetOrCreate(ProgressMerger target)
                {
                    var instance = GetOrCreate();
                    instance._target = target;
                    instance._current = 0d;
                    instance._targetId = target._smallFields._id;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    instance._disposed = false;
#endif

                    SetCreatedStacktrace(instance, 2);
                    return instance;
                }

                internal override void Report(double value, int id)
                {
                    _smallFields._locker.Enter();
                    if (id != _smallFields._id)
                    {
                        _smallFields._locker.Exit();
                        return;
                    }
                    ThrowIfInPool(this);

                    // We report the difference instead of the raw value. The merger adds the difference.
                    var diff = value - _current;
                    _current = value;
                    // We report to the target iteratively instead of recursively to prevent StackOverflowException in the case of very deep call chains.
                    // _target.Report() exits this lock, so we don't do it here.
                    var reportValues = new NewProgressReportValues(this, _target, diff, _targetId);
                    // Call the target directly since we have the concrete sealed type.
                    _target.Report(ref reportValues);
                    while (reportValues._reporter != null)
                    {
                        reportValues._next.Report(ref reportValues);
                    }
                }

                internal override void Dispose(int id)
                {
                    // We don't need to check the id because this is called directly from the merger. 
                    ThrowIfInPool(this);

                    _smallFields._locker.Enter();
                    unchecked
                    {
                        ++_smallFields._id;
                    }
                    _smallFields._locker.Exit();

                    ObjectPool.MaybeRepool(this);
                }

                internal override void Report(ref NewProgressReportValues reportValues) { throw new System.InvalidOperationException(); }
                internal override Promise DisposeAsync(int id) { throw new System.InvalidOperationException(); }
            }
        }

    } // class Internal
} // namespace Proto.Promises