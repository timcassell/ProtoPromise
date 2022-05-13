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
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class RacePromise<TResult> : MultiHandleablePromiseBase<TResult>
            {
                private RacePromise() { }

                protected override void MaybeDispose()
                {
                    if (InterlockedAddWithOverflowCheck(ref _retainCounter, -1, 0) == 0)
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                internal static RacePromise<TResult> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<RacePromise<TResult>>()
                        ?? new RacePromise<TResult>();

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE // _waitCount isn't actually used in Race, but can be useful for debugging.
                    promise._waitCount = pendingAwaits;
#endif
                    unchecked
                    {
                        promise._retainCounter = pendingAwaits + 1;
                    }
                    promise.Reset(depth);

                    while (promisePassThroughs.IsNotEmpty)
                    {
                        var passThrough = promisePassThroughs.Pop();
#if PROMISE_DEBUG
                        lock (promise._previousPromises)
                        {
                            promise._previousPromises.Push(passThrough.Owner);
                        }
#endif
                        passThrough.SetTargetAndAddToOwner(promise);
                        if (promise._rejectContainer != null)
                        {
                            // This was completed potentially before all passthroughs were hooked up. Release all remaining passthroughs.
                            int releaseCount = 0;
                            while (promisePassThroughs.IsNotEmpty)
                            {
                                var p = promisePassThroughs.Pop();
                                p.Owner.MaybeMarkAwaitedAndDispose(p.Id);
                                p.Dispose();
                                ++releaseCount;
                            }
                            if (releaseCount != 0 && InterlockedAddWithOverflowCheck(ref promise._retainCounter, -releaseCount, releaseCount - 1) == 0)
                            {
                                promise.Dispose();
                            }
                        }
                    }

                    return promise;
                }

                public override void Handle(PromisePassThrough passThrough, out HandleablePromiseBase nextHandler)
                {
                    var handler = passThrough.Owner;
                    if (Interlocked.CompareExchange(ref _rejectContainer, RejectContainer.s_completionSentinel, null) == null)
                    {
                        handler.SuppressRejection = true;
                        if (handler.State == Promise.State.Resolved)
                        {
                            _result = handler.GetResult<TResult>();
                        }
                        else
                        {
                            _rejectContainer = handler._rejectContainer;
                        }
                        // Very important, write State must come after write _result or _valueContainer. This is a volatile write, so we don't need a full memory barrier.
                        // State is checked for completion, and if it is read not pending on another thread, _result and _valueContainer must have already been written so the other thread can read them.
                        State = handler.State;
                        nextHandler = TakeOrHandleNextWaiter();
                    }
                    else
                    {
                        nextHandler = null;
                    }
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE // _waitCount isn't actually used in Race, but can be useful for debugging.
                    InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0);
#endif
                    MaybeDispose();
                }
            }

#if PROMISE_PROGRESS
            partial class RacePromise<TResult>
            {
                public override PromiseRefBase IncrementProgress(long amount, ref Fixed32 progress, ushort depth)
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