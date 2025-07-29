// This file makes it easier to see all the fields that each promise type has, and calculate how much memory they should consume.

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

// Fix for IL2CPP compile bug. https://issuetracker.unity3d.com/issues/il2cpp-incorrect-results-when-calling-a-method-from-outside-class-in-a-struct
// Unity fixed in 2020.3.20f1 and 2021.1.24f1, but it's simpler to just check for 2021.2 or newer.
// Don't use optimized mode in DEBUG mode for causality traces.
#if (ENABLE_IL2CPP && !UNITY_2021_2_OR_NEWER) || PROMISE_DEBUG
#undef OPTIMIZED_ASYNC_MODE
#else
#define OPTIMIZED_ASYNC_MODE
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using Proto.Promises.Collections;
using Proto.Timers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Proto.Promises
{
    partial struct Promise
    {
        internal readonly Internal.PromiseRefBase _ref;
        internal readonly short _id;

        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRefBase promiseRef)
        {
            _ref = promiseRef;
            _id = promiseRef.Id;
        }

        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRefBase promiseRef, short id)
        {
            _ref = promiseRef;
            _id = id;
        }
    }

    partial struct Promise<T>
    {
        internal readonly Internal.PromiseRefBase.PromiseRef<T> _ref;
        internal readonly T _result;
        internal readonly short _id;

        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRefBase.PromiseRef<T> promiseRef)
        {
            _result = default;
            _ref = promiseRef;
            _id = promiseRef.Id;
        }

        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRefBase.PromiseRef<T> promiseRef, short id)
        {
            _result = default;
            _ref = promiseRef;
            _id = id;
        }

        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRefBase.PromiseRef<T> promiseRef, short id, in T result)
        {
            _ref = promiseRef;
            _result = result;
            _id = id;
        }

        [MethodImpl(Internal.InlineOption)]
        internal Promise(in T result)
        {
            _ref = null;
            _id = 0;
            _result = result;
        }
    }

    partial class Internal
    {
        // The runtime pads structs to the nearest word size when they are fields in classes, even for empty structs.
        // To save 8 bytes in 64-bit runtime, we use an enum backed by byte instead of an empty struct so that the runtime will pack it efficiently.
        internal enum VoidResult : byte { }

        partial class HandleablePromiseBase
        {
            internal HandleablePromiseBase _next;
        }

        partial class PromiseSynchronousWaiter : HandleablePromiseBase
        {
            volatile private int _waitState; // int for Interlocked.
        }

        // We add a class between HandleablePromiseBase and PromiseRefBase so that we can have a union struct field without affecting derived types sizes.
        // https://github.com/dotnet/runtime/issues/109680
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class PromiseRefBaseWithStructField : HandleablePromiseBase
        {
            // We union the fields together to save space.
            // The field may contain the ExecutionContext or SynchronizationContext of the yielded `async Promise` while it is pending.
            // When the promise is complete in a rejected state, it will contain the IRejectContainer.
            // In theory we could also include the result in this field if it is a reference type, but in practice execution becomes slower when doing it, and the code gets more complicated.
            [StructLayout(LayoutKind.Explicit)]
            private struct ContextRejectUnion
            {
                // Common case this is null. If Promise.Config.AsyncFlowExecutionContextEnabled is true, this may be ExecutionContext.
                // If an awaited Promise was configured, this may be SynchronizationContext. If both cases occurred, this will be ConfiguredAwaitDualContext.
                [FieldOffset(0)]
                internal object _continuationContext;
                [FieldOffset(0)]
                internal IRejectContainer _rejectContainer;
            }

            private ContextRejectUnion _contextOrRejection;

            internal object ContinuationContext
            {
                [MethodImpl(InlineOption)]
                get => _contextOrRejection._continuationContext;
                [MethodImpl(InlineOption)]
                set => _contextOrRejection._continuationContext = value;
            }

            internal IRejectContainer RejectContainer
            {
                [MethodImpl(InlineOption)]
                get => _contextOrRejection._rejectContainer;
                [MethodImpl(InlineOption)]
                set => _contextOrRejection._rejectContainer = value;
            }
        }

        partial class PromiseRefBase : PromiseRefBaseWithStructField
        {
#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
            internal PromiseRefBase _previous; // Used to detect circular awaits.
#endif

            private short _promiseId = 1; // Start with Id 1 instead of 0 to reduce risk of false positives.
            volatile private Promise.State _state;
            private bool _suppressRejection;
            private bool _wasAwaitedOrForgotten;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            protected bool _disposed;
#endif

            partial class PromiseRef<TResult> : PromiseRefBase
            {
                internal TResult _result;
            }

            partial class SingleAwaitPromise<TResult> : PromiseRef<TResult>
            {
            }

            partial class DuplicatePromise<TResult> : SingleAwaitPromise<TResult>
            {
            }

            partial struct CancelationHelper
            {
                private CancelationRegistration _cancelationRegistration;
                // int for Interlocked.Exchange.
                private int _isCompletedFlag;
                // The retain counter is to ensure the async op(s) we're waiting for and the cancelation callback
                // are completed or guaranteed to never invoke before we return the object to the pool.
                private int _retainCounter;
            }

            partial class WaitAsyncWithCancelationPromise<TResult> : SingleAwaitPromise<TResult>
            {
                private CancelationHelper _cancelationHelper;
            }

            partial class WaitAsyncWithTimeoutPromise<TResult> : SingleAwaitPromise<TResult>
            {
                private Timers.Timer _timer;
                // We're waiting on a promise and a timer,
                // so we use an Interlocked counter to ensure this is disposed properly.
                private int _retainCounter;
                // The timer callback can be invoked before the field is actually assigned,
                // and the promise can be completed and the timer fired concurrently from different threads,
                // so we use an Interlocked state to ensure this is completed and the timer is disposed properly.
                private int _waitState;
            }

            partial class WaitAsyncWithTimeoutAndCancelationPromise<TResult> : SingleAwaitPromise<TResult>
            {
                private Timers.Timer _timer;
                private CancelationRegistration _cancelationRegistration;
                // We're waiting on a promise, a timer, and a cancelation token,
                // so we use an Interlocked counter to ensure this is disposed properly.
                private int _retainCounter;
                // The awaited promise, the timer, and the cancelation can race on different threads,
                // and can be invoked before the fields are actually assigned,
                // so we use an Interlocked state to ensure this is completed and the timer
                // and cancelation registration are used and disposed properly.
                private int _waitState;
            }

            partial class DelayPromise : SingleAwaitPromise<VoidResult>
            {
                // Use ITimerSource and int directly instead of the Timer struct
                // so that the fields can be packed efficiently without extra padding.
                private ITimerSource _timerSource;
                private int _timerToken;
                // The timer callback can be invoked before the fields are actually assigned,
                // so we use an Interlocked counter to ensure it is disposed properly.
                private int _timerUseCounter;
            }

            partial class DelayWithCancelationPromise : SingleAwaitPromise<VoidResult>
            {
                private Timers.Timer _timer;
                // The timer and cancelation callbacks can race on different threads,
                // and can be invoked before the fields are actually assigned;
                // we use CancelationHelper to make sure they are used and disposed properly.
                private CancelationHelper _cancelationHelper;
            }

            partial class ConfiguredPromise<TResult> : SingleAwaitPromise<TResult>
            {
                private SynchronizationContext _synchronizationContext;
                // We have to store the previous state in a separate field until the next awaiter is ready to be invoked on the proper context.
                private Promise.State _tempState;
                private CompletedContinuationBehavior _completedBehavior;
            }

            partial class RunPromise<TResult, TDelegate> : SingleAwaitPromise<TResult>
                where TDelegate : IFunc<VoidResult, TResult>
            {
                private TDelegate _callback;
            }

            partial class RunWaitPromise<TDelegate> : CallbackWaitPromiseBase<VoidResult>
                where TDelegate : IFunc<VoidResult, Promise>
            {
                private TDelegate _callback;
            }

            partial class RunWaitPromise<TResult, TDelegate> : CallbackWaitPromiseBase<TResult>
                where TDelegate : IFunc<VoidResult, Promise<TResult>>
            {
                private TDelegate _callback;
            }

            partial class PreservedPromise<TResult> : PromiseRef<TResult>
            {
                private TempCollectionBuilder<HandleablePromiseBase> _nextBranches;
                private int _retainCounter;
            }

            partial class RetainedPromiseBase<TResult> : PromiseRef<TResult>
            {
                private TempCollectionBuilder<HandleablePromiseBase> _nextBranches;
                private int _retainCounter;
            }

            partial class PromiseRetainer<TResult> : RetainedPromiseBase<TResult>
            {
            }

            partial class CallbackWaitPromiseBase<TResult> : SingleAwaitPromise<TResult>
            {
                protected bool _firstContinue;
            }

            partial class DeferredPromiseBase<TResult> : SingleAwaitPromise<TResult>, IDeferredPromise
            {
                protected int _deferredId = 1; // Start with Id 1 instead of 0 to reduce risk of false positives.
            }

            partial class DeferredPromise<TResult> : DeferredPromiseBase<TResult>
            {
            }

            partial class DeferredNewPromise<TResult, TDelegate> : DeferredPromise<TResult>
                where TDelegate : IDelegateNew<TResult>
            {
                private int _disposeCounter;
                private TDelegate _runner;
            }

            partial class ContinueVoidResultPromise<TResult, TDelegate> : SingleAwaitPromise<TResult>
                where TDelegate : IFunc<Promise.ResultContainer, TResult>
            {
                private TDelegate _callback;
            }

            partial class ContinueVoidVoidWaitPromise<TDelegate> : CallbackWaitPromiseBase<VoidResult>
                where TDelegate : IFunc<Promise.ResultContainer, Promise>
            {
                private TDelegate _callback;
            }

            partial class ContinueVoidResultWaitPromise<TResult, TDelegate> : CallbackWaitPromiseBase<TResult>
                where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>
            {
                private TDelegate _callback;
            }

            partial class ContinueArgResultPromise<TArg, TResult, TDelegate> : SingleAwaitPromise<TResult>
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>
            {
                private TDelegate _callback;
            }

            partial class ContinueArgVoidWaitPromise<TArg, TDelegate> : CallbackWaitPromiseBase<VoidResult>
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>
            {
                private TDelegate _callback;
            }

            partial class ContinueArgResultWaitPromise<TArg, TResult, TDelegate> : CallbackWaitPromiseBase<TResult>
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
            {
                private TDelegate _callback;
            }

            partial class CancelableContinueVoidResultPromise<TResult, TDelegate> : SingleAwaitPromise<TResult>
                where TDelegate : IFunc<Promise.ResultContainer, TResult>
            {
                private CancelationHelper _cancelationHelper;
                private TDelegate _callback;
            }

            partial class CancelableContinueVoidVoidWaitPromise<TDelegate> : CallbackWaitPromiseBase<VoidResult>
                where TDelegate : IFunc<Promise.ResultContainer, Promise>
            {
                private CancelationHelper _cancelationHelper;
                private TDelegate _callback;
            }

            partial class CancelableContinueVoidResultWaitPromise<TResult, TDelegate> : CallbackWaitPromiseBase<TResult>
                where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>
            {
                private CancelationHelper _cancelationHelper;
                private TDelegate _callback;
            }

            partial class CancelableContinueArgResultPromise<TArg, TResult, TDelegate> : SingleAwaitPromise<TResult>
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>
            {
                private CancelationHelper _cancelationHelper;
                private TDelegate _callback;
            }

            partial class CancelableContinueArgVoidWaitPromise<TArg, TDelegate> : CallbackWaitPromiseBase<VoidResult>
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>
            {
                private CancelationHelper _cancelationHelper;
                private TDelegate _callback;
            }

            partial class CancelableContinueArgResultWaitPromise<TArg, TResult, TDelegate> : CallbackWaitPromiseBase<TResult>
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
            {
                private CancelationHelper _cancelationHelper;
                private TDelegate _callback;
            }


            partial class ThenPromise<TArg, TResult, TDelegate> : SingleAwaitPromise<TResult>
                where TDelegate : IFunc<TArg, TResult>
            {
                private TDelegate _callback;
            }

            partial class ThenWaitPromise<TArg, TDelegate> : CallbackWaitPromiseBase<VoidResult>
                where TDelegate : IFunc<TArg, Promise>
            {
                private TDelegate _callback;
            }

            partial class ThenWaitPromise<TArg, TResult, TDelegate> : CallbackWaitPromiseBase<TResult>
                where TDelegate : IFunc<TArg, Promise<TResult>>
            {
                private TDelegate _callback;
            }

            partial class ThenPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject> : SingleAwaitPromise<TResult>
                where TDelegateResolve : IFunc<TArg, TResult>
                where TDelegateReject : IFunc<TReject, TResult>
            {
                private TDelegateResolve _resolveCallback;
                private TDelegateReject _rejectCallback;
            }

            partial class ThenWaitPromise<TArg, TReject, TDelegateResolve, TDelegateReject> : CallbackWaitPromiseBase<VoidResult>
                where TDelegateResolve : IFunc<TArg, Promise>
                where TDelegateReject : IFunc<TReject, Promise>
            {
                private TDelegateResolve _resolveCallback;
                private TDelegateReject _rejectCallback;
            }

            partial class ThenWaitPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject> : CallbackWaitPromiseBase<TResult>
                where TDelegateResolve : IFunc<TArg, Promise<TResult>>
                where TDelegateReject : IFunc<TReject, Promise<TResult>>
            {
                private TDelegateResolve _resolveCallback;
                private TDelegateReject _rejectCallback;
            }


            partial class CatchPromise<TResult, TReject, TDelegate> : SingleAwaitPromise<TResult>
                where TDelegate : IFunc<TReject, TResult>
            {
                private TDelegate _callback;
            }

            partial class CatchWaitPromise<TReject, TDelegate> : CallbackWaitPromiseBase<VoidResult>
                where TDelegate : IFunc<TReject, Promise>
            {
                private TDelegate _callback;
            }

            partial class CatchWaitPromise<TResult, TReject, TDelegate> : CallbackWaitPromiseBase<TResult>
                where TDelegate : IFunc<TReject, Promise<TResult>>
            {
                private TDelegate _callback;
            }

            partial class CatchCancelationPromise<TResult, TDelegate> : SingleAwaitPromise<TResult>
                where TDelegate : IFunc<VoidResult, TResult>
            {
                private TDelegate _callback;
            }

            partial class CatchCancelationWaitPromise<TDelegate> : CallbackWaitPromiseBase<VoidResult>
                where TDelegate : IFunc<VoidResult, Promise>
            {
                private TDelegate _callback;
            }

            partial class CatchCancelationWaitPromise<TResult, TDelegate> : CallbackWaitPromiseBase<TResult>
                where TDelegate : IFunc<VoidResult, Promise<TResult>>
            {
                private TDelegate _callback;
            }


            partial class FinallyPromise<TResult, TDelegate> : SingleAwaitPromise<TResult>
                where TDelegate : IAction
            {
                private TDelegate _callback;
            }

            partial class FinallyWaitPromise<TResult, TDelegate> : CallbackWaitPromiseBase<TResult>
                where TDelegate : IFunc<Promise>
            {
                private TDelegate _callback;
                private Promise.State _previousState;
            }

            #region Multi Promises
            partial class MultiHandleablePromiseBase<TResult> : SingleAwaitPromise<TResult>
            {
                protected int _retainCounter;
                protected int _isComplete; // Flag used to indicate that the promise has already been completed. int for Interlocked.
            }

            partial class RacePromise<TResult> : MultiHandleablePromiseBase<TResult>
            {
            }

            partial class RacePromiseWithIndex<TResult> : RacePromise<(int, TResult)>
            {
            }

            partial class FirstPromise<TResult> : RacePromise<TResult>
            {
                protected int _waitCount; // int for Interlocked since it doesn't support uint on older runtimes.
            }

            partial class FirstPromiseWithIndex<TResult> : FirstPromise<(int, TResult)>
            {
            }

            partial class MergePromiseBase<TResult> : MultiHandleablePromiseBase<TResult>
            {
                protected int _waitCount; // int for Interlocked since it doesn't support uint on older runtimes.
            }

            partial class MergePromise<TResult> : MergePromiseBase<TResult>
            {
            }

            partial class MergeSettledPromise<TResult> : MergePromiseBase<TResult>
            {
            }

            partial class PromisePassThrough : HandleablePromiseBase
            {
                protected int _index;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                protected PromiseRefBase _owner;
#endif
            }

            partial class PromisePassThroughForAll : PromisePassThrough
            {
#if !(PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE)
                private PromiseRefBase _owner;
#endif
                private short _id;
            }

            partial class PromisePassThroughForMergeGroup : PromisePassThrough
            {
#if !(PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE)
                private PromiseRefBase _owner;
#endif
            }

            partial class PromiseGroupBase<TResult> : SingleAwaitPromise<TResult>
            {
                internal List<Exception> _exceptions;
                protected CancelationRef _cancelationRef; // Store the reference directly instead of CancelationSource struct to reduce memory.
                private int _cancelationId;
                protected int _waitCount; // int for Interlocked since it doesn't support uint on older runtimes.
            }

            partial class MergePromiseGroupBase<TResult> : PromiseGroupBase<TResult>
            {
                internal ValueLinkedStack<PromisePassThroughForMergeGroup> _completedPassThroughs = new ValueLinkedStack<PromisePassThroughForMergeGroup>();
            }

            partial class MergePromiseGroupVoid : MergePromiseGroupBase<VoidResult>
            {
            }

            partial class MergePromiseGroup<TResult> : SingleAwaitPromise<TResult>
            {
                private List<Exception> _exceptions;
                private ValueLinkedStack<MergeCleanupCallback> _cleanupCallbacks;
                private int _cleanupCount;
                private bool _isExtended;
                private bool _isFinal;
                private bool _isCleaning;
            }

            partial class MergePromiseResultsGroup<TResult> : SingleAwaitPromise<TResult>
            {
                private bool _isExtended;
            }

            partial class AllPromiseGroup<T> : MergePromiseGroupBase<IList<T>>
            {
                private AllCleanupCallback<T> _cleanupCallback;
            }

            partial class AllPromiseResultsGroupVoid : MergePromiseGroupBase<IList<Promise.ResultContainer>>
            {
            }

            partial class AllPromiseResultsGroup<T> : MergePromiseGroupBase<IList<Promise<T>.ResultContainer>>
            {
            }

            partial class RacePromiseGroupBase<TResult> : PromiseGroupBase<TResult>
            {
                protected CancelationRef _sourceCancelationRef; // Store the reference directly instead of CancelationToken struct to reduce memory.
                protected int _isResolved; // Flag used to indicate that the promise has already been resolved. int for Interlocked.
                protected bool _cancelOnNonResolved;
                internal bool _cancelationOrCleanupThrew;
            }

            partial class RacePromiseGroupVoid : RacePromiseGroupBase<VoidResult>
            {
            }

            partial class RacePromiseGroup<TResult> : RacePromiseGroupBase<TResult>
            {
                private RaceCleanupCallback<TResult> _cleanupCallback;
            }
            #endregion

            partial class AsyncPromiseRef<TResult> : SingleAwaitPromise<TResult>
            {
#if !OPTIMIZED_ASYNC_MODE
                partial class PromiseMethodContinuer : HandleablePromiseBase
                {
                    protected AsyncPromiseRef<TResult> _owner;
                    // Cache the delegate to prevent new allocations.
                    private Action _moveNext;

                    // Generic class to hold the state machine without boxing it.
                    partial class Continuer<TStateMachine> : PromiseMethodContinuer where TStateMachine : IAsyncStateMachine
                    {
                        private TStateMachine _stateMachine;
                    }
                }
#else // !OPTIMIZED_ASYNC_MODE
                // Cache the delegate to prevent new allocations.
                private Action _moveNext;

                partial class AsyncPromiseRefMachine<TStateMachine> : AsyncPromiseRef<TResult> where TStateMachine : IAsyncStateMachine
                {
                    // Using a promiseref object as its own continuer saves 16 bytes of object overhead (x64). 24 bytes if we include the `ILinked<T>.Next` field for object pooling purposes.
                    private TStateMachine _stateMachine;
                }
#endif // !OPTIMIZED_ASYNC_MODE
            } // AsyncPromiseRef
        } // PromiseRefBase
    } // Internal

    namespace CompilerServices
    {
        partial struct AsyncPromiseMethodBuilder
        {
            // These must not be readonly.
            private Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult> _ref;
            private short _id;
        }

        partial struct AsyncPromiseMethodBuilder<T>
        {
            // These must not be readonly.
            private Internal.PromiseRefBase.AsyncPromiseRef<T> _ref;
#if OPTIMIZED_ASYNC_MODE
            private T _result;
#else
#pragma warning disable IDE1006 // Naming Styles
            private static T _result => default;
#pragma warning restore IDE1006 // Naming Styles
#endif
            private short _id;
        }
    }
}