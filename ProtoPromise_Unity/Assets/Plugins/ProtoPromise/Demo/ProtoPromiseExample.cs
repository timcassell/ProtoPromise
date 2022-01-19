using Proto.Promises;
using UnityEngine;
using UnityEngine.UI;

public class ProtoPromiseExample : MonoBehaviour
{
    public Image image;
    public string imageUrl = "https://promisesaplus.com/assets/logo-small.png";
    public Image progressBar;
    public Text progressText;
    public Button cancelButton;

    private CancelationSource cancelationSource;

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
            cancelationSource.TryCancel(); // Cancel previous download if it's not yet completed.
            using (var cs = CancelationSource.New())
            {
                cancelationSource = cs;
                cancelButton.interactable = true;
                Texture2D texture = await DownloadHelper.DownloadTexture(imageUrl, cs.Token)
                    .Progress(this, (_this, progress) => // Capture `this` to prevent closure allocation.
                    {
                        _this.progressBar.fillAmount = progress;
                        _this.progressText.text = (progress * 100f).ToString("0.##") + "%";
                    });
                cancelButton.interactable = false;
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
        }
    }
#else
    public void OnClick()
    {
        cancelationSource.TryCancel(); // Cancel previous download if it's not yet completed.
        var cs = CancelationSource.New();
        cancelationSource = cs;
        cancelButton.interactable = true;
        DownloadHelper.DownloadTexture(imageUrl, cs.Token)
            .Progress(this, (_this, progress) => // Capture `this` to prevent closure allocation.
            {
                _this.progressBar.fillAmount = progress;
                _this.progressText.text = (progress * 100f).ToString("0.##") + "%";
            })
            .Then(texture =>
            {
                cancelButton.interactable = false;
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            })
            .Finally(cs, source => source.Dispose()) // Must dispose the source after the async operation completes.
            .Forget();
    }
#endif

    public void OnCancelClick()
    {
        // Cancel download if it's not yet completed.
        if (cancelationSource.TryCancel())
        {
            cancelButton.interactable = false;
            progressText.text = "Canceled";
        }
    }
}