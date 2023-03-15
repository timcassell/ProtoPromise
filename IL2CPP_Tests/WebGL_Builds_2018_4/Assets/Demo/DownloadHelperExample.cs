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
#if UNITY_2017_2_OR_NEWER
        private static IEnumerator WaitForWebRequestWithProgress(Promise.DeferredBase deferred, UnityWebRequestAsyncOperation asyncOp, CancelationToken cancelationToken)
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
            var deferred = Promise.NewDeferred<Texture2D>(cancelationToken);
            var www = UnityWebRequestTexture.GetTexture(url);
            PromiseYielder.WaitFor(WaitForWebRequestWithProgress(deferred, www.SendWebRequest(), cancelationToken))
                .Then(ValueTuple.Create(www, deferred), tuple => // Capture www and deferred to prevent lambda closure allocation.
                {
                    var webRequest = tuple.Item1;
                    var def = tuple.Item2;
#if UNITY_2020_2_OR_NEWER
                    if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.DataProcessingError || webRequest.result == UnityWebRequest.Result.ProtocolError)
#else
                    if (webRequest.isHttpError || webRequest.isNetworkError)
#endif
                    {
                        string error = webRequest.error;
                        def.Reject(error);
                    }
                    else
                    {
                        Texture2D result = ((DownloadHandlerTexture) webRequest.downloadHandler).texture;
                        def.Resolve(result);
                    }
                }, cancelationToken) // CancelationToken will prevent the .Then callback from being invoked if it is canceled before the webrequest completes.
                .Finally(www.Dispose)
                .Forget();
            return deferred.Promise;
        }
#else
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
            var deferred = Promise.NewDeferred<Texture2D>(cancelationToken);
            var www = new WWW(url);
            PromiseYielder.WaitFor(WaitForWebRequestWithProgress(deferred, www, cancelationToken))
                .Then(ValueTuple.Create(www, deferred), tuple => // Capture www and deferred to prevent lambda closure allocation.
                {
                    var webRequest = tuple.Item1;
                    var def = tuple.Item2;
                    if (!string.IsNullOrEmpty(webRequest.error))
                    {
                        string error = webRequest.error;
                        def.Reject(error);
                    }
                    else
                    {
                        Texture2D result = webRequest.texture;
                        def.Resolve(result);
                    }
                }, cancelationToken) // CancelationToken will prevent the .Then callback from being invoked if it is canceled before the webrequest completes.
                .Finally(www.Dispose)
                .Forget();
            return deferred.Promise;
        }
#endif
    }
}