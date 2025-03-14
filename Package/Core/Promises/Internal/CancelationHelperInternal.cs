#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0251 // Make member 'readonly'

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal partial struct CancelationHelper
            {
                internal bool IsCompleted
                {
                    [MethodImpl(InlineOption)]
                    get => _isCompletedFlag != 0;
                }

                [MethodImpl(InlineOption)]
                internal void Reset(int retainCounter = 2)
                {
                    _isCompletedFlag = 0;
                    _retainCounter = retainCounter;
                }

                [MethodImpl(InlineOption)]
                internal void Register(CancelationToken cancelationToken, ICancelable owner)
                    => _cancelationRegistration = cancelationToken.Register(owner);

                [MethodImpl(InlineOption)]
                internal void RegisterWithoutImmediateInvoke(CancelationToken cancelationToken, ICancelable owner, out bool alreadyCanceled)
                    => _cancelationRegistration = cancelationToken.RegisterWithoutImmediateInvoke(owner, out alreadyCanceled);

                [MethodImpl(InlineOption)]
                internal bool TrySetCompleted()
                    => Interlocked.Exchange(ref _isCompletedFlag, 1) == 0;

                [MethodImpl(InlineOption)]
                internal void UnregisterAndWait()
                    => _cancelationRegistration.Dispose();

                [MethodImpl(InlineOption)]
                internal void Retain()
                    => InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);

                [MethodImpl(InlineOption)]
                internal bool TryRelease(int releaseCount = -1)
                    => InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, releaseCount) == 0;

                [MethodImpl(InlineOption)]
                internal void RetainUnchecked(int retainCount)
                    => Interlocked.Add(ref _retainCounter, retainCount);

                [MethodImpl(InlineOption)]
                internal bool TryReleaseUnchecked()
                    => Interlocked.Add(ref _retainCounter, -1) == 0;

                // As an optimization, we can skip one Interlocked operation if the async op completed before the cancelation callback.
                [MethodImpl(InlineOption)]
                internal void ReleaseOne()
                    => _retainCounter = 1;
            }
        } // PromiseRefBase
    } // Internal
} // namespace Proto.Promises