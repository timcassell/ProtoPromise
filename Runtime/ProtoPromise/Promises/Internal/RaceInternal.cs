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

#pragma warning disable RECS0001 // Class is declared partial but has only one part
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class RacePromise : PromiseBranch, IMultiTreeHandleable
            {
                private struct Creator : ICreator<RacePromise>
                {
                    [MethodImpl(InlineOption)]
                    public RacePromise Create()
                    {
                        return new RacePromise();
                    }
                }

                private readonly object _locker = new object();
                private ValueLinkedStack<PromisePassThrough> _passThroughs;
                private int _waitCount;

                private RacePromise() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    // Release all passthroughs.
                    while (_passThroughs.IsNotEmpty)
                    {
                        _passThroughs.Pop().Release();
                    }
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                public static RacePromise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<RacePromise, Creator>();

                    checked
                    {
                        // Extra retain for handle.
                        ++pendingAwaits;
                    }
                    unchecked
                    {
                        promise._waitCount = (int) pendingAwaits;
                    }
                    promise.Reset();
                    promise.SetupProgress(promisePassThroughs);

                    while (promisePassThroughs.IsNotEmpty)
                    {
                        var passThrough = promisePassThroughs.Pop();
                        lock (promise._locker)
                        {
                            promise._passThroughs.Push(passThrough);
                        }
                        passThrough.SetTargetAndAddToOwner(promise);
                        if (promise._valueOrPrevious != null)
                        {
                            // This was completed potentially before all passthroughs were hooked up.
                            // Try to unhook current passthrough (in case of thread race condition), and release all remaining passthroughs.
                            int addCount = passThrough.TryRemoveFromOwner() ? -1 : 0;
                            while (promisePassThroughs.IsNotEmpty)
                            {
                                var p = promisePassThroughs.Pop();
                                p.Owner.MaybeDispose();
                                p.Release2(-2);
                                --addCount;
                            }
                            if (addCount != 0 && Interlocked.Add(ref promise._waitCount, addCount) == 0)
                            {
                                promise.MaybeDispose();
                            }
                        }
                        passThrough.Release();
                    }

                    return promise;
                }

                public override void Handle()
                {
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    Promise.State state = valueContainer.GetState();
                    State = state;
                    HandleWaiter(valueContainer);
                    HandleProgressListener(state);

                    if (Interlocked.Decrement(ref _waitCount) == 0)
                    {
                        MaybeDispose();
                    }
                }

                public bool Handle(PromiseRef owner, IValueContainer valueContainer, PromisePassThrough passThrough, int index) // IMultiTreeHandleable.Handle
                {
                    ThrowIfInPool(this);

                    if (Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) == null)
                    {
                        owner.SuppressRejection = true;
                        valueContainer.Retain();

                        // Try to unhook and release all passthroughs.
                        ValueLinkedStack<PromisePassThrough> removedPassThroughs = new ValueLinkedStack<PromisePassThrough>();
                        ValueLinkedStack<PromisePassThrough> unRemovedPassThroughs = new ValueLinkedStack<PromisePassThrough>();
                        lock (_locker)
                        {
                            while (_passThroughs.IsNotEmpty)
                            {
                                var p = _passThroughs.Pop();
                                if (p.TryRemoveFromOwner())
                                {
                                    removedPassThroughs.Push(p);
                                }
                                else
                                {
                                    unRemovedPassThroughs.Push(p);
                                }
                            }
                            _passThroughs = unRemovedPassThroughs;
                        }

                        int addWaitCount = -1;
                        while (removedPassThroughs.IsNotEmpty)
                        {
                            removedPassThroughs.Pop().Release();
                            --addWaitCount;
                        }
                        Interlocked.Add(ref _waitCount, addWaitCount);
                        return true;
                    }
                    if (Interlocked.Decrement(ref _waitCount) == 0)
                    {
                        MaybeDispose();
                    }
                    return false;
                }

                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs);
            }

#if PROMISE_PROGRESS
            partial class RacePromise : IProgressInvokable
            {
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }

                private UnsignedFixed32 _currentAmount;

                protected override PromiseRef GetPreviousForProgress(ref IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    return null;
                }

                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs)
                {
                    _currentAmount = default(UnsignedFixed32);

                    // Expect the shortest chain to finish first.
                    uint minWaitDepth = uint.MaxValue;
                    foreach (var passThrough in promisePassThroughs)
                    {
                        minWaitDepth = Math.Min(minWaitDepth, passThrough.Owner._smallFields._waitDepthAndProgress.WholePart);
                    }
                    _smallFields._waitDepthAndProgress = new UnsignedFixed32(minWaitDepth);
                }

                protected override bool AddProgressListenerAndContinueLoop(IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    _progressListener = progressListener;
                    return false;
                }

                protected override UnsignedFixed32 CurrentProgress()
                {
                    ThrowIfInPool(this);
                    return _currentAmount;
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    ThrowIfInPool(this);

                    // TODO: thread synchronization.
                    // Use double for better precision.
                    var newAmount = new UnsignedFixed32(senderAmount.ToDouble() * NextWholeProgress / (double) (ownerAmount.WholePart + 1u));
                    if (newAmount > _currentAmount)
                    {
                        _currentAmount = newAmount;
                        if ((_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) == 0) // Was not already in progress queue?
                        {
                            InterlockedRetainDisregardId();
                            AddToFrontOfProgressQueue(this);
                        }
                    }
                }

                void IProgressInvokable.Invoke()
                {
                    var progress = CurrentProgress();
                    _smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.InProgressQueue);
                    IProgressListener progressListener = _progressListener;
                    if (progressListener != null)
                    {
                        progressListener.SetProgress(this, progress);
                    }
                    MaybeDispose();
                }
            }
#endif
        }
    }
}