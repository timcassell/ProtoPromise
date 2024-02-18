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

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0052 // Remove unread private members
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value
#pragma warning disable 0414 // The private field is assigned but its value is never used

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

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRefBase promiseRef, short id)
        {
            _ref = promiseRef;
            _id = id;
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

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRefBase.PromiseRef<T> promiseRef, short id)
        {
            _result = default(T);
            _ref = promiseRef;
            _id = id;
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRefBase.PromiseRef<T> promiseRef, short id, T result)
        {
            _ref = promiseRef;
            _result = result;
            _id = id;
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(T result)
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
            private const int InitialState = 0;
            private const int WaitingState = 1;
            private const int CompletedState = 2;
            private const int WaitedSuccessState = 3;
            private const int WaitedFailedState = 4;

            volatile private int _waitState; // int for Interlocked.
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
            internal PromiseRefBase _previous; // Used to detect circular awaits.
#endif
            internal IRejectContainer _rejectContainer;

            private short _promiseId = 1; // Start with Id 1 instead of 0 to reduce risk of false positives.
            volatile private Promise.State _state;
            private bool _suppressRejection;
            private bool _wasAwaitedorForgotten;

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
                private int _isScheduling; // Flag used so that only Cancel() or Handle() will schedule the continuation. Int for Interlocked.
                // We have to store the previous state in a separate field until the next awaiter is ready to be invoked on the proper context.
                volatile private Promise.State _tempState;
                volatile private bool _wasCanceled;
                private bool _forceAsync;
            }

            partial class RunPromise<TResult, TDelegate> : PromiseSingleAwait<TResult>
                where TDelegate : IDelegateRun
            {
                private SynchronizationContext _synchronizationContext;
                private TDelegate _runner;
            }

            partial class RunWaitPromise<TResult, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IDelegateRunPromise
            {
                private SynchronizationContext _synchronizationContext;
                private TDelegate _runner;
            }

            partial class PromiseMultiAwait<TResult> : PromiseRef<TResult>
            {
                // TODO: we can replace ValueList with TempCollectionBuilder.
                internal ValueList<HandleablePromiseBase> _nextBranches = new ValueList<HandleablePromiseBase>(8);
                private int _retainCounter;
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
                private SynchronizationContext _synchronizationContext;
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
                private int _retainCounter;
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
                protected int _waitCount;
                protected int _retainCounter;
                // TODO: progress was removed, we probably don't need to store the passthroughs anymore.
                // We store the passthroughs for lazy progress subscribe.
                // The passthroughs will be released when this has fully released if a progress listener did not do it already.
                protected ValueLinkedStack<PromisePassThrough> _passThroughs;
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

            partial class PromisePassThrough : HandleablePromiseBase
            {
                private PromiseRefBase _owner;
                private HandleablePromiseBase _target;
                private int _index;
                private short _id;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                private bool _disposed;
#endif
            }
#endregion

            partial class AsyncPromiseRef<TResult> : PromiseSingleAwait<TResult>
            {
                private ExecutionContext _executionContext;

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

    namespace Async.CompilerServices
    {
        partial struct PromiseMethodBuilder
        {
            // This must not be readonly.
            private Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult> _ref;
        }

        partial struct PromiseMethodBuilder<T>
        {
            // These must not be readonly.
            private Internal.PromiseRefBase.AsyncPromiseRef<T> _ref;
#if OPTIMIZED_ASYNC_MODE
            private T _result;
#endif
        }
    }
}