# Combining Multiple Async Operations

Note: It is recommended to use [Structured Concurrency Groups](structured-concurrency.md) instead of these methods.

## All

The `All` function combines multiple async operations that are currently running. It converts a collection of promises or a variable length parameter list of promises into a single promise, and if those promises are non-void, it yields a list containing the results of those promises in the same order.

Say that each promise yields a value of type `string`, the resulting promise then yields an `IList<string>`.

Here is an example that extracts links from multiple pages and merges the results:

```cs
Promise.All(Download("http://www.google.com"), Download("http://www.bing.com"))  // Download each URL.
    .Then(pages =>                          // Receives collection of downloaded pages.
        pages.SelectMany(
            page => ExtractAllLinks(page)   // Extract links from all pages then flatten to single collection of links.
        )
    )
    .Then(links =>                          // Receives the flattened collection of links from all pages at once.
    {
        foreach (var link in links)
        {
            Console.WriteLine(link);
        }
    })
```

## Merge

The `Merge` function behaves just like the `All` function, except that it can be used to combine multiple types, and instead of yielding an `IList<T>`, it yields a `ValueTuple<>` that contains the types of the promises provided to the function.

```cs
Promise.Merge(Download("http://www.google.com"), DownloadImage("http://www.example.com/image.jpg"))  // Download HTML and image.
    .Then(values =>                         // Receives ValueTuple<string, Texture>.
    {
        Console.WriteLine(values.Item1);    // Print the HTML.
        image.SetTexture(values.Item2);     // Assign the texture to an image object.
    })
```

## AllSettled and MergeSettled

The `AllSettled` and `MergeSettled` functions behave very similar to `All` and `Merge`, except they yield a collection/tuple of `ResultContainer`s instead of the raw type. This is because they capture the state of each promise. The returned promise waits until all promises are complete, whether they are resolved, rejected, or canceled (in contrast, `All` and `Merge` immediately reject or cancel the returned promise when any promise is rejected or canceled).

```cs
Promise.MergeSettled(Download("http://www.google.com"), DownloadImage("http://www.example.com/image.jpg"))  // Download HTML and image.
    .Then(resultContainers =>                         // Receives ValueTuple<Promise<string>.ResultContainer, Promise<Texture>.ResultContainer>.
    {
        var result1 = resultContainers.Item1;
        var result2 = resultContainers.Item2;
        if (result1.State == Promise.State.Resolved)
        {
            Console.WriteLine(result1.Value);    // Print the HTML.
        }
        else if (result1.State == Promise.State.Rejected)
        {
            Console.WriteLine(result1.Reason);    // Print the reject reason.
        }
        
        if (result2.State == Promise.State.Resolved)
        {
            image.SetTexture(result2.Value);     // Assign the texture to an image object.
        }
        else if (result2.State == Promise.State.Rejected)
        {
            Console.WriteLine(result2.Reason);    // Print the reject reason.
        }
    })
```

## Race

The `Race` function is similar to the `All` function, but it is the first async operation that settles that wins the race and the promise adopts its state.

```cs
Promise.Race(Download("http://www.google.com"), Download("http://www.bing.com"))  // Download each URL.
    .Then(html => Console.Log(html))                        // Both pages are downloaded, but only
                                                            // log the first one downloaded.
```

## First

The `First` function is almost idential to `Race` except that if a promise is rejected or canceled, the First promise will remain pending until one of the input promises is resolved or they are all rejected/canceled.