# Error Retries and Async Recursion

What I especially love above this system is you can implement retries through asynchronous recursion.

```cs
public Promise<string> Download(string url, int maxRetries = 0)
{
    return Download(url)
        .Catch(() =>
        {
            if (maxRetries <= 0)
            {
                throw Promise.Rethrow; // Rethrow the rejection without processing it so that the caller can catch it.
            }
            Console.Log($"There was an error downloading {url}, retrying..."); 
            return Download(url, maxRetries - 1);
        };
}
```

This can also be done with async functions.

```cs
public async Promise<string> Download(string url, int maxRetries = 0)
{
    try
    {
        return await Download(url);
    }
    catch
    {
        if (maxRetries <= 0)
        {
            throw; // Rethrow the rejection without processing it so that the caller can catch it.
        }
        Console.Log($"There was an error downloading {url}, retrying..."); 
        return await Download(url, maxRetries - 1);
    }
}
```

Async recursion is just as powerful as regular recursion, but it is also just as dangerous. If you mess up on regular recursion, your program will immediately crash from a `StackOverflowException`. You may prevent stack overflows with async recursion by using a [context switching API](context-switching.md) with the `forceAsync` flag set to true. However, you should be aware that if your async recursion continues forever, your program will eventually crash from an `OutOfMemoryException` due to each call waiting for the next and creating a new promise each time, consuming your heap space.
Because promises can remain pending for an indeterminate amount of time, this error can potentially take a long time to show itself and be difficult to track down. So be very careful when implementing async recursion, and remember to always have a base case!

Of course, async functions are powerful enough where this retry behavior can be done in a loop without blowing up the heap.

```cs
public async Promise<string> Download(string url, int maxRetries = 0)
{
    while (true)
    {
        try
        {
            return await Download(url);
        }
        catch
        {
            if (--maxRetries < 0)
            {
                throw; // Rethrow the rejection without processing it so that the caller can catch it.
            }
            Console.Log($"There was an error downloading {url}, retrying..."); 
        }
    }
}
```