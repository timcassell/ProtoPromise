# Configuration

## Config

You can change whether or not objects will be pooled via `Promise.Config.ObjectPoolingEnabled`. Enabling pooling reduces GC pressure, and it is enabled by default.

`Promise.Config.UncaughtRejectionHandler` allows you to route unhandled rejections through a delegate instead of being thrown.

`Promise.Config.ForegroundContext` is the context to which foreground operations are posted, typically used to marshal work to the UI thread. This is automatically set in Unity, but in other UI frameworks it should be set at application startup (usually `Promise.Config.ForegroundContext = SynchronizationContext.Current` is enough). Note: if your application uses multiple `SynchronizationContext`s, you may want to pass `ContinuationOptions.CapturedContext` to the `ConfigureAwait` and other APIs instead of `Foreground`. See [Switching Execution Context](context-switching.md).

`Promise.Config.BackgroundContext` can be set to override how background operations are executed. If this is null, `ThreadPool.QueueUserWorkItem(callback, state)` is used.

`Promise.Config.AsyncFlowExecutionContextEnabled` can be set to true to enable [AsyncLocal support](asynclocal.md).

## Causality Traces

Set `Proto.Diagnostics.CausalityTrace.IsEnabled = true` to capture causality traces, which record where each async operation was started and how it got there. When a rejection is unhandled, its `UnhandledException.StackTrace` contains the causality trace (also available from its `CausalityTrace` property). See [Unhandled Rejections](error-handling.md#unhandled-rejections).

Causality traces are disabled by default. Capturing them is expensive, so you should only enable them while you are debugging an error that you can't find the source of. Object pooling is disabled while they are enabled. Only operations that are started after they are enabled are traced.

Causality traces are most reliable in DEBUG mode (see [Compiler Options](#compiler-options)). In RELEASE mode, they are not always available (for example, for objects that are reported from finalizers on some runtimes).

A causality trace is printed like an async stack trace: the stack trace of where the trace was captured, followed by the stack trace of the operation that started it, and so on. In addition:

- If a trace was captured while other operations' callbacks were also running (for example, a continuation that was invoked while a deferred was being resolved), those operations are outer causality scopes. They are referred to by letter (`--- [Outer causality scope A, see below] ---`) and printed in their own sections below the main trace. An outer scope's own outer scopes extend its letter (`A/A`, `A/B`, ...).
- Every stack trace is printed, but parts that repeat (for example, from loops or recursion) are only printed once, between `--- [#1 Begin] ---` and `--- [#1 End] ---`. Other places where that part occurs refer to it with `--- [#1 repeated 3 times] ---`.

`CausalityTrace.MaxDepth` limits how many levels a trace keeps (default 128), where each level is a callback that was running when a trace was captured. When a trace would exceed it, its oldest levels are dropped. This prevents traces from growing without bound when callbacks are recursively scheduled.

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

Traces captured inside the scope (including by promises) will include the entered trace. You can read a trace's frames with `GetFrames()` (library frames are excluded), its earlier traces with `PreviousTraces`, or the full formatted trace with `ToString()`.

## Manager

If you are using a runtime older than .Net 6, you should assign `Promise.Manager.ThreadStaticSynchronizationContext`. In .Net 6, this is exactly the same as `SynchronizationContext.Current`.
This property is used internally to execute some continuations synchronously if the scheduled context matches it.

This isn't really a config option, but if you have object pooling enabled, you can call `Promise.Manager.ClearObjectPool()` to clear the pool when memory pressure is high.

## Compiler Options

If you're compiling from source (like in Unity Editor):

By default, debug options are tied to the `DEBUG` compiler symbol, which is defined by default in the Unity Editor and not defined in release builds. You can override that by defining `PROTO_PROMISE_DEBUG_ENABLE` to force debugging on in release builds, or `PROTO_PROMISE_DEBUG_DISABLE` to force debugging off in debug builds (or in the Unity Editor). If both symbols are defined, `ENABLE` takes precedence.

## Nuget Options

If you're using the Nuget package (`<PackageReference>` in your csproj):

You can override which dll will be used by setting the `ProtoPromiseConfiguration` property. Nested under a `<PropertyGroup>`, add `<ProtoPromiseConfiguration>Release</ProtoPromiseConfiguration>` to force it to use the Release dll, or `<ProtoPromiseConfiguration>Debug</ProtoPromiseConfiguration>` to force it to use the Debug dll. If the property is not set, or is set to any other value, the default behavior of choosing the dll based on your build configuration will be used. This option requires ProtoPromise v3.1.0 or newer.