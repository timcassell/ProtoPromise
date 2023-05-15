# Release Notes

## v 2.4.1 - February 5, 2023

Fixes:

- Fixed `CancelationToken.IsCancelationRequested` when it was created from a `System.Threading.CancellationToken`.

Optimizations:

- Small performance improvement in `CancelationToken.(Try)Register`.

## v 2.4.0 - December 23, 2022

Enhancements:

- Added `Promise.ParallelFor` and `Promise.ParallelForEach` APIs to run iterations concurrently and asynchronously (compare to `Parallel.ForEach`).
- Exposed `CancelationRegistration.DisposeAsync()` in all supported runtimes.
- Added `AsyncLock` and `AsyncMonitor` to be able to lock around asynchronous resources (in the `Proto.Promises.Threading` namespace).

Fixes:

- Promise result values are no longer stored past the promise's lifetime.
- Fixed compilation errors in Unity 2017 when the experimental .Net 4.6 runtime is selected.
- Fixed compilation warnings in Unity when importing the ProtoPromise package through the Package Manager.
- Fixed default foreground context never being executed in Unity 2019.1 or newer.

Optimizations:

- Increased performance of progress reports.
- Increased performance of promises.
- Reduced memory consumption of `All`, `Merge`, `Race`, and `First` promises.

Misc:

- Progress reports now use full 32-bit float precision instead of 16-bit fixed precision.
- Changed `Promise.Run` `forceAsync` flag to default to `true` instead of `false`.

Breaking Changes:

- Changed `CancelationRegistration.DisposeAsync()` to return `Promise` instead of `ValueTask` (`await using` code still works the same, but may need a recompile).

## v 2.3.0 - September 25, 2022

Enhancements:

- Added `Promise.Wait()` and `Promise<T>.WaitForResult()` synchronous APIs.
- `CancelationRegistration` now implements `IDisposable` and `IAsyncDisposable` interfaces. Disposing the registration will unregister the callback, or wait for the callback to complete if it was already invoked.
- Added `CancelationToken.GetRetainer()` API to reduce `TryRetain` / `Release` boilerplate code.
- Un-deprecated `Deferred.IsValid`, and changed it to return whether the `Deferred.Promise` is valid, instead of returning `Deferred.IsValidAndPending`.

Fixes:

- Fixed `PromiseYielder` when different runners are used on a re-used routine.
- Fixed `NullReferenceException` when the AppDomain is unloaded.

Deprecated:

- deprecated `Promise.ResultContainer.RejectContainer`, replaced with `Promise.ResultContainer.RejectReason`.
- deprecated `CancelationToken.Retain()` (prefer `TryRetain()`).
- deprecated `CancelationRegistration.Unregister()` (prefer `TryUnregister()` or `Dispose()`).

Misc:

- `CancelationToken.Register(callback)` now returns a default registration if it failed to register instead of throwing.

Breaking Changes:

- Removed `Promise<T>.RaceWithIndex` and `Promise<T>.FirstWithIndex` APIs in build targets older than .Net Standard 2.1. `Promise.RaceWithIndex<T>` and `Promise.FirstWithIndex<T>` APIs can be used instead.

## v 2.2.0 - August 6, 2022

Enhancements:

- Added `Promise(<T>).AwaitWithProgress(maxProgress)` API to use current progress instead of passing in minProgress.
- Added `Promise(<T>).{RaceWithIndex, FirstWithIndex}` APIs to be able to tell which promise won the race.
- Added `Promise(<T>).WaitAsync(CancelationToken)` APIs, and added optional `CancelationToken` arguments to existing `WaitAsync` APIs.
- Added optional `bool forceAsync` arguments to existing `WaitAsync` and other APIs that allow changing context.

Fixes:

- Fixed `CancelationToken` callbacks not being invoked after they are registered after the original `System.Threading.CancellationTokenSource` has been reset (.Net 6.0+).
- Fixed a deadlock in `PromiseSynchronizationContext.Send` if the callback throws an exception. The exception is rethrown in .Net 4.5+.
- Fixed `WaitAsync` and `Progress` if null `SynchronizationContext` is passed in.
- Fixed compile errors in Unity 5.

Optimizations:

- Slightly increased performance and decreased memory (at the cost of no longer unwinding the stack, which users can now do with the `forceAsync` flag).

Misc:

- Completely removed internal stacktraces in .Net 6 or later.
- Added net6.0 build target.

## v 2.1.0 - June 19, 2022

Enhancements:

- Added `AsyncLocal<T>` support in `async Promise` functions.
  - Disabled by default, enable with `Promise.Config.AsyncFlowExecutionContextEnabled = true`.
- Added `ValueTask(<T>)` interoperability.
  - Use `promise.AsValueTask()` or implicit cast `ValueTask valueTask = promise;`, or `valueTask.ToPromise()`.
- Added `System.Threading.CancellationToken` interoperability.
  - Use `token.ToCancelationToken()` to convert to `Proto.Promises.CancelationToken`, or `token.ToCancellationToken()` to convert to `System.Threading.CancellationToken`.

Optimizations:

- 2x - 3x performance improvement for promises and cancelation tokens.
- Reduced memory consumption of pending promises.

Misc:

- Added netstandard2.1, netcoreapp2.1, and net5.0 build targets.

## v 2.0.2 - April 25, 2022

Fixes:

- Fixed `Promise.AwaitWithProgress` not working in IL2CPP.
- Fixed a rare race condition where a canceled promise could cause an invalid cast exception.

Optimizations:

- Fixed boxing nullable value-types.
- Reduced size of cancelable promises.
- Subscribing to progress now runs in O(1) time and consumes O(1) memory, down from O(n) for both.

Misc:

- Increased precision of progress, from 1/(2^13) to 1/(2^16).

## v 2.0.1 - April 7, 2022

Fixes:

- Fixed a memory leak with All/Merge/Race/First promises when object pooling is enabled.
- Fixed state check in `PromiseYieldInstruction` when the promise is already complete.

Optimizations:

- Less pooled memory when `T` of `Promise<T>` is a reference type and used by more than 1 types (example: `Promise<string>` and `Promise<object>`).
- More efficient execution in the common case.

## v 2.0.0 - March 7, 2022

Enhancements:

- Full library thread-safety with minimal locks.
- Added `CLSCompliant(true)` to the assembly.
- `Promise<T>.ResultContainer` now has an implicit conversion to `Promise.ResultContainer`.
- Added `Promise<T>.{All, Race, First, New, NewDeferred, Resolved, Rejected, Canceled}`.
- Added `Promise.DeferredBase.AsDeferred(<T>)`.
- Added optional `valueContainer` parameter to `Promise<T>.All` functions to be used instead of allocating a new list.
- Added `Deferred.{TryResolve, TryReject, TryReportProgress}`.
- Added `Deferred.{Cancel, TryCancel}`.
- `Deferred`s now implement `IProgress<float>` and `ICancelable`.
- Added `CancelationSource.TryDispose` and `CancelationSource.TryCancel` methods.
- Added `CancelationToken.TryRetain`.
- Added `CancelationToken.TryRegister`.
- Added `CancelationRegistration.Token` property.
- Added `CancelationRegistration.GetIsRegisteredAndIsCancelationRequested(out bool)` and `CancelationRegistration.TryUnregister(out bool)`.
- Added `CancelationToken.(Try)Register<TCancelable>(TCancelable cancelable) where TCancelable : ICancelable`.
- Added static `CancelationToken.Canceled()` to get a token already in the canceled state without allocating.
- Added `Promise(<T>).Progress<TProgress>(TProgress progressListener, ...) where TProgress : IProgress<float>)` overload.
- Added `Promise(<T>).WaitAsync(SynchronizationOption)` and `Promise(<T>).WaitAsync(SynchronizationContext)` to schedule the next callback/await on the desired context. (Continuations now execute synchronously without `WaitAsync`.)
- Added `Promise.Config.ForegroundContext` and `Promise.Config.BackgroundContext` to compliment `SynchronizationOption`s.
- Added `Promise.Run` static functions.
- Added `Promise.SwitchToForeground()`, `Promise.SwitchToBackground()`, and `Promise.SwitchToContext(SynchronizationContext)` static functions.
- Added synchronization options for `Promise.Progress` and `Promise.New`.
- Added `Promise.AwaitWithProgress(float minProgress, float maxProgress)` API to propagate the progress to an `async Promise(<T>)` function.
- Added circular promise chain detection when awaiting a `Promise(<T>)` in an `async Promise(<T>)` function in DEBUG mode.

Optimizations:

- Promises are now structs, making already resolved promises live only on the stack, increasing performance.
- Optimized progress to consume less memory.
- Decreased cost of garbage collecting promises when object pooling is disabled.
- Optimized `async Promise(<T>)` functions in Unity 2021.2 or newer when IL2CPP is used.
- Optimized `async Promise(<T>)` awaiting another `Promise(<T>)` to hook up directly instead of creating a passthrough object.
- Eliminated potential `StackOverflowException`s from `async`/`await` continuations when both the `async` function and the `await`ed object are `Promise(<T>)`.

Breaking Changes:

- `Promise` and `Promise<T>` are now readonly (c# 7.3+) structs instead of classes (`Promise<T>` has an implicit cast to `Promise`).
- `Promise.{ResultContainer, ReasonContainer}` and `Promise<T>.ResultContainer` are now readonly ref structs (c# 7.3+).
- Added `Promise(<T>).{ContinueAction, ContinueFunc}` delegates to deal with ref structs, and changed signature of `Promise(<T>).ContinueWith` to use them.
- `Promise(<T>).GetAwaiter()` now return `Proto.Promises.Async.CompilerServices.PromiseAwaiter(<T>)`.
- Removed `PromiseYielder.ClearPooledObjects` as its pool is now controlled by `Promise.Config.ObjectPoolingEnabled` and `Promise.Manager.ClearObjectPool`.
- Removed `Proto.Utils` namespace.
- Continuations (`await` keyword or `promise.Then(callback)`) now execute synchronously by default. Use `Promise.WaitAsync()` to execute on a different context or asynchronously.
- Changed `PromiseYielder`'s `public static Promise<TYieldInstruction> WaitFor<TYieldInstruction>(TYieldInstruction)` to `public static Promise WaitFor(object yieldInstruction, MonoBehaviour runner = null)`.
- Removed `Promise(<T>).ResultType`.
- Changed behavior of `Promise.CatchCancelation` to return a new promise and behave more like `Promise.Catch`, where `onCanceled` resolves the returned promise when it returns, or adopts the state of the returned promise.
- Removed cancelation reasons.
- Deprecated with error `Promise(<T>).{Retain, Release}`, added `Promise(<T>).{Preserve, Forget, Duplicate}`.
- Deprecated with error `Deferred(<T>).{State, Retain, Release}` (can no longer check the completed state of a Deferred, only whether it's still pending).
- Deprecated with error all functions in `Promise.Manager` with error except `ClearObjectPool`.
- A rejected promise awaited in an async function now throws the original exception, if the promise was rejected with an exception.

Minor Changes:

- Object pooling is enabled by default.
- Changed `Promise.DeferredBase.ToDeferred(<T>)` to throw if the cast fails.
- Adjusted `CancelationRegistration.{IsRegistered, TryUnregister}` to return false if the token has been canceled and the callback not yet invoked. (Can no longer unregister callbacks once the source has been canceled.)
- Updated `Promise.New` to cancel the returned promise if the resolver throws an `OperationCanceledException`.
- `CancelationSource.Token` no longer throws (may return a token whose `CanBeCanceled` is false).
- Changed `Promise.Finally` to overwrite current rejection if an exception is thrown (follows same behavior as normal try/finally blocks).
- Renamed `ElementNullException` to `InvalidElementException`.
- Removed `PromiseDisposedException`.
- Removed `CancelException`, replaced with already existing `CanceledException`.
- `Promise.Progress` now returns a new promise that inherits the state (instead of returning the same promise).
- Progress `Obsolete` attributes are now warnings instead of errors when progress is disabled.
- Changed behavior of `Deferred.ReportProgress` to do nothing when progress is disabled.
- Changed behavior of `Promise.Progress` to return `Duplicate` when progress is disabled.
- If `Promise.Config.UncaughtRejectionHandler` is null, uncaught rejections will now be thrown as `AggregateException` in the `ForegroundContext`, or in the background if it's null.
- Changed behavior of cancelations to suppress rejections.

Deprecated:

- `Promise.Manager.ObjectPooling` deprecated, replaced with `ObjectPoolingEnabled`.
- Deprecated `Deferred(<T>).IsValid`, replaced with `IsValidAndPending`.
- Deprecated `Promise.Config.WarningHandler`.

Misc:

- In Unity, moved ProtoPromise from `Third Party` to `Plugins`.
- Renamed `Scripts` folder to `Core`.
- Added support for installing from a git url in Unity's package manager. https://github.com/timcassell/ProtoPromise.git?path=ProtoPromise_Unity/Assets/Plugins/ProtoPromise
- Added `InvalidArgumentException`.
- Fixed FormatStacktrace in Unity when a stack frame is captured that it can't inspect.
- Added `PromiseSynchronizationContext` and updated `PromiseBehaviour` to utilize it.
- Changed `UnreleasedObjectException` to `UnobservedPromiseException` when a promise is garbage collected without being awaited or forgotten.

## v 1.0.3 - December 11, 2021

- Fixed a compile error when building with IL2CPP runtime.

## v 1.0.2 - January 2, 2021

- Fixed uncaught rejections not being reported from reused promise objects.

## v 1.0.0 - October 29, 2020

Directory Structure:

- Changed the structure of the folders in the repository so as to allow .Net Core/Standard library solution to sit side-by-side with the Unity solution, attempting to minimize toes stepped on.
- Added a Unity project with Unity version 2017.4.1 (the earliest the Asset Store Tools supports).

Bug Fixes:

- Fixed PromiseMethodBuilders in non-IL2CPP builds when the TStateMachine is a struct.
- Fixed a Promise.Progress callback subscribed to a promise chain where the chain is broken by a cancelation token and continued with Promise.ContinueWith reports incorrect progress (introduced in v 0.9 with Promise.ContinueWith).
- Fixed progress subscribed to a promise that was created from a deferred with a cancelation token.
- Fixed various progress bugs with multi promises (All/Merge/Race/First).
- Fixed CancelationToken.Equals(object).

Behavior changes:

- Added thread checks to make sure the library is only used with one thread (in DEBUG mode only).

Misc:

- Added a temporary optimization in Promise.HandleBranches until background tasks are added.
- Removed class restriction on PromiseYielder.WaitFor (since there are some structs that can be yielded in Coroutines, like `AsyncOperationHandle`s).

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