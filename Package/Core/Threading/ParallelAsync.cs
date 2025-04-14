#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Proto.Promises.Threading
{
    /// <summary>
    /// Provides support for parallel asynchronous loops and regions.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static partial class ParallelAsync
    {
        /// <summary>Executes a for loop in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <param name="fromIndex">The start index, inclusive.</param>
        /// <param name="toIndex">The end index, exclusive.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <returns>A promise that represents the entire for operation.</returns>
        public static Promise For(int fromIndex, int toIndex, Func<int, CancelationToken, Promise> body)
            => For(fromIndex, toIndex, default, body);

        /// <summary>Executes a for loop in which iterations may run in parallel.</summary>
        /// <param name="fromIndex">The start index, inclusive.</param>
        /// <param name="toIndex">The end index, exclusive.</param>
        /// <param name="parallelAsyncOptions">An object that configures the behavior of this operation.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise For(int fromIndex, int toIndex, ParallelAsyncOptions parallelAsyncOptions, Func<int, CancelationToken, Promise> body)
        {
            ValidateArgument(body, nameof(body), 1);
            return ParallelAsyncHelper.For(fromIndex, toIndex, DelegateWrapper.Create(body), parallelAsyncOptions);
        }

        /// <summary>Executes a for loop in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <param name="fromIndex">The start index, inclusive.</param>
        /// <param name="toIndex">The end index, exclusive.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise For<TCapture>(int fromIndex, int toIndex, TCapture captureValue, Func<TCapture, int, CancelationToken, Promise> body)
            => For(fromIndex, toIndex, default, captureValue, body);

        /// <summary>Executes a for loop in which iterations may run in parallel.</summary>
        /// <param name="fromIndex">The start index, inclusive.</param>
        /// <param name="toIndex">The end index, exclusive.</param>
        /// <param name="parallelAsyncOptions">An object that configures the behavior of this operation.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise For<TCapture>(int fromIndex, int toIndex, ParallelAsyncOptions parallelAsyncOptions, TCapture captureValue, Func<TCapture, int, CancelationToken, Promise> body)
        {
            ValidateArgument(body, nameof(body), 1);
            return ParallelAsyncHelper.For(fromIndex, toIndex, DelegateWrapper.Create(captureValue, body), parallelAsyncOptions);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ForEach<TSource>(IEnumerable<TSource> source, Func<TSource, CancelationToken, Promise> body)
            => ForEach(source, default, body);

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="parallelAsyncOptions">An object that configures the behavior of this operation.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ForEach<TSource>(IEnumerable<TSource> source, ParallelAsyncOptions parallelAsyncOptions, Func<TSource, CancelationToken, Promise> body)
        {
            ValidateArgument(source, nameof(source), 1);
            return ParallelAsync<TSource>.ForEach(source.GetEnumerator(), parallelAsyncOptions, body);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ForEach<TSource, TCapture>(IEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise> body)
            => ForEach(source, default, captureValue, body);

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="parallelAsyncOptions">An object that configures the behavior of this operation.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ForEach<TSource, TCapture>(IEnumerable<TSource> source, ParallelAsyncOptions parallelAsyncOptions, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise> body)
        {
            ValidateArgument(body, nameof(body), 1);
            return ParallelAsync<TSource>.ForEach(source.GetEnumerator(), parallelAsyncOptions, captureValue, body);
        }

        /// <summary>Executes a for each operation on an <see cref="AsyncEnumerable{T}"/> in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ForEach<TSource>(AsyncEnumerable<TSource> source, Func<TSource, CancelationToken, Promise> body)
            => ForEach(source, default, body);

        /// <summary>Executes a for each operation on an <see cref="AsyncEnumerable{T}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="parallelAsyncOptions">An object that configures the behavior of this operation.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ForEach<TSource>(AsyncEnumerable<TSource> source, ParallelAsyncOptions parallelAsyncOptions, Func<TSource, CancelationToken, Promise> body)
        {
            ValidateArgument(body, nameof(body), 1);
            return ParallelAsyncHelper<TSource>.ForEach(source, DelegateWrapper.Create(body), parallelAsyncOptions);
        }

        /// <summary>Executes a for each operation on an <see cref="AsyncEnumerable{T}"/> in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ForEach<TSource, TCapture>(AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise> body)
            => ForEach(source, default, captureValue, body);

        /// <summary>Executes a for each operation on an <see cref="AsyncEnumerable{T}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="parallelAsyncOptions">An object that configures the behavior of this operation.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ForEach<TSource, TCapture>(AsyncEnumerable<TSource> source, ParallelAsyncOptions parallelAsyncOptions, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise> body)
        {
            ValidateArgument(body, nameof(body), 1);
            return ParallelAsyncHelper<TSource>.ForEach(source, DelegateWrapper.Create(captureValue, body), parallelAsyncOptions);
        }
    }

    /// <summary>
    /// Provides support for parallel asynchronous loops and regions.
    /// </summary>
    /// <typeparam name="TSource">The type of the data in the source.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static partial class ParallelAsync<TSource>
    {
        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TEnumerator">The type of the enumerator.</typeparam>
        /// <param name="source">An enumerator data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ForEach<TEnumerator>(TEnumerator source, Func<TSource, CancelationToken, Promise> body)
            where TEnumerator : IEnumerator<TSource>
            => ForEach(source, default, body);

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TEnumerator">The type of the enumerator.</typeparam>
        /// <param name="source">An enumerator data source.</param>
        /// <param name="parallelAsyncOptions">An object that configures the behavior of this operation.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ForEach<TEnumerator>(TEnumerator source, ParallelAsyncOptions parallelAsyncOptions, Func<TSource, CancelationToken, Promise> body)
            where TEnumerator : IEnumerator<TSource>
        {
            ValidateArgument(source, nameof(source), 1);
            ValidateArgument(body, nameof(body), 1);
            return ParallelAsyncHelper<TSource>.ForEach(source, DelegateWrapper.Create(body), parallelAsyncOptions);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel on <see cref="Promise.Config.BackgroundContext"/>.</summary>
        /// <typeparam name="TEnumerator">The type of the enumerator.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerator data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ForEach<TEnumerator, TCapture>(TEnumerator source, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise> body)
            where TEnumerator : IEnumerator<TSource>
            => ForEach(source, default, captureValue, body);

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TEnumerator">The type of the enumerator.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">An enumerator data source.</param>
        /// <param name="captureValue">The captured value that will be passed to the <paramref name="body"/>.</param>
        /// <param name="parallelAsyncOptions">An object that configures the behavior of this operation.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/> argument or <paramref name="body"/> argument is null.</exception>
        /// <returns>A promise that represents the entire for each operation.</returns>
        public static Promise ForEach<TEnumerator, TCapture>(TEnumerator source, ParallelAsyncOptions parallelAsyncOptions, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise> body)
            where TEnumerator : IEnumerator<TSource>
        {
            ValidateArgument(source, nameof(source), 1);
            ValidateArgument(body, nameof(body), 1);
            return ParallelAsyncHelper<TSource>.ForEach(source, DelegateWrapper.Create(captureValue, body), parallelAsyncOptions);
        }
    }

    partial class ParallelAsync
    {
        // Calls to this get compiled away in RELEASE mode
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);

#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }
#endif
    }

    partial class ParallelAsync<TSource>
    {
        // Calls to this get compiled away in RELEASE mode
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);

#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }
#endif
    }
}