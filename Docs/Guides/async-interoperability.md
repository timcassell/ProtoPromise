# Async Interoperability

## .Net BCL

### Task

Promises can easily interoperate with Tasks simply by calling the `Promise.ToTask()` or `Task.ToPromise()` extension methods.

### ValueTask

Promises can be converted to ValueTasks by calling `Promise.AsValueTask()` method, or by implicitly casting `ValueTask valueTask = promise`.
ValueTasks can be converted to Promises by calling the `ValueTask.ToPromise()` extension method.

### CancellationToken

`Proto.Promises.CancelationToken` can be converted to and from `System.Threading.CancellationToken` by calling `token.ToCancellationToken()` method or `token.ToCancelationToken()` extension method.

## Unity

### Coroutines

If you are using Coroutines, you can easily convert a promise to a yield instruction via `promise.ToYieldInstruction()` which you can yield return to wait until the promise has settled. You can also convert any yield instruction (including Coroutines themselves) to a promise via `PromiseYielder.WaitFor(yieldInstruction)`. This will wait until the yieldInstruction has completed before resolving the promise.

```cs
public async Promise<Texture2D> DownloadTexture(string url)
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
```

```cs
IEnumerator GetAndAssignTexture(Image image, string url)
{
    using (var textureYieldInstruction = DownloadTexture(url).ToYieldInstruction())
    {
        yield return textureYieldInstruction;
        Texture2D texture = textureYieldInstruction.GetResult();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        image.sprite = sprite;
    }
}
```

### Awaitable

Unity Awaitables can be converted to Promises by calling the `Awaitable.ToPromise()` extension method. You can optionally pass in a `CancelationToken` to cancel the Awaitable when the token is canceled.