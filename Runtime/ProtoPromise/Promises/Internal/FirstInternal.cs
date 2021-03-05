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
            internal sealed partial class FirstPromise : PromiseBranch, IMultiTreeHandleable
            {
                private struct Creator : ICreator<FirstPromise>
                {
                    [MethodImpl(InlineOption)]
                    public FirstPromise Create()
                    {
                        return new FirstPromise();
                    }
                }

                private readonly object _locker = new object();
                private ValueLinkedStack<PromisePassThrough> _passThroughs;
                private uint _waitCount;

                private FirstPromise() { }

                protected override void Dispose()
                {
                    if (_waitCount == 0) // Quick fix until TODO is done.
                    {
                        base.Dispose();
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }
                    else
                    {
                        Interlocked.Increment(ref _idAndRetainCounter);
                    }
                }

                public static FirstPromise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<FirstPromise, Creator>(new Creator());

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

                public override void Handle()
                {
                    HandleSelf((IValueContainer) _valueOrPrevious);
                }

                bool IMultiTreeHandleable.Handle(IValueContainer valueContainer, PromisePassThrough passThrough, int index)
                {
                    ThrowIfInPool(this);
                    // TODO: remove all passthroughs from their owners when this is completed early.
                    _passThroughs.TryRemove(passThrough);
                    PromiseRef owner = passThrough.Owner;
                    owner._suppressRejection = true;
                    bool done = --_waitCount == 0;
                    bool handle = _valueOrPrevious == null & (owner._state == Promise.State.Resolved | done);
                    if (handle)
                    {
                        valueContainer.Retain();
                        _valueOrPrevious = valueContainer;
                    }
                    else if (done & _state != Promise.State.Pending) // Quick fix until TODO is done.
                    {
                        MaybeDispose();
                    }
                    return handle;
                }

                partial void SetupProgress();
            }

#if PROMISE_PROGRESS
            partial class FirstPromise
            {
                private UnsignedFixed32 _currentAmount;

                partial void SetupProgress()
                {
                    _currentAmount = default(UnsignedFixed32);

                    uint minWaitDepth = uint.MaxValue;
                    foreach (var passThrough in _passThroughs)
                    {
                        minWaitDepth = Math.Min(minWaitDepth, passThrough.Owner._waitDepthAndProgress.WholePart);
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

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous, Stack<PromisePassThrough> passThroughs)
                {
                    ThrowIfInPool(this);
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    _progressListeners.Push(progressListener);
                    if (firstSubscribe & _state == Promise.State.Pending)
                    {
                        BorrowPassthroughs(passThroughs);
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