# Channels

A channel is an async producer/consumer data structure, similar to a blocking collection. You can use it to move data from 1 or more producers to 1 or more consumers asynchronously. Both bounded and unbounded channels are supported.

`Proto.Promises.Channels.Channel<T>` was designed very similar to `System.Threading.Channels.Channel<T>`. See the [BCL documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels) to see how channels may be typically used. Channels in this library work very similarly, but were also designed to not allocate. When you no longer need the channel, you can `Dispose` it to return the backing object to the pool for future re-use.

Another difference from the BCL design is, if the channeled objects need to be cleaned up, and you are working with a bounded channel, you can retrieve the dropped item and clean it up, or try to write it to the channel again. `if (channelWriteResult.TryGetDroppedItem(out var droppedItem)) { ... }`