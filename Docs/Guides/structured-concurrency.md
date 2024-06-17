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

## PromiseRaceGroup

`PromiseRaceGroup` races multiple async operations of a single type. If any promise is resolved, the group will be resolved with the result of the promise that resolved first. If no promises are resolved and any promise is rejected, the group will be rejected with an `AggregateException` containing all of the rejections.

```cs
var page = await PromiseRaceGroup<string>.New(cancelationToken, out var groupCancelationToken)
    .Add(Download("http://www.google.com", groupCancelationToken))
    .Add(Download("http://www.bing.com", groupCancelationToken))
    .WaitAsync();
Console.WriteLine(page); // Print the page that was downloaded first.
```