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

using System;
using System.Runtime.CompilerServices;
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

                private ValueLinkedStack<PromisePassThrough> _passThroughs;
                private uint _waitCount;

                private RacePromise() { }

                protected override void Dispose()
                {
                    if (_waitCount == 0) // Quick fix until TODO is done.
                    {
                        base.Dispose();
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }
                }

                public static RacePromise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<RacePromise, Creator>(new Creator());

                    promise._passThroughs = promisePassThroughs;
                    promise._waitCount = pendingAwaits;
                    promise.Reset();
                    promise.SetupProgress();

                    foreach (var passThrough in promisePassThroughs)
                    {
                        passThrough.SetTargetAndAddToOwner(promise);
                    }

                    return promise;
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    HandleSelf(valueContainer);
                }

                bool IMultiTreeHandleable.Handle(IValueContainer valueContainer, PromisePassThrough passThrough, int index)
                {
                    ThrowIfInPool(this);
                    // TODO: remove all passthroughs from their owners when this is completed early.
                    _passThroughs.TryRemove(passThrough);
                    PromiseRef owner = passThrough.Owner;
                    --_waitCount;
                    bool handle = _valueOrPrevious == null;
                    if (handle)
                    {
                        owner._suppressRejection = true;
                        valueContainer.Retain();
                        _valueOrPrevious = valueContainer;
                    }
                    else if (_waitCount == 0) // Quick fix until TODO is done.
                    {
                        MaybeDispose();
                    }
                    return handle;
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    _passThroughs.Push(passThrough);
                }

                partial void SetupProgress();
            }

#if PROMISE_PROGRESS
            partial class RacePromise
            {
                private UnsignedFixed32 _currentAmount;

                partial void SetupProgress()
                {
                    _currentAmount = default(UnsignedFixed32);

                    uint minWaitDepth = uint.MaxValue;
                    foreach (var passThrough in _passThroughs)
                    {
                        PromiseRef owner = passThrough.Owner;
                        if (owner != null)
                        {
                            minWaitDepth = Math.Min(minWaitDepth, owner._waitDepthAndProgress.WholePart);
                        }
                    }

                    // Expect the shortest chain to finish first.
                    _waitDepthAndProgress = new UnsignedFixed32(minWaitDepth);
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous)
                {
                    ThrowIfInPool(this);
                    // This is guaranteed to be pending.
                    previous = this;
                    return true;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    ThrowIfInPool(this);
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    _progressListeners.Push(progressListener);
                    if (firstSubscribe & _state == Promise.State.Pending)
                    {
                        BorrowPassthroughs(ref passThroughs);
                    }

                    previous = null;
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
                    if (_state != Promise.State.Pending) return;

                    // TODO: thread synchronization.
                    // Use double for better precision.
                    var newAmount = new UnsignedFixed32(senderAmount.ToDouble() * NextWholeProgress / (double) (ownerAmount.WholePart + 1u));
                    if (newAmount > _currentAmount)
                    {
                        _currentAmount = newAmount;

                        foreach (var progressListener in _progressListeners)
                        {
                            progressListener.SetProgress(this, newAmount);
                        }
                    }
                }
            }
#endif
        }
    }
}