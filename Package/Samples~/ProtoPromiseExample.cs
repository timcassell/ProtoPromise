using Proto.Promises;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Proto.Promises.Examples
{
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

#if UNITY_2021_2_OR_NEWER // netstandard2.1 added in 2021.2, so `await using` works.
        public void OnClick()
        {
            // Don't use `async void` because that uses Tasks instead of Promises.
            _OnClick().Forget();

            async Promise _OnClick()
            {
                cancelationSource.TryCancel(); // Cancel previous download if it's not yet completed.
                using var cs = CancelationSource.New();
                cancelationSource = cs;
                cancelButton.interactable = true;

                Texture2D texture;
                await using (var progress = Progress.New(this, (_this, value) => _this.OnProgress(value)))
                {
                    texture = await DownloadHelper.DownloadTexture(imageUrl, progress.Token, cs.Token);
                }

                cancelButton.interactable = false;
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
        }
#elif CSHARP_7_3_OR_NEWER
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

                    var progress = Progress.New(this, (_this, value) => _this.OnProgress(value));
                    Texture2D texture;
                    try
                    {
                        texture = await DownloadHelper.DownloadTexture(imageUrl, progress.Token, cs.Token);
                    }
                    finally
                    {
                        await progress.DisposeAsync();
                    }

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
            
            var progress = Progress.New(this, (_this, value) => _this.OnProgress(value));
            DownloadHelper.DownloadTexture(imageUrl, progress.Token, cs.Token)
                .Finally(progress, p => p.DisposeAsync()) // Dispose the progress object.
                .Then(this, (_this, texture) => // Capture `this` to prevent closure allocation.
                {
                    _this.cancelButton.interactable = false;
                    _this.image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                })
                .Finally(cs, source => source.Dispose()) // Must dispose the source after the async operation completes.
                .Forget();
        }
#endif

        private void OnProgress(double progress)
        {
            progressBar.fillAmount = (float) progress;
            progressText.text = (progress * 100f).ToString("0.##") + "%";
        }

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
}