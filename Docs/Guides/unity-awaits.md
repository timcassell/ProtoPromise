## Unity Optimized Await Instructions

In addition to interoping with Coroutines, `PromiseYielder` also has common Unity await functions, optimized for async/await, with zero allocations.

```cs
await PromiseYielder.WaitOneFrame();
await PromiseYielder.WaitForTime(TimeSpan.FromSeconds(1));
await PromiseYielder.WaitForRealTime(TimeSpan.FromSeconds(1));
await PromiseYielder.WaitForAsyncOperation(asyncOp);
await PromiseYielder.WaitForEndOfFrame();
await PromiseYielder.WaitForFixedUpdate();
await PromiseYielder.WaitForLateUpdate();
await PromiseYielder.WaitForUpdate();
await PromiseYielder.WaitUntil(() => cond);
await PromiseYielder.WaitWhile(() => cond);
```

You can also implement your own custom await instructions by implementing `IAwaitInstruction`. You can even implement them using a struct to eliminate allocations!
`IsCompleted` will be called once per frame until it returns `true`. The async function will not continue until such time.

You may optionally append `.WithCancelation(cancelationToken)` to attach a cancelation token to the yield instruction.
These can all be converted to `Promise` via the `ToPromise()` extension method.