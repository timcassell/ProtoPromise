#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable RECS0096 // Type parameter is never used
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
#if PROMISE_DEBUG || PROMISE_PROGRESS
        protected virtual void BorrowPassthroughs(ref ValueLinkedStack<InternalProtected.PromisePassThrough> borrower) { }

        protected static void ExchangePassthroughs(ref ValueLinkedStack<InternalProtected.PromisePassThrough> from, ref ValueLinkedStack<InternalProtected.PromisePassThrough> to)
        {
            // Remove this.passThroughs before adding to passThroughs. They are re-added by the caller.
            while (from.IsNotEmpty)
            {
                var passThrough = from.Pop();
                if (passThrough.Owner != null && passThrough.Owner._state != State.Pending)
                {
                    // The owner already completed.
                    passThrough.Release();
                }
                else
                {
                    to.Push(passThrough);
                }
            }
        }

        partial class InternalProtected
        {
            partial class AllPromise0
            {
                protected override void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> borrower)
                {
                    ExchangePassthroughs(ref _passThroughs, ref borrower);
                }
            }

            partial class MergePromise<T>
            {
                protected override void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> borrower)
                {
                    ExchangePassthroughs(ref _passThroughs, ref borrower);
                }
            }

            partial class RacePromise0
            {
                protected override void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> borrower)
                {
                    ExchangePassthroughs(ref _passThroughs, ref borrower);
                }
            }

            partial class RacePromise<T>
            {
                protected override void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> borrower)
                {
                    ExchangePassthroughs(ref _passThroughs, ref borrower);
                }
            }

            partial class FirstPromise0
            {
                protected override void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> borrower)
                {
                    ExchangePassthroughs(ref _passThroughs, ref borrower);
                }
            }

            partial class FirstPromise<T>
            {
                protected override void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> borrower)
                {
                    ExchangePassthroughs(ref _passThroughs, ref borrower);
                }
            }
        }
#endif

        // Calls to these get compiled away when PROGRESS is undefined.
        partial void SetDepth(Promise next);
        partial void ResetDepth();

        partial void ResolveProgressListeners();
        partial void CancelProgressListeners();

        static partial void ValidateProgress(int skipFrames);
        static partial void InvokeProgressListeners();
        static partial void ClearPooledProgress();

#if !PROMISE_PROGRESS
#pragma warning disable IDE0060 // Remove unused parameter
        protected void ReportProgress(float progress) { }

        static protected void ThrowProgressException(int skipFrames)
        {
            throw new InvalidOperationException("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", Internal.GetFormattedStacktrace(skipFrames + 1));
        }

        static partial void ValidateProgress(int skipFrames)
        {
            ThrowProgressException(skipFrames + 1);
        }

        protected void SubscribeProgress(object onProgress, object cancelationToken)
        {
            ThrowProgressException(2);
        }

        protected void SubscribeProgress(object capturedValue, object onProgress, object cancelationToken)
        {
            ThrowProgressException(2);
        }
#pragma warning restore IDE0060 // Remove unused parameter
#else
        private ValueLinkedStackZeroGC<InternalProtected.IProgressListener> _progressListeners;
        private InternalProtected.UnsignedFixed32 _waitDepthAndProgress;

        static partial void ClearPooledProgress()
        {
            ValueLinkedStackZeroGC<InternalProtected.IProgressListener>.ClearPooledNodes();
            ValueLinkedStackZeroGC<InternalProtected.IInvokable>.ClearPooledNodes();
        }

        // All and Race promises return a value depending on the promises they are waiting on. Other promises return 1.
        protected virtual uint GetIncrementMultiplier()
        {
            return 1u;
        }

        partial void ResolveProgressListeners()
        {
            uint increment = _waitDepthAndProgress.GetDifferenceToNextWholeAsUInt32() * GetIncrementMultiplier();
            while (_progressListeners.IsNotEmpty)
            {
                _progressListeners.Pop().ResolveOrIncrementProgress(this, increment);
            }
        }

        partial void CancelProgressListeners()
        {
            uint increment = _waitDepthAndProgress.GetDifferenceToNextWholeAsUInt32() * GetIncrementMultiplier();
            while (_progressListeners.IsNotEmpty)
            {
                _progressListeners.Pop().CancelOrIncrementProgress(this, increment);
            }
        }

        protected void ReportProgress(float progress)
        {
            if (progress >= 1f | _state != State.Pending)
            {
                // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                return;
            }

            uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress);
            foreach (var progressListener in _progressListeners)
            {
                progressListener.IncrementProgress(this, increment);
            }
        }

        partial void ResetDepth()
        {
            _waitDepthAndProgress = default(InternalProtected.UnsignedFixed32);
        }

        partial void SetDepth(Promise next)
        {
            next.SetDepth(_waitDepthAndProgress);
        }

        protected virtual void SetDepth(InternalProtected.UnsignedFixed32 previousDepth)
        {
            _waitDepthAndProgress = previousDepth;
        }

        protected virtual bool SubscribeProgressAndContinueLoop(ref InternalProtected.IProgressListener progressListener, out Promise previous)
        {
            progressListener.Retain();
            _progressListeners.Push(progressListener);
            return (previous = _valueOrPrevious as Promise) != null;
        }

        protected virtual bool SubscribeProgressIfWaiterAndContinueLoop(ref InternalProtected.IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<InternalProtected.PromisePassThrough> passThroughs)
        {
            return (previous = _valueOrPrevious as Promise) != null;
        }

        protected void SubscribeProgress(Action<float> onProgress, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 2);
            ValidateArgument(onProgress, "onProgress", 2);

            if (_state == State.Pending)
            {
                InternalProtected.IProgressListener progressListener = InternalProtected.ProgressDelegate.GetOrCreate(onProgress, this, cancelationToken);

                // Directly add to listeners for this promise.
                // Sets promise to the one this is waiting on. Returns false if not waiting on another promise.
                Promise promise;
                if (!SubscribeProgressAndContinueLoop(ref progressListener, out promise))
                {
                    // This is the root of the promise tree.
                    progressListener.SetInitialAmount(_waitDepthAndProgress);
                    return;
                }

                SubscribeProgressToBranchesAndRoots(promise, progressListener);
            }
            else if (_state == State.Resolved)
            {
                Internal.AddToHandleQueueBack(InternalProtected.ProgressDelegate.GetOrCreate(onProgress, this));
            }

            // Don't report progress if the promise is canceled or rejected.
        }

        protected void SubscribeProgress<TCapture>(TCapture capturedValue, Action<TCapture, float> onProgress, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 2);
            ValidateArgument(onProgress, "onProgress", 2);

            if (_state == State.Pending)
            {
                InternalProtected.IProgressListener progressListener = InternalProtected.ProgressDelegateCapture<TCapture>.GetOrCreate(capturedValue, onProgress, this, cancelationToken);

                // Directly add to listeners for this promise.
                // Sets promise to the one this is waiting on. Returns false if not waiting on another promise.
                Promise promise;
                if (!SubscribeProgressAndContinueLoop(ref progressListener, out promise))
                {
                    // This is the root of the promise tree.
                    progressListener.SetInitialAmount(_waitDepthAndProgress);
                    return;
                }

                SubscribeProgressToBranchesAndRoots(promise, progressListener);
            }
            else if (_state == State.Resolved)
            {
                Internal.AddToHandleQueueBack(InternalProtected.ProgressDelegateCapture<TCapture>.GetOrCreate(capturedValue, onProgress, this));
            }

            // Don't report progress if the promise is canceled or rejected.
        }

        private static void SubscribeProgressToBranchesAndRoots(Promise promise, InternalProtected.IProgressListener progressListener)
        {
            // This allows us to subscribe progress to AllPromises and RacePromises iteratively instead of recursively
            ValueLinkedStack<InternalProtected.PromisePassThrough> passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>();

        Repeat:
            SubscribeProgressToChain(promise, progressListener, ref passThroughs);

            if (passThroughs.IsNotEmpty)
            {
                // passThroughs are removed from their targets before adding to passThroughs. Add them back here.
                var passThrough = passThroughs.Pop();
                promise = passThrough.Owner;
                progressListener = passThrough;
                passThrough.Target.ReAdd(passThrough);
                goto Repeat;
            }
        }

        private static void SubscribeProgressToChain(Promise promise, InternalProtected.IProgressListener progressListener, ref ValueLinkedStack<InternalProtected.PromisePassThrough> passThroughs)
        {
            Promise next;
            // If the promise is not waiting on another promise (is the root), it sets next to null, does not add the listener, and returns false.
            // If the promise is waiting on another promise that is not its previous, it adds the listener, transforms progresslistener, sets next to the one it's waiting on, and returns true.
            // Otherwise, it sets next to its previous, adds the listener only if it is a WaitPromise, and returns true.
            while (promise.SubscribeProgressIfWaiterAndContinueLoop(ref progressListener, out next, ref passThroughs))
            {
                promise = next;
            }

            // promise is the root of the promise tree.
            switch (promise._state)
            {
                case State.Pending:
                    {
                        progressListener.SetInitialAmount(promise._waitDepthAndProgress);
                        break;
                    }
                case State.Resolved:
                    {
                        progressListener.SetInitialAmount(promise._waitDepthAndProgress.GetIncrementedWholeTruncated());
                        break;
                    }
                default: // Rejected or Canceled:
                    {
                        progressListener.Retain();
                        progressListener.CancelOrIncrementProgress(promise, promise._waitDepthAndProgress.GetIncrementedWholeTruncated().ToUInt32());
                        break;
                    }
            }
        }

        // Handle progress.
        private static ValueLinkedQueueZeroGC<InternalProtected.IInvokable> _progressQueue;
        private static bool _runningProgress;

        private static void AddToFrontOfProgressQueue(InternalProtected.IInvokable progressListener)
        {
            _progressQueue.Push(progressListener);
        }

        private static void AddToBackOfProgressQueue(InternalProtected.IInvokable progressListener)
        {
            _progressQueue.Enqueue(progressListener);
        }

        static partial void InvokeProgressListeners()
        {
            if (_runningProgress)
            {
                // HandleProgress is running higher in the program stack, so just return.
                return;
            }

            _runningProgress = true;

            while (_progressQueue.IsNotEmpty)
            {
                _progressQueue.DequeueRisky().Invoke();
            }

            _progressQueue.ClearLast();
            _runningProgress = false;
        }

        partial class InternalProtected
        {
            /// <summary>
            /// Max Whole Number: 2^(32-<see cref="Config.ProgressDecimalBits"/>)
            /// Precision: 1/(2^<see cref="Config.ProgressDecimalBits"/>)
            /// </summary>
            [System.Diagnostics.DebuggerNonUserCode]
            public struct UnsignedFixed32
            {
                private const uint DecimalMax = 1u << Config.ProgressDecimalBits;
                private const uint DecimalMask = DecimalMax - 1u;
                private const uint WholeMask = ~DecimalMask;

                private uint _value;

                public UnsignedFixed32(uint wholePart)
                {
                    _value = wholePart << Config.ProgressDecimalBits;
                }

                public UnsignedFixed32(float decimalPart)
                {
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    _value = (uint) (decimalPart * DecimalMax);
                }

                public uint WholePart { get { return _value >> Config.ProgressDecimalBits; } }
                private double DecimalPart { get { return (double) DecimalPartAsUInt32 / (double) DecimalMax; } }
                private uint DecimalPartAsUInt32 { get { return _value & DecimalMask; } }

                public uint ToUInt32()
                {
                    return _value;
                }

                public double ToDouble()
                {
                    return (double) WholePart + DecimalPart;
                }

                public uint AssignNewDecimalPartAndGetDifferenceAsUInt32(float decimalPart)
                {
                    uint oldDecimalPart = DecimalPartAsUInt32;
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    uint newDecimalPart = (uint) (decimalPart * DecimalMax);
                    _value = (_value & WholeMask) | newDecimalPart;
                    return newDecimalPart - oldDecimalPart;
                }

                public uint GetDifferenceToNextWholeAsUInt32()
                {
                    return DecimalMax - DecimalPartAsUInt32;
                }

                public UnsignedFixed32 GetIncrementedWholeTruncated()
                {
#if PROMISE_DEBUG
                    checked
#endif
                    {
                        return new UnsignedFixed32()
                        {
                            _value = (_value & WholeMask) + (1u << Config.ProgressDecimalBits)
                        };
                    }
                }

                public void Increment(uint increment)
                {
                    _value += increment;
                }

                public static bool operator >(UnsignedFixed32 a, UnsignedFixed32 b)
                {
                    return a._value > b._value;
                }

                public static bool operator <(UnsignedFixed32 a, UnsignedFixed32 b)
                {
                    return a._value < b._value;
                }
            }

            public interface IInvokable
            {
                void Invoke();
            }

            public interface IProgressListener
            {
                void SetInitialAmount(UnsignedFixed32 amount);
                void IncrementProgress(Promise sender, uint amount);
                void ResolveOrIncrementProgress(Promise sender, uint amount);
                void CancelOrIncrementProgress(Promise sender, uint amount);
                void Retain();
            }

            partial interface IMultiTreeHandleable
            {
                void IncrementProgress(uint increment, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount);
                void CancelOrIncrementProgress(uint increment, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount);
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public abstract class ProgressDelegateBase : IProgressListener, Internal.ITreeHandleable, IInvokable, Internal.ITraceable, Internal.ICancelDelegate
            {
#if PROMISE_DEBUG
                Internal.CausalityTrace Internal.ITraceable.Trace { get; set; }
#endif
                Internal.ITreeHandleable ILinked<Internal.ITreeHandleable>.Next { get; set; }

                private Promise _owner;
                private UnsignedFixed32 _current;
                uint _retainCounter;
                private bool _handling;
                private bool _done;
                private bool _suspended;
                private bool _canceled;
                private CancelationRegistration _cancelationRegistration;

                protected ProgressDelegateBase() { }

                protected void Reset(Promise owner, CancelationToken cancelationToken)
                {
                    _owner = owner;
                    _handling = false;
                    _done = false;
                    _suspended = false;
                    _canceled = false;
                    _current = default(UnsignedFixed32);
                    SetCreatedStacktrace(this, 4);
                    if (cancelationToken.CanBeCanceled)
                    {
                        _cancelationRegistration = cancelationToken.RegisterInternal(this);
                    }
                }

                protected abstract void Invoke(float progress);

                private void InvokeAndCatch(float progress)
                {
                    SetCurrentInvoker(this);
                    try
                    {
                        Invoke(progress);
                    }
                    catch (Exception e)
                    {
                        Internal.AddRejectionToUnhandledStack(e, this);
                    }
                    ClearCurrentInvoker();
                }

                void IInvokable.Invoke()
                {
                    _handling = false;
                    if (_done)
                    {
                        Dispose();
                        return;
                    }
                    if (_suspended | _canceled)
                    {
                        return;
                    }

                    // Calculate the normalized progress for the depth that the listener was added.
                    // Use double for better precision.
                    double expected = _owner._waitDepthAndProgress.WholePart + 1u;
                    InvokeAndCatch((float) (_current.ToDouble() / expected));
                }

                private void IncrementProgress(uint amount)
                {
                    _current.Increment(amount);
                    _suspended = false;
                    if (!_handling & !_canceled)
                    {
                        _handling = true;
                        // This is called by the promise in reverse order that listeners were added, adding to the front reverses that and puts them in proper order.
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IProgressListener.IncrementProgress(Promise sender, uint amount)
                {
                    IncrementProgress(amount);
                }

                void IProgressListener.ResolveOrIncrementProgress(Promise sender, uint amount)
                {
                    if (sender == _owner)
                    {
                        // Add to front of handle queue to invoke this with a value of 1.
                        _canceled = true;
                        Internal.AddToHandleQueueFront(this);
                    }
                    else
                    {
                        IncrementProgress(amount);
                        Release();
                    }
                }

                void IProgressListener.SetInitialAmount(UnsignedFixed32 amount)
                {
                    _current = amount;
                    _handling = true;
                    // Always add new listeners to the back.
                    AddToBackOfProgressQueue(this);
                }

                void IProgressListener.CancelOrIncrementProgress(Promise sender, uint amount)
                {
                    if (sender == _owner)
                    {
                        _canceled = true;
                        Release();
                    }
                    else
                    {
                        _suspended = true;
                        _current.Increment(amount);
                    }
                }

                void IProgressListener.Retain()
                {
                    ++_retainCounter;
                }

                private void MarkOrDispose()
                {
                    if (_handling)
                    {
                        // Mark done so Invoke will dispose.
                        _done = true;
                    }
                    else
                    {
                        // Dispose only if it's not in the progress queue.
                        Dispose();
                    }
                }

                private void Release()
                {
                    if (--_retainCounter == 0)
                    {
                        MarkOrDispose();
                    }
                }

                void Internal.ITreeHandleable.Handle()
                {
                    _cancelationRegistration.TryUnregister();
                    InvokeAndCatch(1f);
                    _retainCounter = 0;
                    _canceled = true;
                    MarkOrDispose();
                }

                protected virtual void Dispose()
                {
                    _owner = null;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                void Internal.ICancelDelegate.Invoke(Internal.ICancelValueContainer valueContainer)
                {
                    // TODO: Remove this from the owner's progress listeners and dispose.
                    _canceled = true;
                }

                void Internal.ICancelDelegate.Dispose() { }

                void Internal.ITreeHandleable.MakeReady(Internal.IValueContainer valueContainer, ref ValueLinkedQueue<Internal.ITreeHandleable> handleQueue) { throw new System.InvalidOperationException(); }
                void Internal.ITreeHandleable.MakeReadyFromSettled(Internal.IValueContainer valueContainer) { throw new System.InvalidOperationException(); }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class ProgressDelegate : ProgressDelegateBase
            {
                private static ValueLinkedStack<Internal.ITreeHandleable> _pool;

                static ProgressDelegate()
                {
                    Internal.OnClearPool += () => _pool.Clear();
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _onProgress = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                private Action<float> _onProgress;

                private ProgressDelegate() { }

                public static ProgressDelegate GetOrCreate(Action<float> onProgress, Promise owner, CancelationToken cancelationToken = default(CancelationToken))
                {
                    var progress = _pool.IsNotEmpty ? (ProgressDelegate) _pool.Pop() : new ProgressDelegate();
                    progress._onProgress = onProgress;
                    progress.Reset(owner, cancelationToken);
                    return progress;
                }

                protected override void Invoke(float progress)
                {
                    _onProgress.Invoke(progress);
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class ProgressDelegateCapture<TCapture> : ProgressDelegateBase
            {
                private static ValueLinkedStack<Internal.ITreeHandleable> _pool;

                static ProgressDelegateCapture()
                {
                    Internal.OnClearPool += () => _pool.Clear();
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _capturedValue = default(TCapture);
                    _onProgress = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
                TCapture _capturedValue;
                private Action<TCapture, float> _onProgress;

                private ProgressDelegateCapture() { }

                public static ProgressDelegateCapture<TCapture> GetOrCreate(TCapture capturedValue, Action<TCapture, float> onProgress, Promise owner, CancelationToken cancelationToken = default(CancelationToken))
                {
                    var progress = _pool.IsNotEmpty ? (ProgressDelegateCapture<TCapture>) _pool.Pop() : new ProgressDelegateCapture<TCapture>();
                    progress._capturedValue = capturedValue;
                    progress._onProgress = onProgress;
                    progress.Reset(owner, cancelationToken);
                    return progress;
                }

                protected override void Invoke(float progress)
                {
                    _onProgress.Invoke(_capturedValue, progress);
                }
            }

            partial class PromiseWaitPromise : IProgressListener, IInvokable
            {
                // This is used to avoid rounding errors when normalizing the progress.
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;
                private bool _secondPrevious;
                protected bool _suspended;

                protected override void Reset()
                {
                    base.Reset();
                    _secondPrevious = false;
                    _suspended = false;
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out Promise previous)
                {
                    // This is guaranteed to be pending.
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    progressListener.Retain();
                    _progressListeners.Push(progressListener);
                    previous = _valueOrPrevious as Promise;
                    if (_secondPrevious)
                    {
                        if (!firstSubscribe)
                        {
                            return false;
                        }
                        // Subscribe this to the returned promise.
                        progressListener = this;
                    }
                    return previous != null;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    if (_state != State.Pending)
                    {
                        previous = null;
                        return false;
                    }
                    return SubscribeProgressAndContinueLoop(ref progressListener, out previous);
                }

                protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
                {
                    _waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
                }

                void IInvokable.Invoke()
                {
                    _invokingProgress = false;

                    if (_state != State.Pending | _suspended)
                    {
                        ReleaseInternal();
                        return;
                    }

                    // Calculate the normalized progress for the depth of the returned promise.
                    // Use double for better precision.
                    double expected = ((Promise) _valueOrPrevious)._waitDepthAndProgress.WholePart + 1u;
                    float progress = (float) (_currentAmount.ToDouble() / expected);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress);

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.IncrementProgress(this, increment);
                    }
                    ReleaseInternal();
                }

                void IProgressListener.SetInitialAmount(UnsignedFixed32 amount)
                {
                    _currentAmount = amount;
                    // Don't allow repool until this is removed from the progress queue.
                    RetainInternal();
                    _invokingProgress = true;
                    AddToFrontOfProgressQueue(this);
                }

                private void IncrementProgress(uint amount)
                {
                    _suspended = false;
                    _currentAmount.Increment(amount);
                    if (!_invokingProgress)
                    {
                        // Don't allow repool until this is removed from the progress queue.
                        RetainInternal();
                        _invokingProgress = true;
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IProgressListener.IncrementProgress(Promise sender, uint amount)
                {
                    IncrementProgress(amount);
                }

                void IProgressListener.ResolveOrIncrementProgress(Promise sender, uint amount)
                {
                    IncrementProgress(amount);
                    ReleaseWithoutDisposeCheck();
                }

                void IProgressListener.CancelOrIncrementProgress(Promise sender, uint amount)
                {
                    _suspended = true;
                    _currentAmount.Increment(amount);
                    ReleaseWithoutDisposeCheck();
                }
            }

            partial class PromiseWaitPromise<T> : IProgressListener, IInvokable
            {
                // This is used to avoid rounding errors when normalizing the progress.
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;
                private bool _secondPrevious;
                protected bool _suspended;

                protected override void Reset()
                {
                    base.Reset();
                    _invokingProgress = false;
                    _secondPrevious = false;
                    _suspended = false;
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out Promise previous)
                {
                    // This is guaranteed to be pending.
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    progressListener.Retain();
                    _progressListeners.Push(progressListener);
                    previous = _valueOrPrevious as Promise;
                    if (_secondPrevious)
                    {
                        if (!firstSubscribe)
                        {
                            return false;
                        }
                        // Subscribe this to the returned promise.
                        progressListener = this;
                    }
                    return previous != null;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    if (_state != State.Pending)
                    {
                        previous = null;
                        return false;
                    }
                    return SubscribeProgressAndContinueLoop(ref progressListener, out previous);
                }

                protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
                {
                    _waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
                }

                void IInvokable.Invoke()
                {
                    _invokingProgress = false;

                    if (_state != State.Pending | _suspended)
                    {
                        ReleaseInternal();
                        return;
                    }

                    // Calculate the normalized progress for the depth of the cached promise.
                    // Use double for better precision.
                    double expected = ((Promise) _valueOrPrevious)._waitDepthAndProgress.WholePart + 1u;
                    float progress = (float) (_currentAmount.ToDouble() / expected);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress);

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.IncrementProgress(this, increment);
                    }
                    ReleaseInternal();
                }

                void IProgressListener.SetInitialAmount(UnsignedFixed32 amount)
                {
                    _currentAmount = amount;
                    // Don't allow repool until this is removed from the progress queue.
                    RetainInternal();
                    _invokingProgress = true;
                    AddToFrontOfProgressQueue(this);
                }

                private void IncrementProgress(uint amount)
                {
                    _suspended = false;
                    _currentAmount.Increment(amount);
                    if (!_invokingProgress)
                    {
                        // Don't allow repool until this is removed from the progress queue.
                        RetainInternal();
                        _invokingProgress = true;
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IProgressListener.IncrementProgress(Promise sender, uint amount)
                {
                    IncrementProgress(amount);
                }

                void IProgressListener.ResolveOrIncrementProgress(Promise sender, uint amount)
                {
                    IncrementProgress(amount);
                    ReleaseWithoutDisposeCheck();
                }

                void IProgressListener.CancelOrIncrementProgress(Promise sender, uint amount)
                {
                    _suspended = true;
                    _currentAmount.Increment(amount);
                    ReleaseWithoutDisposeCheck();
                }
            }

            partial class DeferredPromiseVoid
            {
                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    if (_state != State.Pending)
                    {
                        previous = null;
                        return false;
                    }
                    return SubscribeProgressAndContinueLoop(ref progressListener, out previous);
                }
            }

            partial class DeferredPromise<T>
            {
                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    if (_state != State.Pending)
                    {
                        previous = null;
                        return false;
                    }
                    return SubscribeProgressAndContinueLoop(ref progressListener, out previous);
                }
            }

            partial class PromisePassThrough : IProgressListener
            {
                public UnsignedFixed32 _progress;

                void IProgressListener.SetInitialAmount(UnsignedFixed32 amount)
                {
                    Target.IncrementProgress(amount.ToUInt32(), amount, Owner._waitDepthAndProgress);
                }

                void IProgressListener.IncrementProgress(Promise sender, uint amount)
                {
                    Target.IncrementProgress(amount, sender._waitDepthAndProgress, Owner._waitDepthAndProgress);
                }

                void IProgressListener.ResolveOrIncrementProgress(Promise sender, uint amount)
                {
                    Release();
                }

                void IProgressListener.CancelOrIncrementProgress(Promise sender, uint amount)
                {
                    Release();
                }
            }
        }
#endif
    }

    partial class Promise<T>
    {
        static partial void ValidateProgress(int skipFrames);
#if !PROMISE_PROGRESS
        static partial void ValidateProgress(int skipFrames)
        {
            ThrowProgressException(skipFrames + 1);
        }
#endif
    }
}