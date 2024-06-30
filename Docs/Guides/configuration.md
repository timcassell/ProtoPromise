# Configuration

## Config

You can change whether or not objects will be pooled via `Promise.Config.ObjectPoolingEnabled`. Enabling pooling reduces GC pressure, and it is enabled by default.

If you are in DEBUG mode, you can configure when additional stacktraces will be generated via `Promise.Config.DebugCausalityTracer`.

`Promise.Config.UncaughtRejectionHandler` allows you to route unhandled rejections through a delegate instead of being thrown.

`Promise.Config.ForegroundContext` is the context to which foreground operations are posted, typically used to marshal work to the UI thread. This is automatically set in Unity, but in other UI frameworks it should be set at application startup (usually `Promise.Config.ForegroundContext = SynchronizationContext.Current` is enough). Note: if your application uses multiple `SynchronizationContext`s, you should instead pass the context directly to the `WaitAsync` and other APIs instead of using `SynchronizationOption.Foreground`. See [Switching Execution Context](context-switching.md).

`Promise.Config.BackgroundContext` can be set to override how background operations are executed. If this is null, `ThreadPool.QueueUserWorkItem(callback, state)` is used.

`Promise.Config.AsyncFlowExecutionContextEnabled` can be set to true to enable [AsyncLocal support](asynclocal.md).

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