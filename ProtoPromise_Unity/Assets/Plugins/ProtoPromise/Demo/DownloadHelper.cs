using Proto.Promises;
using System;
using System.Collections;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.Networking;
#endif

public static class DownloadHelper
{
#if UNITY_2017_2_OR_NEWER
    private static IEnumerator WaitForWebRequestWithProgress(Promise.DeferredBase deferred, UnityWebRequestAsyncOperation asyncOp, CancelationToken cancelationToken)
    {
        while (!asyncOp.isDone && !cancelationToken.IsCancelationRequested)
        {
            deferred.ReportProgress(asyncOp.progress);
            yield return null;
        }
    }

    public static Promise<Texture2D> DownloadTexture(string url, CancelationToken cancelationToken = default(CancelationToken))
    {
        var deferred = Promise.NewDeferred<Texture2D>(cancelationToken);
        var www = UnityWebRequestTexture.GetTexture(url);
        PromiseYielder.WaitFor(WaitForWebRequestWithProgress(deferred, www.SendWebRequest(), cancelationToken))
            .Finally(ValueTuple.Create(www, deferred), tuple =>
            {
                var webRequest = tuple.Item1;
                var def = tuple.Item2;
                if (!def.IsValidAndPending) // Was the deferred already canceled from the token?
                {
                    webRequest.Abort();
                    webRequest.Dispose();
                }
#if UNITY_2020_2_OR_NEWER
                else if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.DataProcessingError || webRequest.result == UnityWebRequest.Result.ProtocolError)
#else
                else if (webRequest.isHttpError || webRequest.isNetworkError)
#endif
                {
                    string error = webRequest.error;
                    webRequest.Dispose();
                    def.Reject(error);
                }
                else
                {
                    Texture2D result = ((DownloadHandlerTexture) webRequest.downloadHandler).texture;
                    webRequest.Dispose();
                    def.Resolve(result);
                }
            })
            .Forget();
        return deferred.Promise;
    }
#else
    private static IEnumerator WaitForWebRequestWithProgress(Promise.DeferredBase deferred, WWW asyncOp, CancelationToken cancelationToken)
    {
        while (!asyncOp.isDone && !cancelationToken.IsCancelationRequested)
        {
            deferred.ReportProgress(asyncOp.progress);
            yield return null;
        }
    }

    public static Promise<Texture2D> DownloadTexture(string url, CancelationToken cancelationToken = default(CancelationToken))
    {
        var deferred = Promise.NewDeferred<Texture2D>(cancelationToken);
        var www = new WWW(url);
        PromiseYielder.WaitFor(WaitForWebRequestWithProgress(deferred, www, cancelationToken))
            .Finally(ValueTuple.Create(www, deferred), tuple =>
            {
                var webRequest = tuple.Item1;
                var def = tuple.Item2;
                if (!def.IsValidAndPending) // Was the deferred already canceled from the token?
                {
                    webRequest.Dispose();
                }
                else if (!string.IsNullOrEmpty(webRequest.error))
                {
                    string error = webRequest.error;
                    webRequest.Dispose();
                    def.Reject(error);
                }
                else
                {
                    Texture2D result = webRequest.texture;
                    webRequest.Dispose();
                    def.Resolve(result);
                }
            })
            .Forget();
        return deferred.Promise;
    }
#endif
}
