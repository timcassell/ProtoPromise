# Switching Execution Context

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

If your application uses multiple `SynchronizationContext`s, instead of using `SynchronizationOption.Foreground`, you should pass the proper `SynchronizationContext` directly to `WaitAsync` or `Promise.SwitchToContext`.

For context switching optimized for async/await, use `Promise.SwitchToForegroundAwait`, `Promise.SwitchToBackgroundAwait`, and `Promise.SwitchToContextAwait`. These functions return custom awaiters that avoid the overhead of `Promise`.

Other APIs that allow you to pass `SynchronizationOption` or `SynchronizationContext` to configure the context that the callback executes on are `Progress.New` (default `Foreground`), `Promise.New` (default `Synchronous`), and `Promise.Run` (default `Background`).