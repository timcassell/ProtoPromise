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
#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
            internal bool _ignoreValueTaskContextScheduling;
#endif

            partial class PromiseRef<TResult> : PromiseRefBase
            {
                internal TResult _result;
            }

            partial class PromiseSingleAwait<TResult> : PromiseRef<TResult>
            {
            }

            partial class PromiseDuplicate<TResult> : PromiseSingleAwait<TResult>
            {
            }

            partial class WaitAsyncWithCancelationPromise<TResult> : PromiseSingleAwait<TResult>
            {
                internal CancelationHelper _cancelationHelper;
            }

            partial class WaitAsyncWithTimeoutPromise<TResult> : PromiseSingleAwait<TResult>
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

            partial class WaitAsyncWithTimeoutAndCancelationPromise<TResult> : PromiseSingleAwait<TResult>
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

            partial class DelayPromise : PromiseSingleAwait<VoidResult>
            {
                // Use ITimerSource and int directly instead of the Timer struct
                // so that the fields can be packed efficiently without extra padding.
                private ITimerSource _timerSource;
                private int _timerToken;
                // The timer callback can be invoked before the fields are actually assigned,
                // so we use an Interlocked counter to ensure it is disposed properly.
                private int _timerUseCounter;
            }

            partial class DelayWithCancelationPromise : PromiseSingleAwait<VoidResult>
            {
                private Timers.Timer _timer;
                // The timer and cancelation callbacks can race on different threads,
                // and can be invoked before the fields are actually assigned;
                // we use CancelationHelper to make sure they are used and disposed properly.
                private CancelationHelper _cancelationHelper;
            }

            partial class ConfiguredPromise<TResult> : PromiseSingleAwait<TResult>
            {
                private SynchronizationContext _synchronizationContext;
                // We have to store the previous state in a separate field until the next awaiter is ready to be invoked on the proper context.
                private Promise.State _tempState;
                private CompletedContinuationBehavior _completedBehavior;
            }

            partial class RunPromise<TResult, TDelegate> : PromiseSingleAwait<TResult>
                where TDelegate : IDelegateRun
            {
                private TDelegate _runner;
            }

            partial class RunWaitPromise<TResult, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IDelegateRunPromise
            {
                private TDelegate _runner;
            }

            partial class PromiseMultiAwait<TResult> : PromiseRef<TResult>
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

            partial class PromiseWaitPromise<TResult> : PromiseSingleAwait<TResult>
            {
            }

            partial class DeferredPromiseBase<TResult> : PromiseSingleAwait<TResult>, IDeferredPromise
            {
                protected int _deferredId = 1; // Start with Id 1 instead of 0 to reduce risk of false positives.
            }

            #region Non-cancelable Promises
            partial class DeferredPromise<TResult> : DeferredPromiseBase<TResult>
            {
            }

            partial class DeferredNewPromise<TResult, TDelegate> : DeferredPromise<TResult>
                where TDelegate : IDelegateNew<TResult>
            {
                private int _disposeCounter;
                private TDelegate _runner;
            }

            partial class PromiseResolve<TResult, TResolver> : PromiseSingleAwait<TResult>
                where TResolver : IDelegateResolveOrCancel
            {
                private TResolver _resolver;
            }

            partial class PromiseResolvePromise<TResult, TResolver> : PromiseWaitPromise<TResult>
                where TResolver : IDelegateResolveOrCancelPromise
            {
                private TResolver _resolver;
            }

            partial class PromiseResolveReject<TResult, TResolver, TRejecter> : PromiseSingleAwait<TResult>
                where TResolver : IDelegateResolveOrCancel
                where TRejecter : IDelegateReject
            {
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class PromiseResolveRejectPromise<TResult, TResolver, TRejecter> : PromiseWaitPromise<TResult>
                where TResolver : IDelegateResolveOrCancelPromise
                where TRejecter : IDelegateRejectPromise
            {
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class PromiseContinue<TResult, TContinuer> : PromiseSingleAwait<TResult>
                where TContinuer : IDelegateContinue
            {
                private TContinuer _continuer;
            }

            partial class PromiseContinuePromise<TResult, TContinuer> : PromiseWaitPromise<TResult>
                where TContinuer : IDelegateContinuePromise
            {
                private TContinuer _continuer;
            }

            partial class PromiseFinally<TResult, TFinalizer> : PromiseSingleAwait<TResult>
                where TFinalizer : IAction
            {
                private TFinalizer _finalizer;
            }

            partial class PromiseFinallyWait<TResult, TFinalizer> : PromiseWaitPromise<TResult>
                where TFinalizer : IFunc<Promise>, INullable
            {
                private TFinalizer _finalizer;
                private Promise.State _previousState;
            }

            partial class PromiseCancel<TResult, TCanceler> : PromiseSingleAwait<TResult>
                where TCanceler : IDelegateResolveOrCancel
            {
                private TCanceler _canceler;
            }

            partial class PromiseCancelPromise<TResult, TCanceler> : PromiseWaitPromise<TResult>
                where TCanceler : IDelegateResolveOrCancelPromise
            {
                private TCanceler _canceler;
            }
            #endregion

            #region Cancelable Promises
            partial struct CancelationHelper
            {
                private CancelationRegistration _cancelationRegistration;
                // int for Interlocked.Exchange.
                private int _isCompletedFlag;
                // The retain counter is to ensure the async op(s) we're waiting for and the cancelation callback
                // are completed or guaranteed to never invoke before we return the object to the pool.
                private int _retainCounter;
            }

            partial class CancelablePromiseContinue<TResult, TContinuer> : PromiseSingleAwait<TResult>
                where TContinuer : IDelegateContinue
            {
                internal CancelationHelper _cancelationHelper;
                private TContinuer _continuer;
            }

            partial class CancelablePromiseContinuePromise<TResult, TContinuer> : PromiseWaitPromise<TResult>
                where TContinuer : IDelegateContinuePromise
            {
                internal CancelationHelper _cancelationHelper;
                private TContinuer _continuer;
            }
            #endregion

            #region Multi Promises
            partial class MultiHandleablePromiseBase<TResult> : PromiseSingleAwait<TResult>
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

            partial class PromiseGroupBase<TResult> : PromiseSingleAwait<TResult>
            {
                internal List<Exception> _exceptions;
                protected CancelationRef _cancelationRef; // Store the reference directly instead of CancelationSource struct to reduce memory.
                private int _cancelationId;
                private int _waitCount; // int for Interlocked since it doesn't support uint on older runtimes.
                protected Promise.State _completeState;
            }

            partial class MergePromiseGroupBase<TResult> : PromiseGroupBase<TResult>
            {
                internal ValueLinkedStack<PromisePassThroughForMergeGroup> _completedPassThroughs = new ValueLinkedStack<PromisePassThroughForMergeGroup>();
            }

            partial class MergePromiseGroupVoid : MergePromiseGroupBase<VoidResult>
            {
            }

            partial class MergePromiseGroup<TResult> : PromiseSingleAwait<TResult>
            {
                private bool _isExtended;
            }

            partial class MergePromiseResultsGroup<TResult> : PromiseSingleAwait<TResult>
            {
                private bool _isExtended;
            }

            partial class AllPromiseGroup<T> : MergePromiseGroupBase<IList<T>>
            {
            }

            partial class AllPromiseResultsGroupVoid : MergePromiseGroupBase<IList<Promise.ResultContainer>>
            {
            }

            partial class AllPromiseResultsGroup<T> : MergePromiseGroupBase<IList<Promise<T>.ResultContainer>>
            {
            }

            partial class RacePromiseGroupBase<TResult> : PromiseGroupBase<TResult>
            {
                protected int _isResolved; // Flag used to indicate that the promise has already been resolved. int for Interlocked.
                protected bool _cancelOnNonResolved;
                internal bool _cancelationThrew;
            }

            partial class RacePromiseGroup<TResult> : RacePromiseGroupBase<TResult>
            {
            }

            partial class RacePromiseWithIndexGroupVoid : RacePromiseGroupBase<int>
            {
            }

            partial class RacePromiseWithIndexGroup<TResult> : RacePromiseGroupBase<(int, TResult)>
            {
            }
            #endregion

            partial class AsyncPromiseRef<TResult> : PromiseSingleAwait<TResult>
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