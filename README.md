# ProtoPromise
<a href="https://promisesaplus.com/">
    <img src="https://promisesaplus.com/assets/logo-small.png" alt="Promises/A+ logo"
         title="Promises/A+ 1.0 compliant" align="right" />
</a>
Promise library for C# for management of asynchronous operations.

Made with Unity 3D in mind, but will work for any C# application with a slight modification.

This library took inspiration from <a href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Promise">ES6 Promises</a> (javascript), <a href="https://github.com/Real-Serious-Games/C-Sharp-Promise">RSG Promises</a> (C#), and <a href="https://assetstore.unity.com/packages/tools/upromise-15604">uPromise</a> (C#/Unity) and improved upon their short-comings.

## Contents

- [Promises/A+ Spec](#promisesa-spec)
- [Creating a Promise for an Async Operation](#creating-a-promise-for-an-async-operation)
    - [Creating a Promise, Alternate Method](#creating-a-promise-alternate-method)
- [Waiting for an Async Operation to Complete](#waiting-for-an-async-operation-to-complete)
- [Chaining Async Operations](#chaining-async-operations)
- [Transforming the Results](#transforming-the-results)
- [Promise Types](#promise-types)
- [Error Handling](#error-handling)
    - [Type Matching Error Handling](#type-matching-error-handling)
    - [Caught Error Continuation](#caught-error-continuation)
    - [Unhandled Rejections](#unhandled-rejections)
- [Canceling Promises](#canceling-promises)
    - [Unhandled Cancelations](#unhandled-cancelations)
- [Promises that are already settled](#promises-that-are-already-settled)
- [Progress reporting](#progress-reporting)
- [Combining Multiple Async Operations](#combining-multiple-async-operations)
    - [All Parallel](#all-parallel)
    - [Race Parallel](#race-parallel)
    - [First Parallel](#first-parallel)
    - [Sequence](#sequence)
- [Configuration](#configuration)
- [Advanced](#advanced)
    - [Error Retries](#error-retries)
    - [Promise Retention](#promise-retention)
- [Configuration](#configuration)
- [Additional Information](#additional-information)

## Promises/A+ Spec
This promise library conforms to the <a href="https://promisesaplus.com/">Promises/A+ Spec</a> as far as is possible with C#, and further extends it to support Cancelations and Progress.

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
    var promise = Promise.NewDeferred<string>();    // Create deferred.
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
                promise.Resolve(ev.Result); // Downloaded completed successfully, resolve the promise.
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
                  promise.Resolve(ev.Result); // Downloaded completed successfully, resolve the promise.
              }
          };

          client.DownloadStringAsync(new Uri(url), null); // Initiate async op.
      }
  });
}
```

## Waiting for an Async Operation to Complete ##

The simplest and most common usage is to register a resolve handler to be invoked on completion of the async op:
```cs
Download("http://www.google.com")
    .Then(html => Console.WriteLine(html));
```

This snippet downloads the front page from Google and prints it to the console.

In this case, because the operation can fail, you will also want to register an error hander:
```cs
Download("http://www.google.com")
    .Then(html => Console.WriteLine(html))
    .Catch((Exception error) => Console.WriteLine("An error occured while downloading: " + error));
```

The chain of processing for a promise ends as soon as an error/exception occurs. In this case when an error occurs the *Reject* handler would be called, but not the *Resolve* handler. If there is no error, then only the *Resolve* handler is called.

## Chaining Async Operations

Multiple async operations can be chained one after the other using *Then*:
```cs
Download("http://www.google.com")
    .Then(html => Download(ExtractFirstLink(html))) // Extract the first link and download it and wait for the download to complete.
    .Then(firstLinkHtml => Console.WriteLine(firstLinkHtml))
    .Catch((Exception error) => Console.WriteLine("An error occured while downloading: " + error));
```

Here we are chaining another download onto the end of the first download. The first link in the html is extracted and we then download that. *Then* expects the return value to be another promise. The chained promise can have a different *result type*.

## Transforming the Results

Sometimes you will want to simply transform or modify the resulting value without chaining another async operation.
```cs
Download("http://www.google.com")
    .Then(html => ExtractAllLinks(html)))   // Extract all links and return an array of strings.
    .Then(links =>                      // The input here is an array of strings.
        foreach (var link in links)
        {
            Console.WriteLine(link);
        }
    );
```

As demonstrated, the type of the value can also be changed during transformation. In the previous snippet a `Promise<string>` is transformed to a `Promise<string[]>`.

## Promise Types

Promises may either be a value promise (`Promise<T>`), or a non-value promise (`Promise`). A value promise represents an asynchronous operation that will result in a value, and a non-value promise represents an asynchronous operation that simply does something without returning anything. In games, this is useful for composing and linking animations and other effects.
```cs
RunAnimation("Foo")                         // RunAnimation returns a promise that
    .Then(() => RunAnimation("Bar"))        // is resolved when the animation is complete.
    .Then(() => PlaySound("AnimComplete"));
```

`Promise<T>` inherits from `Promise`, so any value promise can be used like it is a non-value promise (the resolve handler can ignore its value), and can be casted and passed around as such.
Besides casting, you can convert any value promise to a non-value promise and vice-versa via the `Then` method. The type of delegate you pass in will determine the type of the promise that is returned:
```cs
public Promise<TimeSpan> DownloadTest() // Returns a promise that yields how long it took to download google.com
{
    Stopwatch watch = Stopwatch.StartNew();
    return Download("http://www.google.com")    // <---- string promise
        .Then(html =>                           // <---- non-value promise
        {
            watch.Stop(); // Download is done, stop the timer.
            Console.Log("Cool, Google works!");
            // Don't return anything
        }
        .Then(() =>                             // <---- TimeSpan promise
        {
            return watch.Elapsed;               // Return how much time elapsed since the download started.
        };
}
```

If the delegate returns nothing/void, it will become a non-value promise. If it returns an object or value, it will become a value promise of that object/value's type. Likewise, if the delegate returns another promise, the returned promise will adopt the type and state of that promise.

## Error Handling

An error raised in a callback aborts the function and all subsequent callbacks in the chain:
```cs
promise
    .Then(v => Something())                     // <--- An error here aborts all subsequent callbacks...
    .Then(v => SomethingElse())
    .Then(v => AnotherThing())
    .Catch((Exception e) => HandleError(e));    // <--- Until the error handler is invoked here.
```

### Type Matching Error Handling

Promises can be rejected with any type of object or value, so you may decide to filter the type you want to handle:
```cs
rejectedPromise
    .Catch((ArgumentException e) => HandleArgumentError(e))
    .Catch((string e) => HandleStringError(e))
    .Catch((object e) => HandleAnyError(e))
    .Catch(() => HandleError());
```

A rejected reason will propagate down the chain of catches until it encounters a type that it can be assigned to. The very last Catch that does not accept a type or value will catch everything no matter what it is. If an earlier reject handler catches, later catches will not be ran unless another error occurs.

### Caught Error Continuation

When a promise is rejected and that rejection is caught, the next promise is resolved if the rejection handler returns without throwing any exceptions:
```cs
rejectedPromise
    .Catch(() => GetFallbackString())
    .Then((string s) => Console.WriteLine(s));
```

Unlike resolve handlers, which can transform the promise into any other type of promise, reject handlers can only keep the same promise type, or transform it to a non-value promise. Thus, you can also have the reject handler run another async operation and adopt its state the same way we did in [Chaining Async Operations](#chaining-async-operations):
```cs
rejectedPromise
    .Catch(() => DownloadSomethingElse(fallbackUrl));
```

### Unhandled Rejections

When `Catch` is omitted, or none of the filters apply, a System.AggregateException (which contains Promise.UnhandledExceptions that wrap the rejections) is thrown the next time Promise.Manager.HandleCompletes(AndProgress) is called, which happens automatically every frame if you're in Unity.

## Canceling Promises

Promise implementations usually do not allow cancelations, but I thought it would be an invaluable addition to this library.

Promises can be canceled 2 ways: `deferred.Cancel(reason)` or `promise.Cancel(reason)`. When a promise is canceled, all promises that have been chained from it will be canceled with the same reason.
```cs
cancelable = Download("http://www.google.com");     // <---- This will be canceled before the download completes.
cancelable
    .Then(html => Console.Log(html))                // <----
    .Then(() => Download("http://www.bing.com"))    // <---- These will also be canceled and will not run.
    .Then(html => Console.Log(html));               // <----
    
// ... later, before the first download is complete
cancelable.Cancel("Page no longer needed.");
```

Cancelations can be caught similar to how rejections are caught:
```cs
cancelable
    .CatchCancelation((string reason) =>
    {
        Console.Log("Download was canceled! Reason: " + reason);
    };
```

The difference in this case is that CatchCancelation does not return a promise. Cancelations cancel the entire promise chain from the promise that was canceled, and cannot be continued. Also, unlike catching rejections, the cancelation does not stop when it is caught. The delegate is run, then the cancelation continues down the promise chain:
```cs
cancelable
    .CatchCancelation((string reason) =>
    {
        Console.Log("Download was canceled! Reason: " + reason);    // <--- This will run first
    };
cancelable
    .Then(html => Console.Log(html))
    .CatchCancelation(() =>
    {
        Console.Log("Download was canceled for some reason...");    // <--- Then this will run second
    };
```

Cancelations always propagate downwards, and never upwards:
```cs
cancelable =
    Download("http://www.google.com")               // <---- This will *not* be canceled and will run to completion
    .Then(html => Console.Log(html))                // <---- This will also *not* be canceled and will run
    .Then(() => Download("http://www.bing.com"));    // <---- This will be canceled before the download starts and will not run.
cancelable
    .Then(html => Console.Log(html));               // <----This will also be canceled and will not run.
    
// ... later, before the first download is complete
cancelable.Cancel("Page no longer needed.");
```

### Unhandled Cancelations

Unlike rejections, cancelations are considered part of normal program flow, and will not be thrown. Therefore, catching cancelations is entirely optional.

## Promises that are already settled

For convenience and optimizations, there are methods to get promises that are already resolved, rejected, or canceled:
```cs
var resolvedNonValuePromise = Promise.Resolved();

var resolvedStringPromise = Promise.Resolved("That was fast");

var rejectedNonValuePromise = Promise.Rejected("Something went wrong!");

var rejectedIntPromise = Promise.Rejected<int, string>("Something went wrong!");

var canceledNonValuePromise = Promise.Canceled("We don't actually need this anymore.");

var canceledIntPromise = Promise.Canceled<int, string>("We don't actually need this anymore.");
```

This is useful if the operation actually completes synchronously but you still need to return a promise.

## Progress reporting

Promises can additionally report their progress towards completion, allowing the implementor to give the user feedback on the asynchronous operation.

Promises report their progress as a value from `0` to `1`. You can register a progress listener like so:
```cs
promise
    .Progress(progress =>
    {
        progressBar.SetProgress(progress);
        progressText.SetText( ((int) (progress * 100f)).ToString() + "%" );
    };
```

Progress can be reported through the deferred, and if it is reported, progress *must* be reported between 0 and 1 inclusive:
```cs
Promise WaitForSeconds(float seconds)
{
    var deferred = Promise.NewDeferred();
    StartCoroutine(_Countup());
    return deferred.Promise;
    
    IEnumerator _Countup()
    {
        for (float current = 0f; current < seconds; current += Time.Deltatime)
        {
            yield return null;
            deferred.ReportProgress(current / seconds); // Report the progress, normalized between 0 and 1.
        }
        deferred.ReportProgress(1f);
        deferred.Resolve();
    }
}
```

Reporting progress to a deferred is entirely optional, but even if progress is never reported through the deferred, it will always be reported as `1` after the promise resolves.

Progress will always be normalized, no matter how long the promise chain is:
```cs
Download("google.com")                      // <---- This will report 0.0f - 0.25f
    .Then(() => WaitForSeconds(1f))         // <---- This will report 0.25f - 0.5f
    .Then(() => Download("bing.com"))       // <---- This will report 0.5f - 0.75f
    .Then(() => WaitForSeconds(1f))         // <---- This will report 0.75f - 1.0f
    .Progress(progressBar.SetProgress);
    .Then(() => Console.Log("Waited 4 seconds"));
```

## Combining Multiple Async Operations

### All Parallel

The `All` function combines multiple async operations to run in parallel. It converts a collection of promises or a variable length parameter list of promises into a single promise that yields a collection.

Say that each promise yields a value of type T, the resulting promise then yields a collection with values of type T.

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
    });
```

Progress from an All promise will be normalized from all of the input promises.

### Race Parallel

The `Race` function is similar to the `All` function, but it is the first async operation that settles that wins the race and the promise adopts its state.
```cs
Promise.Race(Download("http://www.google.com"), Download("http://www.bing.com"))  // Download each URL.
    .Then(html => Console.Log(html));                       // Both pages are downloaded, but only
                                                            // log the first one downloaded.
```

Progress from a Race promise will be the maximum of those reported by all the input promises.

### First Parallel

The `First` function is almost idential to `Race` except that if a promise is rejected or canceled, the first promise will remain pending until one of the input promises is resolved or they are all rejected/canceled.

### Sequence

The `Sequence` function builds a single promise that wraps multiple sequential operations that will be invoked one after the other.

Multiple promise-yielding functions are provided as input, these are chained one after the other and wrapped in a single promise that is resolved once the sequence has completed.
```cs
var sequence = Promise.Sequence(
    () => RunAnimation("Foo"),
    () => RunAnimation("Bar"),
    () => PlaySound("AnimComplete")
);
```

## Configuration

You can change whether or not objects will be pooled via `Promise.Config.ObjectPooling`. Enabling pooling reduces GC pressure.

If you are in DEBUG mode, you can configure when additional stacktraces will be generated via `Promise.Config.DebugStacktraceGenerator`.

## Advanced

### Error Retries

What I especially love above this system is you can implement retries through a technique I call "Asynchronous Recursion":
```cs
public Promise<string> Download(string url, int maxRetries = 0)
{
    return Download(url)
        .Catch(() =>
        {
            if (maxRetries == 0)
            {
                throw Promise.Rethrow; // Rethrow the rejection without processing it so that the caller can catch it.
            }
            Console.Log($"There was an error downloading {url}, retrying..."); 
            return Download(url, maxRetries - 1);
        };
}
```

Async recursion is just as powerful as regular recursion, but it is also just as dangerous, if not more. If you mess up on regular recursion, your program will immediately crash from a `StackOverflowException`. Async recursion with this library will never crash from a stack overflow due to the iterative implementation, however if you don't do it right, it will eventually crash from an `OutOfMemoryException` due to each call waiting for the next and creating a new promise each time, consuming your heap space.
Because promises can remain pending for an indeterminate amount of time, this error can potentially take a long time to show itself and be difficult to track down. So be very careful when implementing async recursion, and remember to always have a base case!

### Promise Retention

*This is not recommended. See [Promises that are already settled](#promises-that-are-already-settled) for a safer option.*

If for some reason you wish to hold onto a promise reference and re-use it even after it has settled, you may call `promise.Retain();`. Then when you are finished with it, you must call `promise.Release();` and clear your reference `promise = null;`. All retain calls must come before release calls, and they must be made in pairs.

## Additional Information

Almost all calls are asynchronous. This means that calling `promise.Then` will never call the delegate before the method returns, even if the promise is already settled. The same goes for `deferred.Resolve/Reject/Cancel/Progress`. Invoking those methods will not call attached progress or resolve/reject/cancel listeners before the method returns. Callbacks will be invoked later, the next time `Promise.Manager.HandleCompletes(AndProgress)` is called, which happens automatically every frame if you're in Unity.
The exception to this rule is `Retain/Release`. If you release the promise and it is already settled, then it will be put back in the pool (if pooling is enabled) and will no longer be in a usable state. That's why you must always clear your references.
