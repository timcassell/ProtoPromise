#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0251 // Make member 'readonly'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises.Collections
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
    // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
    internal struct PoolBackedConcurrentQueueSmallFields
    {
        internal int _retainCounter;
        /// <summary>
        /// Spin lock used to protect cross-segment operations, including any updates to <see cref="PoolBackedConcurrentQueue{T}._tail"/> or <see cref="PoolBackedConcurrentQueue{T}._head"/>
        /// and any operations that need to get a consistent view of them.
        /// </summary>
        internal Internal.SpinLocker _crossSegmentLock;
    }

    /// <summary>
    /// An implementation of ConcurrentQueue that uses a struct instead of a class,
    /// doesn't include enumeration support, and pools the segments instead of dropping them.
    /// <para/>WARNING: This type is not thread-safe after it has been disposed. Users should make sure that this is
    /// never accessed after it has been disposed (aside from <see cref="Retain"/> and <see cref="Release"/>).
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal struct PoolBackedConcurrentQueue<T>
    {
        /// <summary>Initial length of the segments used in the queue.</summary>
        private const int InitialSegmentLength = 32;
        /// <summary>
        /// Maximum length of the segments used in the queue.  This is a somewhat arbitrary limit:
        /// larger means that as long as we don't exceed the size, we avoid allocating more segments,
        /// but if it's too large, segments may fail to be allocated.
        /// </summary>
        private const int MaxSegmentLength = 1024 * 1024;

        /// <summary>The current head segment.</summary>
        private volatile ConcurrentQueueSegment<T> _head;
        /// <summary>The current tail segment.</summary>
        private volatile ConcurrentQueueSegment<T> _tail;
        /// <summary>Segments that need to be disposed.</summary>
        // These must not be readonly.
        private Internal.ValueLinkedStack<ConcurrentQueueSegment<T>> _needToDispose;
        private PoolBackedConcurrentQueueSmallFields _smallFields;

        [MethodImpl(Internal.InlineOption)]
        // We use a reset method instead of a constructor to avoid mutating the retain counter that could be used on other threads.
        internal void Reset()
        {
            _tail = _head = ConcurrentQueueSegment<T>.GetOrCreate(InitialSegmentLength);
            _needToDispose = new Internal.ValueLinkedStack<ConcurrentQueueSegment<T>>();
            _smallFields._crossSegmentLock = new Internal.SpinLocker();
        }

        [MethodImpl(Internal.InlineOption)]
        internal void Retain()
            => Internal.InterlockedAddWithUnsignedOverflowCheck(ref _smallFields._retainCounter, 1);

        [MethodImpl(Internal.InlineOption)]
        internal void Release()
        {
            // To handle a race condition with a new segment sneaking into the _needToDispose stack on another thread,
            // we read the current head before we modify the retainer, then compare the values after the modification.
            var needToDisposeHead = _needToDispose.Peek();
            if (Internal.InterlockedAddWithUnsignedOverflowCheck(ref _smallFields._retainCounter, -1) != 0
                | needToDisposeHead == null)
            {
                return;
            }

            // This is a safe point to dispose completed segments.
            // We dispose completed segments at safe intervals (the retain counter is zero) instead of waiting
            // for Dispose in order to prevent a memory leak due to caching dropped segments for too long.
            // We do this instead of simply dropping the references and allowing GC to clean them up to keep this non-allocating.

            // It is possible another thread is racing to clean up the same segments, which we need to handle.
            _smallFields._crossSegmentLock.Enter();
            {
                // Handle the case of new completed segments added to the stack on another thread after the retainer was modified.
                var newHead = _needToDispose.Peek();
                if (newHead == needToDisposeHead)
                {
                    _needToDispose.Clear();
                }
                else
                {
                    // It is not safe to dispose newly added segments, we just break the link. The new segments will be cleaned up at the next safe point.
                    ConcurrentQueueSegment<T> temp;
                    var tempNext = newHead;
                    do
                    {
                        temp = tempNext;
                        if (temp == null)
                        {
                            // Another thread already cleaned up the segments.
                            _smallFields._crossSegmentLock.Exit();
                            return;
                        }
                        tempNext = temp._nextForDelayedDispose;
                    } while (tempNext != needToDisposeHead);
                    temp._nextForDelayedDispose = null;
                }
            }
            _smallFields._crossSegmentLock.Exit();

            var disposeStack = new Internal.ValueLinkedStack<ConcurrentQueueSegment<T>>(needToDisposeHead);
            do
            {
                disposeStack.Pop().Dispose();
            } while (disposeStack.IsNotEmpty);
        }

        /// <summary>
        /// Must be called before <see cref="Dispose"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal void PreDispose()
            // We add to the retain counter so it will not go back to 0 to prevent other threads from needlessly
            // cleaning up the completed segments, since we will be doing it in Dispose.
            => Retain();

        /// <summary>
        /// Dispose this <see cref="PoolBackedConcurrentQueue{T}"/>. All cached items will be dropped, and all segments returned to the pool.
        /// <para/>Make sure to call <see cref="PreDispose"/> before calling <see cref="Dispose"/>.
        /// <para/>WARNING: This method should only be called once! Use <see cref="Retain"/> combined with <see cref="Release"/> for proper use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal void Dispose()
        {
            // PreDispose should be called before Dispose.
            Debug.Assert(_smallFields._retainCounter != 0);

            // Spin until all other threads are complete.
            var spinner = new SpinWait();
            while (_smallFields._retainCounter != 1)
            {
                spinner.SpinOnce();
            }

            // This instance should not be mutated after Dispose is called.
            Debug.Assert(_smallFields._crossSegmentLock.TryEnter());
            // We release the retain counter back to zero for future re-use.
            // We can't assert the value, becaues other threads retain this before validation.
            Internal.InterlockedAddWithUnsignedOverflowCheck(ref _smallFields._retainCounter, -1);

            var disposeStack = _needToDispose.TakeAndClear();
            while (disposeStack.IsNotEmpty)
            {
                disposeStack.Pop().Dispose();
            }

            var head = _head;
            _tail = _head = null;
            do
            {
                var temp = head;
                head = head.Next;
                temp.Dispose();
            } while (head != null);
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="PoolBackedConcurrentQueue{T}"/> is empty.
        /// </summary>
        /// <value>true if the <see cref="PoolBackedConcurrentQueue{T}"/> is empty; otherwise, false.</value>
        /// <remarks>
        /// For determining whether the collection contains any items, use of this property is recommended
        /// rather than retrieving the number of items from the <see cref="Count"/> property and comparing it
        /// to 0.  However, as this collection is intended to be accessed concurrently, it may be the case
        /// that another thread will modify the collection after <see cref="IsEmpty"/> returns, thus invalidating
        /// the result.
        /// </remarks>
        internal bool IsEmpty =>
            // IsEmpty == !TryPeek. We use a "resultUsed:false" peek in order to avoid marking
            // segments as preserved for observation, making IsEmpty a cheaper way than either
            // TryPeek(out T) or Count == 0 to check whether any elements are in the queue.
            !TryPeek(out _, resultUsed: false);

        /// <summary>
        /// Gets the number of elements contained in the <see cref="PoolBackedConcurrentQueue{T}"/>.
        /// </summary>
        /// <value>The number of elements contained in the <see cref="PoolBackedConcurrentQueue{T}"/>.</value>
        /// <remarks>
        /// For determining whether the collection contains any items, use of the <see cref="IsEmpty"/>
        /// property is recommended rather than retrieving the number of items from the <see cref="Count"/>
        /// property and comparing it to 0.
        /// </remarks>
        internal int Count
        {
            get
            {
                var spinner = new SpinWait();
                while (true)
                {
                    // Capture the head and tail, as well as the head's head and tail.
                    var head = _head;
                    var tail = _tail;
                    int headHead = Volatile.Read(ref head._headAndTail.head);
                    int headTail = Volatile.Read(ref head._headAndTail.tail);

                    if (head == tail)
                    {
                        // There was a single segment in the queue.  If the captured segments still
                        // match, then we can trust the values to compute the segment's count. (It's
                        // theoretically possible the values could have looped around and still exactly match,
                        // but that would required at least ~4 billion elements to have been enqueued and
                        // dequeued between the reads.)
                        if (head == _head &&
                            tail == _tail &&
                            headHead == Volatile.Read(ref head._headAndTail.head) &&
                            headTail == Volatile.Read(ref head._headAndTail.tail))
                        {
                            return GetCount(head, headHead, headTail);
                        }
                    }
                    else if (head.Next == tail)
                    {
                        // There were two segments in the queue.  Get the positions from the tail, and as above,
                        // if the captured values match the previous reads, return the sum of the counts from both segments.
                        int tailHead = Volatile.Read(ref tail._headAndTail.head);
                        int tailTail = Volatile.Read(ref tail._headAndTail.tail);
                        if (head == _head &&
                            tail == _tail &&
                            headHead == Volatile.Read(ref head._headAndTail.head) &&
                            headTail == Volatile.Read(ref head._headAndTail.tail) &&
                            tailHead == Volatile.Read(ref tail._headAndTail.head) &&
                            tailTail == Volatile.Read(ref tail._headAndTail.tail))
                        {
                            return GetCount(head, headHead, headTail) + GetCount(tail, tailHead, tailTail);
                        }
                    }
                    else
                    {
                        // There were more than two segments in the queue.  Fall back to taking the cross-segment lock,
                        // which will ensure that the head and tail segments we read are stable (since the lock is needed to change them);
                        // for the two-segment case above, we can simply rely on subsequent comparisons, but for the two+ case, we need
                        // to be able to trust the internal segments between the head and tail.
                        _smallFields._crossSegmentLock.Enter();
                        {
                            // Now that we hold the lock, re-read the previously captured head and tail segments and head positions.
                            // If either has changed, start over.
                            if (head == _head && tail == _tail)
                            {
                                // Get the positions from the tail, and as above, if the captured values match the previous reads,
                                // we can use the values to compute the count of the head and tail segments.
                                int tailHead = Volatile.Read(ref tail._headAndTail.head);
                                int tailTail = Volatile.Read(ref tail._headAndTail.tail);
                                if (headHead == Volatile.Read(ref head._headAndTail.head) &&
                                    headTail == Volatile.Read(ref head._headAndTail.tail) &&
                                    tailHead == Volatile.Read(ref tail._headAndTail.head) &&
                                    tailTail == Volatile.Read(ref tail._headAndTail.tail))
                                {
                                    // We got stable values for the head and tail segments, so we can just compute the sizes
                                    // based on those and add them. Note that this and the below additions to count may overflow: previous
                                    // implementations allowed that, so we don't check, either, and it is theoretically possible for the
                                    // queue to store more than int.MaxValue items.
                                    int count = GetCount(head, headHead, headTail) + GetCount(tail, tailHead, tailTail);

                                    // Now add the counts for each internal segment. Since there were segments before these,
                                    // for counting purposes we consider them to start at the 0th element, and since there is at
                                    // least one segment after each, each was frozen, so we can count until each's frozen tail.
                                    // With the cross-segment lock held, we're guaranteed that all of these internal segments are
                                    // consistent, as the head and tail segment can't be changed while we're holding the lock, and
                                    // dequeueing and enqueueing can only be done from the head and tail segments, which these aren't.
                                    for (var s = head.Next; s != tail; s = s.Next)
                                    {
                                        Debug.Assert(s._frozenForEnqueues, "Internal segment must be frozen as there's a following segment.");
                                        count += s._headAndTail.tail - s.FreezeOffset;
                                    }

                                    return count;
                                }
                            }
                        }
                        _smallFields._crossSegmentLock.Exit();
                    }

                    // We raced with enqueues/dequeues and captured an inconsistent picture of the queue.
                    // Spin and try again.
                    spinner.SpinOnce();
                }
            }
        }

        /// <summary>Computes the number of items in a segment based on a fixed head and tail in that segment.</summary>
        private static int GetCount(ConcurrentQueueSegment<T> s, int head, int tail)
        {
            if (head != tail && head != tail - s.FreezeOffset)
            {
                head &= s._slotsMask;
                tail &= s._slotsMask;
                return head < tail ? tail - head : s._slots.Length - head + tail;
            }
            return 0;
        }

        /// <summary>Adds an object to the end of the <see cref="PoolBackedConcurrentQueue{T}"/>.</summary>
        /// <param name="item">
        /// The object to add to the end of the <see cref="PoolBackedConcurrentQueue{T}"/>.
        /// The value can be a null reference (<see langword="Nothing" /> in Visual Basic) for reference types.
        /// </param>
        internal void Enqueue(in T item)
        {
            // Try to enqueue to the current tail.
            if (!_tail.TryEnqueue(item))
            {
                // If we're unable to, we need to take a slow path that will
                // try to add a new tail segment.
                EnqueueSlow(item);
            }
        }

        /// <summary>Adds to the end of the queue, adding a new segment if necessary.</summary>
        private void EnqueueSlow(in T item)
        {
            while (true)
            {
                var tail = _tail;

                // Try to append to the existing tail.
                if (tail.TryEnqueue(item))
                {
                    return;
                }

                // If we were unsuccessful, take the lock so that we can compare and manipulate
                // the tail.  Assuming another enqueuer hasn't already added a new segment,
                // do so, then loop around to try enqueueing again.
                _smallFields._crossSegmentLock.Enter();
                {
                    if (tail == _tail)
                    {
                        // Make sure no one else can enqueue to this segment.
                        tail.EnsureFrozenForEnqueues();

                        // We determine the new segment's length based on the old length.
                        // In general, we double the size of the segment, to make it less likely
                        // that we'll need to grow again.  However, if the tail segment is marked
                        // as preserved for observation, something caused us to avoid reusing this
                        // segment, and if that happens a lot and we grow, we'll end up allocating
                        // lots of wasted space.  As such, in such situations we reset back to the
                        // initial segment length; if these observations are happening frequently,
                        // this will help to avoid wasted memory, and if they're not, we'll
                        // relatively quickly grow again to a larger size.
                        int nextSize = tail._preservedForObservation ? InitialSegmentLength : Math.Min(tail.Capacity * 2, MaxSegmentLength);
                        var newTail = ConcurrentQueueSegment<T>.GetOrCreate(nextSize);

                        // Hook up the new tail.
                        tail.Next = newTail;
                        _tail = newTail;
                    }
                }
                _smallFields._crossSegmentLock.Exit();
            }
        }

        /// <summary>
        /// Attempts to remove and return the object at the beginning of the <see
        /// cref="PoolBackedConcurrentQueue{T}"/>.
        /// </summary>
        /// <param name="result">
        /// When this method returns, if the operation was successful, <paramref name="result"/> contains the
        /// object removed. If no object was available to be removed, the value is unspecified.
        /// </param>
        /// <returns>
        /// true if an element was removed and returned from the beginning of the
        /// <see cref="PoolBackedConcurrentQueue{T}"/> successfully; otherwise, false.
        /// </returns>
        internal bool TryDequeue(out T result)
        {
            // Get the current head
            var head = _head;

            // Try to take.  If we're successful, we're done.
            if (head.TryDequeue(out result))
            {
                return true;
            }

            // Check to see whether this segment is the last. If it is, we can consider
            // this to be a moment-in-time empty condition (even though between the TryDequeue
            // check and this check, another item could have arrived).
            if (head.Next == null)
            {
                result = default;
                return false;
            }

            return TryDequeueSlow(out result); // slow path that needs to fix up segments
        }

        /// <summary>Tries to dequeue an item, removing empty segments as needed.</summary>
        private bool TryDequeueSlow(out T item)
        {
            while (true)
            {
                // Get the current head
                var head = _head;

                // Try to take.  If we're successful, we're done.
                if (head.TryDequeue(out item))
                {
                    return true;
                }

                // Check to see whether this segment is the last. If it is, we can consider
                // this to be a moment-in-time empty condition (even though between the TryDequeue
                // check and this check, another item could have arrived).
                if (head.Next == null)
                {
                    item = default;
                    return false;
                }

                // At this point we know that head.Next != null, which means
                // this segment has been frozen for additional enqueues. But between
                // the time that we ran TryDequeue and checked for a next segment,
                // another item could have been added.  Try to dequeue one more time
                // to confirm that the segment is indeed empty.
                Debug.Assert(head._frozenForEnqueues);
                if (head.TryDequeue(out item))
                {
                    return true;
                }

                // This segment is frozen (nothing more can be added) and empty (nothing is in it).
                // Update head to point to the next segment in the list, assuming no one's beat us to it.
                _smallFields._crossSegmentLock.Enter();
                {
                    if (head == _head)
                    {
                        _head = head.Next;
                        if (Promise.Config.ObjectPoolingEnabled)
                        {
                            // The segment could still be used on another thread, so to prevent race conditions,
                            // we cache it and dispose it at a later safe point.
                            _needToDispose.Push(head);
                        }
                        else
                        {
                            // If object pooling is disabled, we can simply drop the segment.
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                            Internal.Discard(head);
#endif
                        }
                    }
                }
                _smallFields._crossSegmentLock.Exit();
            }
        }

        /// <summary>
        /// Attempts to return an object from the beginning of the <see cref="PoolBackedConcurrentQueue{T}"/>
        /// without removing it.
        /// </summary>
        /// <param name="result">
        /// When this method returns, <paramref name="result"/> contains an object from
        /// the beginning of the <see cref="PoolBackedConcurrentQueue{T}"/> or default(T)
        /// if the operation failed.
        /// </param>
        /// <returns>true if the object was returned successfully; otherwise, false.</returns>
        /// <remarks>
        /// For determining whether the collection contains any items, use of the <see cref="IsEmpty"/>
        /// property is recommended rather than peeking.
        /// </remarks>
        internal bool TryPeek(out T result) => TryPeek(out result, resultUsed: true);

        /// <summary>Attempts to retrieve the value for the first element in the queue.</summary>
        /// <param name="result">The value of the first element, if found.</param>
        /// <param name="resultUsed">true if the result is needed; otherwise false if only the true/false outcome is needed.</param>
        /// <returns>true if an element was found; otherwise, false.</returns>
        private bool TryPeek(out T result, bool resultUsed)
        {
            // Starting with the head segment, look through all of the segments
            // for the first one we can find that's not empty.
            var head = _head;

            while (true)
            {
                // Grab the next segment from this one, before we peek.
                // This is to be able to see whether the value has changed
                // during the peek operation.
                var next = Volatile.Read(ref head._next).UnsafeAs<ConcurrentQueueSegment<T>>();

                // Peek at the segment.  If we find an element, we're done.
                if (head.TryPeek(out result, resultUsed))
                {
                    return true;
                }

                // The current segment was empty at the moment we checked.

                if (next != null)
                {
                    // If prior to the peek there was already a next segment, then
                    // during the peek no additional items could have been enqueued
                    // to it and we can just move on to check the next segment.
                    Debug.Assert(next == head.Next);
                    head = next;
                }
                else if (Volatile.Read(ref head._next).UnsafeAs<ConcurrentQueueSegment<T>>() == null)
                {
                    // The next segment is null.  Nothing more to peek at.
                    break;
                }

                // The next segment was null before we peeked but non-null after.
                // That means either when we peeked the first segment had
                // already been frozen but the new segment not yet added,
                // or that the first segment was empty and between the time
                // that we peeked and then checked _nextSegment, so many items
                // were enqueued that we filled the first segment and went
                // into the next.  Since we need to peek in order, we simply
                // loop around again to peek on the same segment.  The next
                // time around on this segment we'll then either successfully
                // peek or we'll find that next was non-null before peeking,
                // and we'll traverse to that segment.
            }

            result = default;
            return false;
        }
    }
}
