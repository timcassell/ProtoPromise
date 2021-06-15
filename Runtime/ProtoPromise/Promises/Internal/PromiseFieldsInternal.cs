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

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef : ITreeHandleable, ITraceable
        {
            private ITreeHandleable _next;
            volatile private object _valueOrPrevious;
            private IdRetain _idsAndRetains = new IdRetain(1); // Start with Id 1 instead of 0 to reduce risk of false positives.
            private SmallFields _smallFields;

            [StructLayout(LayoutKind.Explicit)]
            private struct IdRetain
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

                internal IdRetain(short initialId)
                {
                    _longValue = 0;
                    _retains = 0;
                    _promiseId = _deferredId = initialId;
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTryIncrementPromiseId(short promiseId)
                {
                    IdRetain initialValue = default, newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        // Make sure id matches.
                        if (initialValue._promiseId != promiseId)
                        {
                            return false;
                        }
                        newValue = initialValue;
                        unchecked // We want the id to wrap around.
                        {
                            ++newValue._promiseId;
                        }
                    }
                    while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTryIncrementDeferredId(short deferredId)
                {
                    IdRetain initialValue = default, newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        // Make sure id matches.
                        if (initialValue._deferredId != deferredId)
                        {
                            return false;
                        }
                        newValue = initialValue;
                        unchecked // We want the id to wrap around.
                        {
                            ++newValue._deferredId;
                        }
                    }
                    while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal void InterlockedIncrementDeferredId()
                {
                    IdRetain initialValue = default, newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        newValue = initialValue;
                        unchecked // We want the id to wrap around.
                        {
                            ++newValue._deferredId;
                        }
                    }
                    while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTryRetain(short promiseId)
                {
                    IdRetain initialValue = default, newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        // Make sure id matches and we're not overflowing.
                        if (initialValue._promiseId != promiseId | initialValue._retains == uint.MaxValue) // Use a single branch for fast-path.
                        {
                            // If either check fails, see which failed.
                            if (initialValue._promiseId != promiseId)
                            {
                                return false;
                            }
                            throw new OverflowException("A promise was retained more than " + uint.MaxValue + " times.");
                        }
                        newValue = initialValue;
                        unchecked
                        {
                            ++newValue._retains;
                        }
                    }
                    while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTryRetainWithDeferredId(short deferredId)
                {
                    IdRetain initialValue = default, newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        // Make sure id matches and we're not overflowing.
                        if (initialValue._deferredId != deferredId | initialValue._retains == uint.MaxValue) // Use a single branch for fast-path.
                        {
                            // If either check fails, see which failed.
                            if (initialValue._deferredId != deferredId)
                            {
                                return false;
                            }
                            throw new OverflowException("A promise was retained more than " + uint.MaxValue + " times.");
                        }
                        newValue = initialValue;
                        unchecked
                        {
                            ++newValue._retains;
                        }
                    }
                    while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTryReleaseComplete()
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    IdRetain initialValue = default, newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        newValue = initialValue;
                        checked
                        {
                            --newValue._retains;
                        }
                    }
                    while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return newValue._retains == 0;
#else
                    unchecked
                    {
                        return InterlockedRetainDisregardId((uint) -1) == 0;
                    }
#endif
                }

                internal uint InterlockedRetainDisregardId(uint retains = 1)
                {
                    IdRetain initialValue = default, newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        newValue = initialValue;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        checked
#else
                        unchecked
#endif
                        {
                            newValue._retains += retains;
                        }
                    }
                    while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return newValue._retains;
                }
            }

            private partial struct SmallFields
            {
                // Wrapping 32-bit struct fields in another struct fixes issue with extra padding
                // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
                internal StateAndFlags _stateAndFlags;
#if PROMISE_PROGRESS
                internal UnsignedFixed32 _waitDepthAndProgress;
#endif

                [StructLayout(LayoutKind.Explicit)]
                internal struct StateAndFlags
                {
                    [FieldOffset(0)]
                    volatile internal Promise.State _state;
                    [FieldOffset(1)]
                    internal bool _suppressRejection;
                    [FieldOffset(2)]
                    internal bool _wasAwaitedOrForgotten;
#if PROMISE_PROGRESS
                    [FieldOffset(3)]
                    volatile internal ProgressFlags _progressFlags;
                    // int value allows us to use interlocked to set the progress flags.
                    [FieldOffset(0)]
                    volatile internal int _intValue;
#endif
                }
            }
        }
    }
}