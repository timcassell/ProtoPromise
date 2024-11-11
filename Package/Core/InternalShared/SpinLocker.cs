#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        /// <summary>
        /// Use instead of Monitor.Enter(object).
        /// Must not be readonly.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal struct SpinLocker
        {
            volatile private int _locker;

            [MethodImpl(InlineOption)]
            internal bool TryEnter()
                => Interlocked.Exchange(ref _locker, 1) == 0;

            [MethodImpl(InlineOption)]
            internal void Enter()
            {
                if (!TryEnter())
                {
                    EnterCore();
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void EnterCore()
            {
                // Spin until we successfully get lock.
                var spinner = new SpinWait();
                do
                {
                    spinner.SpinOnce();
                }
                while (!TryEnter());
            }

            /// <summary>
            /// Used to enter the lock without putting the thread to sleep for very long.
            /// This should only be used when all operations protected by the lock are very short.
            /// </summary>
            [MethodImpl(InlineOption)]
            internal void EnterWithoutSleep1()
            {
                if (!TryEnter())
                {
                    EnterWithoutSleep1Core();
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void EnterWithoutSleep1Core()
            {
                // Spin until we successfully get lock.
                var spinner = new SpinWait();
                do
                {
                    spinner.SpinOnce(sleep1Threshold: -1);
                }
                while (!TryEnter());
            }

            [MethodImpl(InlineOption)]
            internal void Exit()
                => _locker = 0; // Release lock.
        }
    } // class Internal
} // namespace Proto.Promises