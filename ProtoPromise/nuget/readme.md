Robust and efficient library for management of asynchronous operations.

- Allocation-free async operations
- Cancelable operations with custom allocation-free CancelationToken/Source
- Progress with universal automatic or manual normalization
- Full causality traces
- Interoperable with Tasks and Unity's Coroutines
- Thread safe
- .Then API and async/await
- Easily switch to foreground or background context
- Combine async operations
- CLS compliant

This library was built to work in all C#/.Net ecosystems, including Unity, Mono, .Net Framework, .Net Core and UI frameworks. It is CLS compliant, so it is not restricted to only C#, and will work with any .Net language.

ProtoPromise conforms to the [Promises/A+ Spec](https://promisesaplus.com/) as far as is possible with C# (using static typing instead of dynamic), and further extends it to support Cancelations and Progress.

This library took inspiration from [ES6 Promises](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Promise) (javascript), [RSG Promises](https://github.com/Real-Serious-Games/C-Sharp-Promise) (C#), [uPromise](https://assetstore.unity.com/packages/tools/upromise-15604) (C#/Unity), [TPL](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl), and [UniTask](https://github.com/Cysharp/UniTask) (C#/Unity).

[See full README on GitHub.](https://github.com/timcassell/ProtoPromise)