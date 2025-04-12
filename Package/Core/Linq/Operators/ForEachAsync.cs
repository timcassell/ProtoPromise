#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using System;

namespace Proto.Promises.Linq
{
    partial class AsyncEnumerable
    {
        private static async Promise ForEachCore<T, TAction>(AsyncEnumerator<T> asyncEnumerator, TAction action)
            where TAction : IFunc<T, Promise>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    await action.Invoke(asyncEnumerator.Current);
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        private static async Promise ForEachCore<T, TAction>(ConfiguredAsyncEnumerable<T>.Enumerator asyncEnumerator, TAction action)
            where TAction : IFunc<T, Promise>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    await action.Invoke(asyncEnumerator.Current);
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        private static async Promise ForEachWithIndexCore<T, TAction>(AsyncEnumerator<T> asyncEnumerator, TAction action)
            where TAction : IFunc<T, int, Promise>
        {
            try
            {
                int index = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    await action.Invoke(asyncEnumerator.Current, checked(index++));
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        private static async Promise ForEachWithIndexCore<T, TAction>(ConfiguredAsyncEnumerable<T>.Enumerator asyncEnumerator, TAction action)
            where TAction : IFunc<T, int, Promise>
        {
            try
            {
                int index = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    await action.Invoke(asyncEnumerator.Current, checked(index++));
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Invokes an <see cref="Action{T}"/> for each element in the <see cref="AsyncEnumerable{T}"/> sequence.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="action">Action to invoke for each element in the <see cref="AsyncEnumerable{T}"/> sequence.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        public static Promise ForEachAsync<T>(this AsyncEnumerable<T> source, Action<T> action, CancelationToken cancelationToken = default)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(action));
        }

        /// <summary>
        /// Invokes an <see cref="Action{TCapture, T}"/> for each element in the <see cref="AsyncEnumerable{T}"/> sequence, and the <paramref name="captureValue"/>.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="action"/>.</param>
        /// <param name="action">Action to invoke for each element in the <see cref="AsyncEnumerable{T}"/> sequence.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        public static Promise ForEachAsync<T, TCapture>(this AsyncEnumerable<T> source, TCapture captureValue, Action<TCapture, T> action, CancelationToken cancelationToken = default)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, action));
        }

        /// <summary>
        /// Invokes an <see cref="Action{T}"/> for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="action">Action to invoke for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        public static Promise ForEachAsync<T>(this in ConfiguredAsyncEnumerable<T> configuredSource, Action<T> action)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(action));
        }

        /// <summary>
        /// Invokes an <see cref="Action{TCapture, T}"/> for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence, and the <paramref name="captureValue"/>.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="action"/>.</param>
        /// <param name="action">Action to invoke for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        public static Promise ForEachAsync<T, TCapture>(this in ConfiguredAsyncEnumerable<T> configuredSource, TCapture captureValue, Action<TCapture, T> action)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, action));
        }

        /// <summary>
        /// Invokes an <see cref="Action{T, T}"/> for each element in the <see cref="AsyncEnumerable{T}"/> sequence, incorporating the element's index.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="action">Action to invoke for each element in the <see cref="AsyncEnumerable{T}"/> sequence.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        public static Promise ForEachAsync<T>(this AsyncEnumerable<T> source, Action<T, int> action, CancelationToken cancelationToken = default)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachWithIndexCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(action));
        }

        /// <summary>
        /// Invokes an <see cref="Action{TCapture, T, T}"/> for each element in the <see cref="AsyncEnumerable{T}"/> sequence, and the <paramref name="captureValue"/>, incorporating the element's index.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="action"/>.</param>
        /// <param name="action">Action to invoke for each element in the <see cref="AsyncEnumerable{T}"/> sequence.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        public static Promise ForEachAsync<T, TCapture>(this AsyncEnumerable<T> source, TCapture captureValue, Action<TCapture, T, int> action, CancelationToken cancelationToken = default)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachWithIndexCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, action));
        }

        /// <summary>
        /// Invokes an <see cref="Action{T, T}"/> for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence, incorporating the element's index.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="action">Action to invoke for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        public static Promise ForEachAsync<T>(this in ConfiguredAsyncEnumerable<T> configuredSource, Action<T, int> action)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachWithIndexCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(action));
        }

        /// <summary>
        /// Invokes an <see cref="Action{TCapture, T, T}"/> for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence, and the <paramref name="captureValue"/>, incorporating the element's index.
        /// The sequence will not be moved forward until the <see cref="Promise"/> returned from the <paramref name="action"/> is resolved.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="action"/>.</param>
        /// <param name="action">Action to invoke for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        public static Promise ForEachAsync<T, TCapture>(this in ConfiguredAsyncEnumerable<T> configuredSource, TCapture captureValue, Action<TCapture, T, int> action)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachWithIndexCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, action));
        }

        /// <summary>
        /// Invokes an <see cref="Func{T, TResult}"/> for each element in the <see cref="AsyncEnumerable{T}"/> sequence.
        /// The sequence will not be moved forward until the <see cref="Promise"/> returned from the <paramref name="asyncAction"/> is resolved.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="asyncAction">Action to invoke for each element in the <see cref="AsyncEnumerable{T}"/> sequence.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is null.</exception>
        public static Promise ForEachAsync<T>(this AsyncEnumerable<T> source, Func<T, Promise> asyncAction, CancelationToken cancelationToken = default)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(asyncAction));
        }

        /// <summary>
        /// Invokes an <see cref="Func{TCapture, T, TResult}"/> for each element in the <see cref="AsyncEnumerable{T}"/> sequence, and the <paramref name="captureValue"/>.
        /// The sequence will not be moved forward until the <see cref="Promise"/> returned from the <paramref name="asyncAction"/> is resolved.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="asyncAction"/>.</param>
        /// <param name="asyncAction">Action to invoke for each element in the <see cref="AsyncEnumerable{T}"/> sequence.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is null.</exception>
        public static Promise ForEachAsync<T, TCapture>(this AsyncEnumerable<T> source, TCapture captureValue, Func<TCapture, T, Promise> asyncAction, CancelationToken cancelationToken = default)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, asyncAction));
        }

        /// <summary>
        /// Invokes an <see cref="Func{T, TResult}"/> for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.
        /// The sequence will not be moved forward until the <see cref="Promise"/> returned from the <paramref name="asyncAction"/> is resolved.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="asyncAction">Action to invoke for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is null.</exception>
        public static Promise ForEachAsync<T>(this in ConfiguredAsyncEnumerable<T> configuredSource, Func<T, Promise> asyncAction)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(asyncAction));
        }

        /// <summary>
        /// Invokes an <see cref="Func{TCapture, T, TResult}"/> for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence, and the <paramref name="captureValue"/>.
        /// The sequence will not be moved forward until the <see cref="Promise"/> returned from the <paramref name="asyncAction"/> is resolved.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="asyncAction"/>.</param>
        /// <param name="asyncAction">Action to invoke for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is null.</exception>
        public static Promise ForEachAsync<T, TCapture>(this in ConfiguredAsyncEnumerable<T> configuredSource, TCapture captureValue, Func<TCapture, T, Promise> asyncAction)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, asyncAction));
        }

        /// <summary>
        /// Invokes an <see cref="Func{T, T, TResult}"/> for each element in the <see cref="AsyncEnumerable{T}"/> sequence, incorporating the element's index.
        /// The sequence will not be moved forward until the <see cref="Promise"/> returned from the <paramref name="asyncAction"/> is resolved.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="asyncAction">Action to invoke for each element in the <see cref="AsyncEnumerable{T}"/> sequence.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is null.</exception>
        public static Promise ForEachAsync<T>(this AsyncEnumerable<T> source, Func<T, int, Promise> asyncAction, CancelationToken cancelationToken = default)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachWithIndexCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(asyncAction));
        }

        /// <summary>
        /// Invokes an <see cref="Func{TCapture, T, T, TResult}"/> for each element in the <see cref="AsyncEnumerable{T}"/> sequence, and the <paramref name="captureValue"/>, incorporating the element's index.
        /// The sequence will not be moved forward until the <see cref="Promise"/> returned from the <paramref name="asyncAction"/> is resolved.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="asyncAction"/>.</param>
        /// <param name="asyncAction">Action to invoke for each element in the <see cref="AsyncEnumerable{T}"/> sequence.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is null.</exception>
        public static Promise ForEachAsync<T, TCapture>(this AsyncEnumerable<T> source, TCapture captureValue, Func<TCapture, T, int, Promise> asyncAction, CancelationToken cancelationToken = default)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachWithIndexCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, asyncAction));
        }

        /// <summary>
        /// Invokes an <see cref="Func{T, T, TResult}"/> for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence, incorporating the element's index.
        /// The sequence will not be moved forward until the <see cref="Promise"/> returned from the <paramref name="asyncAction"/> is resolved.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="asyncAction">Action to invoke for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is null.</exception>
        public static Promise ForEachAsync<T>(this in ConfiguredAsyncEnumerable<T> configuredSource, Func<T, int, Promise> asyncAction)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachWithIndexCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(asyncAction));
        }

        /// <summary>
        /// Invokes an <see cref="Func{TCapture, T, T, TResult}"/> for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence, and the <paramref name="captureValue"/>, incorporating the element's index.
        /// The sequence will not be moved forward until the <see cref="Promise"/> returned from the <paramref name="asyncAction"/> is resolved.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="asyncAction"/>.</param>
        /// <param name="asyncAction">Action to invoke for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is null.</exception>
        public static Promise ForEachAsync<T, TCapture>(this in ConfiguredAsyncEnumerable<T> configuredSource, TCapture captureValue, Func<TCapture, T, int, Promise> asyncAction)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachWithIndexCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, asyncAction));
        }
    }

    partial class AsyncEnumerable
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