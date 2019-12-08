#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#endif

#pragma warning disable RECS0096 // Type parameter is never used
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter

using System;
using System.Collections.Generic;

namespace Proto.Promises
{
    partial class Promise
    {
        partial class Internal
        {
            public sealed partial class SequencePromise0 : PromiseWaitPromise<SequencePromise0>
            {
                static partial void GetFirstPromise(ref Promise promise, int skipFrames);

                public static Promise GetOrCreate<TEnumerator>(TEnumerator promiseFuncs, int skipFrames) where TEnumerator : IEnumerator<Func<Promise>>
                {
                    if (!promiseFuncs.MoveNext())
                    {
                        // If promiseFuncs is empty, just return a resolved promise.
                        return Resolved();
                    }

                    Promise promise = promiseFuncs.Current.Invoke();
                    GetFirstPromise(ref promise, skipFrames + 1);

                    while (promiseFuncs.MoveNext())
                    {
                        promise = promise.Then(promiseFuncs.Current);
                    }
#if PROMISE_CANCEL
                    return promise.ThenDuplicate(); // Prevents canceling only the very last callback.
#else
                    return promise;
#endif
                }

                protected override void Handle(Promise feed)
                {
                    HandleSelf(feed);
                }

#if PROMISE_PROGRESS
                // Only wrap the promise to normalize its progress. If we're not using progress, we can just use the promise as-is.
                static partial void GetFirstPromise(ref Promise promise, int skipFrames)
                {
                    var newPromise = _pool.IsNotEmpty ? (SequencePromise0) _pool.Pop() : new SequencePromise0();
                    newPromise.Reset(skipFrames + 1);
                    newPromise.ResetDepth();
                    newPromise.WaitFor(promise);
                    promise = newPromise;
                }
#endif
            }
        }
    }
}