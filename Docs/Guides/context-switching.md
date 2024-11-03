# Switching Execution Context

Context switching in this case refers to switching execution between the main/UI thread and background threads. Executing code on a background thread frees up the UI thread to draw the application at a higher frame-rate and not freeze the application when executing an expensive computation.

Promise continuations (`.Then` or `await`) execute synchronously by default, not caring what thread they are executing on. However, you can force continuations to execute either on the foreground context for UI code, or background context for expensive non-UI code. You can use the `promise.ConfigureAwait(ContinuationOptions)` or `promise.ConfigureContinuation(ContinuationOptions)` APIs to force the continuation behavior.

```cs
async void Func()
{
    // Not sure what thread we're on here...
    await DoSomethingAsync()
        .ConfigureAwait(ContinuationOptions.Background);
    // Now we're executing in the background.
    Console.Log("Thread is background: " + Thread.CurrentThread.IsBackground); // true
    await DoSomethingElseAsync()
        .ConfigureAwait(ContinuationOptions.Foreground);
    // Now we're executing in the foreground (UI thread).
    Console.Log("Thread is background: " + Thread.CurrentThread.IsBackground); // false
}
```

```cs
void Func()
{
    // Not sure what thread we're on here...
    DoSomethingAsync()
        .ConfigureContinuation(ContinuationOptions.Background)
        .Then(() =>
        {
            // Now we're executing in the background.
            Console.Log("Thread is background: " + Thread.CurrentThread.IsBackground); // true
            return DoSomethingElseAsync();
        }
        .ConfigureContinuation(ContinuationOptions.Foreground)
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

If your application uses multiple `SynchronizationContext`s, instead of using `SynchronizationOption.Foreground`, you may want to pass the proper `SynchronizationContext` directly to `Promise.SwitchToContext`, or `ContinuationOptions.CapturedContext` to `Promise.ConfigureAwait`.

For context switching optimized for async/await, use `Promise.SwitchToForegroundAwait`, `Promise.SwitchToBackgroundAwait`, and `Promise.SwitchToContextAwait`. These functions return custom awaiters that avoid the overhead of `Promise`.

Other APIs that allow you to pass `SynchronizationOption` or `SynchronizationContext` to configure the context that the callback executes on are `Progress.New` (default `CapturedContext`), `Promise.New` (default `Synchronous`), and `Promise.Run` (default `Background`).