using UnityEngine;
using UnityEngine.Networking;

namespace Proto.Promises.Examples
{
    public static class DownloadHelper
    {
        public static async Promise<Texture2D> DownloadTexture(string url, ProgressToken progressToken = default, CancelationToken cancelationToken = default)
        {
            using (var webRequest = UnityWebRequestTexture.GetTexture(url))
            {
                await PromiseYielder.WaitForAsyncOperation(webRequest.SendWebRequest(), progressToken)
                    .WithCancelation(cancelationToken);

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
    }
}