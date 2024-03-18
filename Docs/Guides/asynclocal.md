# AsyncLocal Support

`AsyncLocal<T>` is supported in `async Promise` functions, but it is disabled by default because it makes execution more expensive and causes allocations in older runtimes. It can be enabled by setting `Promise.Config.AsyncFlowExecutionContextEnabled = true`.

```cs
private AsyncLocal<int> _asyncLocal = new AsyncLocal<int>();

public async Promise Func()
{
    _asyncLocal.Value = 1;

    await FuncNested();

    Assert.AreEqual(1, _asyncLocal.Value);
}

private async Promise FuncNested()
{
    Assert.AreEqual(1, _asyncLocal.Value);
    _asyncLocal.Value = 2;

    await _promise;

    Assert.AreEqual(2, _asyncLocal.Value);
}
```