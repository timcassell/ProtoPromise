#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Async.CompilerServices;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Invokes an <see cref="Action{T}"/> for each element in the <see cref="AsyncEnumerable{T}"/> sequence.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="action">Action to invoke for each element in the <see cref="AsyncEnumerable{T}"/> sequence.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        public static Promise ForEachAsync<T>(this AsyncEnumerable<T> source, Action<T> action, CancelationToken cancelationToken = default)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachCoreSync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(action));
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
        public static Promise ForEachAsync<T, TCapture>(this AsyncEnumerable<T> source, TCapture captureValue, Action<TCapture, T> action, CancelationToken cancelationToken = default)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachCoreSync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, action));
        }

        private static async Promise ForEachCoreSync<T, TAction>(AsyncEnumerator<T> asyncEnumerator, TAction action)
            where TAction : Internal.IAction<T>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    action.Invoke(asyncEnumerator.Current);
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Invokes an <see cref="Action{T}"/> for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="action">Action to invoke for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        public static Promise ForEachAsync<T>(this in ConfiguredAsyncEnumerable<T> configuredSource, Action<T> action)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachCoreSync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(action));
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
        public static Promise ForEachAsync<T, TCapture>(this in ConfiguredAsyncEnumerable<T> configuredSource, TCapture captureValue, Action<TCapture, T> action)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachCoreSync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, action));
        }

        private static async Promise ForEachCoreSync<T, TAction>(ConfiguredAsyncEnumerable<T>.Enumerator asyncEnumerator, TAction action)
            where TAction : Internal.IAction<T>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    action.Invoke(asyncEnumerator.Current);
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private readonly struct ActionElementIndex<T> : Internal.IAction<T, int>
        {
            private readonly Action<T, int> _action;

            [MethodImpl(Internal.InlineOption)]
            public ActionElementIndex(Action<T, int> action)
            {
                _action = action;
            }

            public void Invoke(T arg1, int arg2) => _action.Invoke(arg1, arg2);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private readonly struct ActionElementIndexCapture<T, TCapture> : Internal.IAction<T, int>
        {
            private readonly Action<TCapture, T, int> _action;
            private readonly TCapture _capturedValue;

            [MethodImpl(Internal.InlineOption)]
            public ActionElementIndexCapture(in TCapture capturedValue, Action<TCapture, T, int> action)
            {
                _action = action;
                _capturedValue = capturedValue;
            }

            public void Invoke(T arg1, int arg2) => _action.Invoke(_capturedValue, arg1, arg2);
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
        public static Promise ForEachAsync<T>(this AsyncEnumerable<T> source, Action<T, int> action, CancelationToken cancelationToken = default)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachWithIndexCoreSync(source.GetAsyncEnumerator(cancelationToken), new ActionElementIndex<T>(action));
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
        public static Promise ForEachAsync<T, TCapture>(this AsyncEnumerable<T> source, TCapture captureValue, Action<TCapture, T, int> action, CancelationToken cancelationToken = default)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachWithIndexCoreSync(source.GetAsyncEnumerator(cancelationToken), new ActionElementIndexCapture<T, TCapture>(captureValue, action));
        }

        private static async Promise ForEachWithIndexCoreSync<T, TAction>(AsyncEnumerator<T> asyncEnumerator, TAction action)
            where TAction : Internal.IAction<T, int>
        {
            try
            {
                int index = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    action.Invoke(asyncEnumerator.Current, checked(index++));
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Invokes an <see cref="Action{T, T}"/> for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence, incorporating the element's index.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="action">Action to invoke for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        public static Promise ForEachAsync<T>(this in ConfiguredAsyncEnumerable<T> configuredSource, Action<T, int> action)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachWithIndexCoreSync(configuredSource.GetAsyncEnumerator(), new ActionElementIndex<T>(action));
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
        public static Promise ForEachAsync<T, TCapture>(this in ConfiguredAsyncEnumerable<T> configuredSource, TCapture captureValue, Action<TCapture, T, int> action)
        {
            ValidateArgument(action, nameof(action), 1);

            return ForEachWithIndexCoreSync(configuredSource.GetAsyncEnumerator(), new ActionElementIndexCapture<T, TCapture>(captureValue, action));
        }

        private static async Promise ForEachWithIndexCoreSync<T, TAction>(ConfiguredAsyncEnumerable<T>.Enumerator asyncEnumerator, TAction action)
            where TAction : Internal.IAction<T, int>
        {
            try
            {
                int index = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    action.Invoke(asyncEnumerator.Current, checked(index++));
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
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
        public static Promise ForEachAsync<T>(this AsyncEnumerable<T> source, Func<T, Promise> asyncAction, CancelationToken cancelationToken = default)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachCoreAsync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(asyncAction));
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
        public static Promise ForEachAsync<T, TCapture>(this AsyncEnumerable<T> source, TCapture captureValue, Func<TCapture, T, Promise> asyncAction, CancelationToken cancelationToken = default)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachCoreAsync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, asyncAction));
        }

        private static async Promise ForEachCoreAsync<T, TAction>(AsyncEnumerator<T> asyncEnumerator, TAction action)
            where TAction : Internal.IFunc<T, Promise>
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

        /// <summary>
        /// Invokes an <see cref="Func{T, TResult}"/> for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.
        /// The sequence will not be moved forward until the <see cref="Promise"/> returned from the <paramref name="asyncAction"/> is resolved.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="asyncAction">Action to invoke for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        public static Promise ForEachAsync<T>(this in ConfiguredAsyncEnumerable<T> configuredSource, Func<T, Promise> asyncAction)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachCoreAsync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(asyncAction));
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
        public static Promise ForEachAsync<T, TCapture>(this in ConfiguredAsyncEnumerable<T> configuredSource, TCapture captureValue, Func<TCapture, T, Promise> asyncAction)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachCoreAsync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, asyncAction));
        }

        private static async Promise ForEachCoreAsync<T, TAction>(ConfiguredAsyncEnumerable<T>.Enumerator asyncEnumerator, TAction action)
            where TAction : Internal.IFunc<T, Promise>
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

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private readonly struct AsyncActionElementIndex<T> : Internal.IFunc<T, int, Promise>
        {
            private readonly Func<T, int, Promise> _action;

            [MethodImpl(Internal.InlineOption)]
            public AsyncActionElementIndex(Func<T, int, Promise> action)
            {
                _action = action;
            }

            public Promise Invoke(T arg1, int arg2) => _action.Invoke(arg1, arg2);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private readonly struct AsyncActionElementIndexCapture<T, TCapture> : Internal.IFunc<T, int, Promise>
        {
            private readonly Func<TCapture, T, int, Promise> _action;
            private readonly TCapture _capturedValue;

            [MethodImpl(Internal.InlineOption)]
            public AsyncActionElementIndexCapture(in TCapture capturedValue, Func<TCapture, T, int, Promise> action)
            {
                _action = action;
                _capturedValue = capturedValue;
            }

            public Promise Invoke(T arg1, int arg2) => _action.Invoke(_capturedValue, arg1, arg2);
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
        public static Promise ForEachAsync<T>(this AsyncEnumerable<T> source, Func<T, int, Promise> asyncAction, CancelationToken cancelationToken = default)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachWithIndexCoreAsync(source.GetAsyncEnumerator(cancelationToken), new AsyncActionElementIndex<T>(asyncAction));
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
        public static Promise ForEachAsync<T, TCapture>(this AsyncEnumerable<T> source, TCapture captureValue, Func<TCapture, T, int, Promise> asyncAction, CancelationToken cancelationToken = default)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachWithIndexCoreAsync(source.GetAsyncEnumerator(cancelationToken), new AsyncActionElementIndexCapture<T, TCapture>(captureValue, asyncAction));
        }

        private static async Promise ForEachWithIndexCoreAsync<T, TAction>(AsyncEnumerator<T> asyncEnumerator, TAction action)
            where TAction : Internal.IFunc<T, int, Promise>
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
        /// Invokes an <see cref="Func{T, T, TResult}"/> for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence, incorporating the element's index.
        /// The sequence will not be moved forward until the <see cref="Promise"/> returned from the <paramref name="asyncAction"/> is resolved.
        /// Returns a <see cref="Promise"/> that represents the entire operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="asyncAction">Action to invoke for each element in the <see cref="ConfiguredAsyncEnumerable{T}"/> sequence.</param>
        /// <returns><see cref="Promise"/> that signals the termination of the sequence.</returns>
        public static Promise ForEachAsync<T>(this in ConfiguredAsyncEnumerable<T> configuredSource, Func<T, int, Promise> asyncAction)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachWithIndexCoreAsync(configuredSource.GetAsyncEnumerator(), new AsyncActionElementIndex<T>(asyncAction));
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
        public static Promise ForEachAsync<T, TCapture>(this in ConfiguredAsyncEnumerable<T> configuredSource, TCapture captureValue, Func<TCapture, T, int, Promise> asyncAction)
        {
            ValidateArgument(asyncAction, nameof(asyncAction), 1);

            return ForEachWithIndexCoreAsync(configuredSource.GetAsyncEnumerator(), new AsyncActionElementIndexCapture<T, TCapture>(captureValue, asyncAction));
        }

        private static async Promise ForEachWithIndexCoreAsync<T, TAction>(ConfiguredAsyncEnumerable<T>.Enumerator asyncEnumerator, TAction action)
            where TAction : Internal.IFunc<T, int, Promise>
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
#endif // CSHARP_7_3_OR_NEWER
}