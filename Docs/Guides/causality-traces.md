# Causality Traces

When an async operation fails, the exception's stack trace only shows the callback that threw. It doesn't show what started that callback, which may have been on another thread, long before. Causality traces record where each async operation was started, and where that was started from, so you can follow an error back to its cause.

## Enabling Causality Traces

Set `Proto.Diagnostics.CausalityTrace.IsEnabled = true` to capture causality traces.

Causality traces are disabled by default. Capturing them is expensive, so you should only enable them while you are debugging an error that you can't find the source of. Object pooling is disabled while they are enabled. Only operations that are started after they are enabled are traced.

Causality traces are most reliable in DEBUG mode (see [Compiler Options](configuration.md#compiler-options)). In RELEASE mode, they are not always available (for example, for objects that are reported from finalizers on some runtimes).

## Reading a Causality Trace

When a rejection is unhandled, its `UnhandledException.StackTrace` contains the causality trace, which is also available from its `CausalityTrace` property. See [Unhandled Rejections](error-handling.md#unhandled-rejections).

For example, this game loader is started on a background thread, and the download completes later on the main thread:

```cs
public static void Main()
{
    CausalityTrace.IsEnabled = true;
    Thread.CurrentThread.Name = "Main Thread";

    Promise.Run(LoadGameAsync, SynchronizationOption.Background)
        .Forget();
    // ...
    OnDownloadCompleted("not a number");
}

private static async Promise LoadGameAsync()
{
    await LoadLevelAsync();
}

private static async Promise LoadLevelAsync()
{
    string data = await DownloadAsync();
    int level = int.Parse(data);
    Console.WriteLine(level);
}
```

The unhandled rejection is printed like this (file paths are shortened):

```
Proto.Promises.UnhandledException: An exception was not handled. -- This exception's Stacktrace contains the causality trace of all async continuations.
 ---> System.FormatException: The input string 'not a number' was not in a correct format.
   at System.Number.ThrowFormatException[TChar](ReadOnlySpan`1 value)
   at System.Int32.Parse(String s)
   at Program.LoadLevelAsync() in Program.cs:line 31
   at Program.LoadGameAsync() in Program.cs:line 25
   --- End of inner exception stack trace ---
   --- [Thread 1 "Main Thread"] ---
   at Program.LoadLevelAsync() in Program.cs:line 31
   at Program.OnDownloadCompleted(String text) in Program.cs:line 42
   at Program.Main() in Program.cs:line 19
   --- [Outer causality scope A, see below] ---
--- End of stack trace from the previous location ---
   --- [Thread 4 ".NET TP Worker", 36.229 ms earlier] ---
   at Program.LoadLevelAsync()
   at Program.LoadGameAsync() in Program.cs:line 25
   at Program.LoadGameAsync()
   at Proto.Promises.Internal.PromiseRefBase.RunWaitPromise`1.<>c.<New>b__3_1(Object obj) in RunPromiseInternal.cs:line 95
   at Proto.Promises.Internal.<>c__DisplayClass70_0.<ScheduleContextCallbackDirect>b__0(Object _) in HelperFunctionsInternal.cs:line 59
   at System.Threading.ThreadPoolWorkQueue.Dispatch()
   at System.Threading.PortableThreadPool.WorkerThread.WorkerThreadStart()
--- End of stack trace from the previous location ---
   --- [#1 Begin] ---
   --- [Thread 1 "Main Thread", 52.373 ms earlier] ---
   at Program.Main() in Program.cs:line 16
   --- [#1 End] ---

--- [Outer causality scope A] ---
   --- [Thread 4 ".NET TP Worker", 35.380 ms earlier] ---
   at Program.DownloadAsync() in Program.cs:line 37
   at Program.LoadLevelAsync() in Program.cs:line 30
   at Program.LoadLevelAsync()
   at Program.LoadGameAsync() in Program.cs:line 25
   at Program.LoadGameAsync()
   at Proto.Promises.Internal.PromiseRefBase.RunWaitPromise`1.<>c.<New>b__3_1(Object obj) in RunPromiseInternal.cs:line 95
   at Proto.Promises.Internal.<>c__DisplayClass70_0.<ScheduleContextCallbackDirect>b__0(Object _) in HelperFunctionsInternal.cs:line 59
   at System.Threading.ThreadPoolWorkQueue.Dispatch()
   at System.Threading.PortableThreadPool.WorkerThread.WorkerThreadStart()
--- End of stack trace from the previous location ---
   --- [#1 repeated 1 time] ---
```

The inner exception shows where the exception was thrown, through the async functions that awaited it, but not what started or continued them. The causality trace below it starts where the exception was caught: `LoadLevelAsync` was continued by `OnDownloadCompleted`, on the main thread. That's followed by where the failed async function was called (`LoadLevelAsync` from `LoadGameAsync`, on a thread pool thread), and then where that was started (`Main`). The download that `LoadLevelAsync` was awaiting is printed below as outer causality scope `A`: it was started by `DownloadAsync`, and the rest of its history is the same `Main` stack trace, which it refers to as `#1`.

A causality trace is printed like an async stack trace: the stack trace of where the trace was captured, followed by the stack trace of the operation that started it, and so on. In addition:

- Each stack trace is preceded by the thread that captured it, and how long before the printed trace it was captured, for example `--- [Thread 5 (thread pool), 12.345 ms earlier] ---`. Threads without a name are printed as `(thread pool)`, `(background)` or `(foreground)`.
- If a trace was captured while other operations' callbacks were also running (for example, a continuation that was invoked while a deferred was being resolved), those operations are outer causality scopes. They are referred to by letter (`--- [Outer causality scope A, see below] ---`) and printed in their own sections below the main trace. An outer scope's own outer scopes extend its letter (`A/A`, `A/B`, ...).
- Every stack trace is printed, but parts that repeat (for example, from loops or recursion) are only printed once, between `--- [#1 Begin] ---` and `--- [#1 End] ---`. Other places where that part occurs refer to it with `--- [#1 repeated 3 times] ---`. Where the repeats are first referred to, they are followed by their threads and times, for example `--- [#1 repeated 3 times: Thread 5 (thread pool) at 1.234, 2.345 ms earlier; Thread 1 "Main Thread" at 3.456 ms earlier] ---`. Likewise, outer scopes that are printed as another outer scope with the same stack trace (for example, loop iterations) are followed by their own threads and times. The threads and times of other traces that are folded this way (for example, the outer scopes of repeats) are not printed.

## Limiting the Depth

`CausalityTrace.MaxDepth` limits how many levels a trace keeps (default 128), where each level is a callback that was running when a trace was captured. When a trace would exceed it, its oldest levels are dropped, which is shown with `--- [Older traces truncated, increase CausalityTrace.MaxDepth to see more] ---`. This prevents traces from growing without bound when callbacks are recursively scheduled.

## Tracing Your Own Async Operations

You can also capture and use causality traces in your own code, for example to trace callbacks of your own async types:

```cs
// Capture where the callback was registered (null if causality traces are disabled).
var trace = CausalityTrace.Capture();

// Later, when the callback is invoked.
using (CausalityTrace.EnterScope(trace))
{
    callback.Invoke();
}
```

Traces captured inside the scope (including by promises) will include the entered trace.

For example, this event traces where each listener was added, and invokes each listener inside the scope of that trace:

```cs
public class TracedEvent<TArg>
{
    private readonly List<(Action<TArg> callback, CausalityTrace trace)> _callbacks = new();

    private protected void Invoke(TArg arg)
    {
        List<Exception> exceptions = null;
        foreach (var (callback, trace) in _callbacks.ToArray())
        {
            var scope = CausalityTrace.EnterScope(trace);
            try
            {
                callback.Invoke(arg);
            }
            catch (Exception e)
            {
                exceptions ??= new();
                exceptions.Add(e);
            }
            finally
            {
                scope.Dispose();
            }
        }
        if (exceptions != null)
        {
            throw new AggregateException(exceptions);
        }
    }

    public void AddListener(Action<TArg> callback)
        => _callbacks.Add((callback, CausalityTrace.Capture()));

    public void RemoveListener(Action<TArg> callback)
    {
        for (int i = _callbacks.Count - 1; i >= 0; i--)
        {
            if (_callbacks[i].callback == callback)
            {
                _callbacks.RemoveAt(i);
            }
        }
    }
}

public class TracedEventSource<TArg> : TracedEvent<TArg>
{
    new public void Invoke(TArg arg)
        => base.Invoke(arg);
}
```

If a listener starts an async function, and the async function's rejection is unhandled, its causality trace shows where the event was invoked, followed by where the listener was added:

```cs
levelLoaded.AddListener(level => OnLevelLoadedAsync(level).Forget());
```

You can read a trace's frames with `GetFrames()` (library frames are excluded), its earlier traces with `PreviousTraces`, where it was captured with `ThreadId`, `ThreadName`, `IsThreadPoolThread`, `IsBackgroundThread` and `Timestamp`, or the full formatted trace with `ToString()`.

Causality is followed through anything that captures a trace and enters a scope with it like this, which includes all of ProtoPromise's async operations. It is not followed through other async operations, like `Task`: for example, in an `async Task` function, traces that are captured after awaiting a `Task` (or any awaitable that doesn't participate in causality traces) don't include where the function was called.
