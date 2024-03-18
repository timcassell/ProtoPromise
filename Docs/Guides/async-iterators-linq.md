# Async Iterators and Async Linq

## Async Iterators

C# 8 added async iterators feature (also known as async streams). It looks like this:

```cs
public async IAsyncEnumerable<int> MyEveryUpdate([EnumeratorCancellation] CancellationToken cancelationToken = default)
{
    var frameCount = 0;
    await PromiseYielder.WaitForUpdate();
    while (!cancelationToken.IsCancellationRequested)
    {
        yield return frameCount++;
        await PromiseYielder.WaitForUpdate();
    }
}
```

While the syntax is nice, it's not as efficient as it could be. Every time the function is called, a new object is allocated for the `IAsyncEnumerable<int>`.
Instead, you can use `AsyncEnumerable<T>` (in the `Proto.Promises.Linq` namespace).

```cs
public async AsyncEnumerable<int> MyEveryUpdate() => AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
{
    var frameCount = 0;
    await PromiseYielder.WaitForUpdate();
    while (!cancelationToken.IsCancelationRequested)
    {
        // yield return frameCount++;
        await writer.YieldAsync(frameCount++);
        await PromiseYielder.WaitForUpdate();
    }
}
```

This doesn't have as nice syntax as the C# language feature (because the language doesn't support custom async iterators), but it will not allocate garbage, as long as object pooling is enabled.

You can consume async iterators like this in C# 8+:

```cs
await foreach (var frame in MyEveryUpdate().WithCancelation(cancelationToken))
{
    Debug.Log($"Update() {frame}");
}
```

Or like this in C# 7:

```cs
await MyEveryUpdate().ForEachAsync(frame =>
{
    Debug.Log($"Update() {frame}");
}, cancelationToken);
```

## Async Linq

Linq extensions exist for `AsyncEnumerable<T>` to filter and transform the results, just like regular `System.Linq` (`.Where`, `.Select`, `.GroupBy`, etc). All the same extensions were implemented with async support, so you can use the same extensions as you would on `IEnumerable<T>`. You can choose to use synchronous or asynchronous filter/transform functions, and optionally pass in a capture value to avoid closure allocations.

```cs
public async AsyncEnumerable<int> MyEveryXUpdate(int skipFrameCount)
    => MyEveryUpdate()
        .Where(skipFrameCount + 1, (cv, frame) => frame % cv == 0)
```