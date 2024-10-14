#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
// ArrayPool for old runtime is in Proto.Promises.Collections namespace.
#if (NETCOREAPP || NETSTANDARD2_0_OR_GREATER || UNITY_2021_2_OR_NEWER)
using System.Buffers;
#else
using Proto.Promises.Collections;
#endif
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Proto.Promises.Collections
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal sealed class ConcurrentQueueSegment<T> : Internal.HandleablePromiseBase, Internal.ILinked<ConcurrentQueueSegment<T>>
    {
        public ConcurrentQueueSegment<T> Next
        {
            [MethodImpl(Internal.InlineOption)]
            get => _next.UnsafeAs<ConcurrentQueueSegment<T>>();
            [MethodImpl(Internal.InlineOption)]
            set => _next = value;
        }

        internal ConcurrentQueueSegment<T> _nextForDelayedDispose;

        ConcurrentQueueSegment<T> Internal.ILinked<ConcurrentQueueSegment<T>>.Next
        {
            [MethodImpl(Internal.InlineOption)]
            get => _nextForDelayedDispose;
            [MethodImpl(Internal.InlineOption)]
            set => _nextForDelayedDispose = value;
        }

        /// <summary>The array of items in this queue.  Each slot contains the item in that slot and its "sequence number".</summary>
        internal Slot[] _slots;
        /// <summary>Mask for quickly accessing a position within the queue's array.</summary>
        internal int _slotsMask;
        /// <summary>The head and tail positions, with padding to help avoid false sharing contention.</summary>
        /// <remarks>Dequeuing happens from the head, enqueuing happens at the tail.</remarks>
        internal PaddedHeadAndTail _headAndTail; // mutable struct: do not make this readonly

        /// <summary>Indicates whether the segment has been marked such that dequeues don't overwrite the removed data.</summary>
        internal bool _preservedForObservation;
        /// <summary>Indicates whether the segment has been marked such that no additional items may be enqueued.</summary>
        internal bool _frozenForEnqueues;

        private ConcurrentQueueSegment() { }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        private bool _isDisposed;

        ~ConcurrentQueueSegment()
        {
            if (!_isDisposed)
            {
                Internal.ReportRejection(new UnreleasedObjectException("A ConcurrentQueueSegment was garbage collected without being disposed"), null);
            }
        }
#endif

        [MethodImpl(Internal.InlineOption)]
        private static ConcurrentQueueSegment<T> GetOrCreate()
        {
            var obj = Internal.ObjectPool.TryTakeOrInvalid<ConcurrentQueueSegment<T>>();
            return obj == Internal.PromiseRefBase.InvalidAwaitSentinel.s_instance
                ? new ConcurrentQueueSegment<T>()
                : obj.UnsafeAs<ConcurrentQueueSegment<T>>();
        }


        /// <summary>Get or creates the segment.</summary>
        /// <param name="boundedLength">
        /// The maximum number of elements the segment can contain.  Must be a power of 2.
        /// </param>
        internal static ConcurrentQueueSegment<T> GetOrCreate(int boundedLength)
        {
            // Validate the length
            Debug.Assert(boundedLength >= 2, $"Must be >= 2, got {boundedLength}");
            Debug.Assert((boundedLength & (boundedLength - 1)) == 0, $"Must be a power of 2, got {boundedLength}");

            var segment = GetOrCreate();
            segment._next = null;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            segment._isDisposed = false;
#endif

            var slots = ArrayPool<Slot>.Shared.Rent(boundedLength);
            segment._slots = slots;
            // The mask is used as a way of quickly doing "% _slots.Length", instead letting us do "& _slotsMask".
            segment._slotsMask = boundedLength - 1;
            segment._headAndTail = default;
            segment._preservedForObservation = false;
            segment._frozenForEnqueues = false;

            // Initialize the sequence number for each slot.  The sequence number provides a ticket that
            // allows dequeuers to know whether they can dequeue and enqueuers to know whether they can
            // enqueue.  An enqueuer at position N can enqueue when the sequence number is N, and a dequeuer
            // for position N can dequeue when the sequence number is N + 1.  When an enqueuer is done writing
            // at position N, it sets the sequence number to N + 1 so that a dequeuer will be able to dequeue,
            // and when a dequeuer is done dequeueing at position N, it sets the sequence number to N + _slots.Length,
            // so that when an enqueuer loops around the slots, it'll find that the sequence number at
            // position N is N.  This also means that when an enqueuer finds that at position N the sequence
            // number is < N, there is still a value in that slot, i.e. the segment is full, and when a
            // dequeuer finds that the value in a slot is < N + 1, there is nothing currently available to
            // dequeue. (It is possible for multiple enqueuers to enqueue concurrently, writing into
            // subsequent slots, and to have the first enqueuer take longer, so that the slots for 1, 2, 3, etc.
            // may have values, but the 0th slot may still be being filled... in that case, TryDequeue will
            // return false.)
            for (int i = slots.Length - 1; i >= 0; --i)
            {
                slots[i].sequenceNumber = i;
            }

            return segment;
        }

        public void Dispose()
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            _isDisposed = true;
#endif
            ArrayPool<Slot>.Shared.Return(_slots, true);
            _slots = null;
            Internal.ObjectPool.MaybeRepool(this);
        }

        /// <summary>Gets the number of elements this segment can store.</summary>
        internal int Capacity => _slots.Length;

        /// <summary>Gets the "freeze offset" for this segment.</summary>
        internal int FreezeOffset => _slots.Length * 2;

        /// <summary>
        /// Ensures that the segment will not accept any subsequent enqueues that aren't already underway.
        /// </summary>
        /// <remarks>
        /// When we mark a segment as being frozen for additional enqueues,
        /// we set the <see cref="_frozenForEnqueues"/> bool, but that's mostly
        /// as a small helper to avoid marking it twice.  The real marking comes
        /// by modifying the Tail for the segment, increasing it by this
        /// <see cref="FreezeOffset"/>.  This effectively knocks it off the
        /// sequence expected by future enqueuers, such that any additional enqueuer
        /// will be unable to enqueue due to it not lining up with the expected
        /// sequence numbers.  This value is chosen specially so that Tail will grow
        /// to a value that maps to the same slot but that won't be confused with
        /// any other enqueue/dequeue sequence number.
        /// </remarks>
        internal void EnsureFrozenForEnqueues() // must only be called while queue's segment lock is held
        {
            // This is called inside the _crossSegmentLock, and Release is also
            // called inside the lock, so we don't need to retain/release here.

            if (!_frozenForEnqueues) // flag used to ensure we don't increase the Tail more than once if frozen more than once
            {
                _frozenForEnqueues = true;
                Interlocked.Add(ref _headAndTail.tail, FreezeOffset);
            }
        }

        /// <summary>Tries to dequeue an element from the queue.</summary>
        internal bool TryDequeue(out T item)
        {
            Slot[] slots = _slots;

            // Loop in case of contention...
            SpinWait spinner = default;
            while (true)
            {
                // Get the head at which to try to dequeue.
                int currentHead = Volatile.Read(ref _headAndTail.head);
                int slotsIndex = currentHead & _slotsMask;

                // Read the sequence number for the head position.
                int sequenceNumber = Volatile.Read(ref slots[slotsIndex].sequenceNumber);

                // We can dequeue from this slot if it's been filled by an enqueuer, which
                // would have left the sequence number at pos+1.
                int diff = sequenceNumber - (currentHead + 1);
                if (diff == 0)
                {
                    // We may be racing with other dequeuers.  Try to reserve the slot by incrementing
                    // the head.  Once we've done that, no one else will be able to read from this slot,
                    // and no enqueuer will be able to read from this slot until we've written the new
                    // sequence number. WARNING: The next few lines are not reliable on a runtime that
                    // supports thread aborts. If a thread abort were to sneak in after the CompareExchange
                    // but before the Volatile.Write, enqueuers trying to enqueue into this slot would
                    // spin indefinitely.  If this implementation is ever used on such a platform, this
                    // if block should be wrapped in a finally / prepared region.
                    if (Interlocked.CompareExchange(ref _headAndTail.head, currentHead + 1, currentHead) == currentHead)
                    {
                        // Successfully reserved the slot.  Note that after the above CompareExchange, other threads
                        // trying to dequeue from this slot will end up spinning until we do the subsequent Write.
                        item = slots[slotsIndex].item;
                        if (!Volatile.Read(ref _preservedForObservation))
                        {
                            // If we're preserving, though, we don't zero out the slot, as we need it for
                            // peeking.  And we don't update the sequence number,
                            // so that an enqueuer will see it as full and be forced to move to a new segment.

                            // TODO
                            //if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                            {
                                slots[slotsIndex].item = default;
                            }
                            Volatile.Write(ref slots[slotsIndex].sequenceNumber, currentHead + slots.Length);
                        }
                        return true;
                    }

                    // The head was already advanced by another thread. A newer head has already been observed and the next
                    // iteration would make forward progress, so there's no need to spin-wait before trying again.
                }
                else if (diff < 0)
                {
                    // The sequence number was less than what we needed, which means this slot doesn't
                    // yet contain a value we can dequeue, i.e. the segment is empty.  Technically it's
                    // possible that multiple enqueuers could have written concurrently, with those
                    // getting later slots actually finishing first, so there could be elements after
                    // this one that are available, but we need to dequeue in order.  So before declaring
                    // failure and that the segment is empty, we check the tail to see if we're actually
                    // empty or if we're just waiting for items in flight or after this one to become available.
                    bool frozen = _frozenForEnqueues;
                    int currentTail = Volatile.Read(ref _headAndTail.tail);
                    if (currentTail - currentHead <= 0 || (frozen && (currentTail - FreezeOffset - currentHead <= 0)))
                    {
                        item = default;
                        return false;
                    }

                    // It's possible it could have become frozen after we checked _frozenForEnqueues
                    // and before reading the tail.  That's ok: in that rare race condition, we just
                    // loop around again. This is not necessarily an always-forward-progressing
                    // situation since this thread is waiting for another to write to the slot and
                    // this thread may have to check the same slot multiple times. Spin-wait to avoid
                    // a potential busy-wait, and then try again.
#if NETCOREAPP3_0_OR_GREATER
                    spinner.SpinOnce(sleep1Threshold: -1);
#else
                    spinner.SpinOnce();
#endif
                }
                else
                {
                    // The item was already dequeued by another thread. The head has already been updated beyond what was
                    // observed above, and the sequence number observed above as a volatile load is more recent than the update
                    // to the head. So, the next iteration of the loop is guaranteed to see a new head. Since this is an
                    // always-forward-progressing situation, there's no need to spin-wait before trying again.
                }
            }
        }

        /// <summary>Tries to peek at an element from the queue, without removing it.</summary>
        internal bool TryPeek(out T result, bool resultUsed)
        {
            if (resultUsed)
            {
                // In order to ensure we don't get a torn read on the value, we mark the segment
                // as preserving for observation.  Additional items can still be enqueued to this
                // segment, but no space will be freed during dequeues, such that the segment will
                // no longer be reusable.
                _preservedForObservation = true;
                Interlocked.MemoryBarrier();
            }

            Slot[] slots = _slots;

            // Loop in case of contention...
            SpinWait spinner = default;
            while (true)
            {
                // Get the head at which to try to peek.
                int currentHead = Volatile.Read(ref _headAndTail.head);
                int slotsIndex = currentHead & _slotsMask;

                // Read the sequence number for the head position.
                int sequenceNumber = Volatile.Read(ref slots[slotsIndex].sequenceNumber);

                // We can peek from this slot if it's been filled by an enqueuer, which
                // would have left the sequence number at pos+1.
                int diff = sequenceNumber - (currentHead + 1);
                if (diff == 0)
                {
                    result = resultUsed ? slots[slotsIndex].item : default;
                    return true;
                }
                else if (diff < 0)
                {
                    // The sequence number was less than what we needed, which means this slot doesn't
                    // yet contain a value we can peek, i.e. the segment is empty.  Technically it's
                    // possible that multiple enqueuers could have written concurrently, with those
                    // getting later slots actually finishing first, so there could be elements after
                    // this one that are available, but we need to peek in order.  So before declaring
                    // failure and that the segment is empty, we check the tail to see if we're actually
                    // empty or if we're just waiting for items in flight or after this one to become available.
                    bool frozen = _frozenForEnqueues;
                    int currentTail = Volatile.Read(ref _headAndTail.tail);
                    if (currentTail - currentHead <= 0 || (frozen && (currentTail - FreezeOffset - currentHead <= 0)))
                    {
                        result = default;
                        return false;
                    }

                    // It's possible it could have become frozen after we checked _frozenForEnqueues
                    // and before reading the tail.  That's ok: in that rare race condition, we just
                    // loop around again. This is not necessarily an always-forward-progressing
                    // situation since this thread is waiting for another to write to the slot and
                    // this thread may have to check the same slot multiple times. Spin-wait to avoid
                    // a potential busy-wait, and then try again.
#if NETCOREAPP3_0_OR_GREATER
                    spinner.SpinOnce(sleep1Threshold: -1);
#else
                    spinner.SpinOnce();
#endif
                }
                else
                {
                    // The item was already dequeued by another thread. The head has already been updated beyond what was
                    // observed above, and the sequence number observed above as a volatile load is more recent than the update
                    // to the head. So, the next iteration of the loop is guaranteed to see a new head. Since this is an
                    // always-forward-progressing situation, there's no need to spin-wait before trying again.
                }
            }
        }

        /// <summary>
        /// Attempts to enqueue the item.  If successful, the item will be stored
        /// in the queue and true will be returned; otherwise, the item won't be stored, and false
        /// will be returned.
        /// </summary>
        internal bool TryEnqueue(in T item)
        {
            Slot[] slots = _slots;

            // Loop in case of contention...
            while (true)
            {
                // Get the tail at which to try to return.
                int currentTail = Volatile.Read(ref _headAndTail.tail);
                int slotsIndex = currentTail & _slotsMask;

                // Read the sequence number for the tail position.
                int sequenceNumber = Volatile.Read(ref slots[slotsIndex].sequenceNumber);

                // The slot is empty and ready for us to enqueue into it if its sequence
                // number matches the slot.
                int diff = sequenceNumber - currentTail;
                if (diff == 0)
                {
                    // We may be racing with other enqueuers.  Try to reserve the slot by incrementing
                    // the tail.  Once we've done that, no one else will be able to write to this slot,
                    // and no dequeuer will be able to read from this slot until we've written the new
                    // sequence number. WARNING: The next few lines are not reliable on a runtime that
                    // supports thread aborts. If a thread abort were to sneak in after the CompareExchange
                    // but before the Volatile.Write, other threads will spin trying to access this slot.
                    // If this implementation is ever used on such a platform, this if block should be
                    // wrapped in a finally / prepared region.
                    if (Interlocked.CompareExchange(ref _headAndTail.tail, currentTail + 1, currentTail) == currentTail)
                    {
                        // Successfully reserved the slot.  Note that after the above CompareExchange, other threads
                        // trying to return will end up spinning until we do the subsequent Write.
                        slots[slotsIndex].item = item;
                        Volatile.Write(ref slots[slotsIndex].sequenceNumber, currentTail + 1);
                        return true;
                    }

                    // The tail was already advanced by another thread. A newer tail has already been observed and the next
                    // iteration would make forward progress, so there's no need to spin-wait before trying again.
                }
                else if (diff < 0)
                {
                    // The sequence number was less than what we needed, which means this slot still
                    // contains a value, i.e. the segment is full.  Technically it's possible that multiple
                    // dequeuers could have read concurrently, with those getting later slots actually
                    // finishing first, so there could be spaces after this one that are available, but
                    // we need to enqueue in order.
                    return false;
                }
                else
                {
                    // Either the slot contains an item, or it is empty but because the slot was filled and dequeued. In either
                    // case, the tail has already been updated beyond what was observed above, and the sequence number observed
                    // above as a volatile load is more recent than the update to the tail. So, the next iteration of the loop
                    // is guaranteed to see a new tail. Since this is an always-forward-progressing situation, there's no need
                    // to spin-wait before trying again.
                }
            }
        }

        /// <summary>Represents a slot in the queue.</summary>
        internal struct Slot
        {
            internal T item;
            internal int sequenceNumber;
        }
    }

    /// <summary>Padded head and tail indices, to avoid false sharing between producers and consumers.</summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    [StructLayout(LayoutKind.Explicit, Size = 3 * CACHE_LINE_SIZE)] // padding before/between/after fields
    internal struct PaddedHeadAndTail
    {
        // Some architectures have a different cache line size, but it's impossible for us to adjust for it today. https://github.com/dotnet/runtime/issues/108416
        private const int CACHE_LINE_SIZE = 64;

        [FieldOffset(1 * CACHE_LINE_SIZE)] internal int head;
        [FieldOffset(2 * CACHE_LINE_SIZE)] internal int tail;
    }
}