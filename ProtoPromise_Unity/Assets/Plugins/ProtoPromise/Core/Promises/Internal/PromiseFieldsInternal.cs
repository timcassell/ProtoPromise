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
        internal Promise(Internal.PromiseRef promiseRef, short id, int depth)
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
            internal readonly int _depth;
#endif
            internal readonly short _id;
            internal readonly T _result;

            [MethodImpl(Internal.InlineOption)]
            internal SmallFields(short id, int depth,
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
        internal int Depth
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
        internal Promise(Internal.PromiseRef promiseRef, short id, int depth,
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
        internal struct VoidResult { }

        partial class PromiseRef
        {
            [Flags]
            private enum PromiseFlags : byte
            {
                None = 0,

                // Don't change the layout, very important for InterlockedSetSubscribedIfSecondPrevious().
                SuppressRejection = 1 << 0,
                WasAwaitedOrForgotten = 1 << 1,
                // For progress below
                SecondPrevious = 1 << 2,
                SelfSubscribed = 1 << 3,
                InProgressQueue = 1 << 4,
                Subscribing = 1 << 5,
                Reporting = 1 << 6,
                SettingInitial = 1 << 7,

                All = byte.MaxValue
            }

#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif

            private ITreeHandleable _next;
            volatile private object _valueOrPrevious;
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
                // TODO: use [FieldOffset(6)] for depth to utilize the byte space for non-deferred promises.
                [FieldOffset(6)]
                internal short _deferredId;

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
                volatile protected ITreeHandleable _waiter;
            }

            partial class ConfiguredPromise : PromiseSingleAwait
            {
                private SynchronizationContext _synchronizationContext;
                private bool _isSynchronous;
                volatile private bool _isPreviousComplete;
                volatile private bool _wasForgottenOrHookupFailed;
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
                    internal Fixed32 _depthAndProgress;
                    internal Fixed32 _currentProgress;
#endif
                }

#if PROTO_PROMISE_DEVELOPER_MODE // Must use a queue instead of a stack in developer mode so that the ExecutionSchedule.ScheduleSynchronous can invoke immediately and still be in proper order.
                private ValueLinkedQueue<ITreeHandleable> _nextBranches = new ValueLinkedQueue<ITreeHandleable>();
#else
                private ValueLinkedStack<ITreeHandleable> _nextBranches = new ValueLinkedStack<ITreeHandleable>();
#endif
                private ProgressAndLocker _progressAndLocker;

#if PROMISE_PROGRESS
                private ValueLinkedQueue<IProgressListener> _progressListeners = new ValueLinkedQueue<IProgressListener>(); // TODO: change to ValueLinkedStack to use less memory. Make sure progress is still invoked in order.

                IProgressListener ILinked<IProgressListener>.Next { get; set; }
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }
#endif
            }

            partial class AsyncPromiseBase : PromiseSingleAwaitWithProgress
            {
#if PROMISE_PROGRESS
                protected Fixed32 _currentProgress;
#endif
            }

            partial class PromiseWaitPromise : PromiseSingleAwaitWithProgress
            {
#if PROMISE_PROGRESS
                // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
                // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
                private struct PromiseWaitSmallFields
                {
                    internal int _previousDepthPlusOne;
                    internal Fixed32 _depthAndProgress;
                }
                private PromiseWaitSmallFields _progressFields;

                IProgressListener ILinked<IProgressListener>.Next { get; set; }
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }
#endif
            }

            #region Non-cancelable Promises
            partial class PromiseResolve<TArg, TResult, TResolver> : PromiseSingleAwait
                where TResolver : IDelegate<TArg, TResult>
            {
                private TResolver _resolver;
            }

            partial class PromiseResolvePromise<TArg, TResult, TResolver> : PromiseWaitPromise
                where TResolver : IDelegate<TArg, Promise<TResult>>
            {
                private TResolver _resolver;
            }

            partial class PromiseResolveReject<TArgResolve, TResult, TResolver, TArgReject, TRejecter> : PromiseSingleAwait
                where TResolver : IDelegate<TArgResolve, TResult>
                where TRejecter : IDelegate<TArgReject, TResult>
            {
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class PromiseResolveRejectPromise<TArgResolve, TResult, TResolver, TArgReject, TRejecter> : PromiseWaitPromise
                where TResolver : IDelegate<TArgResolve, Promise<TResult>>
                where TRejecter : IDelegate<TArgReject, Promise<TResult>>
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
                where TCanceler : IDelegateSimple
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

            partial class CancelablePromiseResolve<TArg, TResult, TResolver> : PromiseSingleAwait
                where TResolver : IDelegate<TArg, TResult>
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
            }

            partial class CancelablePromiseResolvePromise<TArg, TResult, TResolver> : PromiseWaitPromise
                where TResolver : IDelegate<TArg, Promise<TResult>>
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
            }

            partial class CancelablePromiseResolveReject<TArgResolve, TResult, TResolver, TArgReject, TRejecter> : PromiseSingleAwait
                where TResolver : IDelegate<TArgResolve, TResult>
                where TRejecter : IDelegate<TArgReject, TResult>
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class CancelablePromiseResolveRejectPromise<TArgResolve, TResult, TResolver, TArgReject, TRejecter> : PromiseWaitPromise
                where TResolver : IDelegate<TArgResolve, Promise<TResult>>
                where TRejecter : IDelegate<TArgReject, Promise<TResult>>
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
                where TCanceler : IDelegateSimple
            {
                private CancelationHelper _cancelationHelper;
                private TCanceler _canceler;
            }
            #endregion

            #region Multi Promises
            partial class MergePromise : PromiseSingleAwaitWithProgress
            {
                private int _waitCount;
#if PROMISE_DEBUG
                private readonly object _locker = new object();
                private ValueLinkedStack<PromisePassThrough> _passThroughs = new ValueLinkedStack<PromisePassThrough>();
#endif

#if PROMISE_PROGRESS
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }

                // These are used to avoid rounding errors when normalizing the progress.
                // Use 64 bits to allow combining many promises with very deep chains.
                private double _progressScaler;
                private UnsignedFixed64 _unscaledProgress;
                private int _maxWaitDepth;
#endif
            }

            partial class RacePromise : PromiseSingleAwaitWithProgress
            {
                // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
                // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
                private struct RaceSmallFields
                {
                    internal int _waitCount;
#if PROMISE_PROGRESS
                    internal Fixed32 _depthAndProgress;
                    internal Fixed32 _currentProgress;
#endif
                }

                private RaceSmallFields _raceSmallFields;
#if PROMISE_DEBUG
                private readonly object _locker = new object();
                private ValueLinkedStack<PromisePassThrough> _passThroughs = new ValueLinkedStack<PromisePassThrough>();
#endif
#if PROMISE_PROGRESS
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }
#endif
            }

            partial class FirstPromise : PromiseSingleAwaitWithProgress
            {
                // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
                // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
                private struct FirstSmallFields
                {
                    internal int _waitCount;
#if PROMISE_PROGRESS
                    internal Fixed32 _depthAndProgress;
                    internal Fixed32 _currentProgress;
#endif
                }

                private FirstSmallFields _firstSmallFields;
#if PROMISE_DEBUG
                private readonly object _locker = new object();
                private ValueLinkedStack<PromisePassThrough> _passThroughs = new ValueLinkedStack<PromisePassThrough>();
#endif
#if PROMISE_PROGRESS
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }
#endif
            }

            partial class PromisePassThrough : ITreeHandleable, ILinked<PromisePassThrough>, IProgressListener
            {
                // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
                // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
                private struct PassThroughSmallFields
                {
                    internal int _index;
                    internal int _retainCounter;
#if PROMISE_PROGRESS
                    internal Fixed32 _depth;
                    internal Fixed32 _currentProgress;
                    internal volatile bool _settingInitialProgress;
                    internal volatile bool _reportingProgress;
#endif
                }

                volatile private PromiseRef _owner;
                volatile private IMultiTreeHandleable _target;
                private PassThroughSmallFields _smallFields;

                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }
                PromisePassThrough ILinked<PromisePassThrough>.Next { get; set; }

#if PROMISE_PROGRESS
                IProgressListener ILinked<IProgressListener>.Next { get; set; }
#endif
            }
            #endregion

#if PROMISE_PROGRESS
            partial class PromiseProgress<TProgress> : PromiseSingleAwaitWithProgress, IProgressListener, IProgressInvokable
                where TProgress : IProgress<float>
            {
                // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
                // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
                private struct ProgressSmallFields
                {
                    internal Fixed32 _depthAndProgress;
                    internal Fixed32 _currentProgress;
                    volatile internal bool _complete;
                    volatile internal bool _canceled;
                    volatile internal bool _didFirstInvoke;
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
        } // PromiseRef
    } // Internal
}