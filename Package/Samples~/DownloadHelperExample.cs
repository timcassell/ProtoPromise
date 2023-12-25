using Proto.Promises;
using System;
using System.Collections;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.Networking;
#endif

namespace Proto.Promises.Examples
{
    public static class DownloadHelper
    {
#if CSHARP_7_3_OR_NEWER

        public static async Promise<Texture2D> DownloadTexture(string url, ProgressToken progressToken = default(ProgressToken), CancelationToken cancelationToken = default(CancelationToken))
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

#elif UNITY_2017_2_OR_NEWER

        public static Promise<Texture2D> DownloadTexture(string url, ProgressToken progressToken = default(ProgressToken), CancelationToken cancelationToken = default(CancelationToken))
        {
            var www = UnityWebRequestTexture.GetTexture(url);
            return PromiseYielder.WaitForAsyncOperation(www.SendWebRequest(), progressToken)
                .ToPromise(cancelationToken)
                .Then(www, webRequest => // Capture www to prevent lambda closure allocation.
                {
                    if (webRequest.isHttpError || webRequest.isNetworkError)
                    {
                        throw Promise.RejectException(webRequest.error);
                    }
                    return ((DownloadHandlerTexture) webRequest.downloadHandler).texture;
                })
                .Finally(www, webRequest => webRequest.Dispose());
        }

#else // 2017.1 or older

        private struct WWWYieldInstruction : IAwaitInstruction
        {
            private readonly WWW _www;
            private readonly ProgressToken _progressToken;

            public WWWYieldInstruction(WWW www, ProgressToken progressToken)
            {
                _www = www;
                _progressToken = progressToken;
            }

            public bool IsCompleted()
            {
                if (_www.isDone)
                {
                    _progressToken.Report(1d);
                    return true;
                }
                _progressToken.Report(_www.progress);
                return false;
            }

            // Old C# compiler thinks the .ToPromise() extension methods are ambiguous, so we add it here explicitly.
            public Promise ToPromise(CancelationToken cancelationToken)
            {
                return PromiseYieldExtensions.ToPromise(this, cancelationToken);
            }
        }

        public static Promise<Texture2D> DownloadTexture(string url, ProgressToken progressToken = default(ProgressToken), CancelationToken cancelationToken = default(CancelationToken))
        {
            var www = new WWW(url);
            return new WWWYieldInstruction(www, progressToken)
                .ToPromise(cancelationToken)
                .Then(www, webRequest => // Capture www to prevent lambda closure allocation.
                {
                    if (!string.IsNullOrEmpty(webRequest.error))
                    {
                        throw Promise.RejectException(webRequest.error);
                    }
                    return webRequest.texture;
                }, cancelationToken) // CancelationToken will prevent the .Then callback from being invoked if it is canceled before the webrequest completes.
                .Finally(www, webRequest => webRequest.Dispose());
        }

#endif
    }
}