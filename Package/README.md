# ProtoPromise

<a href="https://promisesaplus.com/">
    <img src="https://promisesaplus.com/assets/logo-small.png" alt="Promises/A+ logo"
         title="Promises/A+ 1.1 compliant" align="right" />
</a>

[![NuGet](https://img.shields.io/nuget/v/ProtoPromise.svg)](https://www.nuget.org/packages/ProtoPromise)
[![openupm](https://img.shields.io/npm/v/com.timcassell.protopromise?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.timcassell.protopromise/)

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
- Interoperable with Tasks and Unity's Coroutines/Awaitables
- CLS compliant

ProtoPromise conforms to the [Promises/A+ Spec](https://promisesaplus.com/) as far as is possible with C# (using static typing instead of dynamic), and further extends it to support Cancelations and Progress.

This library was built to work in all C#/.Net ecosystems, including Unity, Mono, .Net Framework, .Net Core, and AOT compilation. It is CLS compliant, so it is not restricted to only C#, and will work with any .Net language.

- ProtoPromise v3 supports .Net Standard 2.0 or newer (Unity 2018.3 or newer).
- ProtoPromise v2 supports .Net 3.5 or newer (Unity 5.5 or newer).

See [Guides](https://github.com/timcassell/ProtoPromise/tree/master/Docs/Guides) for information on how to install and use this library.  
See [ChangeLog](https://github.com/timcassell/ProtoPromise/tree/master/Docs/Changelog) for the history of changes.

## Benchmarks

Compare performance to other async libraries:

| Type         | Pending | Mean      | Allocated | Survived |
|------------- |-------- |----------:|----------:|---------:|
| ProtoPromise | False   |  37.56 ns |         - |        - |
| Task         | False   |  49.97 ns |     192 B |        - |
| UniTask      | False   |  81.06 ns |         - |        - |
| UnityFxAsync | False   |  62.05 ns |     360 B |        - |
| ValueTask    | False   |  70.66 ns |         - |        - |
|              |         |           |           |          |
| ProtoPromise | True    | 401.37 ns |         - |    624 B |
| Task         | True    | 455.15 ns |    1120 B |        - |
| UniTask      | True    | 478.92 ns |         - |    744 B |
| UnityFxAsync | True    | 433.54 ns |    1952 B |        - |
| ValueTask    | True    | 498.24 ns |     968 B |     40 B |

See the [C# Asynchronous Benchmarks Repo](https://github.com/timcassell/CSharpAsynchronousBenchmarks) for a full performance comparison.

## Acknowledgements

This library was inspired by [ES6 Promises](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Promise), [RSG Promises](https://github.com/Real-Serious-Games/C-Sharp-Promise), [uPromise](https://assetstore.unity.com/packages/tools/upromise-15604), [TPL](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl), [UniTask](https://github.com/Cysharp/UniTask), [AsyncEx](https://github.com/StephenCleary/AsyncEx), and [UnityAsync](https://github.com/muckSponge/UnityAsync).