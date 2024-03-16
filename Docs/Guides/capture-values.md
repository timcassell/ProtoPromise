# Capture Values

The C# compiler allows capturing variables inside delegates, known as closures. This involves creating a new object and a new delegate for every closure. These objects will eventually need to be garbage collected when the delegate is no longer reachable.

To solve this issue, capture values was added to the library. Every method that accepts a delegate can optionally take any value as a parameter, and pass that value as the first argument to the delegate. To capture multiple values, you should pass a `System.ValueTuple<>` that contains the values you wish to capture. The error retry example can be rewritten to reduce allocations:

```cs
public Promise<string> Download(string url, int maxRetries = 0)
{
    return Download(url)
        .Catch((url, maxRetries), cv => // Capture url and maxRetries in a System.ValueTuple<string, int>
        {
            var (_url, retryCount) = cv; // Deconstruct the value tuple (C# 7 feature)
            if (retryCount <= 0)
            {
                throw Promise.Rethrow; // Rethrow the rejection without processing it so that the caller can catch it.
            }
            Console.Log($"There was an error downloading {_url}, retrying..."); 
            return Download(_url, retryCount - 1);
        });
}
```

When the C# compiler sees a lambda expression that does not capture/close any variables, it will cache the delegate statically, so there is only one instance in the program, and no extra memory will be allocated every time it's used.

Note: Visual Studio will tell you what variables are captured/closed if you hover the `=>`. You can use that information to optimize your delegates. In C# 9 and later, you can use the `static` modifier on your lambdas so that the compiler will not let you accidentally capture variables the expensive way.

See [Understanding Then](the-basics.md#understanding-then) for information on all the different ways you can capture values with the `Then` overloads.