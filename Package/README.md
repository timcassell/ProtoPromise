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

|         Type | Pending |       Mean |   Gen0 | Allocated | Survived |
|------------- |-------- |-----------:|-------:|----------:|---------:|
| ProtoPromise |   False |   498.1 ns |      - |         - |        - |
|   RsgPromise |   False |   580.7 ns | 0.3290 |    1032 B |    648 B |
|         Task |   False | 2,017.9 ns | 0.3891 |    1224 B |        - |
|      UniTask |   False |   704.6 ns |      - |         - |        - |
| UnityFxAsync |   False | 1,696.3 ns | 0.4139 |    1304 B |    656 B |
|              |         |            |        |           |          |
| ProtoPromise |    True | 2,177.5 ns |      - |         - |    384 B |
|   RsgPromise |    True | 5,011.0 ns | 3.2196 |   10104 B |    728 B |
|         Task |    True | 2,529.4 ns | 0.5112 |    1608 B |     16 B |
|      UniTask |    True | 2,938.6 ns |      - |         - |  3,960 B |
| UnityFxAsync |    True | 2,284.8 ns | 0.4959 |    1560 B |    552 B |

See the [C# Asynchronous Benchmarks Repo](https://github.com/timcassell/CSharpAsynchronousBenchmarks) for a full performance comparison.

## Latest Updates

### v3.0.0 - TBD

- Added `UnityEngine.Awaitable` extensions to convert to `Promise`.
- Added API overloads accepting `ReadOnlySpan<T>` parameter.
- Improved performance and reduced memory.
- Removed support for runtimes older than .Net Standard 2.0.
- Removed deprecated and useless APIs.

See [ChangeLog](https://github.com/timcassell/ProtoPromise/tree/master/Docs/Changelog) for the full changelog.

## Acknowledgements

This library was inspired by [ES6 Promises](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Promise), [RSG Promises](https://github.com/Real-Serious-Games/C-Sharp-Promise), [uPromise](https://assetstore.unity.com/packages/tools/upromise-15604), [TPL](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl), [UniTask](https://github.com/Cysharp/UniTask), [AsyncEx](https://github.com/StephenCleary/AsyncEx), and [UnityAsync](https://github.com/muckSponge/UnityAsync).