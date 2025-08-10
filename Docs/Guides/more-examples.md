# More Examples

You can implement timeouts on operations that don't natively support timeouts, as long as they do support cancelations. Here is an example using `TcpClient`:

```cs
public static async Promise ConnectAsync(this TcpClient tcpClient, IPEndPoint endPoint, TimeSpan timeout, CancelationToken cancelationToken)
{
    using var _ = cancelationToken.GetRetainer();
    using var cancelationSource = CancelationSource.New(timeout, cancelationToken);
    try
    {
        await tcpClient.ConnectAsync(endPoint, cancelationSource.Token.ToCancellationToken());
    }
    catch (OperationCanceledException) when (!cancelationToken.IsCancelationRequested)
    {
        throw new TimeoutException();
    }
}
```

Here is an example using `UnityWebRequest` to download a texture while reporting progress and observing a timeout and cancelation:

```cs
public static async Promise<Texture2D> DownloadTexture(string url, ProgressToken progressToken, TimeSpan timeout, CancelationToken cancelationToken)
{
    using var _ = cancelationToken.GetRetainer();
    using var cancelationSource = CancelationSource.New(timeout, cancelationToken);
    using var request = UnityWebRequestTexture.GetTexture(url);
    try
    {
        await PromiseYielder.WaitForAsyncOperation(request.SendWebRequest(), progressToken).WithCancelation(cancelationSource.Token);
    }
    catch (OperationCanceledException) when (!cancelationToken.IsCancelationRequested)
    {
        throw new TimeoutException();
    }
    if (request.result != UnityWebRequest.Result.Success)
    {
        throw new Exception($"Failed to load texture at {url}, error: {request.error}");
    }
    return ((DownloadHandlerTexture) request.downloadHandler).texture;
}
```

Some async operations have no cancelation support, but you may wish to provide cancelation support to higher callers regardless. You can do this via the `promise.WaitAsync(CancelationToken)` method. Here is an example using `Firebase`:

```cs
public static async Promise<UserDocument> ReadUserDocument(string userId, CancelationToken cancelationToken)
{
    var userDocumentSnapshot = await FirebaseFirestore.DefaultInstance
        .Collection("users")
        .Document(userId)
        .GetSnapshotAsync()
        .ToPromise().WaitAsync(cancelationToken);
    return userDocumentSnapshot.ConvertTo<UserDocument>();
}
```

NOTE: Using this method, if the cancelation token is canceled before the async operation is complete, the operation will be _orphaned_, continuing to run in the background without any way to interact with it. When the operation eventually completes, no further code will be executed (unless the operation errored, in which case the error may be sent to the uncaught rejection handler).