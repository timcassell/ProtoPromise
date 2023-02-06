# ProtoPromise

<a href="https://promisesaplus.com/">
    <img src="https://promisesaplus.com/assets/logo-small.png" alt="Promises/A+ logo"
         title="Promises/A+ 1.1 compliant" align="right" />
</a>

Robust and efficient library for management of asynchronous operations.

- Allocation-free async operations
- Cancelable operations with custom allocation-free CancelationToken/Source
- Progress with universal automatic or manual normalization
- Full causality traces
- Interoperable with Tasks and Unity's Coroutines
- Thread safe
- .Then API and async/await
- Easily switch to foreground or background context
- Combine async operations
- Circular await detection
- CLS compliant

This library was built to work in all C#/.Net ecosystems, including Unity, Mono, .Net Framework, .Net Core, UI frameworks, and AOT compilation. It is CLS compliant, so it is not restricted to only C#, and will work with any .Net language.

ProtoPromise conforms to the [Promises/A+ Spec](https://promisesaplus.com/) as far as is possible with C# (using static typing instead of dynamic), and further extends it to support Cancelations and Progress.

This library took inspiration from [ES6 Promises](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Promise) (javascript), [RSG Promises](https://github.com/Real-Serious-Games/C-Sharp-Promise) (C#), [uPromise](https://assetstore.unity.com/packages/tools/upromise-15604) (C#/Unity), [TPL](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl), and [UniTask](https://github.com/Cysharp/UniTask) (C#/Unity).

Compare performance to other async libraries:

|         Type | Pending |       Mean |   Gen0 | Allocated | Survived |
|------------- |-------- |-----------:|-------:|----------:|---------:|
| ProtoPromise |   False |   498.1 ns |      - |         - |        - |
|   RsgPromise |   False |   580.7 ns | 0.3290 |    1032 B |    648 B |
|         Task |   False | 2,017.9 ns | 0.3891 |    1224 B |        - |
|      UniTask |   False |   704.6 ns |      - |         - |        - |
| UnityFxAsync |   False | 1,696.3 ns | 0.4139 |    1304 B |    656 B |
|              |         |            |        |           |          |
| ProtoPromise |    True | 2,177.5 ns |      - |         - |    384 B |
|   RsgPromise |    True | 5,011.0 ns | 3.2196 |   10104 B |    728 B |
|         Task |    True | 2,529.4 ns | 0.5112 |    1608 B |     16 B |
|      UniTask |    True | 2,938.6 ns |      - |         - |  3,960 B |
| UnityFxAsync |    True | 2,284.8 ns | 0.4959 |    1560 B |    552 B |

See the [C# Asynchronous Benchmarks Repo](https://github.com/timcassell/CSharpAsynchronousBenchmarks) for a full performance comparison.

## Latest Updates

## v 2.4.1 - February 5, 2023

- Fixed `CancelationToken.IsCancelationRequested` when it was created from a `System.Threading.CancellationToken`.
- Small performance improvement in `CancelationToken.(Try)Register`.

See [Release Notes](ReleaseNotes.md) for the full changelog.

## Contents

- [Package Installation](#package-installation)
    - [Unity](#unity)
    - [Nuget](#nuget)
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
- [Forget](#forget)
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
    - [Cancelations](#cancelations)
        - [Cancelation Source](#cancelation-source)
        - [Cancelation Token](#cancelation-token)
        - [Cancelation Registration](#cancelation-registration)
    - [Canceling Promises](#canceling-promises)
        - [Unhandled Cancelations](#unhandled-cancelations)
    - [Special Exceptions](#special-exceptions)
    - [Error Retries and Async Recursion](#error-retries-and-async-recursion)
    - [Multiple-Consumer](#multiple-consumer)
    - [Capture Values](#capture-values)
    - [Switching Execution Context](#switching-execution-context)
    - [AsyncLocal Support](#asynclocal-support)
    - [Parallel Iterations](#parallel-iterations)
    - [Async Lock](#async-lock)
- [Additional Information](#additional-information)
    - [Understanding Then](#understanding-then)
    - [Finally](#finally)
    - [ContinueWith](#continuewith)
- [Task Interoperability](#task-interoperability)
- [Unity Yield Instructions and Coroutines Interoperability](#unity-yield-instructions-and-coroutines-interoperability)

## Package Installation

### Unity

- Install via Unity's Asset Store

Add to your assets from the Asset Store at https://assetstore.unity.com/packages/tools/integration/protopromise-181997.

- Install via package manager

In the Package Manager, open the dropdown and click on `Add Package from git url` and enter `https://github.com/TimCassell/ProtoPromise.git?path=ProtoPromise_Unity/Assets/Plugins/ProtoPromise`.
Or add `"com.timcassell.protopromise": "https://github.com/TimCassell/ProtoPromise.git?path=ProtoPromise_Unity/Assets/Plugins/ProtoPromise"` to `Packages/manifest.json`.
You may append `#vX.X.X` to use a specific version, for exampe `#v2.0.0`.

- Download unitypackage from GitHub

Go to the latest [release](https://github.com/timcassell/ProtoPromise/releases) and download the unitypackage. Import the unitypackage into your Unity project.

### Nuget

Install from https://www.nuget.org/packages/ProtoPromise/

`dotnet add package ProtoPromise`

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

## Error Handling

An error raised in a callback aborts the function and rejects the promise, and all subsequent onResolved callbacks in the chain are ignored, until an onRejected callback is encountered that matches the rejection.

```cs
void Func()
{
    Download("http://www.google.com")
        .Then(v => { throw new Exception(); return v; })    // <--- An error here aborts all subsequent callbacks...
        .Then(v => DoSomething())
        .Then(() => DoSomethingElse())
        .Catch((Exception e) => HandleError(e))             // <--- Until the error handler is invoked here.
        .Forget();
}
```

When a promise is awaited in an async function and is rejected, the exception behaves similar to a non-async function, skipping the rest of the code up to a `catch` clause that matches the exception.

```cs
async void Func()
{
    try
    {
        string html = await Download("http://www.google.com");  // <--- An error from Download will be thrown here, and aborts all subsequent code...
        await DoSomething();
        DoSomethingElse();
    }
    catch (Exception e)                                         // <--- Until the catch clause is matched to the exception here.
    {
        HandleError(e);
    }
}
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

A rejected reason will propagate down the chain of catches until it encounters a type that it can be assigned to. The very last `Catch` that does not accept a type or value will catch everything no matter what it is. If an earlier reject handler catches, later catches will not be ran unless another error occurs.

Note: the rejected value passed into the onRejected callback will never be null. If a null value is passed into `Deferred.Reject`, it will be transformed into a `NullReferenceException`.

### Caught Error Continuation

When a promise is rejected and that rejection is caught, the next promise is resolved if the rejection handler returns without throwing any exceptions:

```cs
rejectedPromise
    .Catch(() => GetFallbackString())
    .Then((string s) => Console.WriteLine(s));
```

Unlike resolve handlers, which can transform the promise into any other type of promise, reject handlers can only keep the same promise type, or transform it to a non-value promise. You can also have the reject handler run another async operation and adopt its state the same way we did in [Chaining Async Operations](#chaining-async-operations), as long as the other promise's type is the same or non-value.

```cs
rejectedPromise
    .Catch(() => DownloadSomethingElse(fallbackUrl));
```

### Unhandled Rejections

When `Catch` is omitted, or none of the filters apply, an `UnhandledException` is sent to the `Promise.Config.UncaughtRejectionHandler` if it exists. If it does not exist, a `System.AggregateException` (which contains `UnhandledException`s) is thrown on the `Promise.Config.ForegroundContext` if it exists, or `Promise.Config.BackgroundContext` if it does not.

`UnhandledException`s wrap the rejections and contain the full causality trace so you can more easily debug what caused an error to occur in your async functions. Causality traces for `.Then` API is only available in `DEBUG` mode, for performance reasons (See [Compiler Options](#compiler-options)). Exceptions in async functions contain the async stack traces natively.

## Forget

All `Promise` and `Promise<T>` objects must either be awaited (using the `await` keyword or `.Then` or `.Catch`, etc or passing into Promise.`All`, `Race`, etc), returned, or forgotten. `promise.Forget()` means you are done with the promise and no more operations will be performed on it. It is the call that allows the backing object to be repooled and uncaught rejections to be reported. The C# compiler will warn when you do not await or use an awaitable object, and calling `Forget()` is the proper way to fix that warning.

See previous examples that demonstrate returning promises, awaiting promises, and forgetting promises after a promise chain.

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
    })
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
        for (float current = 0f; current < seconds; current += Time.deltaTime)
        {
            yield return null;
            deferred.ReportProgress(current / seconds); // Report the progress, normalized between 0 and 1.
        }
        deferred.Resolve();
    }
}
```

Reporting progress to a deferred is entirely optional, but even if progress is never reported through the deferred, it will always be reported as `1` after the promise is resolved.

Progress will always be automatically normalized from the `.Then` API, no matter how long the promise chain is.

```cs
Download("google.com")                      // <---- This will report 0.0f - 0.25f
    .Then(() => WaitForSeconds(1f))         // <---- This will report 0.25f - 0.5f
    .Then(() => Download("bing.com"))       // <---- This will report 0.5f - 0.75f
    .Then(() => WaitForSeconds(1f))         // <---- This will report 0.75f - 1.0f
    .Progress(progressBar.SetProgress)
    .Then(() => Console.Log("Downloads and extra waits complete."))
    .Forget();
```

Progress must be manually normalized in an `async Promise` function via the `.AwaitWithProgress` API.

```cs
async Promise Func()
{
    await Download("google.com").AwaitWithProgress(0f, 0.25f);      // <---- This will report 0.0f - 0.25f
    await WaitForSeconds(1f).AwaitWithProgress(0.25f, 0.5f);        // <---- This will report 0.25f - 0.5f
    await Download("bing.com").AwaitWithProgress(0.5f, 0.75f);      // <---- This will report 0.5f - 0.75f
    await WaitForSeconds(1f).AwaitWithProgress(0.75f, 1f);          // <---- This will report 0.75f - 1.0f
}
```

or

```cs
async Promise Func()
{
    await Download("google.com").AwaitWithProgress(0.25f);      // <---- This will report 0.0f - 0.25f
    await WaitForSeconds(1f).AwaitWithProgress(0.5f);           // <---- This will report 0.25f - 0.5f
    await Download("bing.com").AwaitWithProgress(0.75f);        // <---- This will report 0.5f - 0.75f
    await WaitForSeconds(1f).AwaitWithProgress(1f);             // <---- This will report 0.75f - 1.0f
}
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
    })
```

Progress from an All promise will be normalized from all of the input promises.

### Merge Parallel

The `Merge` function behaves just like the `All` function, except that it can be used to combine multiple types, and instead of yielding an `IList<T>`, it yields a `ValueTuple<>` that contains the types of the promises provided to the function.

```cs
Promise.Merge(Download("http://www.google.com"), DownloadImage("http://www.example.com/image.jpg"))  // Download HTML and image.
    .Then(values =>                         // Receives ValueTuple<string, Texture>.
    {
        Console.WriteLine(values.Item1);    // Print the HTML.
        image.SetTexture(values.Item2);     // Assign the texture to an image object.
    })
```

### Race Parallel

The `Race` function is similar to the `All` function, but it is the first async operation that settles that wins the race and the promise adopts its state.

```cs
Promise.Race(Download("http://www.google.com"), Download("http://www.bing.com"))  // Download each URL.
    .Then(html => Console.Log(html))                        // Both pages are downloaded, but only
                                                            // log the first one downloaded.
```

Progress from a Race promise will be the maximum of those reported by all the input promises.

### First Parallel

The `First` function is almost idential to `Race` except that if a promise is rejected or canceled, the First promise will remain pending until one of the input promises is resolved or they are all rejected/canceled.

### Sequence

The `Sequence` function builds a single promise that wraps multiple sequential operations that will be invoked one after the other.

Multiple promise-returning functions are provided as input, these are chained one after the other and wrapped in a single promise that is resolved once the sequence has completed.

```cs
var sequencePromise = Promise.Sequence(
    () => RunAnimation("Foo"),
    () => RunAnimation("Bar"),
    () => PlaySound("AnimComplete")
);
```

## Configuration

You can change whether or not objects will be pooled via `Promise.Config.ObjectPoolingEnabled`. Enabling pooling reduces GC pressure, and it is enabled by default.

If you are in DEBUG mode, you can configure when additional stacktraces will be generated via `Promise.Config.DebugCausalityTracer`.

`Promise.Config.UncaughtRejectionHandler` allows you to route unhandled rejections through a delegate instead of being thrown.

`Promise.Config.ForegroundContext` is the context to which foreground operations are posted, typically used to marshal work to the UI thread. This is automatically set in Unity, but in other UI frameworks it should be set at application startup (usually `Promise.Config.ForegroundContext = SynchronizationContext.Current` is enough). Note: if your application uses multiple `SynchronizationContext`s, you should instead pass the context directly to the `WaitAsync` and other APIs instead of using `SynchronizationOption.Foreground`. See [Switching Execution Context](#switching-execution-context).

`Promise.Config.BackgroundContext` can be set to override how background operations are executed. If this is null, `ThreadPool.QueueUserWorkItem(callback, state)` is used.

`Promise.Config.AsyncFlowExecutionContextEnabled` can be set to true to enable [AsyncLocal support](#asynclocal-support).

### Compiler Options

If you're compiling from source (not from dll), you can configure some compilation options.

Progress can be disabled if you don't intend to use it and want to save a little memory/cpu cycles.
You can disable progress by adding `PROTO_PROMISE_PROGRESS_DISABLE` to your compiler symbols.

By default, debug options are tied to the `DEBUG` compiler symbol, which is defined by default in the Unity Editor and not defined in release builds. You can override that by defining `PROTO_PROMISE_DEBUG_ENABLE` to force debugging on in release builds, or `PROTO_PROMISE_DEBUG_DISABLE` to force debugging off in debug builds (or in the Unity Editor). If both symbols are defined, `ENABLE` takes precedence.

## Advanced

### Cancelations

Cancelation tokens are primarily used to cancel promises, but can be used to cancel anything. They come in 3 parts: `CancelationSource`, `CancelationToken`, and `CancelationRegistration`.

#### Cancelation Source

A `CancelationSource` is what is used to actually cancel a token. When a consumer wants to cancel a producer's operation, it creates a `CancelationSource` via `CancelationSource.New()` and caches it somewhere (usually in a private field). When it determines it no longer needs the result of the operation, it calls `CancelationSource.Cancel()`.

When you are sure that the operation has been fully cleaned up, you must dispose of the source: `CancelationSource.Dispose()`. This usually makes most sense to do it in a promise's [Finally](#finally) callback (or try/finally clause in an async function, or you can use the `using` keyword).

You can get the token to pass to the producer from the `CancelationSource.Token` property.

#### Cancelation Token

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

#### Cancelation Registration

When you register a callback to a token, it returns a `CancelationRegistration` which can be used to unregister the callback.

```cs
CancelationRegistration registration = token.Register(() => Console.Log("This won't get called."));

// ... later, before the source is canceled
registration.TryUnregister();
```

If the registration is unregistered before the source is canceled, the callback will not be invoked. Once a registration has been unregistered, it cannot be re-registered. You must register a new callback to the token if you wish to do so.

You may also `Dispose` the registration to unregister the callback, or wait for it to complete if it was already invoked.

### Canceling Promises

Promise implementations usually do not allow cancelations, but it has proven to be invaluable to asynchronous libraries, and ProtoPromise is no exception.

Promises can be canceled 2 ways: passing a `CancelationToken` into `Promise.{Then, Catch, ContinueWith}` or `Promise.NewDeferred`, or by throwing a [Cancelation Exception](#special-exceptions). When a promise is canceled, all promises that have been chained from it will be canceled, until a `CatchCancelation`.

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

#### Unhandled Cancelations

Unlike rejections, cancelations are considered part of normal program flow, and will not be thrown outside of `async` functions. Therefore, catching cancelations is entirely optional.

### Special Exceptions

Normally, an `Exception` thrown in an `onResolved` or `onRejected` callback will reject the promise with that exception. There are, however, a few special exceptions that can be thrown to produce different behaviour:

#### Rethrow

`throw Promise.Rethrow` can be used if you want to do something if a rejection occurs, but not suppress that rejection. Throwing `Promise.Rethrow` will rethrow that rejection, preserving its stacktrace (if applicable). This works just like `throw;` in synchronous catch clauses. This is only valid when used in `onRejected` callbacks. If accessed in other contexts, it will throw an `InvalidOperationException`.

#### RejectException

`throw Promise.RejectException(reason)` can be used to reject the promise with a reason that is not an `Exception`. If reason is an `Exception`, you may want to just throw it directly, unless you want to preserve its stacktrace.

#### CancelException

`throw Promise.CancelException()` can be used to cancel the promise. You can also throw an `OperationCanceledException`, which is equivalent.

### Error Retries and Async Recursion

What I especially love above this system is you can implement retries through asynchronous recursion.

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

Even though the recursion can go extremely deep or shallow, the promise's progress will still be normalized between 0 and 1. Though, a caveat to this is if the first attempt succeeds, the progress will go up to 0.5, then immediately jump to 1. Otherwise you might notice it behave like 0.5, 0.75, 0.875, 0.9375, ...

This can also be done with async functions.

```cs
public async Promise<string> Download(string url, int maxRetries = 0)
{
    try
    {
        return await Download(url).AwaitWithProgress(0f, 1f);
    }
    catch
    {
        if (maxRetries <= 0)
        {
            throw; // Rethrow the rejection without processing it so that the caller can catch it.
        }
        Console.Log($"There was an error downloading {url}, retrying..."); 
        return await Download(url, maxRetries - 1).AwaitWithProgress(0f, 1f);
    }
}
```

Async recursion is just as powerful as regular recursion, but it is also just as dangerous. If you mess up on regular recursion, your program will immediately crash from a `StackOverflowException`. You may prevent stack overflows with async recursion by using a [context switching API](#switching-execution-context) with the `forceAsync` flag set to true. However, you should be aware that if your async recursion continues forever, your program will eventually crash from an `OutOfMemoryException` due to each call waiting for the next and creating a new promise each time, consuming your heap space.
Because promises can remain pending for an indeterminate amount of time, this error can potentially take a long time to show itself and be difficult to track down. So be very careful when implementing async recursion, and remember to always have a base case!

Of course, async functions are powerful enough where this retry behavior can be done in a loop without blowing up the heap.

```cs
public async Promise<string> Download(string url, int maxRetries = 0)
{
Retry:
    try
    {
        return await Download(url).AwaitWithProgress(0f, 1f);
    }
    catch
    {
        if (--maxRetries < 0)
        {
            throw; // Rethrow the rejection without processing it so that the caller can catch it.
        }
        Console.Log($"There was an error downloading {url}, retrying..."); 
        goto Retry;
    }
}
```

### Multiple-Consumer

Most promises can only be awaited once, and if they are not awaited, they must be returned or forgotten (see [Forget](#forget)).
You can preserve a promise so that it can be awaited multiple times via the `promise.Preserve()` API. When you are finished with the promise, you must call `promise.Forget()`.
Callbacks added to a preserved promise will be invoked in the order that they are added.

Note: a preserved promise should not be returned from a public API, because the consumer could immediately call `Forget()` and invalidate the promise. Instead, you should use `promise.Duplicate()` to get a promise that will adopt its state, but can only be awaited once.

### Capture Values

The C# compiler allows capturing variables inside delegates, known as closures. This involves creating a new object and a new delegate for every closure. These objects will eventually need to be garbage collected when the delegate is no longer reachable.

To solve this issue, capture values was added to the library. Every method that accepts a delegate can optionally take any value as a parameter, and pass that value as the first argument to the delegate. To capture multiple values, you should pass a `System.ValueTuple<>` that contains the values you wish to capture. The error retry example can be rewritten to reduce allocations:

```cs
public Promise<string> Download(string url, int maxRetries = 0)
{
    return Download(url)
        .Catch((url, maxRetries), cv => // Capture url and maxRetries in a System.ValueTuple<string, int>
        {
            var (_url, retryCount) = cv; // Deconstruct the value tuple (C# 7 feature)
            if (retryCount <= 0)
            {
                throw Promise.Rethrow; // Rethrow the rejection without processing it so that the caller can catch it.
            }
            Console.Log($"There was an error downloading {_url}, retrying..."); 
            return Download(_url, retryCount - 1);
        });
}
```

When the C# compiler sees a lamda expression that does not capture/close any variables, it will cache the delegate statically, so there is only one instance in the program, and no extra memory will be allocated every time it's used.

Note: Visual Studio will tell you what variables are captured/closed if you hover the `=>`. You can use that information to optimize your delegates. In C# 9 and later, you can use the `static` modifier on your lambdas so that the compiler will not let you accidentally capture variables the expensive way.

See [Understanding Then](#understanding-then) for information on all the different ways you can capture values with the `Then` overloads.

### Switching Execution Context

Context switching in this case refers to switching execution between the main/UI thread and background threads. Executing code on a background thread frees up the UI thread to draw the application at a higher frame-rate and not freeze the application when executing an expensive computation.

Promise continuations (`.Then` or `await`) normally execute synchronously, not caring what thread they are executing on. However, you can force continuations to execute either on the foreground context for UI code, or background context for expensive non-UI code. You can use the `promise.WaitAsync(SynchronizationOption)` to force the next continuation to execute on the given context (`Synchronous` (default), `Foreground`, or `Background`).

```cs
async void Func()
{
    // Not sure what thread we're on here...
    await DoSomethingAsync()
        .WaitAsync(SynchronizationOption.Background);
    // Now we're executing in the background.
    Console.Log("Thread is background: " + Thread.CurrentThread.IsBackground); // true
    await DoSomethingElseAsync()
        .WaitAsync(SynchronizationOption.Foreground);
    // Now we're executing in the foreground (UI thread).
    Console.Log("Thread is background: " + Thread.CurrentThread.IsBackground); // false
}
```

```cs
void Func()
{
    // Not sure what thread we're on here...
    DoSomethingAsync()
        .WaitAsync(SynchronizationOption.Background)
        .Then(() =>
        {
            // Now we're executing in the background.
            Console.Log("Thread is background: " + Thread.CurrentThread.IsBackground); // true
            return DoSomethingElseAsync();
        }
        .WaitAsync(SynchronizationOption.Foreground)
        .Then(() =>
        {
            // Now we're executing in the foreground (UI thread).
            Console.Log("Thread is background: " + Thread.CurrentThread.IsBackground); // false
        })
        .Forget();
}
```

To make things a little easier, there are shortcut functions to simply hop to the foreground or background context: `Promise.SwitchToForeground()` and `Promise.SwitchToBackground()`.

The `Foreground` option posts the continuation to `Promise.Config.ForegroundContext`. This property is set automatically in Unity, but in other UI frameworks it should be set at application startup (usually `Promise.Config.ForegroundContext = SynchronizationContext.Current` is enough).

If your application uses multiple `SynchronizationContext`s, instead of using `SynchronizationOption.Foreground`, you should pass the proper `SynchronizationContext` directly to `WaitAsync`.

Other APIs that allow you to pass `SynchronizationOption` or `SynchronizationContext` to configure the context that the callback executes on are `Promise.Progress` (default `Foreground`), `Promise.New` (default `Synchronous`), and `Promise.Run` (default `Background`).

### AsyncLocal Support

`AsyncLocal<T>` is supported in `async Promise` functions, but it is disabled by default because it makes execution more expensive and causes allocations in older runtimes. It can be enabled by setting `Promise.Config.AsyncFlowExecutionContextEnabled = true`.

```cs
private AsyncLocal<int> _asyncLocal = new AsyncLocal<int>();

public async Promise Func()
{
    _asyncLocal.Value = 1;

    await FuncNested();

    Assert.AreEqual(1, _asyncLocal.Value);
}

private async Promise FuncNested()
{
    Assert.AreEqual(1, _asyncLocal.Value);
    _asyncLocal.Value = 2;

    await _promise;

    Assert.AreEqual(2, _asyncLocal.Value);
}
```

### Parallel Iterations

You may have a very large collection that you need to iterate as quickly as possible without blocking the current thread. A `foreach` loop will do the job, but it will only run each iteration sequentially on the current thread.
`Parallel.ForEach` will do the job concurrently, possibly utilizing multiple threads, but it will still block the current thread until all iterations are complete. `Promise.ParallelForEach` to the rescue!

```cs
public static Promise IterateAsync<T>(this IEnumerable<T> enumerable, Action<T> action)
{
    return Promise.ParallelForEach(enumerable, (item, cancelationToken) =>
    {
        action(item);
        return Promise.Resolved();
    });
}
```

This will run each iteration concurrently without blocking the current thread.

You may also wish to parallelize a `for` loop, which you can do with `Promise.ParallelFor`.

```cs
public static Promise ForAsync(int min, int max, Action<int> action)
{
    return Promise.ParallelFor(min, max, (item, cancelationToken) =>
    {
        action(item);
        return Promise.Resolved();
    });
}
```

(`Promise.ParallelForEach` is similar to `Parallel.ForEachAsync` in .Net 6+, but uses `Promise` instead of `Task` to be more efficient, and works in older runtimes. And even in newer runtimes, `Parallel.ForAsync` does not exist.)

### Async Lock

`AsyncLock` (in the `Proto.Promises.Threading` namespace) can be used to coordinate mutual exclusive execution around asynchronous resources. It is similar to the normal `lock` keyword, except `lock` cannot be used around `await`s.
The syntax is a little different: instead of `lock (mutex) { }`, the syntax is `using (await mutex.LockAsync()) { }`. Everything inside the `using` block will be protected by the lock.

```cs
private readonly AsyncLock _mutex = new AsyncLock();

public async Promise DoStuffAsync()
{
    using (await _mutex.LockAsync())
    {
        // Do mutually exclusive async work here.
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
}
```

You may also use the `AsyncMonitor` class to `WaitAsync` and `Pulse` in a fashion very similar to the normal `Monitor` class.

```cs
private readonly AsyncLock _mutex = new AsyncLock();

public async Promise DoStuffAsync()
{
    using (var key = await _mutex.LockAsync())
    {
        // Wait for a pulse.
        await AsyncMonitor.WaitAsync(key);
        // Continue after another context has pulsed the lock.
    }
}
```

`AsyncLock` also supports synchronous locks, so a thread may try to enter a lock synchronously, even while another thread is holding it asynchronously. `using (_mutex.Lock()) { }`
WARNING: doing so may cause a deadlock if the async function that holds the lock tries to switch context to the same thread before releasing the lock. Although it is supported, it is not recommended to mix synchronous and asynchronous locks. If at all possible, normal locks should be preferred.

WARNING: `AsyncLock` does _not_ support re-entrance (recursion). If you attempt to enter the lock while it is already entered, it will result in a deadlock.

Note: `AsyncLock` and `AsyncMonitor` are only available in .Net Standard 2.1 (or Unity 2021.2) or newer platforms.

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
- If either `onResolved` or `onRejected` throws an `Exception`, the returned promise will be rejected with that exception, unless that exception is one of the [Special Exceptions](#special-exceptions).

- You may optionally provide a `CancelationToken` as the last parameter.
    - If the token is canceled while the promise is pending, the callback(s) will not be invoked, and the returned promise will be canceled.

You may realize that `Catch(onRejected)` also works just like `onRejected` in `Then`. There is, however, one key difference: with `Then(onResolved, onRejected)`, only one of the callbacks will ever be invoked. With `Then(onResolved).Catch(onRejected)`, both callbacks can be invoked if `onResolved` throws an exception.

### Finally

`Finally` adds an `onFinally` delegate that will be invoked when the promise is resolved, rejected, or canceled. If the promise is rejected, that rejection will _not_ be handled by the finally callback. That way it works just like finally clauses in normal synchronous code. `Finally`, therefore, should be used to clean up resources, like `IDisposable`s.

### ContinueWith

`ContinueWith` adds an `onContinue` delegate that will be invoked when the promise is resolved, rejected, or canceled. A `Promise.ResultContainer` or `Promise<T>.ResultContainer` will be passed into the delegate that can be used to check the promise's state and result or reject reason. The promise returned from `ContinueWith` will be resolved/rejected/canceled with the same rules as `Then` in [Understanding Then](#understanding-then). `Promise.Rethrow` is an invalid operation during an `onContinue` invocation, instead you can use `resultContainer.RethrowIfRejected()` and `resultContainer.RethrowIfCanceled()`

## Task Interoperability

Promises can easily interoperate with Tasks simply by calling the `Promise.ToTask()` or `Task.ToPromise()` extension methods.

Promises can also be converted to ValueTasks by calling `Promise.AsValueTask()` method, or by implicitly casting `ValueTask valueTask = promise`. ValueTasks can be converted to Promises by calling the `ValueTask.ToPromise()` extension method.

`Proto.Promises.CancelationToken` can be converted to and from `System.Threading.CancellationToken` by calling `token.ToCancellationToken()` method or `token.ToCancelationToken()` extension method.

## Unity Yield Instructions and Coroutines Interoperability

If you are using coroutines, you can easily convert a promise to a yield instruction via `promise.ToYieldInstruction()` which you can yield return to wait until the promise has settled. You can also convert any yield instruction (including coroutines themselves) to a promise via `PromiseYielder.WaitFor(yieldInstruction)`. This will wait until the yieldInstruction has completed before resolving the promise.

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
