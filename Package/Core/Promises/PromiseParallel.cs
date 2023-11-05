#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

using Proto.Promises.Linq;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Proto.Promises
{
    public partial struct Promise
    {
        /// <summary>Executes a for loop in which iterations may run in parallel on <see cref="Config.BackgroundContext"/>.</summary>
        /// <param name="fromIndex">The start index, inclusive.</param>
        /// <param name="toIndex">The end index, exclusive.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelFor(int fromIndex, int toIndex, Func<int, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            ValidateArgument(body, "body", 1);

            return Internal.ParallelFor(fromIndex, toIndex, new Internal.ParallelBody<int>(body), cancelationToken, Config.BackgroundContext, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for loop in which iterations may run in parallel on <see cref="Config.BackgroundContext"/>.</summary>
        /// <param name="fromIndex">The start index, inclusive.</param>
        /// <param name="toIndex">The end index, exclusive.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelFor(int fromIndex, int toIndex, Func<int, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            ValidateArgument(body, "body", 1);

            return Internal.ParallelFor(fromIndex, toIndex, new Internal.ParallelBody<int>(body), cancelationToken, synchronizationContext, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for loop in which iterations may run in parallel on <see cref="Config.BackgroundContext"/>.</summary>
        /// <param name="fromIndex">The start index, inclusive.</param>
        /// <param name="toIndex">The end index, exclusive.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelFor<TCapture>(int fromIndex, int toIndex, TCapture captureValue, Func<int, TCapture, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            ValidateArgument(body, "body", 1);

            return Internal.ParallelFor(fromIndex, toIndex, new Internal.ParallelCaptureBody<int, TCapture>(captureValue, body), cancelationToken, Config.BackgroundContext, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for loop in which iterations may run in parallel on <see cref="Config.BackgroundContext"/>.</summary>
        /// <param name="fromIndex">The start index, inclusive.</param>
        /// <param name="toIndex">The end index, exclusive.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelFor<TCapture>(int fromIndex, int toIndex, TCapture captureValue, Func<int, TCapture, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            ValidateArgument(body, "body", 1);

            return Internal.ParallelFor(fromIndex, toIndex, new Internal.ParallelCaptureBody<int, TCapture>(captureValue, body), cancelationToken, synchronizationContext, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel on <see cref="Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEach<TSource>(IEnumerable<TSource> source, Func<TSource, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            return Promise<TSource>.ParallelForEach(source, body, Config.BackgroundContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEach<TSource>(IEnumerable<TSource> source, Func<TSource, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            return Promise<TSource>.ParallelForEach(source, body, synchronizationContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel on <see cref="Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEach<TSource, TCapture>(IEnumerable<TSource> source, TCapture captureValue, Func<TSource, TCapture, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            return Promise<TSource>.ParallelForEach(source, captureValue, body, Config.BackgroundContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEach<TSource, TCapture>(IEnumerable<TSource> source, TCapture captureValue, Func<TSource, TCapture, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            return Promise<TSource>.ParallelForEach(source, captureValue, body, synchronizationContext, cancelationToken, maxDegreeOfParallelism);
        }

#if NET47_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
        /// <summary>Executes a for each operation on an <see cref="IAsyncEnumerable{T}"/> in which iterations may run in parallel on <see cref="Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            return Promise<TSource>.ParallelForEachAsync(source, body, Config.BackgroundContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IAsyncEnumerable{T}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            return Promise<TSource>.ParallelForEachAsync(source, body, synchronizationContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IAsyncEnumerable{T}"/> in which iterations may run in parallel on <see cref="Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync<TSource, TCapture>(IAsyncEnumerable<TSource> source, TCapture captureValue, Func<TSource, TCapture, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            return Promise<TSource>.ParallelForEachAsync(source, captureValue, body, Config.BackgroundContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IAsyncEnumerable{T}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync<TSource, TCapture>(IAsyncEnumerable<TSource> source, TCapture captureValue, Func<TSource, TCapture, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            return Promise<TSource>.ParallelForEachAsync(source, captureValue, body, synchronizationContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="AsyncEnumerable{T}"/> in which iterations may run in parallel on <see cref="Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync<TSource>(AsyncEnumerable<TSource> source, Func<TSource, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            return Promise<TSource>.ParallelForEachAsync(source, body, Config.BackgroundContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="AsyncEnumerable{T}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync<TSource>(AsyncEnumerable<TSource> source, Func<TSource, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            return Promise<TSource>.ParallelForEachAsync(source, body, synchronizationContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="AsyncEnumerable{T}"/> in which iterations may run in parallel on <see cref="Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync<TSource, TCapture>(AsyncEnumerable<TSource> source, TCapture captureValue, Func<TSource, TCapture, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            return Promise<TSource>.ParallelForEachAsync(source, captureValue, body, Config.BackgroundContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="AsyncEnumerable{T}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync<TSource, TCapture>(AsyncEnumerable<TSource> source, TCapture captureValue, Func<TSource, TCapture, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            return Promise<TSource>.ParallelForEachAsync(source, captureValue, body, synchronizationContext, cancelationToken, maxDegreeOfParallelism);
        }
#endif // NET47_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
    }

    public partial struct Promise<T>
    {
        /// <summary>Executes a for each operation on an <see cref="IEnumerable{T}"/> in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEach(IEnumerable<T> source, Func<T, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            ValidateArgument(source, "source", 1);

            return ParallelForEach(source.GetEnumerator(), body, Promise.Config.BackgroundContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{T}"/> in which iterations may run in parallel.</summary>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEach(IEnumerable<T> source, Func<T, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            ValidateArgument(source, "source", 1);

            return ParallelForEach(source.GetEnumerator(), body, synchronizationContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{T}"/> in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEach<TCapture>(IEnumerable<T> source, TCapture captureValue, Func<T, TCapture, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            ValidateArgument(source, "source", 1);

            return ParallelForEach(source.GetEnumerator(), captureValue, body, Promise.Config.BackgroundContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{T}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEach<TCapture>(IEnumerable<T> source, TCapture captureValue, Func<T, TCapture, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            ValidateArgument(source, "source", 1);

            return ParallelForEach(source.GetEnumerator(), captureValue, body, synchronizationContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerator{T}"/> in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TEnumerator">The type of the enumerator.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEach<TEnumerator>(TEnumerator source, Func<T, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
            where TEnumerator : IEnumerator<T>
        {
            return ParallelForEach(source, body, Promise.Config.BackgroundContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerator{T}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TEnumerator">The type of the enumerator.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEach<TEnumerator>(TEnumerator source, Func<T, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
            where TEnumerator : IEnumerator<T>
        {
            ValidateArgument(source, "source", 1);
            ValidateArgument(body, "body", 1);

            return Internal.ParallelForEach<TEnumerator, Internal.ParallelBody<T>, T>(source, new Internal.ParallelBody<T>(body), cancelationToken, synchronizationContext, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerator{T}"/> in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TEnumerator">The type of the enumerator.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEach<TEnumerator, TCapture>(TEnumerator source, TCapture captureValue, Func<T, TCapture, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
            where TEnumerator : IEnumerator<T>
        {
            return ParallelForEach(source, captureValue, body, Promise.Config.BackgroundContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerator{T}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TEnumerator">The type of the enumerator.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEach<TEnumerator, TCapture>(TEnumerator source, TCapture captureValue, Func<T, TCapture, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
            where TEnumerator : IEnumerator<T>
        {
            ValidateArgument(source, "source", 1);
            ValidateArgument(body, "body", 1);

            return Internal.ParallelForEach<TEnumerator, Internal.ParallelCaptureBody<T, TCapture>, T>(source, new Internal.ParallelCaptureBody<T, TCapture>(captureValue, body), cancelationToken, synchronizationContext, maxDegreeOfParallelism);
        }

#if NET47_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
        /// <summary>Executes a for each operation on an <see cref="IAsyncEnumerable{T}"/> in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync(IAsyncEnumerable<T> source, Func<T, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            ValidateArgument(source, "source", 1);

            return ParallelForEachAsync(source, body, Promise.Config.BackgroundContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IAsyncEnumerable{T}"/> in which iterations may run in parallel.</summary>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync(IAsyncEnumerable<T> source, Func<T, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            ValidateArgument(source, "source", 1);

            return ParallelForEachAsync(source.ToAsyncEnumerable(), body, synchronizationContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IAsyncEnumerable{T}"/> in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync<TCapture>(IAsyncEnumerable<T> source, TCapture captureValue, Func<T, TCapture, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            ValidateArgument(source, "source", 1);

            return ParallelForEachAsync(source, captureValue, body, Promise.Config.BackgroundContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="IAsyncEnumerable{T}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync<TCapture>(IAsyncEnumerable<T> source, TCapture captureValue, Func<T, TCapture, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            ValidateArgument(source, "source", 1);

            return ParallelForEachAsync(source.ToAsyncEnumerable(), captureValue, body, synchronizationContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="AsyncEnumerable{T}"/> in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync(AsyncEnumerable<T> source, Func<T, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            return ParallelForEachAsync(source, body, Promise.Config.BackgroundContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="AsyncEnumerable{T}"/> in which iterations may run in parallel.</summary>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync(AsyncEnumerable<T> source, Func<T, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            ValidateArgument(body, "body", 1);

            return Internal.ParallelForEachAsync(source, new Internal.ParallelBody<T>(body), cancelationToken, synchronizationContext, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="AsyncEnumerable{T}"/> in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync<TCapture>(AsyncEnumerable<T> source, TCapture captureValue, Func<T, TCapture, CancelationToken, Promise> body,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            return ParallelForEachAsync(source, captureValue, body, Promise.Config.BackgroundContext, cancelationToken, maxDegreeOfParallelism);
        }

        /// <summary>Executes a for each operation on an <see cref="AsyncEnumerable{T}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <param name="synchronizationContext">The synchronization context on which the iterations will be ran. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="cancelationToken">A cancelation token that may be used to cancel the for each operation.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of concurrent iterations. If -1, this value will be set to <see cref="Environment.ProcessorCount"/>.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ParallelForEachAsync<TCapture>(AsyncEnumerable<T> source, TCapture captureValue, Func<T, TCapture, CancelationToken, Promise> body, SynchronizationContext synchronizationContext,
            CancelationToken cancelationToken = default(CancelationToken), int maxDegreeOfParallelism = -1)
        {
            ValidateArgument(body, "body", 1);

            return Internal.ParallelForEachAsync(source, new Internal.ParallelCaptureBody<T, TCapture>(captureValue, body), cancelationToken, synchronizationContext, maxDegreeOfParallelism);
        }
#endif // NET47_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
    }
}