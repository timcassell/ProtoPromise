#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0251 // Make member 'readonly'

using System;
using System.Diagnostics;
using System.Threading;

namespace Proto.Promises.Threading
{
    /// <summary>
    /// Stores options that configure the operation of methods on the <see cref="ParallelAsync"/> class.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public struct ParallelAsyncOptions
    {
        private int _maxDegreeOfParallelism;

        /// <summary>
        /// Gets or sets the <see cref="System.Threading.SynchronizationContext"/> associated with this <see cref="ParallelAsyncOptions"/> instance.
        /// If <see langword="null"/>, <see cref="Promise.Config.BackgroundContext"/> will be used.
        /// </summary>
        public SynchronizationContext SynchronizationContext { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Promises.CancelationToken"/> associated with this <see cref="ParallelAsyncOptions"/> instance.
        /// </summary>
        /// <remarks>
        /// Providing a <see cref="Promises.CancelationToken"/> to a <see cref="ParallelAsync"/> method enables the operation to be exited early.
        /// Code external to the operation may cancel the token, and if the operation observes the token being set, it may exit early by throwing an <see cref="OperationCanceledException"/>.
        /// </remarks>
        public CancelationToken CancelationToken { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of concurrent operations enabled by this <see cref="ParallelAsyncOptions"/> instance.
        /// </summary>
        /// <remarks>
        /// The <see cref="MaxDegreeOfParallelism"/> limits the number of concurrent operations run by <see cref="ParallelAsync"/>
        /// method calls that are passed this <see cref="ParallelAsyncOptions"/> instance to the set value, if it is positive.
        /// If <see cref="MaxDegreeOfParallelism"/> is 0, the default limit of <see cref="Environment.ProcessorCount"/> will be used.
        /// If it is -1, then there is no limit placed on the number of concurrently running operations.
        /// </remarks>
        /// <exception cref="System.ArgumentOutOfRangeException"><see cref="MaxDegreeOfParallelism"/> is set to some value less than -1.</exception>
        public int MaxDegreeOfParallelism
        {
            get => _maxDegreeOfParallelism;
            set
            {
                if (value < -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(MaxDegreeOfParallelism)} must be greater than or equal to -1.");
                }
                _maxDegreeOfParallelism = value;
            }
        }

        internal SynchronizationContext EffectiveSynchronizationContext
            => SynchronizationContext ?? Promise.Config.BackgroundContext ?? Internal.BackgroundSynchronizationContextSentinel.s_instance;

        internal int EffectiveMaxDegreeOfParallelism
        {
            get
            {
                int maxDegreeOfParallelism = MaxDegreeOfParallelism;
                return maxDegreeOfParallelism == 0 ? Environment.ProcessorCount
                    : maxDegreeOfParallelism == -1 ? int.MaxValue
                    : maxDegreeOfParallelism;
            }
        }
    }
}