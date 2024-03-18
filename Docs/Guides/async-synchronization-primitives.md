# Async Synchronization Primitives

These types are asynchronous near-equivalent counterparts to `System.Threading` synchronization primitives (except `AsyncConditionVariable`, which does not currently have a synchronous counterpart in the BCL).

## AsyncLock

`AsyncLock` (in the `Proto.Promises.Threading` namespace) can be used to coordinate mutual exclusive execution around asynchronous resources. It is similar to the normal `lock` keyword, except `lock` cannot be used around `await`s.
The syntax is a little different: instead of `lock (mutex) { }`, the syntax is `using (await mutex.LockAsync()) { }`. Everything inside the `using` block will be protected by the lock.

```cs
private readonly AsyncLock _mutex = new AsyncLock();

public async Promise DoStuffAsync()
{
    using (await _mutex.LockAsync())
    {
        // Do mutually exclusive async work here.
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
}
```

`AsyncLock` also supports synchronous locks, so a thread may try to enter a lock synchronously, even while another thread is holding it asynchronously. `using (_mutex.Lock()) { }`
WARNING: doing so may cause a deadlock if the async function that holds the lock tries to switch context to the same thread before releasing the lock. Although it is supported, it is not recommended to mix synchronous and asynchronous locks. If at all possible, normal locks should be preferred.

WARNING: `AsyncLock` does _not_ support re-entrance (recursion). If you attempt to enter the lock while it is already entered, it will result in a deadlock.

Note: `AsyncLock` is only available in .Net Standard 2.1 (or Unity 2021.2) or newer platforms.

## AsyncMonitor

You may also use the `AsyncMonitor` class to `WaitAsync` and `Pulse` in a fashion very similar to the normal `Monitor` class.

```cs
private readonly AsyncLock _mutex = new AsyncLock();

public async Promise DoStuffAsync()
{
    using (var key = await _mutex.LockAsync())
    {
        // Wait for a pulse.
        await AsyncMonitor.WaitAsync(key);
        // Continue after another context has pulsed the lock.
    }
}
```

Note: `AsyncMonitor` is only available in .Net Standard 2.1 (or Unity 2021.2) or newer platforms.

## AsyncReaderWriterLock

Similar to `System.Threading.ReaderWriterLockSlim`, except, just like `AsyncLock`, recursion is not supported.

```cs
private readonly AsyncReaderWriterLock _rwl = new AsyncReaderWriterLock();

public async Promise DoStuffAsync()
{
    using (await _rwl.ReaderLockAsync())
    {
        // Reader lock is shared, multiple readers can enter at the same time.
    }
    
    using (await _rwl.WriterLockAsync())
    {
        // Writer lock is mutually exclusive, only one writer can enter at a time, and no readers can enter while a writer is entered.
    }
    
    using (var upgradeableReaderKey = await _rwl.UpgradeableReaderLockAsync())
    {
        // Upgradeable lock is shared with regular reader locks, but is mutually exclusive with respect to regular writer locks and other upgradeable reader locks.
        // Only one upgradeable reader can enter at a time, and no regular writers can enter while an upgradeable reader is entered.
        // Regular readers can enter while an upgradeable reader is entered.
    
        using (await _rwl.UpgradeToWriterLockAsync(upgradeableReaderKey))
        {
            // Upgraded writer lock is mutually exclusive, only one writer can enter at a time, and no readers can enter while a writer is entered.
        }
    }
}
```

Note: `AsyncReaderWriterLock` is only available in .Net Standard 2.1 (or Unity 2021.2) or newer platforms.

## AsyncManualResetEvent

Async version of [System.Threading.ManualResetEventSlim](https://learn.microsoft.com/en-us/dotnet/api/system.threading.manualreseteventslim).

## AsyncAutoResetEvent

Async version of [System.Threading.AutoResetEvent](https://learn.microsoft.com/en-us/dotnet/api/system.threading.autoresetevent).

## AsyncSemaphore

Async version of [System.Threading.SemaphoreSlim](https://learn.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim).

`AsyncSemaphore` can be used as a sort of "multi-lock" to constrain the number of consumers of a limited resource. For example, you can limit how many downloads can be in flight at a time.

```cs
// Only allow 4 downloads at once.
private readonly AsyncSemaphore _sem = new AsyncSemaphore(4);

public async Promise<string> DownloadConstrained(string url)
{
    using (await _sem.EnterScopeAsync())
    {
        // Up to 4 consumers can enter this protected region at a time.
        return await Download(url);
    }
}
```

## AsyncCountdownEvent

Async version of [System.Threading.CountdownEvent](https://learn.microsoft.com/en-us/dotnet/api/system.threading.countdownevent).

## AsyncConditionVariable

Local version of `AsyncMonitor`. Use in place of `AsyncMonitor.WaitAsync(AsyncLock.Key)` and `AsyncMonitor.Pulse(AsyncLock.Key)` if you need wait/pulse logic for different conditions instead of global (like for an async version of a blocking collection). See `std::condition_variable` in C++ for inspiration.

Note: `AsyncConditionVariable` is only available in .Net Standard 2.1 (or Unity 2021.2) or newer platforms.