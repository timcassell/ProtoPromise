# Error Handling

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

## Type Matching Error Handling

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

## Caught Error Continuation

When a promise is rejected and that rejection is caught, the next promise is resolved if the rejection handler returns without throwing any exceptions:

```cs
rejectedPromise
    .Catch(() => GetFallbackString())
    .Then((string s) => Console.WriteLine(s));
```

Unlike resolve handlers, which can transform the promise into any other type of promise, reject handlers can only keep the same promise type, or transform it to a non-value promise. You can also have the reject handler run another async operation and adopt its state the same way we did in [Chaining Async Operations](the-basics.md#chaining-async-operations), as long as the other promise's type is the same or non-value.

```cs
rejectedPromise
    .Catch(() => DownloadSomethingElse(fallbackUrl));
```

## Unhandled Rejections

When `Catch` is omitted, or none of the filters apply, an `UnhandledException` is sent to the `Promise.Config.UncaughtRejectionHandler` if it exists. If it does not exist, a `System.AggregateException` (which contains `UnhandledException`s) is thrown on the `Promise.Config.ForegroundContext` if it exists, or `Promise.Config.BackgroundContext` if it does not.

`UnhandledException`s wrap the rejections and contain the full causality trace so you can more easily debug what caused an error to occur in your async functions. Causality traces for `.Then` API is only available in `DEBUG` mode, for performance reasons (See [Compiler Options](configuration.md#compiler-options)). Exceptions in async functions contain the async stack traces natively.

## Suppress Throws

Normally when you await a promise in an `async Promise` function, it will throw if it was canceled or rejected. You can suppress that and manually check the state and rethrow if you need to. Use `promise.AwaitNoThrow()`. The awaited result will be `Promise.ResultContainer` or `Promise<T>.ResultContainer` that wraps the state and result or reject reason of the promise.

```cs
var resultContainer = await promise.AwaitNoThrow();
resultContainer.RethrowIfRejected();
bool isCanceled = resultContainer.State == Promise.State.Canceled;
var result = resultContainer.Value;
```