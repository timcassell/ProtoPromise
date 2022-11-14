// This file makes it easier to see all the fields that each promise type has, and calculate how much memory they should consume.

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

// Fix for IL2CPP compile bug. https://issuetracker.unity3d.com/issues/il2cpp-incorrect-results-when-calling-a-method-from-outside-class-in-a-struct
// Unity fixed in 2020.3.20f1 and 2021.1.24f1, but it's simpler to just check for 2021.2 or newer.
// Don't use optimized mode in DEBUG mode for causality traces.
#if (ENABLE_IL2CPP && !UNITY_2021_2_OR_NEWER) || PROMISE_DEBUG
#undef OPTIMIZED_ASYNC_MODE
#else
#define OPTIMIZED_ASYNC_MODE
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Proto.Promises
{
    partial struct Promise
    {
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly Internal.PromiseRefBase _ref;
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly short _id;
#if PROMISE_PROGRESS || PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        private readonly ushort _depth;
#endif

        /// <summary>
        /// Internal use.
        /// </summary>
        internal ushort Depth
        {
            [MethodImpl(Internal.InlineOption)]
#if PROMISE_PROGRESS || PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            get { return _depth; }
#else
            get { return 0; }
#endif
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRefBase promiseRef, short id, ushort depth)
        {
            _ref = promiseRef;
            _id = id;
#if PROMISE_PROGRESS || PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            _depth = depth;
#endif
        }
    }

    partial struct Promise<T>
    {
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly Internal.PromiseRefBase.PromiseRef<T> _ref;
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly T _result;
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly short _id;
#if PROMISE_PROGRESS || PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        private readonly ushort _depth;
#endif

        /// <summary>
        /// Internal use.
        /// </summary>
        internal ushort Depth
        {
            [MethodImpl(Internal.InlineOption)]
#if PROMISE_PROGRESS || PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            get { return _depth; }
#else
            get { return 0; }
#endif
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRefBase.PromiseRef<T> promiseRef, short id, ushort depth)
        {
            _result = default(T);
            _ref = promiseRef;
            _id = id;
#if PROMISE_PROGRESS || PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            _depth = depth;
#endif
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRefBase.PromiseRef<T> promiseRef, short id, ushort depth, T result)
        {
            _ref = promiseRef;
            _result = result;
            _id = id;
#if PROMISE_PROGRESS || PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            _depth = depth;
#endif
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(T result)
        {
            _ref = null;
            _id = 0;
#if PROMISE_PROGRESS || PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            _depth = 0;
#endif
            _result = result;
        }
    }

    partial class Internal
    {
        internal struct VoidResult { }

        partial class HandleablePromiseBase
        {
            volatile internal HandleablePromiseBase _next;
        }

        partial class PromiseSynchronousWaiter : HandleablePromiseBase
        {
            private bool _isHookingUp;
            volatile private bool _didWaitSuccessfully;
            volatile private bool _didWait;
        }

        partial class PromiseRefBase : HandleablePromiseBase
        {
            internal enum WaitState : byte
            {
                First,
                SettingSecond,
                Second,
            }

#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
            volatile internal PromiseRefBase _previous; // Used to detect circular awaits.
#endif
            // This is either a reference to the previous promise for progress to iterate over the promise chain,
            // or it is a reference to its old waiter as part of the registered progress promises linked-list,
            // or it is the reject container if the promise was rejected (null if it was resolved).
            volatile internal object _rejectContainerOrPreviousOrLink;

            private short _promiseId = 1; // Start with Id 1 instead of 0 to reduce risk of false positives.
            private ushort _depth;
            volatile private Promise.State _state;
            private bool _suppressRejection;
            private bool _wasAwaitedorForgotten;
            // Wait state is only used in PromiseWaitPromise, but it's placed here to take advantage of the available bit space.
            volatile private WaitState _waitState;

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

            partial class PromiseDuplicateCancel<TResult> : PromiseSingleAwait<TResult>
            {
                internal CancelationHelper _cancelationHelper;
            }

            partial class PromiseConfigured<TResult> : PromiseSingleAwait<TResult>
            {
                private SynchronizationContext _synchronizationContext;
                internal CancelationHelper _cancelationHelper;
                // We have to store previous reject container in a separate field so we won't break the registered progress promises chain until this is invoked on the synchronization context.
                volatile private object _tempRejectContainer;
                private int _isScheduling; // Flag used so that only Cancel() or Handle() will schedule the continuation. Int for Interlocked.
                // We have to store the previous state in a separate field until the next awaiter is ready to be invoked on the proper context.
                volatile private Promise.State _tempState;
                volatile private bool _wasCanceled;
                private bool _forceAsync;
            }

            partial class PromiseMultiAwait<TResult> : PromiseRef<TResult>
            {
                internal ValueList<HandleablePromiseBase> _nextBranches = new ValueList<HandleablePromiseBase>(8);
                private int _retainCounter;
            }

            partial class PromiseWaitPromise<TResult> : PromiseSingleAwait<TResult>
            {
#if PROMISE_PROGRESS
                ProgressRange _progressRange;
#endif
            }

            [StructLayout(LayoutKind.Explicit)]
            partial struct DeferredIdAndProgress
            {
                // _interlocker overlaps both fields so that we can use Interlocked to CompareExchange both fields at the same time.
                [FieldOffset(0)]
                internal int _id;
                [FieldOffset(4)]
                internal float _currentProgress;
                [FieldOffset(0)]
                private long _interlocker;

                [MethodImpl(InlineOption)]
                internal DeferredIdAndProgress(int initialId)
                {
                    _interlocker = 0;
                    _id = initialId;
                    _currentProgress = 0f;
                }
            }

            partial class DeferredPromiseBase<TResult> : PromiseSingleAwait<TResult>, IDeferredPromise
            {
                protected DeferredIdAndProgress _idAndProgress = new DeferredIdAndProgress(1); // Start with Id 1 instead of 0 to reduce risk of false positives.
            }

            #region Non-cancelable Promises
            partial class DeferredPromise<TResult> : DeferredPromiseBase<TResult>
            {
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
                private int _retainCounter;
            }

            partial class DeferredPromiseCancel<TResult> : DeferredPromise<TResult>
            {
                private CancelationRegistration _cancelationRegistration;
            }

            partial class CancelablePromiseResolve<TResult, TResolver> : PromiseSingleAwait<TResult>
                where TResolver : IDelegateResolveOrCancel
            {
                internal CancelationHelper _cancelationHelper;
                private TResolver _resolver;
            }

            partial class CancelablePromiseResolvePromise<TResult, TResolver> : PromiseWaitPromise<TResult>
                where TResolver : IDelegateResolveOrCancelPromise
            {
                internal CancelationHelper _cancelationHelper;
                private TResolver _resolver;
            }

            partial class CancelablePromiseResolveReject<TResult, TResolver, TRejecter> : PromiseSingleAwait<TResult>
                where TResolver : IDelegateResolveOrCancel
                where TRejecter : IDelegateReject
            {
                internal CancelationHelper _cancelationHelper;
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter> : PromiseWaitPromise<TResult>
                where TResolver : IDelegateResolveOrCancelPromise
                where TRejecter : IDelegateRejectPromise
            {
                internal CancelationHelper _cancelationHelper;
                private TResolver _resolver;
                private TRejecter _rejecter;
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

            partial class CancelablePromiseCancel<TResult, TCanceler> : PromiseSingleAwait<TResult>
                where TCanceler : IDelegateResolveOrCancel
            {
                internal CancelationHelper _cancelationHelper;
                private TCanceler _canceler;
            }

            partial class CancelablePromiseCancelPromise<TResult, TCanceler> : PromiseWaitPromise<TResult>
                where TCanceler : IDelegateResolveOrCancelPromise
            {
                internal CancelationHelper _cancelationHelper;
                private TCanceler _canceler;
            }
            #endregion

            #region Multi Promises
            partial class MultiHandleablePromiseBase<TResult> : PromiseSingleAwait<TResult>
            {
                volatile protected int _waitCount;
                protected int _retainCounter;
                // We store the passthroughs for lazy progress subscribe.
                // The passthroughs will be released when this has fully released if a progress listener did not do it already.
                protected ValueLinkedStack<PromisePassThrough> _passThroughs;

#if PROMISE_DEBUG
                // This is used for circular promise chain detection.
                protected Stack<PromiseRefBase> _previousPromises = new Stack<PromiseRefBase>();
#endif
            }

            internal delegate void PromiseResolvedDelegate<TResult>(PromiseRefBase handler, ref TResult result, int index);

            partial class MergePromise<TResult> : MultiHandleablePromiseBase<TResult>
            {
#if PROMISE_PROGRESS
                private ulong _completeProgress;
#endif
                partial class MergePromiseT : MergePromise<TResult>
                {
                    // TODO: this can be made static
                    private PromiseResolvedDelegate<TResult> _onPromiseResolved;
                }
            }

            partial class RacePromise<TResult> : MultiHandleablePromiseBase<TResult>
            {
            }

            partial class RacePromiseWithIndex<TResult> : RacePromise<ValueTuple<int, TResult>>
            {
            }

            partial class FirstPromise<TResult> : RacePromise<TResult>
            {
            }

            partial class FirstPromiseWithIndex<TResult> : FirstPromise<ValueTuple<int, TResult>>
            {
            }

            partial class PromisePassThrough : HandleablePromiseBase, ILinked<PromisePassThrough>
            {
                volatile private PromiseRefBase _owner;
                volatile private HandleablePromiseBase _target;
                private int _index;
                private short _id;
                private ushort _depth;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                private bool _disposed;
#endif

                // TODO: this can point to _next instead of adding an extra field
                PromisePassThrough ILinked<PromisePassThrough>.Next { get; set; }
            }
            #endregion

#if PROMISE_PROGRESS

            partial struct ProgressRange
            {
                internal float _min;
                internal float _max;
            }

            partial struct ProgressListenerFields
            {
                // Promises that are registered to the progress listener and have not yet completed.
                // This is a linked-list stack of promises that the progress was subscribed to, using _rejectContainerOrPreviousOrLink field as the links to the next.
                // The field type is HandleablePromiseBase so that the initial head (tail) can be the progress listener, but the actual registered promises are casted to PromiseRefBase.
                // This doubles as both a collection of the registered promises, and references to their old waiters (the _rejectContainerOrPreviousOrLink link is also the old waiter).
                // This is much more complex than using standard LinkedList nodes, but it makes the process zero-alloc (we're just re-using an existing field), well worth the added complexity.
                internal HandleablePromiseBase _registeredPromisesHead;

                // Promises that have been attempted to be unregistered due to a canceled promise in the chain, but were unsuccessful due to them having already completed on another thread.
                // This is lazily initialized (because it shouldn't be needed in the vast majority of cases).
                internal Dictionary<PromiseRefBase, HandleablePromiseBase> _unregisteredPromises;

                // Current reporter is used to check if a progress report should be propagated or ignored.
                internal HandleablePromiseBase _currentReporter;

                internal float _current;
                internal float _min;
                internal float _max;
                internal int _retainCounter;
            }

            partial class PromiseProgress<TResult, TProgress> : PromiseSingleAwait<TResult>
                where TProgress : IProgress<float>
            {
                private TProgress _progress;
                private SynchronizationContext _synchronizationContext;
                private CancelationRegistration _cancelationRegistration;

                private ProgressListenerFields _progressFields;
                // We have to store previous reject container in a separate field so we won't break the registered progress promises chain until this is invoked on the synchronization context.
                volatile private object _previousRejectContainer;
                volatile private Promise.State _previousState;
                private bool _isProgressScheduled;
                volatile private bool _canceled;
                private bool _isSynchronous;
                private bool _forceAsync;
                private bool _hookingUp;
            }

            partial class IndividualPromisePassThrough<TResult> : PromiseRef<TResult>
            {
                private PromiseMultiAwait<TResult> _owner;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                private bool _disposed;
#endif
            }

            partial class ProgressMultiAwait<TResult> : ProgressPassThrough
            {
                private PromiseMultiAwait<TResult> _owner;
                private ValueList<HandleablePromiseBase> _progressListeners = new ValueList<HandleablePromiseBase>(8);
                private ProgressListenerFields _progressFields;
                private bool _hookingUp;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                private bool _disposed;
#endif
            }

            partial class ProgressMerger : ProgressPassThrough
            {
                private PromiseRefBase _targetMergePromise;
                // double for better precision to decrease severity of floating point errors when adding and multiplying.
                private double _currentProgress;
                private double _divisorReciprocal; // 1 / expectedProgress since multiplying is faster than dividing.
                volatile private int _retainCounter;
                // The passthroughs are only stored during the hookup.
                private ValueLinkedStack<PromisePassThrough> _passThroughs;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                private bool _disposed;
#endif
            }

            partial class MergeProgressPassThrough : ProgressPassThrough
            {
                private ProgressMerger _target;
                private ProgressListenerFields _progressFields;
                private float _currentProgress;
                private int _index;
                private bool _hookingUp;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                private bool _disposed;
#endif
            }

            partial class ProgressRacer : ProgressPassThrough
            {
                private PromiseRefBase _targetRacePromise;
                private double _currentProgress;
                private int _retainCounter;
                // The passthroughs are only stored during the hookup.
                private ValueLinkedStack<PromisePassThrough> _passThroughs;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                private bool _disposed;
#endif
            }

            partial class RaceProgressPassThrough : ProgressPassThrough
            {
                private ProgressRacer _target;
                private ProgressListenerFields _progressFields;
                private int _index;
                private bool _hookingUp;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                private bool _disposed;
#endif
            }

#endif // PROMISE_PROGRESS

            partial class AsyncPromiseRef<TResult> : PromiseSingleAwait<TResult>
            {
                private ExecutionContext _executionContext;
#if PROMISE_PROGRESS
                private ProgressRange _listenerProgressRange;
                private ProgressRange _userProgressRange;
#endif

#if !OPTIMIZED_ASYNC_MODE
                partial class PromiseMethodContinuer : HandleablePromiseBase
                {
                    protected AsyncPromiseRef<TResult> _owner;
                    // Cache the delegate to prevent new allocations.
                    private Action _moveNext;

                    // Generic class to reference the state machine without boxing it.
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

#if PROMISE_PROGRESS
        partial class ProgressPassThrough : HandleablePromiseBase
        {
        }
#endif
    } // Internal

    namespace Async.CompilerServices
    {
        partial struct PromiseMethodBuilder
        {
            private Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult> _ref;
        }

        partial struct PromiseMethodBuilder<T>
        {
            private Internal.PromiseRefBase.AsyncPromiseRef<T> _ref;
#if OPTIMIZED_ASYNC_MODE
            private T _result;
#endif
        }
    }
}