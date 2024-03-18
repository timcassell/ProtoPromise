# Cancelations

Cancelation tokens are primarily used to cancel promises, but can be used to cancel anything. They come in 3 parts: `CancelationSource`, `CancelationToken`, and `CancelationRegistration`.

## Cancelation Source

A `CancelationSource` is what is used to actually cancel a token. When a consumer wants to cancel a producer's operation, it creates a `CancelationSource` via `CancelationSource.New()` and caches it somewhere (usually in a private field). When it determines it no longer needs the result of the operation, it calls `CancelationSource.Cancel()`.

When you are sure that the operation has been fully cleaned up, you must dispose of the source: `CancelationSource.Dispose()`. This usually makes most sense to do it in a promise's [Finally](the-basics.md#finally) callback (or try/finally clause in an async function, or you can use the `using` keyword).

You can get the token to pass to the producer from the `CancelationSource.Token` property.

## Cancelation Token

A `CancelationToken` is what is passed around to listen for a cancelation event. Tokens are read-only, meaning it cannot be canceled without the source. You can use the token to pass into functions (like `Promise.Then`) without worrying about it being canceled from within those functions.

You can register a callback to the token that will be invoked when the source is canceled:

```cs
public void Func(CancelationToken token)
{
    token.Register(() => Console.Log("token was canceled"));
}
```

If the source is disposed without being canceled, the callback will not be invoked.

You can check whether the token is already canceled:

```cs
public IEnumerator FuncEnumerator(CancelationToken token)
{
    using (token.GetRetainer()) // Retain the token for the duration of the operation.
    {
        while (!token.IsCancelationRequested)
        {
            Console.Log("Doing something");
            if (DoSomething())
            {
                yield break;
            }
            yield return null;
        }
        Console.Log("token was canceled");
    }
}
```

Note: If you are checking the `IsCancelationRequested` property instead of registering a callback, you should retain the token for the duration of the operation, then release it when the operation is complete. If you do not retain it, it could be disposed before the next time the `IsCancelationRequested` is accessed.

## Cancelation Registration

When you register a callback to a token, it returns a `CancelationRegistration` which can be used to unregister the callback.

```cs
CancelationRegistration registration = token.Register(() => Console.Log("This won't get called."));

// ... later, before the source is canceled
registration.TryUnregister();
```

If the registration is unregistered before the source is canceled, the callback will not be invoked. Once a registration has been unregistered, it cannot be re-registered. You must register a new callback to the token if you wish to do so.

You may also `Dispose` the registration to unregister the callback, or wait for it to complete if it was already invoked.

## Canceling Promises

Promise implementations usually do not allow cancelations, but it has proven to be invaluable to asynchronous libraries, and ProtoPromise is no exception.

Promises can be canceled 3 ways: passing a `CancelationToken` into `Promise.{Then, Catch, ContinueWith}`, calling `Promise.Deferred.Cancel()`, or by throwing a [Cancelation Exception](special-exceptions.md). When a promise is canceled, all promises that have been chained from it will be canceled, until a `CatchCancelation`.

```cs
CancelationSource cancelationSource = CancelationSource.New();

Download("http://www.google.com");                                  // <---- This will run to completion if no errors occur.
    .Then(html => Console.Log(html), cancelationSource.Token).      // <---- This will be canceled before the download completes and will not run.
    .Then(() => Download("http://www.bing.com"))                    // <---- This will also be canceled and will not run.
    .Then(html => Console.Log(html))                                // <---- This will also be canceled and will not run.
    .Finally(cancelationSource.Dispose)                             // Remember to always dispose of the cancelation source when it's no longer needed.
    .Forget();
    
// ... later, before the first download is complete
cancelationSource.Cancel();                 // <---- This will stop the callbacks from being ran, but will not stop the google download.
```

Cancelations can be caught, similar to how rejections are caught, except there is no value.

```cs
cancelablePromise
    .CatchCancelation(() =>
    {
        Console.Log("Download was canceled!");
    })
```

Just like `.Catch`, `.CatchCancelation` can return a value of the same type, or no value to transform the promise to a non-value promise, or it can return another promise of the same type or non-value to have its state adopted.

```cs
cancelablePromise
    .CatchCancelation(() =>
    {
        Console.Log("Download was canceled! Getting fallback string.");
        return GetFallbackString();
    })
    .Then(result => Console.Log(result))                                    // <--- This will log the fallback string
```

Cancelations always propagate downwards, and never upwards:

```cs
CancelationSource cancelationSource = CancelationSource.New();

Download("http://www.google.com")                                           // <---- This will *not* be canceled and will run to completion
    .Then(html => Console.Log(html))                                        // <---- This will *not* be canceled and will run
    .Then(() => Download("http://www.bing.com"), cancelationSource.Token)   // <---- This will be canceled before the download starts and will not run.
    .Then(html => Console.Log(html))                                        // <---- This will be canceled and will not run.
    .Finally(cancelationSource.Dispose)                                     // Remember to always dispose of the cancelation source when it's no longer needed.
    .Forget();
    
// ... later, before the first download is complete
cancelationSource.Cancel();
```

In an `async` function, you can use the `token.ThrowIfCancelationRequested` API.

```cs
async Promise Func(CancelationToken token)
{
    string html = await Download("http://www.google.com");      // <---- This will *not* be canceled and will run to completion
    Console.Log(html);                                          // <---- This will *not* be canceled and will run
    token.ThrowIfCancelationRequested();                        // <---- This will throw a CanceledException and cancel the `async Promise`
    html = await Download("http://www.bing.com");               // <---- This will not run
    Console.Log(html);                                          // <---- This will not run
}
```

## Unhandled Cancelations

Unlike rejections, cancelations are considered part of normal program flow, and will not be thrown outside of `async` functions. Therefore, catching cancelations is entirely optional.