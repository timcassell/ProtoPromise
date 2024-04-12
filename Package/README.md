# ProtoPromise

<a href="https://promisesaplus.com/">
    <img src="https://promisesaplus.com/assets/logo-small.png" alt="Promises/A+ logo"
         title="Promises/A+ 1.1 compliant" align="right" />
</a>

Robust and efficient library for management of asynchronous operations.

- Allocation-free async operations
- Cancelable operations with custom allocation-free CancelationToken/Source
- Allocation-free async iterators with async Linq
- Progress with enforced normalization
- async/await support and .Then API
- Thread safe
- Full causality traces
- Easily switch to foreground or background context
- Combine async operations
- Circular await detection
- Interoperable with Tasks and Unity's Coroutines/Awaitables
- CLS compliant

ProtoPromise conforms to the [Promises/A+ Spec](https://promisesaplus.com/) as far as is possible with C# (using static typing instead of dynamic), and further extends it to support Cancelations and Progress.

This library was built to work in all C#/.Net ecosystems, including Unity, Mono, .Net Framework, .Net Core, and AOT compilation. It is CLS compliant, so it is not restricted to only C#, and will work with any .Net language.

- ProtoPromise v3 supports .Net Standard 2.0 or newer (Unity 2018.3 or newer).
- ProtoPromise v2 supports .Net 3.5 or newer (Unity 5.5 or newer).

See [Guides](https://github.com/timcassell/ProtoPromise/tree/master/Docs/Guides) for information on how to install and use this library.

Compare performance to other async libraries:

| Type         | Pending | Mean       | Allocated | Survived |
|------------- |-------- |-----------:|----------:|---------:|
| ProtoPromise | False   |   172.6 ns |         - |        - |
| Task         | False   |   260.9 ns |     192 B |        - |
| UniTask      | False   |   306.8 ns |         - |        - |
| UnityFxAsync | False   |   368.6 ns |     360 B |        - |
| ValueTask    | False   |   337.4 ns |         - |        - |
|              |         |            |           |          |
| ProtoPromise | True    | 1,402.3 ns |         - |    648 B |
| Task         | True    | 2,110.8 ns |    1120 B |        - |
| UniTask      | True    | 1,817.7 ns |         - |    744 B |
| UnityFxAsync | True    | 1,998.2 ns |    1952 B |        - |
| ValueTask    | True    | 2,486.9 ns |     968 B |     40 B |

See the [C# Asynchronous Benchmarks Repo](https://github.com/timcassell/CSharpAsynchronousBenchmarks) for a full performance comparison.

## Latest Updates

### v3.0.2 - April 12, 2024

- Fixed `Promise.ParallelFor*` canceling workers too early.
- `Promise.ParallelFor*` and `AsyncEnumerable.Merge` propagate exceptions from cancelation token callbacks instead of deadlocking.
- `AsyncEnumerable.Merge` more eagerly stops iteration if a rejection or cancelation occurs.

See [ChangeLog](https://github.com/timcassell/ProtoPromise/tree/master/Docs/Changelog) for the full changelog.

## Acknowledgements

This library was inspired by [ES6 Promises](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Promise), [RSG Promises](https://github.com/Real-Serious-Games/C-Sharp-Promise), [uPromise](https://assetstore.unity.com/packages/tools/upromise-15604), [TPL](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl), [UniTask](https://github.com/Cysharp/UniTask), [AsyncEx](https://github.com/StephenCleary/AsyncEx), and [UnityAsync](https://github.com/muckSponge/UnityAsync).