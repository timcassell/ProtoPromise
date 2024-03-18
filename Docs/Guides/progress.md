# Progress

Asynchronous operations can optionally report their progress towards completion, allowing the implementor to give the user feedback on the status of the async operation.
Progress is reported as a value between `0` and `1` inclusive. All you need to do is accept a `ProgressToken` parameter, and report the progress to the token.

```cs
Promise WaitForSeconds(double seconds, ProgressToken progressToken = default) // Optional progress token
{
    var deferred = Promise.NewDeferred();
    StartCoroutine(_Countup());
    return deferred.Promise;
    
    IEnumerator _Countup()
    {
        for (double current = 0; current < seconds; current += Time.deltaTime)
        {
            progressToken.Report(current / seconds); // Report the progress, normalized between 0 and 1.
            yield return null;
        }
        progressToken.Report(1);
        deferred.Resolve();
    }
}
```

To listen for the progress, you first create a `Progress` instance, then pass its `Token` into the async function. In C# 8 you can use `await using`. If `await using` is not available in your language version, you will need to call `DisposeAsync` on the progress instance after the operation is complete.

```cs
// C# 8 with `await using`
async Promise Func()
{
    await using var progress = Progress.New(value => OnProgress(value));
    await WaitForSeconds(1, progress.Token);
}

// With .Finally
Promise Func()
{
    var progress = Progress.New(value => OnProgress(value));
    return WaitForSeconds(1, progress.Token)
        .Finally(progress, p => p.DisposeAsync());
}
```

You can split the progress over multiple async operations, so all operations will be normalized into the `0` to `1` range:

```cs
async Promise Func()
{
    await using var progress = Progress.New(value => OnProgress(value));
    await WaitForSeconds(1, progress.Token.Slice(0, 0.5));
    await WaitForSeconds(1, progress.Token.Slice(0.5, 1));
}
```

Progress tokens are infinitely sliceable (until you reach the epsilon limit of `double`), so you can keep slicing them for recursive async functions and continue to have the progress reported properly between `0` and `1`.

If you need to include the progress from async operations that are combined via `Promise.Race/First` or `Promise.All/Merge`, you can create a `Progress.RaceBuilder` or `Progress.MergeBuilder` to handle it.

```
async Promise Func()
{
    await using var progress = Progress.New(value => OnProgress(value));
    using var progressRacer = Progress.NewRaceBuilder(progress.Token);
    await Promise.Race(
        WaitForSeconds(1, progressRacer.NewToken())
        WaitForSeconds(2, progressRacer.NewToken())
    );
}
```