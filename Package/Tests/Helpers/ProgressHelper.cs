using NUnit.Framework;
using Proto.Promises;
using System;
using System.Threading;

namespace ProtoPromiseTests
{
    public enum ProgressType
    {
        Callback,
        CallbackWithCapture,
        Interface
    }

    public class ProgressHelper : IProgress<float>, IProgress<double>
    {
        private readonly float _delta;
        private readonly object _locker = new object();
        private readonly ProgressType _progressType;
        private readonly SynchronizationType _synchronizationType;
        private readonly bool _forceAsync;
        private readonly Action<float> _onProgress;
        volatile private bool _wasInvoked;
        private double _currentProgress = double.NaN;

        public ProgressHelper(ProgressType progressType, SynchronizationType synchronizationType, Action<float> onProgress = null, float delta = float.NaN, bool forceAsync = false)
        {
            _progressType = progressType;
            _synchronizationType = synchronizationType;
            _onProgress = onProgress;
            _delta = float.IsNaN(delta) ? TestHelper.progressEpsilon : delta;
            _forceAsync = forceAsync;
        }

        public void MaybeEnterLock()
        {
            if (_synchronizationType == TestHelper.backgroundType)
            {
                Monitor.Enter(_locker);
            }
        }

        public void MaybeExitLock()
        {
            if (_synchronizationType == TestHelper.backgroundType)
            {
                Monitor.Exit(_locker);
            }
        }

        public void PrepareForInvoke()
        {
            _wasInvoked = false;
        }

        public void Report(float value)
        {
            ReportNew(value);
        }

        void IProgress<double>.Report(double value)
        {
            ReportNew(value);
        }

        public void ReportNew(double value)
        {
            if (_onProgress != null)
            {
                _onProgress.Invoke((float) value);
            }
            lock (_locker)
            {
                _currentProgress = value;
                _wasInvoked = true;
                Monitor.Pulse(_locker);
            }
        }

        public void MaybeWaitForInvoke(bool waitForInvoke, bool executeForeground, TimeSpan waitTimeout = default(TimeSpan))
        {
            lock (_locker)
            {
                if (executeForeground)
                {
                    TestHelper.ExecuteForegroundCallbacks();
                }
                if (waitForInvoke)
                {
                    // Wait for Report to be called in case it happens in a separate thread.
                    if (!_wasInvoked)
                    {
                        if (waitTimeout <= TimeSpan.Zero)
                        {
                            waitTimeout = TimeSpan.FromSeconds(2);
                        }
                        if (!Monitor.Wait(_locker, waitTimeout) && !_wasInvoked)
                        {
                            throw new TimeoutException("Progress was not invoked after " + waitTimeout + ", _currentProgress: " + _currentProgress + ", current thread is background: " + Thread.CurrentThread.IsBackground);
                        }
                    }
                }
            }
        }

        private bool AreEqual(double expected, double actual)
        {
            if (double.IsNaN(expected))
            {
                return double.IsNaN(actual);
            }
            double dif = Math.Abs(expected - actual);
            return dif <= _delta;
        }

        public void AssertCurrentProgress(double expectedProgress, bool waitForInvoke = true, bool executeForeground = true, TimeSpan timeout = default(TimeSpan))
        {
            if (!waitForInvoke)
            {
                // If waitForInvoke is false, it means the value is expected to be unchanged.
                lock (_locker)
                {
                    Assert.AreEqual(expectedProgress, _currentProgress, _delta);
                }
                if (executeForeground)
                {
                    TestHelper.ExecuteForegroundCallbacks();
                }
                Assert.AreEqual(expectedProgress, _currentProgress, _delta);
                return;
            }

            if (!GetCurrentProgressEqualsExpected(expectedProgress, waitForInvoke, executeForeground, timeout))
            {
                WaitForExpectedProgress(expectedProgress, executeForeground, timeout);
            }
        }

        private bool GetCurrentProgressEqualsExpected(double expectedProgress, bool waitForInvoke = true, bool executeForeground = true, TimeSpan timeout = default(TimeSpan))
        {
            try
            {
                double currentProgress = GetCurrentProgress(waitForInvoke, executeForeground, timeout);
                return AreEqual(expectedProgress, currentProgress);
            }
            catch (TimeoutException e)
            {
                throw new TimeoutException("expectedProgress: " + expectedProgress + ", executeForeground: " + executeForeground, e);
            }
        }

        private void WaitForExpectedProgress(double expectedProgress, bool executeForeground, TimeSpan timeout = default(TimeSpan))
        {
            if (executeForeground)
            {
                TestHelper.ExecuteForegroundCallbacks();
            }

            if (timeout <= TimeSpan.Zero)
            {
                timeout = TimeSpan.FromSeconds(2);
            }
            double current = double.NaN;
            if (!SpinWait.SpinUntil(() => { current = _currentProgress; return AreEqual(expectedProgress, current); }, timeout))
            {
                throw new TimeoutException("Progress was not invoked with expected progress " + expectedProgress + " after " + timeout + ", _currentProgress: " + _currentProgress + ", current thread is background: " + Thread.CurrentThread.IsBackground);
            }
        }

        public double GetCurrentProgress(bool waitForInvoke, bool executeForeground, TimeSpan timeout = default(TimeSpan))
        {
            lock (_locker)
            {
                MaybeWaitForInvoke(waitForInvoke, executeForeground, timeout);
                return _currentProgress;
            }
        }

        public void CancelAndAssertResult(CancelationSource cancelationSource, float expectedProgress, bool waitForInvoke = true, bool executeForeground = true, TimeSpan timeout = default(TimeSpan))
        {
            if (!waitForInvoke)
            {
                // If waitForInvoke is false, it means the value is expected to be unchanged.
                lock (_locker)
                {
                    Assert.AreEqual(expectedProgress, _currentProgress, _delta);
                    cancelationSource.Cancel();
                }
                if (executeForeground)
                {
                    TestHelper.ExecuteForegroundCallbacks();
                }
                Assert.AreEqual(expectedProgress, _currentProgress, _delta);
                return;
            }

            bool areEqual;
            lock (_locker)
            {
                PrepareForInvoke();
                cancelationSource.Cancel();
                areEqual = GetCurrentProgressEqualsExpected(expectedProgress, waitForInvoke, executeForeground, timeout);
            }
            if (!areEqual)
            {
                WaitForExpectedProgress(expectedProgress, executeForeground, timeout);
            }
        }

        public void ResolveAndAssertResult<T>(Promise<T>.Deferred deferred, T result, float expectedProgress, bool waitForInvoke = true, bool executeForeground = true, TimeSpan timeout = default(TimeSpan))
        {
            if (!waitForInvoke)
            {
                // If waitForInvoke is false, it means the value is expected to be unchanged.
                lock (_locker)
                {
                    Assert.AreEqual(expectedProgress, _currentProgress, _delta);
                    deferred.Resolve(result);
                }
                if (executeForeground)
                {
                    TestHelper.ExecuteForegroundCallbacks();
                }
                Assert.AreEqual(expectedProgress, _currentProgress, _delta);
                return;
            }

            bool areEqual;
            lock (_locker)
            {
                PrepareForInvoke();
                deferred.Resolve(result);
                areEqual = GetCurrentProgressEqualsExpected(expectedProgress, waitForInvoke, executeForeground, timeout);
            }
            if (!areEqual)
            {
                WaitForExpectedProgress(expectedProgress, executeForeground, timeout);
            }
        }

        public void ReportProgressAndAssertResult(ProgressToken progressToken, double reportValue, double expectedProgress, bool waitForInvoke = true, bool executeForeground = true, TimeSpan timeout = default(TimeSpan))
        {
            if (!waitForInvoke)
            {
                // If waitForInvoke is false, it means the value is expected to be unchanged.
                lock (_locker)
                {
                    Assert.AreEqual(expectedProgress, _currentProgress, _delta);
                    progressToken.Report(reportValue);
                }
                if (executeForeground)
                {
                    TestHelper.ExecuteForegroundCallbacks();
                }
                Assert.AreEqual(expectedProgress, _currentProgress, _delta);
                return;
            }

            bool areEqual;
            lock (_locker)
            {
                PrepareForInvoke();
                progressToken.Report(reportValue);
                areEqual = GetCurrentProgressEqualsExpected(expectedProgress, waitForInvoke, executeForeground, timeout);
            }
            if (!areEqual)
            {
                WaitForExpectedProgress(expectedProgress, executeForeground, timeout);
            }
        }

        public Progress ToProgress(CancelationToken cancelationToken = default(CancelationToken))
        {
            if (_synchronizationType == SynchronizationType.Explicit)
            {
                switch (_progressType)
                {
                    case ProgressType.Callback:
                        return Progress.New(ReportNew, TestHelper._foregroundContext, _forceAsync, cancelationToken);
                    case ProgressType.CallbackWithCapture:
                        return Progress.New(this, (helper, v) => helper.ReportNew(v), TestHelper._foregroundContext, _forceAsync, cancelationToken);
                    default:
                        return Progress.New(this, TestHelper._foregroundContext, _forceAsync, cancelationToken);
                }
            }
            else
            {
                switch (_progressType)
                {
                    case ProgressType.Callback:
                        return Progress.New(ReportNew, (SynchronizationOption) _synchronizationType, _forceAsync, cancelationToken);
                    case ProgressType.CallbackWithCapture:
                        return Progress.New(this, (helper, v) => helper.ReportNew(v), (SynchronizationOption) _synchronizationType, _forceAsync, cancelationToken);
                    default:
                        return Progress.New(this, (SynchronizationOption) _synchronizationType, _forceAsync, cancelationToken);
                }
            }
        }
    }
}