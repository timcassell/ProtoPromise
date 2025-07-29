#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class AllPromiseGroup<T> : MergePromiseGroupBase<IList<T>>
            {
                [MethodImpl(InlineOption)]
                private static AllPromiseGroup<T> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AllPromiseGroup<T>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AllPromiseGroup<T>()
                        : obj.UnsafeAs<AllPromiseGroup<T>>();
                }

                [MethodImpl(InlineOption)]
                internal static AllPromiseGroup<T> GetOrCreate(CancelationRef cancelationRef, IList<T> result, AllCleanupCallback<T> cleanupCallback)
                {
                    var promise = GetOrCreate();
                    promise._result = result;
                    promise._cleanupCallback = cleanupCallback;
                    promise.Reset(cancelationRef);
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    // This is called from cleanup promises.
                    ThrowIfInPool(this);

                    RemovePromiseAndSetCompletionState(handler, state);
                    if (state == Promise.State.Rejected)
                    {
                        RecordException(handler.RejectContainer.GetValueAsException());
                    }
                    handler.MaybeDispose();
                    if (!TryComplete())
                    {
                        return;
                    }

                    if (_exceptions == null)
                    {
                        state = _cancelationRef.IsCanceledUnsafe() ? Promise.State.Canceled : Promise.State.Resolved;
                    }
                    else
                    {
                        state = Promise.State.Rejected;
                        RejectContainer = CreateRejectContainer(new AggregateException(_exceptions), int.MinValue, null, this);
                        _exceptions = null;
                    }

                    HandleNextInternal(state);
                }

                internal override void Handle(PromisePassThroughForMergeGroup passthrough, PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    // We store the passthrough until all promises are complete,
                    // so that items won't be written to the list while it's being expanded on another thread.
                    RemovePromiseAndSetCompletionState(handler, state);
                    _completedPassThroughs.PushInterlocked(passthrough);
                    if (state != Promise.State.Resolved)
                    {
                        CancelGroup();
                    }
                    if (TryComplete())
                    {
                        // All promises are complete.
                        CompleteAndCleanup();
                    }
                }

                private void CompleteAndCleanup()
                {
                    var passthroughs = _completedPassThroughs.TakeAndClear();
                    var resolvedPassThroughs = new ValueLinkedStack<PromisePassThroughForMergeGroup>();
                    while (passthroughs.IsNotEmpty)
                    {
                        var passthrough = passthroughs.Pop();
                        var owner = passthrough.Owner;
                        _result[passthrough.Index] = owner.GetResult<T>();
                        if (owner.State == Promise.State.Resolved)
                        {
                            resolvedPassThroughs.Push(passthrough);
                        }
                        else
                        {
                            if (owner.State == Promise.State.Rejected)
                            {
                                RecordException(owner.RejectContainer.GetValueAsException());
                            }
                            passthrough.Dispose();
                        }
                    }

                    var cleanupCallback = _cleanupCallback;
                    _cleanupCallback = null;
                    bool canceled = _cancelationRef.IsCanceledUnsafe();
                    if (resolvedPassThroughs.IsNotEmpty)
                    {
                        if (!canceled | cleanupCallback == null)
                        {
                            do
                            {
                                resolvedPassThroughs.Pop().Dispose();
                            } while (resolvedPassThroughs.IsNotEmpty);
                            cleanupCallback?.Dispose();
                        }
                        else
                        {
                            // We reuse the _waitCount to know when all cleanup promises are complete.
                            _waitCount = 1;
                            do
                            {
                                var passThrough = resolvedPassThroughs.Pop();
                                T arg = passThrough.Owner.GetResult<T>();
                                int index = passThrough.Index;
                                passThrough.Dispose();
                                InvokeAndWaitForCleanup(cleanupCallback, arg, index);
                            } while (resolvedPassThroughs.IsNotEmpty);

                            foreach (var index in cleanupCallback.ResolvedIndices)
                            {
                                InvokeAndWaitForCleanup(cleanupCallback, _result[index], index);
                            }

                            cleanupCallback.Dispose();
                            if (!TryComplete())
                            {
                                return;
                            }
                        }
                    }
                    else if (cleanupCallback != null)
                    {
                        // We reuse the _waitCount to know when all cleanup promises are complete.
                        _waitCount = 1;
                        foreach (var index in cleanupCallback.ResolvedIndices)
                        {
                            InvokeAndWaitForCleanup(cleanupCallback, _result[index], index);
                        }

                        cleanupCallback.Dispose();
                        if (!TryComplete())
                        {
                            return;
                        }
                    }

                    Promise.State state;
                    if (_exceptions == null)
                    {
                        state = canceled ? Promise.State.Canceled : Promise.State.Resolved;
                    }
                    else
                    {
                        state = Promise.State.Rejected;
                        RejectContainer = CreateRejectContainer(new AggregateException(_exceptions), int.MinValue, null, this);
                        _exceptions = null;
                    }

                    HandleNextInternal(state);
                }

                private void InvokeAndWaitForCleanup(AllCleanupCallback<T> cleanupCallback, in T arg, int index)
                {
                    var cleanupPromise = cleanupCallback.Invoke(arg, index);
                    if (cleanupPromise._ref == null)
                    {
                        return;
                    }
                    try
                    {
                        Interlocked.Increment(ref _waitCount);
                        ValidateReturn(cleanupPromise);
#if PROMISE_DEBUG
                        AddForCircularAwaitDetection(cleanupPromise._ref);
#endif
                        cleanupPromise._ref.HookupExistingWaiter(cleanupPromise._id, this);
                    }
                    catch (Exception e)
                    {
                        Interlocked.Decrement(ref _waitCount);
#if PROMISE_DEBUG
                        RemoveForCircularAwaitDetection(cleanupPromise._ref);
#endif
                        RecordException(e is InvalidReturnException ? e : new InvalidReturnException("onCleanup returned an invalid promise.", string.Empty));
                    }
                }

                internal void MarkReady(int totalPromises)
                {
                    // This method is called after all promises have been hooked up to this.
                    if (MarkReadyAndGetIsComplete(totalPromises))
                    {
                        // All promises already completed.
                        CompleteAndCleanup();
                    }
                }
            }
        } // class PromiseRefBase

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.AllPromiseGroup<T> GetOrCreateAllPromiseGroup<T>(CancelationRef groupCancelationRef, IList<T> result, AllCleanupCallback<T> cleanupCallback)
            => PromiseRefBase.AllPromiseGroup<T>.GetOrCreate(groupCancelationRef, result, cleanupCallback);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidAllGroup(int skipFrames)
            => throw new InvalidOperationException("The promise all group is invalid.", GetFormattedStacktrace(skipFrames + 1));
    } // class Internal
}