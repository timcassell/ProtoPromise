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
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

using System;
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
        internal readonly Promise<Internal.VoidResult> _target;

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRef promiseRef, short id, ushort depth)
        {
            _target = new Promise<Internal.VoidResult>(promiseRef, id, depth);
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Promise<Internal.VoidResult> target)
        {
            _target = target;
        }
    }

    partial struct Promise<T>
    {
        // This is used so that _result will be packed efficiently and not padded with extra bytes (only relevant for small, non-primitive struct T types).
        // Otherwise, if all fields are on the same level as _ref, because it is a class type, it will pad T up to IntPtr.Size if T is not primitive, causing the Promise<T> struct to be larger than necessary.
        // This is especially needed for Promise, which has an internal Promise<Internal.VoidResult> field (and sadly, the runtime does not allow 0-sized structs, minimum size is 1 byte).
        // See https://stackoverflow.com/questions/24742325/why-does-struct-alignment-depend-on-whether-a-field-type-is-primitive-or-user-de
        [StructLayout(LayoutKind.Auto)]
        private
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            struct SmallFields
        {
#if PROMISE_PROGRESS
            internal readonly ushort _depth;
#endif
            internal readonly short _id;
            internal readonly T _result;

            [MethodImpl(Internal.InlineOption)]
            internal SmallFields(short id, ushort depth,
#if CSHARP_7_3_OR_NEWER
                in
#endif
                T result)
            {
#if PROMISE_PROGRESS
                _depth = depth;
#endif
                _id = id;
                _result = result;
            }
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly Internal.PromiseRef _ref;
        private readonly SmallFields _smallFields;

        /// <summary>
        /// Internal use.
        /// </summary>
        internal short Id
        {
            [MethodImpl(Internal.InlineOption)]
            get { return _smallFields._id; }
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        internal T Result
        {
            [MethodImpl(Internal.InlineOption)]
            get { return _smallFields._result; }
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        internal ushort Depth
        {
            [MethodImpl(Internal.InlineOption)]
#if PROMISE_PROGRESS
            get { return _smallFields._depth; }
#else
            get { return 0; }
#endif
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRef promiseRef, short id, ushort depth,
#if CSHARP_7_3_OR_NEWER
                in
#endif
                T value = default(T))
        {
            _ref = promiseRef;
            _smallFields = new SmallFields(id, depth, value);
        }
    }

    partial class Internal
    {
        [Flags]
        internal enum PromiseFlags : byte
        {
            None = 0,

            SuppressRejection = 1 << 0,
            WasAwaitedOrForgotten = 1 << 1,
            HadCallback = 1 << 7, // Shares bit with Subscribed, since HadCallback is only used for SingleAwait and Subscribed is only used for MultiAwait.
            // For progress below
            InProgressQueue = 1 << 2,
            Subscribing = 1 << 3,
            SettingInitial = 1 << 4,
            ReportingPriority = 1 << 5,
            ReportingInitial = 1 << 6,
            Subscribed = 1 << 7,

            All = byte.MaxValue
        }

        internal struct VoidResult { }

        partial class HandleablePromiseBase
        {
            private HandleablePromiseBase _next;
        }

        partial class PromiseRef : HandleablePromiseBase
        {
#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif

            volatile internal object _valueOrPrevious;
            private SmallFields _smallFields = new SmallFields(1); // Start with Id 1 instead of 0 to reduce risk of false positives.

            [StructLayout(LayoutKind.Explicit)]
            private partial struct SmallFields
            {
                // long value with [FieldOffset(0)] allows us to use Interlocked to set fields atomically without consuming more memory than necessary.
                [FieldOffset(0)]
                private long _longValue;
                [FieldOffset(0)]
                volatile internal Promise.State _state;
                [FieldOffset(1)]
                volatile private PromiseFlags _flags;
                [FieldOffset(2)]
                private ushort _retains;
                [FieldOffset(4)]
                internal short _promiseId;
                [FieldOffset(6)]
                internal short _deferredId;
                // _depth shares bit space with _deferredId, because it is always 0 on deferred promises, and _deferredId is not used in non-deferred promises.
                [FieldOffset(6)]
                volatile internal ushort _depth;

                [MethodImpl(InlineOption)]
                internal SmallFields(short initialId)
                {
                    this = default(SmallFields);
                    _promiseId = initialId;
                    _deferredId = initialId;
                }
            } // SmallFields

            partial class PromiseSingleAwait : PromiseRef
            {
                volatile protected HandleablePromiseBase _waiter;
            }

            partial class PromiseConfigured : PromiseSingleAwait
            {
                private enum ScheduleMethod : int
                {
                    None,
                    MakeReady,
                    AddWaiter,
                    OnForgetOrHookupFailed
                }

                private SynchronizationContext _synchronizationContext;
                volatile private int _mostRecentPotentialScheduleMethod; // ScheduleMethod casted to int for Interlocked. This is to make sure this is only scheduled once, even if multiple threads are racing.
            }

            partial class PromiseSingleAwaitWithProgress : PromiseSingleAwait
            {
#if PROMISE_PROGRESS
                volatile protected IProgressListener _progressListener;
#endif
            }

            partial class PromiseMultiAwait : PromiseRef
            {
                // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
                // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
                private partial struct ProgressAndLocker
                {
                    internal SpinLocker _branchLocker;
#if PROMISE_PROGRESS
                    internal SpinLocker _progressCollectionLocker;
                    internal Fixed32 _currentProgress;
#endif
                }

#if PROTO_PROMISE_DEVELOPER_MODE // Must use a queue instead of a stack in developer mode so that the ExecutionSchedule.ScheduleSynchronous can invoke immediately and still be in proper order.
                private ValueLinkedQueue<HandleablePromiseBase> _nextBranches = new ValueLinkedQueue<HandleablePromiseBase>();
#else
                private ValueLinkedStack<HandleablePromiseBase> _nextBranches = new ValueLinkedStack<HandleablePromiseBase>();
#endif
                private ProgressAndLocker _progressAndLocker;

#if PROMISE_PROGRESS
                private ValueLinkedQueue<IProgressListener> _progressListeners = new ValueLinkedQueue<IProgressListener>();

                IProgressListener ILinked<IProgressListener>.Next { get; set; }
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }
#endif
            }

#if PROMISE_PROGRESS
            // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
            // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
            protected partial struct ProgressSubscribeFields
            {
                volatile private int _previousDepthAndFlags;
                internal Fixed32 _currentProgress; // Fixed32 is only used for progress suspension. It's simpler to just re-use the functionality there than to rewrite it for PromiseWaitPromise.
            }
#endif

            partial class AsyncPromiseBase : PromiseSingleAwaitWithProgress
            {
#if PROMISE_PROGRESS
                protected ProgressSubscribeFields _progressFields;
#endif
            }

            partial class PromiseWaitPromise : PromiseSingleAwaitWithProgress
            {
#if PROMISE_PROGRESS
                private ProgressSubscribeFields _progressFields;

                IProgressListener ILinked<IProgressListener>.Next { get; set; }
#endif
            }

            #region Non-cancelable Promises
            partial class PromiseResolve<TResolver> : PromiseSingleAwait
                where TResolver : IDelegateResolveOrCancel
            {
                private TResolver _resolver;
            }

            partial class PromiseResolvePromise<TResolver> : PromiseWaitPromise
                where TResolver : IDelegateResolveOrCancelPromise
            {
                private TResolver _resolver;
            }

            partial class PromiseResolveReject<TResolver, TRejecter> : PromiseSingleAwait
                where TResolver : IDelegateResolveOrCancel
                where TRejecter : IDelegateReject
            {
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class PromiseResolveRejectPromise<TResolver, TRejecter> : PromiseWaitPromise
                where TResolver : IDelegateResolveOrCancelPromise
                where TRejecter : IDelegateRejectPromise
            {
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class PromiseContinue<TContinuer> : PromiseSingleAwait
                where TContinuer : IDelegateContinue
            {
                private TContinuer _continuer;
            }

            partial class PromiseContinuePromise<TContinuer> : PromiseWaitPromise
                where TContinuer : IDelegateContinuePromise
            {
                private TContinuer _continuer;
            }

            partial class PromiseFinally<TFinalizer> : PromiseSingleAwait
                where TFinalizer : IDelegateSimple
            {
                private TFinalizer _finalizer;
            }

            partial class PromiseCancel<TCanceler> : PromiseSingleAwait
                where TCanceler : IDelegateResolveOrCancel
            {
                private TCanceler _canceler;
            }

            partial class PromiseCancelPromise<TCanceler> : PromiseWaitPromise
                where TCanceler : IDelegateResolveOrCancelPromise
            {
                private TCanceler _canceler;
            }
            #endregion

            #region Cancelable Promises
            partial struct CancelationHelper
            {
                private CancelationRegistration _cancelationRegistration;
                private int _retainAndCanceled; // 17th bit is canceled, lower 16 bits are retains. This allows us to use Interlocked for both.
            }

            partial class DeferredPromiseCancel<T> : DeferredPromise<T>
            {
                private CancelationRegistration _cancelationRegistration;
            }

            partial class CancelablePromiseResolve<TResolver> : PromiseSingleAwait
                where TResolver : IDelegateResolveOrCancel
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
            }

            partial class CancelablePromiseResolvePromise<TResolver> : PromiseWaitPromise
                where TResolver : IDelegateResolveOrCancelPromise
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
            }

            partial class CancelablePromiseResolveReject<TResolver, TRejecter> : PromiseSingleAwait
                where TResolver : IDelegateResolveOrCancel
                where TRejecter : IDelegateReject
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class CancelablePromiseResolveRejectPromise<TResolver, TRejecter> : PromiseWaitPromise
                where TResolver : IDelegateResolveOrCancelPromise
                where TRejecter : IDelegateRejectPromise
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class CancelablePromiseContinue<TContinuer> : PromiseSingleAwait
                where TContinuer : IDelegateContinue
            {
                private CancelationHelper _cancelationHelper;
                private TContinuer _continuer;
            }

            partial class CancelablePromiseContinuePromise<TContinuer> : PromiseWaitPromise
                where TContinuer : IDelegateContinuePromise
            {
                private CancelationHelper _cancelationHelper;
                private TContinuer _continuer;
            }

            partial class CancelablePromiseCancel<TCanceler> : PromiseSingleAwait
                where TCanceler : IDelegateResolveOrCancel
            {
                private CancelationHelper _cancelationHelper;
                private TCanceler _canceler;
            }

            partial class CancelablePromiseCancelPromise<TCanceler> : PromiseWaitPromise
                where TCanceler : IDelegateResolveOrCancelPromise
            {
                private CancelationHelper _cancelationHelper;
                private TCanceler _canceler;
            }
            #endregion

            #region Multi Promises
            partial class MultiHandleablePromiseBase : PromiseSingleAwaitWithProgress
            {
#if PROMISE_DEBUG
                protected readonly object _locker = new object();
                protected ValueLinkedStack<PromisePassThrough> _passThroughs = new ValueLinkedStack<PromisePassThrough>();
#endif
            }

            partial class MergePromise : MultiHandleablePromiseBase
            {
                private int _waitCount;

#if PROMISE_PROGRESS
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }

                // These are used to avoid rounding errors when normalizing the progress.
                // Use 64 bits to allow combining many promises with very deep chains.
                private double _progressScaler;
                private UnsignedFixed64 _unscaledProgress;
#endif
            }

            partial class RacePromise : MultiHandleablePromiseBase
            {
                // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
                // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
                private struct RaceSmallFields
                {
                    internal int _waitCount;
#if PROMISE_PROGRESS
                    internal Fixed32 _currentProgress;
#endif
                }

                private RaceSmallFields _raceSmallFields;
#if PROMISE_PROGRESS
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }
#endif
            }

            partial class FirstPromise : MultiHandleablePromiseBase
            {
                // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
                // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
                private struct FirstSmallFields
                {
                    internal int _waitCount;
#if PROMISE_PROGRESS
                    internal Fixed32 _currentProgress;
#endif
                }

                private FirstSmallFields _firstSmallFields;
#if PROMISE_PROGRESS
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }
#endif
            }

            partial class PromisePassThrough : HandleablePromiseBase, ILinked<PromisePassThrough>
            {
                // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
                // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
                private struct PassThroughSmallFields
                {
                    internal int _index;
                    internal int _retainCounter;
#if PROMISE_PROGRESS
                    internal Fixed32 _currentProgress;
                    internal ushort _depth;
                    internal volatile bool _settingInitialProgress;
                    internal volatile bool _reportingProgress;
#endif
                }

                volatile private PromiseRef _owner;
                volatile private MultiHandleablePromiseBase _target;
                private PassThroughSmallFields _smallFields;

                PromisePassThrough ILinked<PromisePassThrough>.Next { get; set; }

#if PROMISE_PROGRESS
                IProgressListener ILinked<IProgressListener>.Next { get; set; }
#endif
            }
            #endregion

#if PROMISE_PROGRESS
            partial class PromiseProgress<TProgress> : PromiseSingleAwaitWithProgress
                where TProgress : IProgress<float>
            {
                // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
                // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
                private struct ProgressSmallFields
                {
                    internal Fixed32 _currentProgress;
                    volatile internal bool _complete;
                    volatile internal bool _canceled;
                    internal bool _isSynchronous;
                }

                private ProgressSmallFields _smallProgressFields;
                private CancelationRegistration _cancelationRegistration;
                private TProgress _progress;
                private SynchronizationContext _synchronizationContext;

                IProgressListener ILinked<IProgressListener>.Next { get; set; }
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }
            }
#endif

#if CSHARP_7_3_OR_NEWER
#if PROMISE_PROGRESS
            partial class AsyncProgressPassThrough
            {
                // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
                // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
                [StructLayout(LayoutKind.Explicit)]
                private partial struct SmallFields
                {
                    [FieldOffset(0)]
                    internal Fixed32 _currentProgress;
                    [FieldOffset(4)]
                    internal ushort _expectedProgress;
                    [FieldOffset(6)]
                    private ushort _retainCounter;
                    [FieldOffset(4)]
                    volatile private int _intValue; // int for Interlocked.
                }

                private AsyncPromiseRef _target;
                private SmallFields _smallFields;
                AsyncProgressPassThrough ILinked<AsyncProgressPassThrough>.Next { get; set; }
                IProgressListener ILinked<IProgressListener>.Next { get; set; }
            }
#endif

            partial class AsyncPromiseRef : AsyncPromiseBase
            {
#if PROMISE_PROGRESS
                private float _minProgress;
                private float _maxProgress;
#endif

#if !OPTIMIZED_ASYNC_MODE
                partial class PromiseMethodContinuer
                {
#if PROMISE_DEBUG
                    protected ITraceable _owner;
#endif
                    // Cache the delegate to prevent new allocations.
                    private Action _moveNext;

                    // Generic class to reference the state machine without boxing it.
                    partial class Continuer<TStateMachine> : PromiseMethodContinuer, ILinked<Continuer<TStateMachine>> where TStateMachine : IAsyncStateMachine
                    {
                        Continuer<TStateMachine> ILinked<Continuer<TStateMachine>>.Next { get; set; }
                        private TStateMachine _stateMachine;
                    }
                }
#else // !OPTIMIZED_ASYNC_MODE
                // Cache the delegate to prevent new allocations.
                private Action _moveNext;

                partial class AsyncPromiseRefMachine<TStateMachine> : AsyncPromiseRef where TStateMachine : IAsyncStateMachine
                {
                    // Using a promiseref object as its own continuer saves 16 bytes of object overhead (x64). 24 bytes if we include the `ILinked<T>.Next` field for object pooling purposes.
                    private TStateMachine _stateMachine;
                }
#endif // !OPTIMIZED_ASYNC_MODE
            } // AsyncPromiseRef
#endif // CSHARP_7_3_OR_NEWER
        } // PromiseRef
    } // Internal
}