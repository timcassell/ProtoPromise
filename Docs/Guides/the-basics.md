# Basic Promise Usage

## Creating a Promise for an Async Operation

Import the namespace:

```cs
using Proto.Promises;
```
Create a deferred before you start the async operation:

```cs
var deferred = Promise.NewDeferred<string>();
```
The type of the deferred should reflect the result of the asynchronous operation.

Then initiate your async operation and return the promise to the caller.

```cs
return deferred.Promise;
```

Upon completion of the async op the promise is resolved via the deferred:

```cs
deferred.Resolve(value);
```
The promise is rejected on error/exception:

```cs
deferred.Reject(error);
```
To see it in context, here is an example function that downloads text from a URL. The promise is resolved when the download completes. If there is an error during download, say *unresolved domain name*, then the promise is rejected:

```cs
public Promise<string> Download(string url)
{
    var deferred = Promise.NewDeferred<string>();    // Create deferred.
    using (var client = new WebClient())
    {
        client.DownloadStringCompleted += (s, ev) =>  // Monitor event for download completed.
        {
            if (ev.Error != null)
            {
                deferred.Reject(ev.Error);   // Error during download, reject the promise.
            }
            else
            {
                deferred.Resolve(ev.Result); // Downloaded completed successfully, resolve the promise.
            }
        };

        client.DownloadStringAsync(new Uri(url), null); // Initiate async op.
    }

    return deferred.Promise; // Return the promise so the caller can await resolution (or error).
}
```

### Creating a Promise, Alternate Method

There is another way to create a promise that replicates the JavaScript convention of passing a *resolver* function into the constructor. The only difference is a deferred is passed to the function instead of another function. The deferred is what controls the resolution or rejection of the promise, just as it was above. This allows you to express the previous example like this:

```cs
public Promise<string> Download(string url)
{
  return Promise.New<string>(deferred =>
  {
      using (var client = new WebClient())
      {
          client.DownloadStringCompleted += (s, ev) =>  // Monitor event for download completed.
          {
              if (ev.Error != null)
              {
                  deferred.Reject(ev.Error);   // Error during download, reject the promise.
              }
              else
              {
                  deferred.Resolve(ev.Result); // Downloaded completed successfully, resolve the promise.
              }
          };

          client.DownloadStringAsync(new Uri(url), null); // Initiate async op.
      }
  });
}
```

With this method, if the function throws an exception before the deferred is settled, the deferred/promise will be rejected with that exception.

## Waiting for an Async Operation to Complete ##

The simplest and most common usage is to use the `await` keyword in an async function. Code that comes after the await will not be ran until the promise has completed. `await promise` will also return its result when it completes so that it can be assigned to a variable.

```cs
async void Func()
{
    string html = await Download("http://www.google.com");
    Console.WriteLine(html);
}
```

This snippet downloads the front page from Google and prints it to the console.

If you are using a language that does not support async/await (or an older version of C#), or if you prefer the javascript "thenable" convention, you can use the `.Then` API.

```cs
void Func()
{
    Download("http://www.google.com")
        .Then(html => Console.WriteLine(html))
        .Forget();
}
```

In this case, because the operation can fail, you will also want to handle the error:

```cs
async void Func()
{
    try
    {
        string html = await Download("http://www.google.com");
        Console.WriteLine(html);
    }
    catch (Exception error)
    {
        Console.WriteLine("An error occured while downloading: " + error);
    }
}
```

```cs
void Func()
{
    Download("http://www.google.com")
        .Then(html => Console.WriteLine(html))
        .Catch((Exception error) => Console.WriteLine("An error occured while downloading: " + error));
        .Forget();
}
```

The chain of processing for a promise ends as soon as an error/exception occurs. In this case when an error occurs the *Reject* handler would be called, but not the *Resolve* handler. If there is no error, then only the *Resolve* handler is called.

## Chaining Async Operations

Multiple async operations can be chained one after the other using `await` or `.Then`.

```cs
async void Func()
{
    try
    {
        string html = await Download("http://www.google.com");
        // Extract the first link and download it and wait for the download to complete.
        string firstLink = ExtractFirstLink(html)
        string firstLinkHtml = await Download(firstLink);
        Console.WriteLine(firstLinkHtml);
    }
    catch (Exception error)
    {
        Console.WriteLine("An error occured while downloading: " + error);
    }
}
```

```cs
void Func()
{
    Download("http://www.google.com")
        .Then(html =>
        {
            // Extract the first link and download it and wait for the download to complete.
            string firstLink = ExtractFirstLink(html)
            return Download(firstLink);
        })
        .Then(firstLinkHtml => Console.WriteLine(firstLinkHtml))
        .Catch((Exception error) => Console.WriteLine("An error occured while downloading: " + error))
        .Forget();
}
```

Here we are chaining another download onto the end of the first download. The first link in the html is extracted and we then download that. `.Then` expects the return value to be another promise. The chained promise can have a different *result type*.

## Transforming the Results

Sometimes you will want to simply transform or modify the resulting value without chaining another async operation.

```cs
async Promise<string[]> GetAllLinks(string url)
{
    string html = await Download(url);
    return ExtractAllLinks(html);   // Extract all links and return an array of strings.
}
```

```cs
Promise<string[]> GetAllLinks(string url)
{
    return Download(url)
        .Then(html => ExtractAllLinks(html)))   // Extract all links and return an array of strings.
}
```

As demonstrated, the type of the value can also be changed during transformation. In the previous snippet a `Promise<string>` is transformed to a `Promise<string[]>`.

## Promise Types

Promises may either be a value promise (`Promise<T>`), or a non-value promise (`Promise`). A value promise represents an asynchronous operation that will result in a value, and a non-value promise represents an asynchronous operation that simply does something without returning anything. In games, this is useful for composing and linking animations and other effects.

```cs
RunAnimation("Foo")                         // RunAnimation returns a promise that
    .Then(() => RunAnimation("Bar"))        // is resolved when the animation is complete.
    .Then(() => PlaySound("AnimComplete"))
    .Forget();
```

`Promise<T>` contains much of the same methods as `Promise`, so any value promise can be used like it is a non-value promise (the onResolved delegate can ignore its value), and can be implicitly casted and passed around as such.
Besides casting, you can convert any value promise to a non-value promise and vice-versa via the `Then` method. The type of delegate you pass in will determine the type of the promise that is returned.

```cs
public Promise<TimeSpan> DownloadTest(string url) // Returns a promise that yields how long it took to download the url
{
    Stopwatch watch = Stopwatch.StartNew();
    return Download(url)                        // <---- string promise
        .Then(html =>                           // <---- non-value promise
        {
            watch.Stop(); // Download is done, stop the timer.
            Console.Log("Cool, Google works!");
            // Don't return anything
        })
        .Then(() =>                             // <---- TimeSpan promise
        {
            return watch.Elapsed;               // Return how much time elapsed since the download started.
        });
}
```

If the delegate returns nothing/void, it will become a non-value promise. If it returns an object or value, it will become a value promise of that object/value's type. Likewise, if the delegate returns another promise, the promise returned from `.Then` will adopt the type and state of that promise.

See previous examples that demonstrate returning promises, awaiting promises, and forgetting promises after a promise chain.

## Finally

`Finally` adds an `onFinally` delegate that will be invoked when the promise is resolved, rejected, or canceled. If the promise is rejected, that rejection will _not_ be handled by the finally callback. That way it works just like finally clauses in normal synchronous code. `Finally`, therefore, should be used to clean up resources, like `IDisposable`s.

## ContinueWith

`ContinueWith` adds an `onContinue` delegate that will be invoked when the promise is resolved, rejected, or canceled. A `Promise.ResultContainer` or `Promise<T>.ResultContainer` will be passed into the delegate that can be used to check the promise's state and result or reject reason. The promise returned from `ContinueWith` will be resolved/rejected/canceled with the same rules as `Then` in [Understanding Then](#understanding-then). `Promise.Rethrow` is an invalid operation during an `onContinue` invocation, instead you can use `resultContainer.RethrowIfRejected()` and `resultContainer.RethrowIfCanceled()`

## Forget

All `Promise` and `Promise<T>` objects must either be awaited (using the `await` keyword or `.Then` or `.Catch`, etc or passing into Promise.`All`, `Race`, etc), returned, or forgotten. `promise.Forget()` means you are done with the promise and no more operations will be performed on it. It is the call that allows the backing object to be repooled and uncaught rejections to be reported. The C# compiler will warn when you do not await or use an awaitable object, and calling `Forget()` is the proper way to fix that warning.

## Promises that are already settled

For convenience and optimizations, there are methods to get a promise that is already resolved, rejected, or canceled:

```cs
var resolvedNonValuePromise = Promise.Resolved();

var resolvedStringPromise = Promise.Resolved("That was fast");

var rejectedNonValuePromise = Promise.Rejected("Something went wrong!");

var rejectedIntPromise = Promise<int>.Rejected("Something went wrong!");

var canceledNonValuePromise = Promise.Canceled();

var canceledIntPromise = Promise<int>.Canceled();
```

These are useful if the operation actually completed synchronously and you need to return a promise.

## Additional Information

### Understanding Then

There are 144 overloads for the `Then` method (72 for `Promise` and another 72 for `Promise<T>`). Rather than trying to remember all 144 overloads, it's easier to remember these rules:

- `Then` must always be given at least 1 delegate.
- The first delegate is `onResolved`.
- `onResolved` will be invoked if the promise is resolved.
- If the promise provides a value (`Promise<T>`), onResolved may take that value as an argument.
- If a capture value is provided to `onResolved`, the capture value must be the first argument to `Then` and the first argument to `onResolved`

- A second delegate is optional. If it is provided, it is `onRejected`.
- If `onRejected` does not accept any arguments, it will be invoked if the promise is rejected for any reason.
- If `onRejected` accepts an argument without a capture value, it will be invoked if the promise is rejected with a reason that is convertible to that argument's type.
- If a capture value is provided to `onRejected`, it must come after `onResolved` and before `onRejected` in the `Then` arguments, and it must be the first argument to `onRejected`.
- If a capture value is provided to `onRejected` and that is the only argument `onRejected` accepts, it will be invoked if the promise is rejected for any reason.
- If a capture value is provided to `onRejected` and `onRejected` accepts another argument, it will be invoked if the promise is rejected with a reason that is convertible to the second argument's type.

- If `onResolved` does not return a value, or it returns a non-value `Promise`:
    - the returned promise will be a non-value `Promise`.
    - `onRejected` must not return a value, or it must return a non-value `Promise`.
- If `onResolved` returns a value, or it returns a `Promise<T>`:
    - the returned promise will be a `Promise<T>` of the type of that value (or the same type of promise).
    - `onRejected` must return a value of the same type, or a `Promise<T>` of the same type.
    
- If either `onResolved` or `onRejected` return a promise, the promise returned from `Then` will adopt the state of that promise (waits until it completes).
- If either `onResolved` or `onRejected` throws an `Exception`, the returned promise will be rejected with that exception, unless that exception is one of the [Special Exceptions](special-exceptions.md).

- You may optionally provide a `CancelationToken` as the last parameter.
    - If the token is canceled while the promise is pending, the callback(s) will not be invoked, and the returned promise will be canceled.

You may realize that `Catch(onRejected)` also works just like `onRejected` in `Then`. There is, however, one key difference: with `Then(onResolved, onRejected)`, only one of the callbacks will ever be invoked. With `Then(onResolved).Catch(onRejected)`, both callbacks can be invoked if `onResolved` throws an exception.

Note: It is usually simpler and cleaner to just use async/await. `Then` API is mostly legacy from before async/await was introduced to C#, and remains to adhere to the Promises/A+ spec.