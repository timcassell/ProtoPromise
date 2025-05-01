Robust and efficient library for management of asynchronous operations.

- Allocation-free async operations
- Cancelable operations with custom allocation-free CancelationToken/Source
- Allocation-free async iterators with async Linq
- Progress with enforced normalization
- Structured concurrency
- async/await support and .Then API
- Thread safe
- Easily switch to foreground or background context
- Circular await detection
- Full causality traces
- Interoperable with Tasks
- CLS compliant

This library was built to work in all C#/.Net ecosystems, including Unity, Mono, .Net Framework, .Net Core, and AOT compilation. It is CLS compliant, so it works with any .Net language.

ProtoPromise conforms to the [Promises/A+ Spec](https://promisesaplus.com/) as far as is possible with C#.

[See full README on GitHub.](https://github.com/timcassell/ProtoPromise)