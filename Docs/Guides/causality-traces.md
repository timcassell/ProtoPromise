# Causality Traces

When an async operation fails, the exception's stack trace only shows the callback that threw. It doesn't show what started that callback, which may have been on another thread, long before. Causality traces record where each async operation was started, and where that was started from, so you can follow an error back to its cause. They also record the operations that were running around it, such as where a callback was scheduled from when it runs on another thread or context.

## Enabling Causality Traces

Set `Proto.Diagnostics.CausalityTrace.IsEnabled = true` to capture causality traces.

Causality traces are disabled by default. Capturing them is expensive, so you should only enable them while you are debugging an error that you can't find the source of. Object pooling is disabled while they are enabled. Only operations that are started after they are enabled are traced. When they are disabled, ProtoPromise releases the traces of operations that are still running, so those operations have no trace when they complete. In DEBUG mode, those traces are kept with their operations instead, so an operation that completes after causality traces are enabled again has its trace.

Causality traces are most reliable in DEBUG mode (see [Compiler Options](configuration.md#compiler-options)). In RELEASE mode, they are not always available (for example, for objects that are reported from finalizers on some runtimes).

## Reading a Causality Trace

When a rejection is unhandled, its `UnhandledException.StackTrace` contains the causality trace, which is also available from its `CausalityTrace` property. See [Unhandled Rejections](error-handling.md#unhandled-rejections).

For example, this game loader is started on a background thread. The download completes later on the main thread, and the loader continues on a background thread:

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
    string data = await DownloadAsync().ConfigureAwait(ContinuationOptions.Background);
    int level = int.Parse(data);
    Console.WriteLine(level);
}
```

The unhandled rejection is printed like this in DEBUG mode (file paths are shortened, and the line numbers are from the full program, which has code that is omitted above):

```
Proto.Promises.UnhandledException: An exception was not handled. -- This exception's StackTrace contains the causality trace of the rejection (the async operations that led to it).
 ---> System.FormatException: The input string 'not a number' was not in a correct format.
   at System.Number.ThrowFormatException[TChar](ReadOnlySpan`1 value)
   at System.Int32.Parse(String s)
   at Program.LoadLevelAsync() in Program.cs:line 29
   at Program.LoadGameAsync() in Program.cs:line 23
   --- End of inner exception stack trace ---
   --- Thread 4 ".NET TP Worker" ---
   at Program.LoadLevelAsync() in Program.cs:line 29
   at Proto.Promises.Internal.PromiseRefBase.AsyncPromiseRef`1.PromiseMethodContinuer.Continuer`1.<>c.<ContinueOnContext>b__7_1(Object obj) in AsyncInternal.cs:line 301
   at Proto.Promises.Internal.<>c__DisplayClass71_0.<ScheduleContextCallbackDirect>b__0(Object _) in HelperFunctionsInternal.cs:line 60
   at System.Threading.ThreadPoolWorkQueue.Dispatch()
   at System.Threading.PortableThreadPool.WorkerThread.WorkerThreadStart()
      --- Thread 1 "Main Thread", 4.518 ms earlier ---
      at Program.OnDownloadCompleted(String text) in Program.cs:line 41
      at Program.Main() in Program.cs:line 18
--- End of stack trace from the previous location ---
   --- Thread 4 ".NET TP Worker", 39.378 ms earlier ---
   at Program.LoadLevelAsync()
   at Program.LoadGameAsync() in Program.cs:line 23
   at Program.LoadGameAsync()
   at Proto.Promises.Internal.PromiseRefBase.RunWaitPromise`1.<>c.<New>b__3_1(Object obj) in RunPromiseInternal.cs:line 95
   at Proto.Promises.Internal.<>c__DisplayClass71_0.<ScheduleContextCallbackDirect>b__0(Object _) in HelperFunctionsInternal.cs:line 60
   at System.Threading.ThreadPoolWorkQueue.Dispatch()
   at System.Threading.PortableThreadPool.WorkerThread.WorkerThreadStart()
--- End of stack trace from the previous location ---
   --- Thread 1 "Main Thread", 75.253 ms earlier ---
   at Program.Main() in Program.cs:line 15
```

The inner exception shows where the exception was thrown, through the async functions that awaited it, but not what started or continued them. The causality trace below it starts where the exception was caught: `LoadLevelAsync` was continued on a thread pool thread, inside the scope of where `OnDownloadCompleted` scheduled it from the main thread (indented below it). That's followed by where the failed async function was called (`LoadLevelAsync` from `LoadGameAsync`, on a thread pool thread), and then where that was started (`Main`).

A causality trace is printed like an async stack trace: the stack trace of where the trace was captured, followed by the stack trace of the operation that started it, and so on. In addition:

- Each stack trace is preceded by the thread that captured it, and how long before the printed trace it was captured, for example `--- Thread 5 (thread pool), 12.345 ms earlier ---`. Threads without a name are printed as `(thread pool)`, `(background)` or `(foreground)`.
- A trace is followed by the traces of the other operations that were running around it when it was captured, from the innermost, each indented one more level than the one before it. Only their own stack traces are printed, so that each cause is printed once. For example, a callback that was scheduled to run on a `SynchronizationContext` or the thread pool (for example, a continuation after `ConfigureAwait`) only has the context or the thread pool below it on its stack, so it runs inside the scope of where it was scheduled from, which is printed with it. A continuation that a deferred invoked synchronously runs inside the deferred's scope, so it's printed with where the deferred was created (what resolved the deferred is already on its stack). A trace isn't printed again if it's already printed (for example, with the trace before it), or if its frames are already on the stack (for example, a callback that a context ran synchronously).
- A trace that was captured on the same thread by a call that is still running is skipped, because its frames are already printed. For example, `Promise.New` captures the promise's trace and then invokes its callback, so a trace captured in the callback is followed by the promise's cause instead of the promise's trace. Likewise, when an async function switches to another context before it returns to its caller (for example, its first await is `Promise.SwitchToContextAwait`), where it was called is printed only once. Traces are compared by what is printed (the methods and lines of their frames), so a trace that was captured by an earlier call on the same line (or in the same method, if line numbers are not available) may also be skipped.
- A trace that was captured at the same location as the trace before it, on the same thread or on any thread pool thread (for example, a callback that schedules itself again), replaces that trace, which is counted as a repeat. A trace's repeats are printed after its stack trace, grouped by thread, for example `--- Repeated 3 times: Thread 1 "Main Thread" 2 times at 1.234 to 5.678 ms earlier; thread pool 1 time at 9.012 ms earlier ---`. Repeats on any thread pool thread are grouped together, because a loop on the thread pool may run on a different thread each time.
- Parts of the chain that repeat (for example, a callback that schedules itself again from another thread, or callbacks that schedule each other) are only printed once. A single repeated trace is printed with the others as its repeats. A longer repeated part is printed between `--- #1 Begin ---` and `--- #1 End ---`, followed by `--- #1 repeated 3 times: ... ---` with the threads and times of the repeats.

## Limiting the Depth

`CausalityTrace.MaxDepth` limits how many traces a trace's chain of causes keeps (default 128). When a trace would exceed it, its oldest traces are dropped, which is shown with `--- Older traces truncated, increase CausalityTrace.MaxDepth to see more ---`.

`CausalityTrace.MaxOuterTraces` limits how many traces of the other operations that were running around it a trace keeps (default 8). When more operations were running (for example, in a deep chain of async functions that complete synchronously), the outermost ones are dropped, which is shown with `--- Outer traces truncated, increase CausalityTrace.MaxOuterTraces to see more ---`.

A chain only grows when an operation is started inside a callback of another operation, for example a callback that schedules a different callback, which schedules the first callback again (the repeated part is still printed once, see above). Operations that an async function awaits are started inside its own scope, so a loop in an async function doesn't grow the chain, and a callback that schedules itself again replaces its previous trace (see the repeats above).

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

Traces captured inside the scope (including by promises) will have the entered trace as their `Previous` trace, or a copy of it without its history in their `OuterTraces` if they're captured inside a scope that was entered inside it, unless they skip it or fold it into their repeats (see above).

For example, this event traces where each listener was added, and invokes each listener inside the scope of that trace:

```cs
public class TracedEvent<TArg>
{
    private readonly List<(Action<TArg> callback, CausalityTrace trace)> _callbacks = new();

    public void Invoke(TArg arg)
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
```

If a listener starts an async function, and the async function's rejection is unhandled, its causality trace shows where the event was invoked, followed by where the listener was added:

```cs
levelLoaded.AddListener(level => OnLevelLoadedAsync(level).Forget());
```

If an event is raised now and invoked later (for example, from a queue that is processed once per frame), where it was raised is not on the stack when the listeners run. Capture where it was raised, and invoke the listeners inside the scope of that trace, so that their traces also show where the event was raised (indented below their stack traces, before where the listener was added). For example, with these methods added to the event above:

```cs
private readonly Queue<(TArg arg, CausalityTrace trace)> _raised = new();

public void Raise(TArg arg)
    => _raised.Enqueue((arg, CausalityTrace.Capture()));

// Called once per frame.
public void ProcessRaised()
{
    while (_raised.Count > 0)
    {
        var raised = _raised.Dequeue();
        using (CausalityTrace.EnterScope(raised.trace))
        {
            Invoke(raised.arg);
        }
    }
}
```

You can read a trace's frames with `GetFrames()` (frames that are hidden from stack traces, such as ProtoPromise's internal frames, are excluded), its cause with `Previous` (which may be `null` while it has outer traces, if its cause was skipped or folded), the traces of the other operations that were running around it with `OuterTraces` (copies without their own `Previous`, `OuterTraces` and `Repeats`), its repeats with `Repeats`, where it was captured with `ThreadId`, `ThreadName`, `IsThreadPoolThread`, `IsBackgroundThread` and `Timestamp`, or the full formatted trace with `ToString()`. Which traces are skipped, folded or dropped is decided on a best-effort basis and depends on the runtime's frames, so `Previous`, `OuterTraces` and `Repeats` are meant for diagnostics, not for program logic.

Causality is followed through anything that captures a trace and enters a scope with it like this, which includes all of ProtoPromise's async operations. It is not followed through other async operations, like `Task`: for example, in an `async Task` function, traces that are captured after awaiting a `Task` (or any awaitable that doesn't participate in causality traces) don't include where the function was called.
