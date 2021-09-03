using Proto.Promises;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.Networking;
#endif

public class ProtoPromiseExample : MonoBehaviour
{
    public Image image;
    public string imageUrl = "https://promisesaplus.com/assets/logo-small.png";

    private void Awake()
    {
        image.preserveAspect = true;
    }

#if CSHARP_7_3_OR_NEWER
    public void OnClick()
    {
        // Don't use `async void` because that uses Tasks instead of Promises.
        _OnClick().Forget();

        async Promise _OnClick()
        {
            Texture2D texture = await DownloadTexture(imageUrl);
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
#else
    public void OnClick()
    {
        DownloadTexture(imageUrl)
            .Then(texture =>
            {
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            })
            .Forget();
    }
#endif

#if CSHARP_7_3_OR_NEWER
    public static async Promise<Texture2D> DownloadTexture(string url)
    {
        using (var www = UnityWebRequestTexture.GetTexture(url))
        {
            await PromiseYielder.WaitFor(www.SendWebRequest());
            if (www.isHttpError || www.isNetworkError)
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
