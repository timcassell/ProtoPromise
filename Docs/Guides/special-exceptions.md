# Special Exceptions

Normally, an `Exception` thrown in an `onResolved` or `onRejected` callback will reject the promise with that exception. There are, however, a few special exceptions that can be thrown to produce different behaviour:

## Rethrow

`throw Promise.Rethrow` can be used if you want to do something if a rejection occurs, but not suppress that rejection. Throwing `Promise.Rethrow` will rethrow that rejection, preserving its stacktrace (if applicable). This works just like `throw;` in synchronous catch clauses. This is only valid when used in `onRejected` callbacks. If accessed in other contexts, it will throw an `InvalidOperationException`.

## RejectException

`throw Promise.RejectException(reason)` can be used to reject the promise with a reason that is not an `Exception`. If reason is an `Exception`, you may want to just throw it directly, unless you want to preserve its stacktrace.

## CancelException

`throw Promise.CancelException()` can be used to cancel the promise. You can also throw an `OperationCanceledException`, which is equivalent.