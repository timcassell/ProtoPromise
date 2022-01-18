using Proto.Promises;
using UnityEngine;
using UnityEngine.UI;

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
            Texture2D texture = await DownloadHelper.DownloadTexture(imageUrl);
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
#else
    public void OnClick()
    {
        DownloadHelper.DownloadTexture(imageUrl)
            .Then(texture =>
            {
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            })
            .Forget();
    }
#endif
}