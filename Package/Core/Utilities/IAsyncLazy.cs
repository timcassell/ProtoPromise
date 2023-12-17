#pragma warning disable IDE0034 // Simplify 'default' expression

namespace Proto.Promises
{
    /// <summary>
    /// Defines a provider for asynchronous lazy initialization.
    /// </summary>
    /// <typeparam name="T">The type of object that is being lazily initialized.</typeparam>
    public interface IAsyncLazy<T>
    {
        /// <summary>
        /// Starts the asynchronous factory method, if it has not already started, and returns the resulting <see cref="Promise{T}"/>.
        /// </summary>
        Promise<T> GetResultAsync(ProgressToken progressToken = default(ProgressToken));
    }

    partial class Extensions
    {
        /// <summary>
        /// Asynchronous infrastructure support. This method permits instances of <see cref="IAsyncLazy{T}"/> to be awaited.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static Async.CompilerServices.PromiseAwaiter<T> GetAwaiter<T>(this IAsyncLazy<T> lazy)
        {
            return lazy.GetResultAsync().GetAwaiter();
        }
    }
}