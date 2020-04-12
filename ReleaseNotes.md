# Release Notes

## v 0.9 - April 12, 2020

- New Async/Await support in C# 7.0 or later (see <a href="https://github.com/timcassell/ProtoPromise/blob/master/README.md#asyncawait">README</a>).
  - You can now declare `async Promise` and `async Promise<T>` functions.
  - You can now `await` any `Promise` or `Promise<T>` in an async function.
- Removed `Promise.Complete`, replaced with `Promise.ContinueWith` (see <a href="https://github.com/timcassell/ProtoPromise/blob/master/README.md#continuewith">README</a>).
- Debugging:
  - Enhanced Causality Trace, readable from an `UnhandledException`'s `Stacktrace` property. All ProtoPromise library functions are stripped from the stacktrace.
  - Added `System.Diagnostics.DebuggerNonUserCodeAttribute` to all classes in the library so that you will no longer step into library code using Visual Studio's debugger.
- Reduced memory when ObjectPooling is set to `Internal` or `All`.
- `Promise.Release()` was changed to be an asynchronous function under the hood. This means the promise is still usable for the duration of the function that called `Release`.
- ObjectPooling defaults to `Internal`.
- Fixed ahead-of-time compiler issues by removing all virtual generic methods.
  - Removed `Promise.Config.ValueConverter`.
  - Refactor: `CatchCancelation`
    - No longer returns `IPotentialCancelation`, instead returns the same `Promise` object. (Don't have to break the promise chain anymore.)
    - Can no longer catch cancelations by Type, instead a `Promise.ReasonContainer` is passed into the `onCanceled` delegate.
- Refactor:
  - Renamed `Promise.GeneratedStacktrace` to `TraceLevel`.
  - Renamed `Promise.Config.DebugStacktraceGenerator` to `DebugCausalityTracer`.
  - Special CancelExceptions and RejectExceptions adjustments (`Promise.CancelException` and `.RejectException` return types changed).
- `Config.UncaughtRejectionHandler` defaults to `UnityEngine.Debug.LogException` in 2019.2 or later since Unity no longer prints the innerException of an `AggregateException`.
- Rejecting a Promise with an `UnhandledException` or canceling with a `CanceledException` no longer wraps those, and will instead use the underlying value.
- Added extensions for `Task.ToPromise()` and `Promise.ToTask()`
- Added `Promise<T>.YieldInstruction` (returned from `Promise<T>.ToYieldInstruction()`) and added `YieldInstruction.GetResult`.

## v 0.8 - February 3, 2020

- Added method overloads to capture a value to pass into the delegate (see <a href="https://github.com/timcassell/ProtoPromise/blob/master/README.md#capture-values">README</a>).
- Removed `ThenDefer`, `CatchDefer`, and `CompleteDefer` (alternatively, you can `return Promise.New(deferred => {...});`).
- Removed Catches that accept a <T> without a value.
- Disable object pooling in debug mode.
- Remove `Reject()` without a value.
- Added `Then` overloads to allow returning a value/void in onresolved and a promise in onrejected, or vice-versa.
- Removed "NonAlloc" from static multi promise functions (except one to provide an IList to Promise.All).
- Fixed a retain bug with canceling the promise from a deferred.
- Added `Promise.Config.UncaughtRejectionHandler` to allow intercepting uncaught rejections before they are thrown.