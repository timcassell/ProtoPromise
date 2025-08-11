# Structured Concurrency

Structured concurrency is a pattern that makes it simple to reason about multiple async operations running concurrently, and not needing to worry about orphaned operations continuing to run in the background after the current async context has continued. We implement this pattern via promise groups. A group is created which owns a `CancelationToken`, then async operations are started by passing in the token, and the promises are added to the group. The entire group is then awaited, which will only continue when all of the added promises have completed. If the group is expected to complete early, the `CancelationToken` will be canceled.

## PromiseAllGroup

`PromiseAllGroup` combines multiple async operations of a single type. When all promises have completed successfully, it yields a list containing the results of those promises in the same order they were added. If any promise is rejected, the group will be rejected with an `AggregateException` containing all of the rejections.

```cs
var pages = await PromiseAllGroup<string>.New(cancelationToken, out var groupCancelationToken)
    .Add(Download("http://www.google.com", groupCancelationToken))
    .Add(Download("http://www.bing.com", groupCancelationToken))
    .WaitAsync();
foreach (var link in pages.SelectMany(page => ExtractAllLinks(page)))
{
    Console.WriteLine(link);
}
```

If you are loading disposable resources, you can pass an onCleanup delegate to the group to automatically dispose the results if the group is rejected or canceled.

```cs
var resources = await PromiseAllGroup<IDisposable>.New(cancelationToken, out var groupCancelationToken, resource => resource.Dispose())
    .Add(LoadResource("resource1", groupCancelationToken, resource => resource.Dispose()))
    .Add(LoadResource("resource2", groupCancelationToken, resource => resource.Dispose()))
    .WaitAsync();
// ...
foreach (var resource in resources)
{
    resource.Dispose();
}
```

## PromiseMergeGroup

`PromiseMergeGroup` combines multiple async operations of one or more types. The group's `WaitAsync()` method either returns a void `Promise` if only void promises were added to it, or a `Promies<T>` that yields a `ValueTuple<>` containing the type of each promise in the order it was added. When all promises have completed successfully, the group will be resolved with the appropriate value. If any promise is rejected, the group will be rejected with an `AggregateException` containing all of the rejections.

```cs
var (page1, page2) = await PromiseMergeGroup.New(cancelationToken, out var groupCancelationToken)
    .Add(Download("http://www.google.com", groupCancelationToken))
    .Add(Download("http://www.bing.com", groupCancelationToken))
    .WaitAsync();
foreach (var link in ExtractAllLinks(page1))
{
    Console.WriteLine(link);
}
foreach (var link in ExtractAllLinks(page2))
{
    Console.WriteLine(link);
}
```

If you are loading disposable resources, you can pass an onCleanup delegate to the `Add` method to automatically dispose the result if the group is rejected or canceled.

```cs
var (resource1, resource2) = await PromiseMergeGroup.New(cancelationToken, out var groupCancelationToken)
    .Add(LoadResource("resource1", groupCancelationToken, resource => resource.Dispose()))
    .Add(LoadResource("resource2", groupCancelationToken, resource => resource.Dispose()))
    .WaitAsync();
// ...
resource1.Dispose();
resource2.Dispose();
```

## PromiseAllResultsGroup and PromiseMergeResultsGroup

The `PromiseAllResultsGroup` and `PromiseMergeResultsGroup` groups behave very similar to `PromiseAllGroup` and `PromiseMergeGroup`, except they yield a list/tuple of `ResultContainer`s instead of the raw type, and they won't be rejected with an `AggregateException` if any of the promises are rejected.

```cs
var (googleResult, imageResult) = await PromiseMergeGroup.New(cancelationToken, out var groupCancelationToken)
    .Add(Download("http://www.google.com", groupCancelationToken))
    .Add(DownloadImage("http://www.example.com/image.jpg", groupCancelationToken))
    .WaitAsync();
if (googleResult.State == Promise.State.Resolved)
{
    PrintLinks(googleResult.Value);
}
else if (googleResult.State == Promise.State.Rejected)
{
    Console.WriteLine(googleResult.Reason);
}

if (imageResult.State == Promise.State.Resolved)
{
    image.SetTexture(imageResult.Value);
}
else if (imageResult.State == Promise.State.Rejected)
{
    Console.WriteLine(imageResult.Reason);
}
```

These group types do not support onCleanup delegates, because the results contain all of the information necessary to do any custom cleanup logic.

## PromiseRaceGroup

`PromiseRaceGroup` races multiple async operations of a single type. If any promise is resolved, the group will be resolved with the result of the promise that resolved first. If no promises are resolved and any promise is rejected, the group will be rejected with an `AggregateException` containing all of the rejections.

```cs
var page = await PromiseRaceGroup<string>.New(cancelationToken, out var groupCancelationToken)
    .Add(Download("http://www.google.com", groupCancelationToken))
    .Add(Download("http://www.bing.com", groupCancelationToken))
    .WaitAsync();
Console.WriteLine(page); // Print the page that was downloaded first.
```

If you are loading disposable resources, you can pass an onCleanup delegate to the group to automatically dispose the results if more than 1 result is obtained, or if the group is rejected or canceled.

```cs
var resources = await PromiseRaceGroup<IDisposable>.New(cancelationToken, out var groupCancelationToken, resource => resource.Dispose())
```

## PromiseEachGroup

`PromiseEachGroup` combines multiple async operations of a single type into an `AsyncEnumerable<T>` that will yield each operation's result in the order that they complete.

```cs
var asyncEnumerable = PromiseEachGroup<string>.New(cancelationToken, out var groupCancelationToken)
    .Add(Download("http://www.google.com", groupCancelationToken))
    .Add(Download("http://www.bing.com", groupCancelationToken))
    .GetAsyncEnumerable();
// Print each page in the order that their downloads complete.
await foreach (var downloadResult in asyncEnumerable)
{
    if (downloadResult.State == Promise.State.Resolved)
    {
        Console.WriteLine(downloadResult.Value);    // Print the HTML.
    }
    else if (downloadResult.State == Promise.State.Rejected)
    {
        Console.WriteLine(downloadResult.Reason);    // Print the reject reason.
    }
    else
    {
        Console.WriteLine("Download was canceled");
    }
}
```