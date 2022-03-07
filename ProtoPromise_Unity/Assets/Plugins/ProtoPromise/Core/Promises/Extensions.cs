#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#if !NET_LEGACY || NET40
using System.Threading.Tasks;
#endif

namespace Proto.Promises
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public static partial class Extensions
    {
#if !NET_LEGACY
        /// <summary>
        /// Convert the <see cref="Promise"/> to a <see cref="Task"/>.
        /// </summary>
        public static async Task ToTask(this Promise promise)
        {
            await promise;
        }

        /// <summary>
        /// Convert the <see cref="Promise{T}"/> to a <see cref="Task{T}"/>.
        /// </summary>
        public static async Task<T> ToTask<T>(this Promise<T> promise)
        {
            return await promise;
        }

        /// <summary>
        /// Convert the <see cref="Task"/> to a <see cref="Promise"/>.
        /// </summary>
        public static async Promise ToPromise(this Task task)
        {
            await task;
        }

        /// <summary>
        /// Convert the <see cref="Task{T}"/> to a <see cref="Promise{T}"/>.
        /// </summary>
        public static async Promise<T> ToPromise<T>(this Task<T> task)
        {
            return await task;
        }
#elif NET40
        /// <summary>
        /// Convert the <see cref="Promise"/> to a <see cref="Task"/>.
        /// </summary>
        public static Task ToTask(this Promise promise)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            promise
                .ContinueWith(taskCompletionSource, (source, resultContainer) =>
                {
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        source.SetResult(true);
                    }
                    else if (resultContainer.State == Promise.State.Canceled)
                    {
                        source.SetCanceled();
                    }
                    else
                    {
                        if (!resultContainer.RejectContainer.TryGetValueAs(out System.Exception exception))
                        {
                            exception = Promise.RejectException(resultContainer.RejectContainer.Value);
                        }
                        source.SetException(exception);
                    }
                })
                .Forget();
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Convert the <see cref="Promise{T}"/> to a <see cref="Task{T}"/>.
        /// </summary>
        public static Task<T> ToTask<T>(this Promise<T> promise)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();
            promise
                .ContinueWith(taskCompletionSource, (source, resultContainer) =>
                {
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        source.SetResult(resultContainer.Result);
                    }
                    else if (resultContainer.State == Promise.State.Canceled)
                    {
                        source.SetCanceled();
                    }
                    else
                    {
                        if (!resultContainer.RejectContainer.TryGetValueAs(out System.Exception exception))
                        {
                            exception = Promise.RejectException(resultContainer.RejectContainer.Value);
                        }
                        source.SetException(exception);
                    }
                })
                .Forget();
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Convert the <see cref="Task"/> to a <see cref="Promise"/>.
        /// </summary>
        public static Promise ToPromise(this Task task)
        {
            var deferred = Promise.NewDeferred();
            task.ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    deferred.Resolve();
                }
                else if (t.Status == TaskStatus.Canceled)
                {
                    deferred.Cancel();
                }
                else
                {
                    deferred.Reject(t.Exception);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return deferred.Promise;
        }

        /// <summary>
        /// Convert the <see cref="Task{T}"/> to a <see cref="Promise{T}"/>.
        /// </summary>
        public static Promise<T> ToPromise<T>(this Task<T> task)
        {
            var deferred = Promise.NewDeferred<T>();
            task.ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    deferred.Resolve(t.Result);
                }
                else if (t.Status == TaskStatus.Canceled)
                {
                    deferred.Cancel();
                }
                else
                {
                    deferred.Reject(t.Exception);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return deferred.Promise;
        }
#endif // elif NET40
    }
}
