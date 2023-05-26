using Proto.Promises;
using System;
using System.Collections;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.Networking;
#endif

#pragma warning disable CS0618 // Type or member is obsolete

namespace Proto.Promises.Examples
{
    public static class DownloadHelper
    {
#if CSHARP_7_3_OR_NEWER

        public static async Promise<Texture2D> DownloadTexture(string url, CancelationToken cancelationToken = default(CancelationToken))
        {
            using (var webRequest = UnityWebRequestTexture.GetTexture(url))
            {
                await PromiseYielder.WaitForAsyncOperation(webRequest.SendWebRequest()).AwaitWithProgress(0f, 1f, cancelationToken);

#if UNITY_2020_2_OR_NEWER
                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.DataProcessingError || webRequest.result == UnityWebRequest.Result.ProtocolError)
#else
                if (webRequest.isHttpError || webRequest.isNetworkError)
#endif
                {
                    throw Promise.RejectException(webRequest.error);
                }
                return ((DownloadHandlerTexture) webRequest.downloadHandler).texture;
            }
        }

#elif UNITY_2017_2_OR_NEWER

        public static Promise<Texture2D> DownloadTexture(string url, CancelationToken cancelationToken = default(CancelationToken))
        {
            var www = UnityWebRequestTexture.GetTexture(url);
            return PromiseYielder.WaitForAsyncOperation(www.SendWebRequest())
                .ToPromise(cancelationToken)
                .Then(www, webRequest => // Capture www to prevent lambda closure allocation.
                {
#if UNITY_2020_2_OR_NEWER
                    if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.DataProcessingError || webRequest.result == UnityWebRequest.Result.ProtocolError)
#else
                    if (webRequest.isHttpError || webRequest.isNetworkError)
#endif
                    {
                        throw Promise.RejectException(webRequest.error);
                    }
                    return ((DownloadHandlerTexture) webRequest.downloadHandler).texture;
                })
                .Finally(www.Dispose);
        }

#else // 2017.1 or older

        private static IEnumerator WaitForWebRequestWithProgress(Promise.DeferredBase deferred, WWW asyncOp, CancelationToken cancelationToken)
        {
            using (cancelationToken.GetRetainer()) // Retain the token for the duration of the coroutine.
            {
                while (!cancelationToken.IsCancelationRequested && !asyncOp.isDone)
                {
                    deferred.ReportProgress(asyncOp.progress);
                    yield return null;
                }
            }
        }

        public static Promise<Texture2D> DownloadTexture(string url, CancelationToken cancelationToken = default(CancelationToken))
        {
            var deferred = Promise.NewDeferred<Texture2D>();
            var www = new WWW(url);
            // Start the Coroutine and convert it to a Promise with PromiseYielder.
            PromiseYielder.WaitFor(WaitForWebRequestWithProgress(deferred, www, cancelationToken))
                .Then(ValueTuple.Create(www, deferred), tuple => // Capture www and deferred to prevent lambda closure allocation.
                {
                    var webRequest = tuple.Item1;
                    var def = tuple.Item2;
                    if (!string.IsNullOrEmpty(webRequest.error))
                    {
                        def.Reject(webRequest.error);
                    }
                    else
                    {
                        def.Resolve(webRequest.texture);
                    }
                }, cancelationToken) // CancelationToken will prevent the .Then callback from being invoked if it is canceled before the webrequest completes.
                .Finally(www.Dispose)
                .CatchCancelation(deferred.Cancel)
                .Forget();
            return deferred.Promise;
        }

#endif
    }
}