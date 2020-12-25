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

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Runtime.CompilerServices;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
#if PROMISE_DEBUG || PROMISE_PROGRESS
            protected virtual void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> borrower) { }

            protected static void ExchangePassthroughs(ref ValueLinkedStack<PromisePassThrough> from, ref ValueLinkedStack<PromisePassThrough> to)
            {
                // Remove this.passThroughs before adding to passThroughs. They are re-added by the caller.
                while (from.IsNotEmpty)
                {
                    var passThrough = from.Pop();
                    if (passThrough.Owner != null && passThrough.Owner._state != Promise.State.Pending)
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

            partial class RacePromise
            {
                protected override void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> borrower)
                {
                    ExchangePassthroughs(ref _passThroughs, ref borrower);
                }
            }

            partial class FirstPromise
            {
                protected override void BorrowPassthroughs(ref ValueLinkedStack<PromisePassThrough> borrower)
                {
                    ExchangePassthroughs(ref _passThroughs, ref borrower);
                }
            }
#endif

            // Calls to these get compiled away when PROGRESS is undefined.
            partial void SetDepth(PromiseRef previous);
            partial void ResetDepth();

            partial void ResolveProgressListeners();
            partial void CancelProgressListeners();

            static partial void ValidateProgress(int skipFrames);

#if PROMISE_PROGRESS
            private ValueLinkedStackZeroGC<IProgressListener> _progressListeners;
            private UnsignedFixed32 _waitDepthAndProgress;

            private uint NextWholeProgress { get { return _waitDepthAndProgress.WholePart + 1u; } }

            partial void ResolveProgressListeners()
            {
                UnsignedFixed32 progress = _waitDepthAndProgress.GetIncrementedWholeTruncated();
                while (_progressListeners.IsNotEmpty)
                {
                    _progressListeners.Pop().ResolveOrSetProgress(this, progress);
                }
            }

            partial void CancelProgressListeners()
            {
                // TODO: this algorithm is O(n^3), refactor progress to reduce runtime costs of cancelations.
                UnsignedFixed32 progress = _waitDepthAndProgress.GetIncrementedWholeTruncated();
                while (_progressListeners.IsNotEmpty)
                {
                    var listener = _progressListeners.Pop();
                    listener.CancelOrSetProgress(this, progress);

#if CSHARP_7_OR_LATER
                    object previous = _valueOrPrevious;
                    while (previous is PromiseRef promise)
                    {
                        promise._progressListeners.Remove(listener);
                        listener.CancelOrSetProgress(promise, progress);
                        previous = promise._valueOrPrevious;
                    }
#else
                PromiseRef promise = _valueOrPrevious as Promise;
                while (promise != null)
                {
                    promise._progressListeners.Remove(listener);
                    listener.CancelOrSetProgress(promise, progress);
                    promise = promise._valueOrPrevious as Promise;
                }
#endif
                }
            }

            partial void ResetDepth()
            {
                _waitDepthAndProgress = default(UnsignedFixed32);
            }

            partial void SetDepth(PromiseRef previous)
            {
                SetDepth(previous._waitDepthAndProgress);
            }

            protected virtual void SetDepth(UnsignedFixed32 previousDepth)
            {
                _waitDepthAndProgress = previousDepth;
            }

            protected virtual bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous)
            {
                progressListener.Retain();
                _progressListeners.Push(progressListener);
                return (previous = _valueOrPrevious as PromiseRef) != null;
            }

            protected virtual bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
            {
                return (previous = _valueOrPrevious as PromiseRef) != null;
            }

            private static void SubscribeProgress(PromiseRef _ref, Action<float> onProgress, CancelationToken cancelationToken)
            {
                if (_ref == null || _ref._state == Promise.State.Resolved)
                {
                    AddToHandleQueueBack(ProgressDelegate.GetOrCreate(onProgress, _ref));
                }
                else if (_ref._state == Promise.State.Pending)
                {
                    SubscribeListenerToTree(_ref, ProgressDelegate.GetOrCreate(onProgress, _ref, cancelationToken));
                }

                // Don't report progress if the promise is canceled or rejected.
            }

            private static void SubscribeProgress<TCapture>(PromiseRef _ref, TCapture capturedValue, Action<TCapture, float> onProgress, CancelationToken cancelationToken)
            {
                if (_ref == null || _ref._state == Promise.State.Resolved)
                {
                    AddToHandleQueueBack(ProgressDelegateCapture<TCapture>.GetOrCreate(capturedValue, onProgress, _ref));
                }
                else if (_ref._state == Promise.State.Pending)
                {
                    SubscribeListenerToTree(_ref, ProgressDelegateCapture<TCapture>.GetOrCreate(capturedValue, onProgress, _ref, cancelationToken));
                }

                // Don't report progress if the promise is canceled or rejected.
            }

            private static void SubscribeListenerToTree(PromiseRef _ref, IProgressListener progressListener)
            {
                // Directly add to listeners for this promise.
                // Sets promise to the one this is waiting on. Returns false if not waiting on another promise.
                PromiseRef promise;
                if (_ref.SubscribeProgressAndContinueLoop(ref progressListener, out promise))
                {
                    SubscribeProgressToBranchesAndRoots(promise, progressListener);
                }
                else
                {
                    // _ref is the root of the promise tree.
                    progressListener.SetInitialProgress(_ref._waitDepthAndProgress);
                }
            }

            private static void SubscribeProgressToBranchesAndRoots(PromiseRef promise, IProgressListener progressListener)
            {
                // This allows us to subscribe progress to AllPromises and RacePromises iteratively instead of recursively
                ValueLinkedStack<PromisePassThrough> passThroughs = new ValueLinkedStack<PromisePassThrough>();

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

            private static void SubscribeProgressToChain(PromiseRef promise, IProgressListener progressListener, ref ValueLinkedStack<PromisePassThrough> passThroughs)
            {
                PromiseRef next;
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
                    case Promise.State.Pending:
                    {
                        progressListener.SetInitialProgress(promise.CurrentProgress());
                        break;
                    }
                    case Promise.State.Resolved:
                    {
                        progressListener.SetInitialProgress(promise._waitDepthAndProgress.GetIncrementedWholeTruncated());
                        break;
                    }
                    default: // Rejected or Canceled:
                    {
                        progressListener.Retain();
                        progressListener.CancelOrSetProgress(promise, promise._waitDepthAndProgress);
                        break;
                    }
                }
            }

            protected virtual UnsignedFixed32 CurrentProgress()
            {
                return _waitDepthAndProgress;
            }

            // Handle progress.
            private static ValueLinkedQueueZeroGC<IInvokable> _progressQueue;
            private static bool _runningProgress;

            private static void AddToFrontOfProgressQueue(IInvokable progressListener)
            {
                _progressQueue.Push(progressListener);
            }

            private static void AddToBackOfProgressQueue(IInvokable progressListener)
            {
                _progressQueue.Enqueue(progressListener);
            }

            internal static void InvokeProgressListeners()
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

            /// <summary>
            /// Max Whole Number: 2^(32-<see cref="Promise.Config.ProgressDecimalBits"/>)
            /// <para/>Precision: 1/(2^<see cref="Promise.Config.ProgressDecimalBits"/>)
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            public struct UnsignedFixed32
            {
                private const double DecimalMax = 1u << Promise.Config.ProgressDecimalBits;
                private const uint DecimalMask = (1u << Promise.Config.ProgressDecimalBits) - 1u;
                private const uint WholeMask = ~DecimalMask;

                private uint _value;

                public UnsignedFixed32(uint wholePart)
                {
                    _value = wholePart << Promise.Config.ProgressDecimalBits;
                }

                public UnsignedFixed32(double decimalPart)
                {
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    _value = (uint) (decimalPart * DecimalMax);
                }

                public uint WholePart { get { return _value >> Promise.Config.ProgressDecimalBits; } }
                private double DecimalPart { get { return (double) DecimalPartAsUInt32 / DecimalMax; } }
                private uint DecimalPartAsUInt32 { get { return _value & DecimalMask; } }

                public uint ToUInt32()
                {
                    return _value;
                }

                public double ToDouble()
                {
                    return (double) WholePart + DecimalPart;
                }

                public void AssignNewDecimalPart(float decimalPart)
                {
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    uint newDecimalPart = (uint) (decimalPart * DecimalMax);
                    _value = (_value & WholeMask) | newDecimalPart;
                }

                public UnsignedFixed32 GetIncrementedWholeTruncated()
                {
#if PROMISE_DEBUG
                    checked
#endif
                    {
                        return new UnsignedFixed32()
                        {
                            _value = (_value & WholeMask) + (1u << Promise.Config.ProgressDecimalBits)
                        };
                    }
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

            /// <summary>
            /// Max Whole Number: 2^(64-<see cref="Promise.Config.ProgressDecimalBits"/>)
            /// Precision: 1/(2^<see cref="Promise.Config.ProgressDecimalBits"/>)
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            public struct UnsignedFixed64 // Simplified compared to UnsignedFixed32 to remove unused functions.
            {
                private const double DecimalMax = 1ul << Promise.Config.ProgressDecimalBits;
                private const ulong DecimalMask = (1ul << Promise.Config.ProgressDecimalBits) - 1ul;

                private ulong _value;

                public ulong WholePart { get { return _value >> Promise.Config.ProgressDecimalBits; } }
                private double DecimalPart { get { return (double) DecimalPartAsUInt32 / DecimalMax; } }
                private ulong DecimalPartAsUInt32 { get { return _value & DecimalMask; } }

                public double ToDouble()
                {
                    return (double) WholePart + DecimalPart;
                }

                public void Increment(uint increment)
                {
                    _value += increment;
                }
            }

            public interface IInvokable
            {
                void Invoke();
            }

            public interface IProgressListener
            {
                void SetInitialProgress(UnsignedFixed32 progress);
                void SetProgress(PromiseRef sender, UnsignedFixed32 progress);
                void ResolveOrSetProgress(PromiseRef sender, UnsignedFixed32 progress);
                void CancelOrSetProgress(PromiseRef sender, UnsignedFixed32 progress);
                void Retain();
            }

            partial interface IMultiTreeHandleable
            {
                void IncrementProgress(uint increment, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            public abstract class ProgressDelegateBase : IProgressListener, ITreeHandleable, IInvokable, ITraceable, ICancelDelegate
            {
#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif
                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

                private PromiseRef _owner;
                private UnsignedFixed32 _current;
                uint _retainCounter;
                private bool _handling;
                private bool _done;
                private bool _suspended;
                private bool _canceled;
                private CancelationRegistration _cancelationRegistration;

                protected ProgressDelegateBase() { }

                protected void Reset(PromiseRef owner, CancelationToken cancelationToken)
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
                        AddRejectionToUnhandledStack(e, this);
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

                private void SetProgress(UnsignedFixed32 progress)
                {
                    _current = progress;
                    _suspended = false;
                    if (!_handling & !_canceled)
                    {
                        _handling = true;
                        // This is called by the promise in reverse order that listeners were added, adding to the front reverses that and puts them in proper order.
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    SetProgress(progress);
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    if (sender == _owner & !_canceled)
                    {
                        // Add to front of handle queue to invoke this with a value of 1.
                        _canceled = true;
                        AddToHandleQueueFront(this);
                    }
                    else
                    {
                        SetProgress(progress);
                        Release();
                    }
                }

                void IProgressListener.SetInitialProgress(UnsignedFixed32 progress)
                {
                    _current = progress;
                    _handling = true;
                    // Always add new listeners to the back.
                    AddToBackOfProgressQueue(this);
                }

                void IProgressListener.CancelOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    if (sender == _owner)
                    {
                        _canceled = true;
                        Release();
                    }
                    else
                    {
                        _suspended = true;
                        _current = progress;
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

                void ITreeHandleable.Handle()
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

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    // TODO: Remove this from the owner's progress listeners and dispose.
                    _canceled = true;
                }

                void ICancelDelegate.Dispose() { }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue) { throw new System.InvalidOperationException(); }
                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer) { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            public sealed class ProgressDelegate : ProgressDelegateBase
            {
                private struct Creator : ICreator<ProgressDelegate>
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public ProgressDelegate Create()
                    {
                        return new ProgressDelegate();
                    }
                }

                private Action<float> _onProgress;

                private ProgressDelegate() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    _onProgress = null;
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                public static ProgressDelegate GetOrCreate(Action<float> onProgress, PromiseRef owner, CancelationToken cancelationToken = default(CancelationToken))
                {
                    var progress = ObjectPool<ITreeHandleable>.GetOrCreate<ProgressDelegate, Creator>(new Creator());
                    progress._onProgress = onProgress;
                    progress.Reset(owner, cancelationToken);
                    return progress;
                }

                protected override void Invoke(float progress)
                {
                    _onProgress.Invoke(progress);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            public sealed class ProgressDelegateCapture<TCapture> : ProgressDelegateBase
            {
                private struct Creator : ICreator<ProgressDelegateCapture<TCapture>>
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public ProgressDelegateCapture<TCapture> Create()
                    {
                        return new ProgressDelegateCapture<TCapture>();
                    }
                }

                private TCapture _capturedValue;
                private Action<TCapture, float> _onProgress;

                private ProgressDelegateCapture() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    _capturedValue = default(TCapture);
                    _onProgress = null;
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                public static ProgressDelegateCapture<TCapture> GetOrCreate(TCapture capturedValue, Action<TCapture, float> onProgress, PromiseRef owner, CancelationToken cancelationToken = default(CancelationToken))
                {
                    var progress = ObjectPool<ITreeHandleable>.GetOrCreate<ProgressDelegateCapture<TCapture>, Creator>(new Creator());
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

                partial void SubscribeProgressToOther(PromiseRef other)
                {
                    _secondPrevious = true;
                    if (_progressListeners.IsNotEmpty)
                    {
                        SubscribeProgressToBranchesAndRoots(other, this);
                    }
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous)
                {
                    // This is guaranteed to be pending.
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    progressListener.Retain();
                    _progressListeners.Push(progressListener);
                    previous = _valueOrPrevious as PromiseRef;
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

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    if (_state != Promise.State.Pending)
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

                    if (_state != Promise.State.Pending | _suspended)
                    {
                        ReleaseInternal();
                        return;
                    }

                    // Calculate the normalized progress for the depth of the returned promise.
                    // Use double for better precision.
                    double expected = ((PromiseRef) _valueOrPrevious)._waitDepthAndProgress.WholePart + 1u;
                    float progress = (float) (_currentAmount.ToDouble() / expected);

                    _waitDepthAndProgress.AssignNewDecimalPart(progress);

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.SetProgress(this, _waitDepthAndProgress);
                    }
                    ReleaseInternal();
                }

                void IProgressListener.SetInitialProgress(UnsignedFixed32 progress)
                {
                    _currentAmount = progress;
                    // Don't allow repool until this is removed from the progress queue.
                    RetainInternal();
                    _invokingProgress = true;
                    AddToFrontOfProgressQueue(this);
                }

                private void SetProgress(UnsignedFixed32 progress)
                {
                    _suspended = false;
                    _currentAmount = progress;
                    if (!_invokingProgress)
                    {
                        // Don't allow repool until this is removed from the progress queue.
                        RetainInternal();
                        _invokingProgress = true;
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    SetProgress(progress);
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    SetProgress(progress);
                    ReleaseWithoutDisposeCheck();
                }

                void IProgressListener.CancelOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    _suspended = true;
                    _currentAmount = progress;
                    ReleaseWithoutDisposeCheck();
                }

                void IProgressListener.Retain()
                {
                    RetainInternal();
                }
            }

            partial class DeferredPromiseBase
            {
                public bool TryReportProgress(float progress, ushort deferredId)
                {
                    if (deferredId != _deferredId) return false;

                    // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                    if (progress >= 1f) return true;

                    _waitDepthAndProgress.AssignNewDecimalPart(progress);
                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.SetProgress(this, _waitDepthAndProgress);
                    }
                    return true;
                }

                protected override sealed bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    if (_state != Promise.State.Pending)
                    {
                        previous = null;
                        return false;
                    }
                    return SubscribeProgressAndContinueLoop(ref progressListener, out previous);
                }
            }

            partial class PromisePassThrough : IProgressListener
            {
                private UnsignedFixed32 _currentProgress;

                void IProgressListener.SetInitialProgress(UnsignedFixed32 progress)
                {
                    _currentProgress = progress;
                    Target.IncrementProgress(progress.ToUInt32(), progress, Owner._waitDepthAndProgress);
                }

                void IProgressListener.SetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    uint dif = progress.ToUInt32() - _currentProgress.ToUInt32();
                    _currentProgress = progress;
                    Target.IncrementProgress(dif, progress, Owner._waitDepthAndProgress);
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    Release();
                }

                void IProgressListener.CancelOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    Release();
                }

                public uint GetProgressDifferenceToCompletion()
                {
                    return Owner._waitDepthAndProgress.GetIncrementedWholeTruncated().ToUInt32() - _currentProgress.ToUInt32();
                }

                partial void ResetProgress()
                {
                    _currentProgress = default(UnsignedFixed32);
                }
            }
#endif
        }
    }
}