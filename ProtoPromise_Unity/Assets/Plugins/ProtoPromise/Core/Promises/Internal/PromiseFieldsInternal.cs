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

        partial class PromiseRefBase : HandleablePromiseBase
        {
            [StructLayout(LayoutKind.Explicit)]
            private partial struct SmallFields
            {
                [FieldOffset(0)]
                volatile internal Promise.State _state;
                [FieldOffset(1)]
                internal bool _suppressRejection;
                [FieldOffset(2)]
                internal bool _wasAwaitedorForgotten;
                [FieldOffset(3)]
                internal bool _secondPrevious;
                // Offset 4 so that we can use Interlocked to increment _deferredId.
                // Hopefully this won't be necessary in the future when .Net adds Interlocked methods for byte/sbyte/ushort/short. https://github.com/dotnet/runtime/issues/64658
                [FieldOffset(4)]
                volatile internal int _idInterlocker;
                [FieldOffset(4)]
                internal short _promiseId;
                // _depth shares bit space with _deferredId, because _depth is always 0 on deferred promises, and _deferredId is not used in non-deferred promises.
                [FieldOffset(6)]
                internal short _deferredId;
                [FieldOffset(6)]
                internal ushort _depth;
#if PROMISE_PROGRESS
                [FieldOffset(8)]
                internal Fixed32 _currentProgress;
                [FieldOffset(12)]
                volatile internal int _reportingProgressCount;
#endif

                [MethodImpl(InlineOption)]
                internal SmallFields(short initialId)
                {
                    this = default(SmallFields);
                    _promiseId = initialId;
                    _deferredId = initialId;
                }
            } // SmallFields

#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
            internal PromiseRefBase _previous; // Used to detect circular awaits.
#endif
            volatile internal RejectContainer _rejectContainer;
            private SmallFields _smallFields = new SmallFields(1); // Start with Id 1 instead of 0 to reduce risk of false positives.

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
                private CancelationHelper _cancelationHelper;
            }

            partial class PromiseConfigured<TResult> : PromiseSingleAwait<TResult>
            {
                private SynchronizationContext _synchronizationContext;
                private CancelationHelper _cancelationHelper;
                private int _isScheduling; // Flag used so that only Cancel() or Handle() will call CompareExchangeWaiter and schedule the continuation. Int for Interlocked.
                volatile private bool _wasCanceled;
                volatile private Promise.State _previousState;
                private bool _forceAsync;
            }

            partial class PromiseMultiAwait<TResult> : PromiseRef<TResult>
            {
                private ValueList<HandleablePromiseBase> _nextBranches = new ValueList<HandleablePromiseBase>(8);
                private int _retainCounter;

#if PROMISE_PROGRESS
                private bool _isProgressScheduled;
#endif
            }

            #region Non-cancelable Promises
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

            partial class DeferredPromiseBase<TResult> : AsyncPromiseBase<TResult>, IDeferredPromise
            {
                // Interlocked is more efficient on int than forcing it on a short with a CompareExchange loop.
                // Hopefully this won't be necessary in the future when .Net adds Interlocked methods for byte/sbyte/ushort/short. https://github.com/dotnet/runtime/issues/64658
                // Then we can use the bit space from SmallFields._deferredId instead of an extra field here.
                volatile protected int _deferredId;
            }

            partial class DeferredPromise<TResult> : DeferredPromiseBase<TResult>
            {
            }

            partial class DeferredPromiseCancel<TResult> : DeferredPromise<TResult>
            {
                private CancelationRegistration _cancelationRegistration;
            }

            partial class CancelablePromiseResolve<TResult, TResolver> : PromiseSingleAwait<TResult>
                where TResolver : IDelegateResolveOrCancel
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
            }

            partial class CancelablePromiseResolvePromise<TResult, TResolver> : PromiseWaitPromise<TResult>
                where TResolver : IDelegateResolveOrCancelPromise
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
            }

            partial class CancelablePromiseResolveReject<TResult, TResolver, TRejecter> : PromiseSingleAwait<TResult>
                where TResolver : IDelegateResolveOrCancel
                where TRejecter : IDelegateReject
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter> : PromiseWaitPromise<TResult>
                where TResolver : IDelegateResolveOrCancelPromise
                where TRejecter : IDelegateRejectPromise
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class CancelablePromiseContinue<TResult, TContinuer> : PromiseSingleAwait<TResult>
                where TContinuer : IDelegateContinue
            {
                private CancelationHelper _cancelationHelper;
                private TContinuer _continuer;
            }

            partial class CancelablePromiseContinuePromise<TResult, TContinuer> : PromiseWaitPromise<TResult>
                where TContinuer : IDelegateContinuePromise
            {
                private CancelationHelper _cancelationHelper;
                private TContinuer _continuer;
            }

            partial class CancelablePromiseCancel<TResult, TCanceler> : PromiseSingleAwait<TResult>
                where TCanceler : IDelegateResolveOrCancel
            {
                private CancelationHelper _cancelationHelper;
                private TCanceler _canceler;
            }

            partial class CancelablePromiseCancelPromise<TResult, TCanceler> : PromiseWaitPromise<TResult>
                where TCanceler : IDelegateResolveOrCancelPromise
            {
                private CancelationHelper _cancelationHelper;
                private TCanceler _canceler;
            }
            #endregion

            #region Multi Promises
            partial class MultiHandleablePromiseBase<TResult> : PromiseSingleAwait<TResult>
            {
                protected int _waitCount;
                protected int _retainCounter;

#if PROMISE_DEBUG
                // This is used for circular promise chain detection.
                protected Stack<PromiseRefBase> _previousPromises = new Stack<PromiseRefBase>();
#endif
            }

            internal delegate void PromiseResolvedDelegate<TResult>(PromiseRefBase handler, ref TResult result, int index);

            partial class MergePromise<TResult> : MultiHandleablePromiseBase<TResult>
            {
#if PROMISE_PROGRESS
                // These are used to avoid rounding errors when normalizing the progress.
                // Use 64 bits to allow combining many promises with very deep chains.
                private double _progressScaler;
                private UnsignedFixed64 _unscaledProgress;
#endif
                partial class MergePromiseT : MergePromise<TResult>
                {
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
                // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
                // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
                private struct PassThroughSmallFields
                {
                    internal int _index;
                    internal short _id;
#if PROMISE_PROGRESS
                    internal ushort _depth;
                    internal Fixed32 _currentProgress;
#endif
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    internal bool _disposed;
#endif
                }

                private PromiseRefBase _ownerOrTarget;
                private PassThroughSmallFields _smallFields;

                PromisePassThrough ILinked<PromisePassThrough>.Next { get; set; }
            }
            #endregion

#if PROMISE_PROGRESS
            partial struct Fixed32
            {
                private volatile int _value; // int for Interlocked.
            }

            partial struct UnsignedFixed64
            {
                private long _value; // long for Interlocked.
            }

            partial class PromiseProgress<TResult, TProgress> : PromiseSingleAwait<TResult>
                where TProgress : IProgress<float>
            {
                private TProgress _progress;
                private SynchronizationContext _synchronizationContext;
                private CancelationRegistration _cancelationRegistration;

                volatile private int _isProgressScheduled; // int for Interlocked. 1 if scheduled, 0 if not.
                private int _retainCounter;
                volatile private bool _canceled;
                volatile private Promise.State _previousState;
                private bool _isSynchronous;
                private bool _forceAsync;
            }
#endif

            partial class AsyncPromiseRef<TResult> : AsyncPromiseBase<TResult>
            {
                private HandleablePromiseBase _nextForComplete;
                private ExecutionContext _executionContext;
#if PROMISE_PROGRESS
                private float _minProgress;
                private float _maxProgress;
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