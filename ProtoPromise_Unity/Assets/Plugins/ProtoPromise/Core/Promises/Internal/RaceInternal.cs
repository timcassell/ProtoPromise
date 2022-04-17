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
                        promise._waitCount = (int) pendingAwaits;
                    }
                    promise.Reset(depth, 2);

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
                        if (promise._valueContainer != null)
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
                            if (addCount != 0 && InterlockedAddWithOverflowCheck(ref promise._waitCount, addCount, 0) == 0)
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

                    if (Interlocked.CompareExchange(ref _valueContainer, valueContainer, null) == null)
                    {
                        handler.SuppressRejection = true;
                        _valueContainer = valueContainer.Clone();
                        Handle(ref _waitCount, ref handler, out nextHandler, ref executionScheduler);
                    }
                    else
                    {
                        nextHandler = null;
                    }
                    if (InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0) == 0)
                    {
                        _smallFields.InterlockedTryReleaseComplete();
                    }

                    MaybeDispose();
                }
            }

#if PROMISE_PROGRESS
            partial class RacePromise
            {
                internal override PromiseSingleAwait IncrementProgress(uint amount, ref Fixed32 progress, ushort depth)
                {
                    ThrowIfInPool(this);

                    var newAmount = progress.MultiplyAndDivide(Depth + 1, depth + 1);
                    if (_smallFields._currentProgress.InterlockedTrySetIfGreater(newAmount))
                    {
                        progress = newAmount;
                        return this;
                    }
                    return null;
                }
            }
#endif
        }
    }
}