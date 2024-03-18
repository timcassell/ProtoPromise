# Multiple Consumers

Most promises can only be awaited once, and if they are not awaited, they must be returned or forgotten (see [Forget](the-basics.md#forget)).
You can preserve a promise so that it can be awaited multiple times via the `promise.Preserve()` API. When you are finished with the promise, you must call `promise.Forget()`.
Callbacks added to a preserved promise will be invoked in the order that they are added.

Note: a preserved promise should not be returned from a public API, because the consumer could immediately call `Forget()` and invalidate the promise. Instead, you should use `promise.Duplicate()` to get a promise that will adopt its state, but can only be awaited once.