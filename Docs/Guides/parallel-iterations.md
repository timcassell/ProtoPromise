# Parallel Iterations

You may have a very large collection that you need to iterate as quickly as possible without blocking the current thread. A `foreach` loop will do the job, but it will only run each iteration sequentially on the current thread.
`Parallel.ForEach` will do the job concurrently, possibly utilizing multiple threads, but it will still block the current thread until all iterations are complete. `Promise.ParallelForEach` to the rescue!

```cs
public static Promise IterateAsync<T>(this IEnumerable<T> enumerable, Action<T> action)
{
    return Promise.ParallelForEach(enumerable, (item, cancelationToken) =>
    {
        action(item);
        return Promise.Resolved();
    });
}
```

This will run each iteration concurrently without blocking the current thread.

You may also wish to parallelize a `for` loop, which you can do with `Promise.ParallelFor`.

```cs
public static Promise ForAsync(int min, int max, Action<int> action)
{
    return Promise.ParallelFor(min, max, (item, cancelationToken) =>
    {
        action(item);
        return Promise.Resolved();
    });
}
```

(`Promise.ParallelForEach` is similar to `Parallel.ForEachAsync` in .Net 6+, but uses `Promise` instead of `Task` to be more efficient, and works in older runtimes.)

You can also use `Promise.ParallelForEachAsync` to iterate `AsyncEnumerable<T>` in parallel.