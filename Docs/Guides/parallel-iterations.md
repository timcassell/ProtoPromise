# Parallel Iterations

You may have a very large data set that you need to iterate as quickly as possible. You can use the `ParallelAsync` class (in the `Proto.Promises.Threading` namespace) to perform parallel and asynchronous iterations to greatly speed up the execution and prevent blocking the UI thread.

You can use `ParallelAsync.ForEach` in place of a `foreach` loop to iterate over a collection or sequence.

```cs
public static async Promise IterateAsync<T>(this IEnumerable<T> enumerable, Action<T> action)
{
    await ParallelAsync.ForEach(enumerable, (item, cancelationToken) =>
    {
        action(item);
        return Promise.Resolved();
    });
}
```

This will run each iteration concurrently without blocking the current thread. It also supports `AsyncEnumerable<T>` iterations.

You may also wish to parallelize a `for` loop, which you can do with `ParallelAsync.For`.

```cs
public static async Promise ForAsync(int min, int max, Action<int> action)
{
    await ParallelAsync.For(min, max, (item, cancelationToken) =>
    {
        action(item);
        return Promise.Resolved();
    });
}
```