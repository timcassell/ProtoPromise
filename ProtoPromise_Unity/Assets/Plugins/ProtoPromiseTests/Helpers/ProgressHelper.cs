using NUnit.Framework;
using Proto.Promises;
using System;
using System.Threading;

#pragma warning disable CS0618 // Type or member is obsolete

namespace ProtoPromiseTests
{
    public enum ProgressType
    {
        Callback,
        CallbackWithCapture,
        Interface
    }

    public class ProgressHelper : IProgress<float>
    {
        private readonly float _delta;
        private readonly object _locker = new object();
        private readonly ProgressType _progressType;
        private readonly SynchronizationType _synchronizationType;
        private readonly Action<float> _onProgress;
        volatile private bool _wasInvoked;
        volatile private float _currentProgress = float.NaN;

        public ProgressHelper(ProgressType progressType, SynchronizationType synchronizationType, Action<float> onProgress = null, float delta = float.NaN)
        {
            _progressType = progressType;
            _synchronizationType = synchronizationType;
            _onProgress = onProgress;
            _delta = float.IsNaN(delta) ? TestHelper.progressEpsilon : delta;
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
            if (_onProgress != null)
            {
                _onProgress.Invoke(value);
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

        private bool AreEqual(float expected, float actual)
        {
            if (float.IsNaN(expected))
            {
                return float.IsNaN(actual);
            }
            float dif = Math.Abs(expected - actual);
            return dif <= _delta;
        }

        public void AssertCurrentProgress(float expectedProgress, bool waitForInvoke = true, bool executeForeground = true, TimeSpan timeout = default(TimeSpan))
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
                WaitForExpectedProgress(expectedProgress, waitForInvoke, executeForeground, timeout);
            }
        }

        private bool GetCurrentProgressEqualsExpected(float expectedProgress, bool waitForInvoke = true, bool executeForeground = true, TimeSpan timeout = default(TimeSpan))
        {
            try
            {
                float currentProgress = GetCurrentProgress(waitForInvoke, executeForeground, timeout);
                return AreEqual(expectedProgress, currentProgress);
            }
            catch (TimeoutException e)
            {
                throw new TimeoutException("expectedProgress: " + expectedProgress + ", executeForeground: " + executeForeground, e);
            }
        }

        private void WaitForExpectedProgress(float expectedProgress, bool waitForInvoke, bool executeForeground, TimeSpan timeout = default(TimeSpan))
        {
            if (executeForeground)
            {
                TestHelper.ExecuteForegroundCallbacks();
            }
            if (!waitForInvoke)
            {
                return;
            }

            if (timeout <= TimeSpan.Zero)
            {
                timeout = TimeSpan.FromSeconds(2);
            }
            float current = float.NaN;
            if (!SpinWait.SpinUntil(() => { current = _currentProgress; return AreEqual(expectedProgress, current); }, timeout))
            {
                throw new TimeoutException("Progress was not invoked with expected progress " + expectedProgress + " after " + timeout + ", _currentProgress: " + _currentProgress + ", current thread is background: " + Thread.CurrentThread.IsBackground);
            }
        }

        public float GetCurrentProgress(bool waitForInvoke, bool executeForeground, TimeSpan timeout = default(TimeSpan))
        {
            lock (_locker)
            {
                MaybeWaitForInvoke(waitForInvoke, executeForeground, timeout);
                return _currentProgress;
            }
        }

        public void ReportProgressAndAssertResult(Promise.DeferredBase deferred, float reportValue, float expectedProgress, bool waitForInvoke = true, bool executeForeground = true, TimeSpan timeout = default(TimeSpan))
        {
            if (!waitForInvoke)
            {
                // If waitForInvoke is false, it means the value is expected to be unchanged.
                lock (_locker)
                {
                    Assert.AreEqual(expectedProgress, _currentProgress, _delta);
                    deferred.ReportProgress(reportValue);
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
                deferred.ReportProgress(reportValue);
                areEqual = GetCurrentProgressEqualsExpected(expectedProgress, waitForInvoke, executeForeground, timeout);
            }
            if (!areEqual)
            {
                WaitForExpectedProgress(expectedProgress, waitForInvoke, executeForeground, timeout);
            }
        }

        public void RejectAndAssertResult<TReject>(Promise.DeferredBase deferred, TReject reason, float expectedProgress, bool waitForInvoke = true, bool executeForeground = true, TimeSpan timeout = default(TimeSpan))
        {
            if (!waitForInvoke)
            {
                // If waitForInvoke is false, it means the value is expected to be unchanged.
                lock (_locker)
                {
                    Assert.AreEqual(expectedProgress, _currentProgress, _delta);
                    deferred.Reject(reason);
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
                deferred.Reject(reason);
                areEqual = GetCurrentProgressEqualsExpected(expectedProgress, waitForInvoke, executeForeground, timeout);
            }
            if (!areEqual)
            {
                WaitForExpectedProgress(expectedProgress, waitForInvoke, executeForeground, timeout);
            }
        }

        public void CancelAndAssertResult(Promise.DeferredBase deferred, float expectedProgress, bool waitForInvoke = true, bool executeForeground = true, TimeSpan timeout = default(TimeSpan))
        {
            if (!waitForInvoke)
            {
                // If waitForInvoke is false, it means the value is expected to be unchanged.
                lock (_locker)
                {
                    Assert.AreEqual(expectedProgress, _currentProgress, _delta);
                    deferred.Cancel();
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
                deferred.Cancel();
                areEqual = GetCurrentProgressEqualsExpected(expectedProgress, waitForInvoke, executeForeground, timeout);
            }
            if (!areEqual)
            {
                WaitForExpectedProgress(expectedProgress, waitForInvoke, executeForeground, timeout);
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
                WaitForExpectedProgress(expectedProgress, waitForInvoke, executeForeground, timeout);
            }
        }

        public void ResolveAndAssertResult(Promise.Deferred deferred, float expectedProgress, bool waitForInvoke = true, bool executeForeground = true, TimeSpan timeout = default(TimeSpan))
        {
            if (!waitForInvoke)
            {
                // If waitForInvoke is false, it means the value is expected to be unchanged.
                lock (_locker)
                {
                    Assert.AreEqual(expectedProgress, _currentProgress, _delta);
                    deferred.Resolve();
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
                deferred.Resolve();
                areEqual = GetCurrentProgressEqualsExpected(expectedProgress, waitForInvoke, executeForeground, timeout);
            }
            if (!areEqual)
            {
                WaitForExpectedProgress(expectedProgress, waitForInvoke, executeForeground, timeout);
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
                WaitForExpectedProgress(expectedProgress, waitForInvoke, executeForeground, timeout);
            }
        }

        public Promise SubscribeAndAssertCurrentProgress(Promise promise, float expectedProgress, CancelationToken cancelationToken = default(CancelationToken), TimeSpan timeout = default(TimeSpan))
        {
            bool areEqual;
            lock (_locker)
            {
                PrepareForInvoke();
                promise = Subscribe(promise, cancelationToken);
                areEqual = GetCurrentProgressEqualsExpected(expectedProgress, timeout: timeout);
            }
            if (!areEqual)
            {
                WaitForExpectedProgress(expectedProgress, true, true, timeout);
            }
            return promise;
        }

        public Promise<T> SubscribeAndAssertCurrentProgress<T>(Promise<T> promise, float expectedProgress, CancelationToken cancelationToken = default(CancelationToken), TimeSpan timeout = default(TimeSpan))
        {
            bool areEqual;
            lock (_locker)
            {
                PrepareForInvoke();
                promise = Subscribe(promise, cancelationToken);
                areEqual = GetCurrentProgressEqualsExpected(expectedProgress, timeout: timeout);
            }
            if (!areEqual)
            {
                WaitForExpectedProgress(expectedProgress, true, true, timeout);
            }
            return promise;
        }

        public Promise Subscribe(Promise promise, CancelationToken cancelationToken = default(CancelationToken))
        {
            if (_synchronizationType == SynchronizationType.Explicit)
            {
                switch (_progressType)
                {
                    case ProgressType.Callback:
                        return promise.Progress(Report, TestHelper._foregroundContext, cancelationToken);
                    case ProgressType.CallbackWithCapture:
                        return promise.Progress(this, (helper, v) => helper.Report(v), TestHelper._foregroundContext, cancelationToken);
                    default:
                        return promise.Progress(this, TestHelper._foregroundContext, cancelationToken);
                }
            }
            else
            {
                switch (_progressType)
                {
                    case ProgressType.Callback:
                        return promise.Progress(Report, (SynchronizationOption) _synchronizationType, cancelationToken);
                    case ProgressType.CallbackWithCapture:
                        return promise.Progress(this, (helper, v) => helper.Report(v), (SynchronizationOption) _synchronizationType, cancelationToken);
                    default:
                        return promise.Progress(this, (SynchronizationOption) _synchronizationType, cancelationToken);
                }
            }
        }

        public Promise<T> Subscribe<T>(Promise<T> promise, CancelationToken cancelationToken = default(CancelationToken))
        {
            if (_synchronizationType == SynchronizationType.Explicit)
            {
                switch (_progressType)
                {
                    case ProgressType.Callback:
                        return promise.Progress(Report, TestHelper._foregroundContext, cancelationToken);
                    case ProgressType.CallbackWithCapture:
                        return promise.Progress(this, (helper, v) => helper.Report(v), TestHelper._foregroundContext, cancelationToken);
                    default:
                        return promise.Progress(this, TestHelper._foregroundContext, cancelationToken);
                }
            }
            else
            {
                switch (_progressType)
                {
                    case ProgressType.Callback:
                        return promise.Progress(Report, (SynchronizationOption) _synchronizationType, cancelationToken);
                    case ProgressType.CallbackWithCapture:
                        return promise.Progress(this, (helper, v) => helper.Report(v), (SynchronizationOption) _synchronizationType, cancelationToken);
                    default:
                        return promise.Progress(this, (SynchronizationOption) _synchronizationType, cancelationToken);
                }
            }
        }
    }
}