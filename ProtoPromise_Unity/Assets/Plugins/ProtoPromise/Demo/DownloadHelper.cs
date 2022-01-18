using Proto.Promises;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.Networking;
#endif

public static class DownloadHelper
{
#if CSHARP_7_3_OR_NEWER
    public static async Promise<Texture2D> DownloadTexture(string url)
    {
        using (var www = UnityWebRequestTexture.GetTexture(url))
        {
            await PromiseYielder.WaitFor(www.SendWebRequest());
#if UNITY_2020_2_OR_NEWER
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.DataProcessingError || www.result == UnityWebRequest.Result.ProtocolError)
#else
            if (www.isHttpError || www.isNetworkError)
#endif
            {
                throw Promise.RejectException(www.error);
            }
            return ((DownloadHandlerTexture) www.downloadHandler).texture;
        }
    }
#elif UNITY_2017_2_OR_NEWER
            public static Promise<Texture2D> DownloadTexture(string url)
    {
        var www = UnityWebRequestTexture.GetTexture(url);
        return PromiseYielder.WaitFor(www.SendWebRequest())
            .Then(www, webRequest =>
            {
                if (webRequest.isHttpError || webRequest.isNetworkError)
                {
                    throw Promise.RejectException(webRequest.error);
                }
                return ((DownloadHandlerTexture) webRequest.downloadHandler).texture;
            })
            .Finally(www.Dispose);
    }
#else
    public static Promise<Texture2D> DownloadTexture(string url)
    {
        var www = new WWW(url);
        return PromiseYielder.WaitFor(www)
            .Then(www, asyncOperation =>
            {
                if (!string.IsNullOrEmpty(asyncOperation.error))
                {
                    throw Promise.RejectException(asyncOperation.error);
                }
                return asyncOperation.texture;
            })
            .Finally(www.Dispose);
    }
#endif
}
