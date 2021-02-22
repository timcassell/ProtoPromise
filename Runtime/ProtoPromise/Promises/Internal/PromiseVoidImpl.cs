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

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal static partial class PromiseImplVoid
            {
                internal static void Progress(Promise _this, Action<float> onProgress, CancelationToken cancelationToken)
                {
#if !PROMISE_PROGRESS
                    ThrowProgressException(2);
#else
                    ValidateOperation(_this, 2);
                    ValidateArgument(onProgress, "onProgress", 2);

                    SubscribeProgress(_this, onProgress, cancelationToken);
#endif
                }

                // Capture values below.

                internal static void Progress<TCaptureProgress>(Promise _this, ref TCaptureProgress progressCaptureValue, Action<TCaptureProgress, float> onProgress, CancelationToken cancelationToken)
                {
#if !PROMISE_PROGRESS
                    ThrowProgressException(1);
#else
                    ValidateOperation(_this, 2);
                    ValidateArgument(onProgress, "onProgress", 2);

                    SubscribeProgress(_this, progressCaptureValue, onProgress, cancelationToken);
#endif
                }
            }
        }
    }
}