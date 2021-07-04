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

using Proto.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Proto.Promises
{
    [StructLayout(LayoutKind.Auto)]
    partial struct Promise
    {
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly Internal.PromiseRef _ref;
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly short _id;

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRef promiseRef, short id)
        {
            _ref = promiseRef;
            _id = id;
        }
    }

    [StructLayout(LayoutKind.Auto)]
    partial struct Promise<T>
    {
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly Internal.PromiseRef _ref;
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly short _id;
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly T _result;

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRef promiseRef, short id)
        {
            _ref = promiseRef;
            _id = id;
            _result = default(T);
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRef promiseRef, short id, ref T value)
        {
            _ref = promiseRef;
            _id = id;
            _result = value;
        }
    }

    partial class Internal
    {
        partial class PromiseRef
        {
#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif

            private ITreeHandleable _next;
            volatile private object _valueOrPrevious;
            private IdRetain _idsAndRetains = new IdRetain(1); // Start with Id 1 instead of 0 to reduce risk of false positives.
            private SmallFields _smallFields;

            [StructLayout(LayoutKind.Explicit)]
            private partial struct IdRetain
            {
                [FieldOffset(0)]
                internal short _promiseId;
                [FieldOffset(2)]
                internal short _deferredId;
                [FieldOffset(4)]
                private uint _retains;
                // We can check Id and retain/release atomically.
                [FieldOffset(0)]
                private long _longValue;

                [MethodImpl(Internal.InlineOption)]
                internal IdRetain(short initialId)
                {
                    _longValue = 0;
                    _retains = 0;
                    _promiseId = _deferredId = initialId;
                }
            } // IdRetain

            private partial struct SmallFields
            {
                // Wrapping 32-bit struct fields in another struct fixes issue with extra padding
                // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
                internal StateAndFlags _stateAndFlags;
#if PROMISE_PROGRESS
                internal UnsignedFixed32 _waitDepthAndProgress;
#endif

                [StructLayout(LayoutKind.Explicit)]
                internal partial struct StateAndFlags
                {
                    [FieldOffset(0)]
                    volatile internal Promise.State _state;
                    [FieldOffset(1)]
                    internal bool _suppressRejection;
                    [FieldOffset(2)]
                    internal bool _wasAwaitedOrForgotten;
#if PROMISE_PROGRESS
                    [FieldOffset(3)]
                    volatile private ProgressFlags _progressFlags;
                    // int value with [FieldOffset(0)] allows us to use Interlocked to set the progress flags without consuming more memory than necessary.
                    [FieldOffset(0)]
                    volatile private int _intValue;
#endif
                } // StateAndFlags
            } // SmallFields

            partial class PromiseSingleAwait : PromiseRef
            {
#if PROMISE_PROGRESS
                volatile protected IProgressListener _progressListener;
#endif
            }

            partial class PromiseBranch : PromiseSingleAwait
            {
                private ITreeHandleable _waiter;
            }

            partial class PromiseMultiAwait
            {
                private readonly object _branchLocker = new object();
                private ValueLinkedStack<ITreeHandleable> _nextBranches;

#if PROMISE_PROGRESS
                private readonly object _progressCollectionLocker = new object();
                private ValueLinkedStack<IProgressListener> _progressListeners;

                IProgressListener ILinked<IProgressListener>.Next { get; set; }
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }
#endif
            }

            partial class PromiseWaitPromise : PromiseBranch
            {
#if PROMISE_PROGRESS
                IProgressListener ILinked<IProgressListener>.Next { get; set; }
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }
#endif
            }

            partial class PromisePassThrough : ITreeHandleable, ILinked<PromisePassThrough>, IProgressListener
            {
                volatile private PromiseRef _owner;
                private IMultiTreeHandleable _target;
                private int _index;
                private int _retainCounter;

                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }
                PromisePassThrough ILinked<PromisePassThrough>.Next { get; set; }

#if PROMISE_PROGRESS
                IProgressListener ILinked<IProgressListener>.Next { get; set; }

                private SmallFields _smallFields;
#endif
            }

            #region Non-cancelable Promises
            partial class PromiseResolve<TResolver> : PromiseBranch
                where TResolver : IDelegateResolve
            {
                private TResolver _resolver;
            }

            partial class PromiseResolvePromise<TResolver> : PromiseWaitPromise
                where TResolver : IDelegateResolvePromise
            {
                private TResolver _resolver;
            }

            partial class PromiseResolveReject<TResolver, TRejecter> : PromiseBranch
                where TResolver : IDelegateResolve
                where TRejecter : IDelegateReject
            {
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class PromiseResolveRejectPromise<TResolver, TRejecter> : PromiseWaitPromise
                where TResolver : IDelegateResolvePromise
                where TRejecter : IDelegateRejectPromise
            {
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class PromiseContinue<TContinuer> : PromiseBranch
                where TContinuer : IDelegateContinue
            {
                private TContinuer _continuer;
            }

            partial class PromiseContinuePromise<TContinuer> : PromiseWaitPromise
                where TContinuer : IDelegateContinuePromise
            {
                private TContinuer _continuer;
            }

            partial class PromiseFinally<TFinalizer> : PromiseBranch
                where TFinalizer : IDelegateSimple
            {
                private TFinalizer _finalizer;
            }

            partial class PromiseCancel<TCanceler> : PromiseBranch
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

            partial class DeferredPromiseVoidCancel : DeferredPromiseVoid
            {
                private CancelationRegistration _cancelationRegistration;
            }

            partial class DeferredPromiseCancel<T> : DeferredPromise<T>
            {
                private CancelationRegistration _cancelationRegistration;
            }

            partial class CancelablePromiseResolve<TResolver> : PromiseBranch
                where TResolver : IDelegateResolve
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
            }

            partial class CancelablePromiseResolvePromise<TResolver> : PromiseWaitPromise
                where TResolver : IDelegateResolvePromise
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
            }

            partial class CancelablePromiseResolveReject<TResolver, TRejecter> : PromiseBranch
                where TResolver : IDelegateResolve
                where TRejecter : IDelegateReject
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class CancelablePromiseResolveRejectPromise<TResolver, TRejecter> : PromiseWaitPromise
                where TResolver : IDelegateResolvePromise
                where TRejecter : IDelegateRejectPromise
            {
                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
                private TRejecter _rejecter;
            }

            partial class CancelablePromiseContinue<TContinuer> : PromiseBranch
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

            partial class CancelablePromiseCancel<TCanceler> : PromiseBranch
                where TCanceler : IDelegateSimple
            {
                private CancelationHelper _cancelationHelper;
                private TCanceler _canceler;
            }
            #endregion

            #region Multi Promises
            partial class MergePromise : PromiseBranch
            {
                private readonly object _locker = new object();
                private ValueLinkedStack<PromisePassThrough> _passThroughs;
                private int _waitCount;

#if PROMISE_PROGRESS
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }

                // These are used to avoid rounding errors when normalizing the progress.
                // Use 64 bits to allow combining many promises with very deep chains.
                private double _progressScaler;
                private UnsignedFixed64 _unscaledProgress;
#endif
            }

            partial class RacePromise : PromiseBranch
            {
                private readonly object _locker = new object();
                private ValueLinkedStack<PromisePassThrough> _passThroughs;
                private int _waitCount;

#if PROMISE_PROGRESS
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }

                private UnsignedFixed32 _currentAmount;
#endif
            }

            partial class FirstPromise : PromiseBranch
            {
                private readonly object _locker = new object();
                private ValueLinkedStack<PromisePassThrough> _passThroughs;
                private int _waitCount;

#if PROMISE_PROGRESS
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }

                private UnsignedFixed32 _currentAmount;
#endif
            }

#if PROMISE_PROGRESS
            internal partial class PromiseProgressBase : PromiseBranch
            {
                [StructLayout(LayoutKind.Explicit)]
                private struct SmallProgressFields
                {
                    [FieldOffset(0)]
                    volatile internal bool _handling;
                    [FieldOffset(1)]
                    volatile internal bool _suspended;
                    [FieldOffset(2)]
                    volatile internal bool _complete;
                    [FieldOffset(3)]
                    volatile internal bool _canceled;
                    // int value with [FieldOffset(0)] allows us to use Interlocked to set the flags without consuming more memory than necessary.
                    [FieldOffset(0)]
                    volatile internal int _intValue;
                }

                private SmallProgressFields _smallProgressFields;
                protected CancelationRegistration _cancelationRegistration;
            }

            partial class PromiseProgress<TProgress> : PromiseProgressBase, IProgressListener, IProgressInvokable
                where TProgress : IProgress<float>
            {
                IProgressListener ILinked<IProgressListener>.Next { get; set; }
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }

                private TProgress _progress;
            }
#endif
            #endregion
        } // PromiseRef
    } // Internal
}