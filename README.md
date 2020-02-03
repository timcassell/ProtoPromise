# ProtoPromise
<a href="https://promisesaplus.com/">
    <img src="https://promisesaplus.com/assets/logo-small.png" alt="Promises/A+ logo"
         title="Promises/A+ 1.1 compliant" align="right" />
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
    - [Merge Parallel](#merge-parallel)
    - [Race Parallel](#race-parallel)
    - [First Parallel](#first-parallel)
    - [Sequence](#sequence)
- [Configuration](#configuration)
    - [Compiler Options](#compiler-options)
- [Advanced](#advanced)
    - [Special Exceptions](#special-exceptions)
    - [Error Retries](#error-retries)
    - [Multiple Callbacks](#multiple-callbacks)
    - [Capture Values](#capture-values)
    - [Promise Retention](#promise-retention)
- [Additional Information](#additional-information)
    - [Understanding Then](#understanding-then)
    - [Complete and Finally](#complete-and-finally)
    - [Unity Yield Instructions and Coroutines](#unity-yield-instructions-and-coroutines)

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

Promises can be canceled 3 ways: `deferred.Cancel(reason)` or `promise.Cancel(reason)` or with a [Special Exception](#special-exceptions). When a promise is canceled, all promises that have been chained from it will be canceled with the same reason.
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

Unless CatchCancelations are chain added, then it acts like typical catch clauses in synchronous code:
```cs
cancelable
    .CatchCancelation((string reason) =>
    {
        Console.Log("Download was canceled! Reason: " + reason);        // <--- This will run if reason is a string.
    }
    .CatchCancelation(() =>
    {
        Console.Log("This will only run if reason is not a string.")    // <--- This will not run if reason is a string.
    };
```

Cancelations always propagate downwards, and never upwards:
```cs
cancelable =
    Download("http://www.google.com")               // <---- This will *not* be canceled and will run to completion
    .Then(html => Console.Log(html))                // <---- This will also *not* be canceled and will run
    .Then(() => Download("http://www.bing.com"));   // <---- This will be canceled before the download starts and will not run.
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
    .Then(() => Console.Log("Downloads and extra waits complete."));
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

### Merge Parallel

The `Merge` function behaves just like the `All` function, except that it can be used to combine multiple types, and instead of yielding an `IList<T>`, it yields a `ValueTuple<>` that contains the types of the promises provided to the function.
```cs
Promise.Merge(Download("http://www.google.com"), DownloadImage("http://www.example.com/image.jpg"))  // Download HTML and image.
    .Then(values =>                         // Receives ValueTuple<string, Texture>.
        Console.WriteLine(values.Item1);    // Print the HTML.
        image.SetTexture(values.Item2);     // Assign the texture to an image object.
    );
```

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

### Compiler Options

Cancelations and Progress can be disabled if you don't intend to use them and want to save a little memory/cpu cycles.
You can disable cancelations by adding `PROTO_PROMISE_CANCEL_DISABLE` to your compiler symbols.
Similarly, you can disable progress by adding `PROTO_PROMISE_PROGRESS_DISABLE` to your compiler symbols.

By default, debug options are tied to the `DEBUG` compiler symbol, which is defined by default in the Unity Editor and not defined in release builds. You can override that by defining `PROTO_PROMISE_DEBUG_ENABLE` to force debugging on in release builds, or `PROTO_PROMISE_DEBUG_DISABLE` to force debugging off in debug builds (or in the Unity Editor). If both symbols are defined, `ENABLE` takes precedence.

## Advanced

### Special Exceptions

Normally, an `Exception` thrown in an `onResolved` or `onRejected` callback will reject the promise with that exception. There are, however, a few special exceptions that can be thrown to produce different behaviour:

#### Rethrow

`throw Promise.Rethrow` can be used if you want to do something if a rejection occurs, but not suppress that rejection. Throwing `Promise.Rethrow` will rethrow that rejection, preserving its stacktrace (if applicable). This works just like `throw;` in synchronous catch clauses. This is only valid when used in `onRejected` callbacks. If accessed in other contexts, it will throw an `InvalidOperationException`.

#### RejectException

`throw Promise.RejectException(reason)` can be used to reject the promise with a reason that is not an `Exception`. If reason is an `Exception`, you can just throw that reason directly. This is only valid when used in `onResolved`, `onRejected`, or `onComplete` callbacks. If accessed in other contexts, it will throw an `InvalidOperationException`.

#### CancelException

`throw Promise.CancelException(reason)` can be used to cancel the promise with any reason, or `throw Promise.CancelException()` to cancel the promise without a reason. You can also throw an `OperationCanceledException`, which is equivalent to `Promise.CancelException()`. `Promise.CancelException` functions are only valid when used in `onResolved`, `onRejected`, or `onComplete` callbacks. If accessed in other contexts, it will throw an `InvalidOperationException`.

### Error Retries

What I especially love above this system is you can implement retries through a technique I call "Asynchronous Recursion":
```cs
public Promise<string> Download(string url, int maxRetries = 0)
{
    return Download(url)
        .Catch(() =>
        {
            if (maxRetries <= 0)
            {
                throw Promise.Rethrow; // Rethrow the rejection without processing it so that the caller can catch it.
            }
            Console.Log($"There was an error downloading {url}, retrying..."); 
            return Download(url, maxRetries - 1);
        };
}
```

Even though the recursion can go extremely deep or shallow, the promise's progress will still be normalized between 0 and 1. Though you might notice it behave like 0.5, 0.75, 0.875, 0.9375,...

Async recursion is just as powerful as regular recursion, but it is also just as dangerous, if not more. If you mess up on regular recursion, your program will immediately crash from a `StackOverflowException`. Async recursion with this library will never crash from a stack overflow due to the iterative implementation, however if you don't do it right, it will eventually crash from an `OutOfMemoryException` due to each call waiting for the next and creating a new promise each time, consuming your heap space.
Because promises can remain pending for an indeterminate amount of time, this error can potentially take a long time to show itself and be difficult to track down. So be very careful when implementing async recursion, and remember to always have a base case!

### Multiple Callbacks

Multiple callbacks can be added to a single promise object which will be invoked in the order that they are added. Adding multiple resolve or reject callbacks creates promise branches. Sometimes you might want to use a single promise for multiple consumers. One or more of those consumers might want to cancel the promise, and if one consumer cancels the base promise, all branches will be canceled. In order to solve this issue, you can use `promise.ThenDuplicate()` to get a new promise that will adopt the state of the base promise, which can be canceled without canceling all the other branches.

### Capture Values

The C# compiler allows capturing variables inside delegates, known as closures. This involves creating a new object and a new delegate for every closure. These objects will eventually need to be garbage collected when the delegate is no longer reachable.

To solve this issue, I added capture values to the library. Every method that accepts a delegate can optionally take any value as a parameter, and pass that value as the first argument to the delegate. To capture multiple values, you should pass a `System.ValueTuple<>` that contains the values you wish to capture. The error retry example can be rewritten to reduce allocations:

```cs
public Promise<string> Download(string url, int maxRetries = 0)
{
    return Download(url)
        .Catch((url, maxRetries), cv =>
        {
            var (_url, retryCount) = cv; // Deconstruct the value tuple (C# 7 feature)
            if (retryCount <= 0)
            {
                throw Promise.Rethrow; // Rethrow the rejection without processing it so that the caller can catch it.
            }
            Console.Log($"There was an error downloading {_url}, retrying..."); 
            return Download(_url, retryCount - 1);
        };
}
```

When the C# compiler sees a lamda expression that does not capture/close any variables, it will cache the delegate statically, so there is only one instance in the program. If the lambda only captures `this`,  it's not quite as bad as capturing local variables, as the compiler will generate a cached delegate in the class. This means there is one delegate per instance. We can reduce that to one instance in the program by passing `this` as the capture value.

Visual Studio will tell you what variables are captured/closed if you hover the `=>`. You can use that information to optimize your delegates.

See [Understanding Then](#understanding-then) for information on all the different ways you can capture values with the `Then` overloads.

### Promise Retention

*This is not recommended. See [Promises that are already settled](#promises-that-are-already-settled) for a safer option.*

If for some reason you wish to hold onto a promise reference and re-use it even after it has settled, you may call `promise.Retain();`. Then when you are finished with it, you must call `promise.Release();` and clear your reference `promise = null;`. All retain calls must come before release calls, and they must be made in pairs.

## Additional Information

Almost all calls are asynchronous. This means that calling `promise.Then` will never call the delegate before the method returns, even if the promise is already settled. The same goes for `deferred.Resolve/Reject/Cancel/Progress`. Invoking those methods will not call attached progress or resolve/reject/cancel listeners before the method returns. Callbacks will be invoked later, the next time `Promise.Manager.HandleCompletes(AndProgress)` is called, which happens automatically every frame if you're in Unity.
The exception to this rule is `Retain/Release`. If you release the promise and it is already settled, then it will be put back in the pool (if pooling is enabled) and will no longer be in a usable state. That's why you must always clear your references.

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
- If either `onResolved` or `onRejected` throws an `Exception`, the returned promise will be rejected with that exception, unless that exception is one of the [Special Exceptions](#special-exceptions).

You may realize that `Catch(onRejected)` also works just like `onRejected` in `Then`. There is, however, one key difference: with `Then(onResolved, onRejected)`, only one of the callbacks will ever be invoked. With `Then(onResolved).Catch(onRejected)`, both callbacks can be invoked if `onResolved` throws.

### Complete and Finally

`Complete` adds an `onComplete` delegate that will be invoked if/when the promise is resolved or rejected. This is logically equivalent to Then(onComplete, onComplete), but more efficient. If the promise is rejected, that rejection will be caught and handled, and the returned promise will be resolved with the same rules as `onRejected` in [Understanding Then](#understanding-then).

`Finally` adds an `onFinally` delegate that will be invoked when the promise is resolved, rejected, or canceled. Unlike `Complete`, if the promise is rejected, that rejection will _not_ be handled by the finally callback. That way it works just like finally clauses in normal synchronous code. `Finally`, therefore, should be used to clean up resources, like `IDisposable`s.

### Unity Yield Instructions and Coroutines

If you are using coroutines, you can easily convert a promise to a yield instruction via `promise.ToYieldInstruction()` which you can yield return to wait until the promise has settled. You can also convert any yield instruction (including coroutines themselves) to a promise via `PromiseYielder.WaitFor(yieldInstruction)`. This will wait until the yieldInstruction has completed and provide the same instruction to an onResolved callback.
```cs
public Promise<Texture2D> DownloadTexture(string url)
{
    var www = UnityWebRequestTexture.GetTexture(url);
    return PromiseYielder.WaitFor(www.SendWebRequest())
        .Then(asyncOperation =>
        {
            if (asyncOperation.webRequest.isHttpError || asyncOperation.webRequest.isNetworkError)
            {
                throw Promise.RejectException(asyncOperation.webRequest.error);
            }
            return ((DownloadHandlerTexture) asyncOperation.webRequest.downloadHandler).texture;
        })
        .Finally(www.Dispose);
}
```

In this example, we use the `Finally` method to dispose the UnityWebRequest.
