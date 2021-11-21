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

    public class ProgressHelper : IProgress<float>
    {
        public readonly object _locker = new object();
        private readonly ProgressType _progressType;
        private readonly SynchronizationType _synchronizationType;
        private readonly Action<float> _onProgress;
        volatile private bool _wasInvoked;
        volatile private float _currentProgress = float.NaN;

        public ProgressHelper(ProgressType progressType, SynchronizationType synchronizationType, Action<float> onProgress = null)
        {
            _progressType = progressType;
            _synchronizationType = synchronizationType;
            _onProgress = onProgress;
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
                            waitTimeout = TimeSpan.FromSeconds(1);
                        }
                        if (!Monitor.Wait(_locker, waitTimeout))
                        {
                            throw new TimeoutException();
                        }
                    }
                }
            }
        }

        public void AssertCurrentProgress(float expectedProgress, bool waitForInvoke = true, bool executeForeground = true, TimeSpan waitTimeout = default(TimeSpan))
        {
            float currentProgress = GetCurrentProgress(waitForInvoke, executeForeground, waitTimeout);
            if (float.IsNaN(expectedProgress))
            {
                Assert.IsNaN(currentProgress);
            }
            else
            {
                Assert.AreEqual(expectedProgress, currentProgress, TestHelper.progressEpsilon);
            }
        }

        public float GetCurrentProgress(bool waitForInvoke, bool executeForeground, TimeSpan waitTimeout = default(TimeSpan))
        {
            lock (_locker)
            {
                MaybeWaitForInvoke(waitForInvoke, executeForeground, waitTimeout);
                return _currentProgress;
            }
        }

        public void ReportProgressAndAssertResult(Promise.DeferredBase deferred, float reportValue, float expectedProgress, bool waitForInvoke = true, bool executeForeground = true)
        {
            lock (_locker)
            {
                PrepareForInvoke();
                deferred.ReportProgress(reportValue);
                AssertCurrentProgress(expectedProgress, waitForInvoke, executeForeground);
            }
        }

        public void RejectAndAssertResult<TReject>(Promise.DeferredBase deferred, TReject reason, float expectedProgress, bool waitForInvoke = true, bool executeForeground = true)
        {
            lock (_locker)
            {
                PrepareForInvoke();
                deferred.Reject(reason);
                AssertCurrentProgress(expectedProgress, waitForInvoke, executeForeground);
            }
        }

        public void CancelAndAssertResult(Promise.DeferredBase deferred, float expectedProgress, bool waitForInvoke = true, bool executeForeground = true)
        {
            lock (_locker)
            {
                PrepareForInvoke();
                deferred.Cancel();
                AssertCurrentProgress(expectedProgress, waitForInvoke, executeForeground);
            }
        }

        public void CancelAndAssertResult(CancelationSource cancelationSource, float expectedProgress, bool waitForInvoke = true, bool executeForeground = true)
        {
            lock (_locker)
            {
                PrepareForInvoke();
                cancelationSource.Cancel();
                AssertCurrentProgress(expectedProgress, waitForInvoke, executeForeground);
            }
        }

        public void ResolveAndAssertResult(Promise.Deferred deferred, float expectedProgress, bool waitForInvoke = true, bool executeForeground = true)
        {
            lock (_locker)
            {
                PrepareForInvoke();
                deferred.Resolve();
                AssertCurrentProgress(expectedProgress, waitForInvoke, executeForeground);
            }
        }

        public void ResolveAndAssertResult<T>(Promise<T>.Deferred deferred, T result, float expectedProgress, bool waitForInvoke = true, bool executeForeground = true)
        {
            lock (_locker)
            {
                PrepareForInvoke();
                deferred.Resolve(result);
                AssertCurrentProgress(expectedProgress, waitForInvoke, executeForeground);
            }
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