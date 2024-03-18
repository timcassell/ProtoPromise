# AsyncLazy

`AsyncLazy<T>` behaves very similar to its `System.Lazy<T>` synchronous counterpart, but it supports async value retrieval.

```cs
AsyncLazy<int> lazy;

lazy = new AsyncLazy<int>(async () =>
{
    await FuncAsync();
    return 42;
});

int value = await lazy;
```