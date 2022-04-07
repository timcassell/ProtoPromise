﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class RacePromise : MultiHandleablePromiseBase
            {
                private RacePromise() { }

                protected override void Dispose()
                {
                    base.Dispose();
#if PROMISE_DEBUG
                    lock (_locker)
                    {
                        while (_passThroughs.IsNotEmpty)
                        {
                            _passThroughs.Pop().Release();
                        }
                    }
#endif
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                internal static RacePromise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<RacePromise>()
                        ?? new RacePromise();

                    checked
                    {
                        // Extra retain for handle.
                        ++pendingAwaits;
                    }
                    unchecked
                    {
                        promise._raceSmallFields._waitCount = (int) pendingAwaits;
                    }
                    promise.Reset(depth);

                    while (promisePassThroughs.IsNotEmpty)
                    {
                        var passThrough = promisePassThroughs.Pop();
#if PROMISE_DEBUG
                        passThrough.Retain();
                        lock (promise._locker)
                        {
                            promise._passThroughs.Push(passThrough);
                        }
#endif
                        passThrough.SetTargetAndAddToOwner(promise);
                        if (promise._valueOrPrevious != null)
                        {
                            // This was completed potentially before all passthroughs were hooked up. Release all remaining passthroughs.
                            int addCount = 0;
                            while (promisePassThroughs.IsNotEmpty)
                            {
                                var p = promisePassThroughs.Pop();
                                p.Owner.MaybeDispose();
                                p.Release();
                                --addCount;
                            }
                            if (addCount != 0 && InterlockedAddWithOverflowCheck(ref promise._raceSmallFields._waitCount, addCount, 0) == 0)
                            {
                                promise.MaybeDispose();
                            }
                        }
                    }

                    return promise;
                }

                internal override void Handle(ref PromiseRef handler, ValueContainer valueContainer, PromisePassThrough passThrough, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    // Retain while handling, then release when complete for thread safety.
                    InterlockedRetainDisregardId();

                    if (Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) == null)
                    {
                        handler.SuppressRejection = true;
                        _valueOrPrevious = valueContainer.Clone();
                        Handle(ref _raceSmallFields._waitCount, ref handler, out nextHandler, ref executionScheduler);
                    }
                    else
                    {
                        nextHandler = null;
                    }
                    if (InterlockedAddWithOverflowCheck(ref _raceSmallFields._waitCount, -1, 0) == 0)
                    {
                        _smallFields.InterlockedTryReleaseComplete();
                    }

                    MaybeDispose();
                }
            }

#if PROMISE_PROGRESS
            partial class RacePromise
            {
                new private void Reset(ushort depth)
                {
                    _raceSmallFields._currentProgress = default(Fixed32);
                    base.Reset(depth);
                }

                protected override sealed PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    // Unnecessary to set last known since we know SetInitialProgress will be called on this.
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    _progressListener = progressListener;
                    return null;
                }

                protected override sealed void SetInitialProgress(IProgressListener progressListener, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    progress = _raceSmallFields._currentProgress;
                    SetInitialProgress(progressListener, ref progress, Fixed32.FromWholePlusOne(Depth), out nextRef, ref executionScheduler);
                }

                internal override void IncrementProgress(uint amount, ref Fixed32 progress, ushort depth, out PromiseSingleAwaitWithProgress nextRef)
                {
                    ThrowIfInPool(this);

                    var newAmount = progress.MultiplyAndDivide(Depth + 1, depth + 1);
                    if (_raceSmallFields._currentProgress.InterlockedTrySetIfGreater(newAmount, progress))
                    {
                        nextRef = this;
                        progress = newAmount;
                    }
                    else
                    {
                        nextRef = null;
                    }
                }
            }
#endif
        }
    }
}