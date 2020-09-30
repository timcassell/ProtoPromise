# Release Notes

## v 0.11.0 - September 30, 2020

Optimizations:

- Added object pooling for allocation-free async Promise functions.
- Removed LitePromises, using DeferredPromises for Promise.Resolved/Rejected/Canceled instead to reduce amount of code.

Bug Fixes:

- Fixed causality traces in async Promise functions.
- Fixed aggregate exception not capturing the unhandled exceptions.
- Fixed PromiseMethodBuilders for async Promise functions with the IL2CPP compiler. (v0.10.2)

Misc:

- Added message to unhandled exceptions, when not in DEBUG mode, to explain how to see causality traces.
- Added implicit cast operators for `Promise.Deferred` -> `Promise.DeferredBase` and `Promise<T>.Deferred` -> `Promise.DeferredBase`. (v0.10.1)
- Ignore Internal functions and PromiseYielder.Routines in causality traces. (v0.10.2)
- Include causality trace from invalid returned promise. (v0.10.2)

## v 0.10 - August 21, 2020

API changes:

- Added `Proto.Promises.{CancelationSource, CancelationToken, CancelationRegistration}` structs which can be used for synchronous cancelation callbacks.
- Added optional `CancelationToken` parameter to `Promise.{Then, Catch, CatchCancelation, ContinueWith, Progress, Sequence, NewDeferred}`.
- Added `Promise(<T>).Deferred.New(CancelationToken)` that does the same thing as `Promise.NewDeferred(<T>)(CancelationToken)`.
- Changed Deferreds to structs with an implicit cast to DeferredBase (`DeferredBase.ToDeferred(<T>)()` to cast back explicitly).
- Removed `DeferredBase.Cancel`, replaced with the cancelation token.
- Removed `Promise.{Cancel, ThenDuplicate}`.
- Removed `Proto.Logger`, replaced it with `Promise.Config.WarningHandler`.

Behavior changes:

- Changed `Promise.First` to suppress rejections from any promises passed in that did not result in the rejection of the returned promise.
- Change `Deferred.Reject(null)` to convert to a `NullReferenceException` on any `T`. This means `Promise.Catch<T>` will never give a null value. This more closely matches normal `throw null;` behavior.
- `CancelationSource.Cancel(null)` now converts cancelation to a non-value. This mean if `Promise.ReasonContainer.ValueType` is not null, `Promise.ReasonContainer.Value` is also not null, and if `Promise.ReasonContainer.TryGetValue<T>` succeeds, the value will not be null.
- Removed requirement to include `Proto.Promises.Await` for awaiting promises in an async function.
- Check to make sure `RethrowException` is used properly, change to `InvalidOperationException` if not.

Optimizations:

- Deferreds are now structs, removing the memory overhead of classes.
- Added a static cached `Promise.Canceled()` void in RELEASE mode.
- When canceled, promises are now unlinked from their previous.
- No longer need check if promise is canceled when doing internal operations.
- Removed skipFrame calculations since that's now handled in `Internal.FormatStackTrace` in DEBUG mode.
- Pass by ref to pass large structs around internally more efficiently.
- Changed delegate wrappers to structs and changed internal promises to use generics with constraints to optimize composition (and use less memory in some promise objects).
- `PromiseMethodBuilder` caches `stateMachine.MoveNext` directly instead of lambda capturing.

Misc:

- Removed recursive type definitions so that Unity 2019.1.14 and older on .Net 4.X scripting runtime version will compile.
- Removed PROTO_PROMISE_CANCEL_DISABLE preprocessor checks since cancelations no longer slow down normal execution.
- No longer check for internal promise invoking when accessing `Promise.{RejectException, CancelException}`.
- Consolidated Rejection and Cancelation value container creations. Handle all internal rejection/cancelation types to not wrap them.
- Renamed asmdefs to include Proto in the names. Removed Utilities.asmdef.

Known Issues:

- A `Promise.Progress` callback subscribed to a promise chain where the chain is broken by a cancelation token and continued with `Promise.ContinueWith` reports incorrect progress (introduced in v 0.9 with `Promise.ContinueWith`).

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