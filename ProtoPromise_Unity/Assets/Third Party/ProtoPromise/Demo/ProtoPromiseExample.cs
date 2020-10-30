using Proto.Promises;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.Networking;
#endif

public class ProtoPromiseExample : MonoBehaviour
{
    public Image image;
    public string imageUrl = "https://www.google.com/images/branding/googlelogo/1x/googlelogo_color_816x276dp.png";

    private void Awake()
    {
        image.preserveAspect = true;
    }

    public void OnClick()
    {
        DownloadTexture(imageUrl)
        .Then(texture =>
        {
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        });
    }

    public static Promise<Texture2D> DownloadTexture(string url)
    {
#if UNITY_2017_2_OR_NEWER
        var www = UnityWebRequestTexture.GetTexture(url);
        return PromiseYielder.WaitFor(www.SendWebRequest())
        .Then(asyncOperation =>
        {
            if (asyncOperation.webRequest.isHttpError || asyncOperation.webRequest.isNetworkError)
            {
                throw Promise.RejectException(asyncOperation.webRequest.error);
            }
            return ((DownloadHandlerTexture) asyncOperation.webRequest.downloadHandler).texture;
        })
        .Finally(www.Dispose);
#else
        var www = new WWW(url);
        return PromiseYielder.WaitFor(www)
        .Then(asyncOperation =>
        {
            if (!string.IsNullOrEmpty(asyncOperation.error))
            {
                throw Promise.RejectException(asyncOperation.error);
            }
            return asyncOperation.texture;
        })
        .Finally(www.Dispose);
#endif
    }
}
