#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0270 // Use coalesce expression
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

using Proto.Promises.Async.CompilerServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    public partial struct Promise
    {
        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Sequence(params Func<Promise>[] promiseFuncs)
        {
            return Sequence(default(CancelationToken), promiseFuncs.GetGenericEnumerator());
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled before all of the <paramref name="promiseFuncs"/> have been invoked,
        /// the <paramref name="promiseFuncs"/> will stop being invoked, and the returned <see cref="Promise"/> will be canceled.
        /// </summary>
        public static Promise Sequence(CancelationToken cancelationToken, params Func<Promise>[] promiseFuncs)
        {
            return Sequence(cancelationToken, promiseFuncs.GetGenericEnumerator());
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Sequence(IEnumerable<Func<Promise>> promiseFuncs)
        {
            return Sequence(default(CancelationToken), promiseFuncs.GetEnumerator());
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled before all of the <paramref name="promiseFuncs"/> have been invoked,
        /// the <paramref name="promiseFuncs"/> will stop being invoked, and the returned <see cref="Promise"/> will be canceled.
        /// </summary>
        public static Promise Sequence(CancelationToken cancelationToken, IEnumerable<Func<Promise>> promiseFuncs)
        {
            return Sequence(cancelationToken, promiseFuncs.GetEnumerator());
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Sequence<TEnumerator>(TEnumerator promiseFuncs) where TEnumerator : IEnumerator<Func<Promise>>
        {
            return Sequence(default(CancelationToken), promiseFuncs);
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled before all of the <paramref name="promiseFuncs"/> have been invoked,
        /// the <paramref name="promiseFuncs"/> will stop being invoked, and the returned <see cref="Promise"/> will be canceled.
        /// </summary>
        public static Promise Sequence<TEnumerator>(CancelationToken cancelationToken, TEnumerator promiseFuncs) where TEnumerator : IEnumerator<Func<Promise>>
        {
            ValidateArgument(promiseFuncs, "promiseFuncs", 2);

            using (promiseFuncs)
            {
                if (!promiseFuncs.MoveNext())
                {
                    return Internal.CreateResolved(0);
                }

                // Invoke funcs and normalize the progress.
                var promise = new Promise(null, 0, Internal.NegativeOneDepth);
                do
                {
                    promise = promise.Then(promiseFuncs.Current, cancelationToken);
                } while (promiseFuncs.MoveNext());
                return promise;
            }
        }

        private static Promise SwitchToContext(SynchronizationOption synchronizationOption, bool forceAsync)
        {
            return Internal.CreateResolved(0)
                .WaitAsync(synchronizationOption, forceAsync);
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/> that will resolve on the foreground context.
        /// </summary>
        /// <param name="forceAsync">If true, forces the context switch to happen asynchronously.</param>
        public static Promise SwitchToForeground(bool forceAsync = false)
        {
            return SwitchToContext(SynchronizationOption.Foreground, forceAsync);
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/> that will resolve on the background context.
        /// </summary>
        /// <param name="forceAsync">If true, forces the context switch to happen asynchronously.</param>
        public static Promise SwitchToBackground(bool forceAsync = false)
        {
            return SwitchToContext(SynchronizationOption.Background, forceAsync);
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/> that will resolve on the provided <paramref name="synchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The context to switch to. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the context switch to happen asynchronously.</param>
        public static Promise SwitchToContext(SynchronizationContext synchronizationContext, bool forceAsync = false)
        {
            return Internal.CreateResolved(0)
                .WaitAsync(synchronizationContext, forceAsync);
        }

        /// <summary>
        /// Switch to the foreground context in an async method.
        /// </summary>
        /// <param name="forceAsync">If true, forces the context switch to happen asynchronously.</param>
        /// <remarks>
        /// This method is equivalent to <see cref="SwitchToForeground(bool)"/>, but is more efficient when used directly with the <see langword="await"/> keyword.
        /// </remarks>
        public static PromiseSwitchToContextAwaiter SwitchToForegroundAwait(bool forceAsync = false)
        {
            var context = Config.ForegroundContext;
            if (context == null)
            {
                throw new InvalidOperationException("Promise.Config.ForegroundContext is null. You should set Promise.Config.ForegroundContext at the start of your application" +
                    " (which may be as simple as 'Promise.Config.ForegroundContext = SynchronizationContext.Current;'). Alternatively, you may pass the context directly via SwitchToContextAwait(context).",
                    Internal.GetFormattedStacktrace(1));
            }
            return new PromiseSwitchToContextAwaiter(context, forceAsync);
        }

        /// <summary>
        /// Switch to the background context in an async method.
        /// </summary>
        /// <param name="forceAsync">If true, forces the context switch to happen asynchronously.</param>
        /// <remarks>
        /// This method is equivalent to <see cref="SwitchToBackground(bool)"/>, butbut is more efficient when used directly with the <see langword="await"/> keyword.
        /// </remarks>
        public static PromiseSwitchToContextAwaiter SwitchToBackgroundAwait(bool forceAsync = false)
        {
            return new PromiseSwitchToContextAwaiter(Config.BackgroundContext, forceAsync);
        }

        /// <summary>
        /// Switch to the provided context in an async method.
        /// </summary>
        /// <param name="synchronizationContext">The context to switch to. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the context switch to happen asynchronously.</param>
        /// <remarks>
        /// This method is equivalent to <see cref="SwitchToContext(SynchronizationContext, bool)"/>, but is more efficient when used directly with the <see langword="await"/> keyword.
        /// </remarks>
        public static PromiseSwitchToContextAwaiter SwitchToContextAwait(SynchronizationContext synchronizationContext, bool forceAsync = false)
        {
            return new PromiseSwitchToContextAwaiter(synchronizationContext, forceAsync);
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/>. <paramref name="resolver"/> is invoked with a <see cref="Deferred"/> that controls the state of the new <see cref="Promise"/>.
        /// You may provide a <paramref name="synchronizationOption"/> to control the context on which the <paramref name="resolver"/> is invoked.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="resolver">The resolver delegate that will control the completion of the returned <see cref="Promise"/> via the passed in <see cref="Deferred"/>.</param>
        /// <param name="synchronizationOption">Indicates on which context the <paramref name="resolver"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces the <paramref name="resolver"/> to be invoked asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
		public static Promise New(Action<Deferred> resolver, SynchronizationOption synchronizationOption = SynchronizationOption.Synchronous, bool forceAsync = false)
        {
            ValidateArgument(resolver, "resolver", 1);

            Deferred deferred = Deferred.New();
            Run(ValueTuple.Create(deferred, resolver), cv =>
            {
                Deferred def = cv.Item1;
                try
                {
                    cv.Item2.Invoke(def);
                }
                catch (OperationCanceledException)
                {
                    def.TryCancel(); // Don't rethrow cancelation.
                }
                catch (Exception e)
                {
                    if (!def.TryReject(e)) throw;
                }
            }, synchronizationOption, forceAsync)
                .Forget();
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/>. <paramref name="resolver"/> is invoked with a <see cref="Deferred"/> that controls the state of the new <see cref="Promise"/> on the provided <paramref name="synchronizationContext"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="resolver">The resolver delegate that will control the completion of the returned <see cref="Promise"/> via the passed in <see cref="Deferred"/>.</param>
        /// <param name="synchronizationContext">The context on which the <paramref name="resolver"/> will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the <paramref name="resolver"/> to be invoked asynchronously.</param>
		public static Promise New(Action<Deferred> resolver, SynchronizationContext synchronizationContext, bool forceAsync = false)
        {
            ValidateArgument(resolver, "resolver", 1);

            Deferred deferred = Deferred.New();
            Run(ValueTuple.Create(deferred, resolver), cv =>
            {
                Deferred def = cv.Item1;
                try
                {
                    cv.Item2.Invoke(def);
                }
                catch (OperationCanceledException)
                {
                    def.TryCancel(); // Don't rethrow cancelation.
                }
                catch (Exception e)
                {
                    if (!def.TryReject(e)) throw;
                }
            }, synchronizationContext, forceAsync)
                .Forget();
            return deferred.Promise;
        }

        [Obsolete("Prefer Promise<T>.New()"), EditorBrowsable(EditorBrowsableState.Never)]
        public static Promise<T> New<T>(Action<Promise<T>.Deferred> resolver, SynchronizationOption synchronizationOption = SynchronizationOption.Synchronous, bool forceAsync = false)
        {
            return Promise<T>.New(resolver, synchronizationOption, forceAsync);
        }

        [Obsolete("Prefer Promise<T>.New()"), EditorBrowsable(EditorBrowsableState.Never)]
        public static Promise<T> New<T>(Action<Promise<T>.Deferred> resolver, SynchronizationContext synchronizationContext, bool forceAsync = false)
        {
            return Promise<T>.New(resolver, synchronizationContext, forceAsync);
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/>. <paramref name="resolver"/> is invoked with <paramref name="captureValue"/> and a <see cref="Deferred"/> that controls the state of the <see cref="Promise"/>.
        /// You may provide a <paramref name="synchronizationOption"/> to control the context on which the <paramref name="resolver"/> is invoked.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="captureValue">The value that will be passed to <paramref name="resolver"/>.</param>
        /// <param name="resolver">The resolver delegate that will control the completion of the returned <see cref="Promise"/> via the passed in <see cref="Deferred"/>.</param>
        /// <param name="synchronizationOption">Indicates on which context the <paramref name="resolver"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces the <paramref name="resolver"/> to be invoked asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
        public static Promise New<TCapture>(TCapture captureValue, Action<TCapture, Deferred> resolver, SynchronizationOption synchronizationOption = SynchronizationOption.Synchronous, bool forceAsync = false)
        {
            ValidateArgument(resolver, "resolver", 1);

            Deferred deferred = Deferred.New();
            Run(ValueTuple.Create(deferred, resolver, captureValue), cv =>
            {
                Deferred def = cv.Item1;
                try
                {
                    cv.Item2.Invoke(cv.Item3, def);
                }
                catch (OperationCanceledException)
                {
                    def.TryCancel(); // Don't rethrow cancelation.
                }
                catch (Exception e)
                {
                    if (!def.TryReject(e)) throw;
                }
            }, synchronizationOption, forceAsync)
                .Forget();
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/>. <paramref name="resolver"/> is invoked with <paramref name="captureValue"/> and a <see cref="Deferred"/> that controls the state of the <see cref="Promise"/> on the provided <paramref name="synchronizationContext"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="captureValue">The value that will be passed to <paramref name="resolver"/>.</param>
        /// <param name="resolver">The resolver delegate that will control the completion of the returned <see cref="Promise"/> via the passed in <see cref="Deferred"/>.</param>
        /// <param name="synchronizationContext">The context on which the <paramref name="resolver"/> will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the <paramref name="resolver"/> to be invoked asynchronously.</param>
        public static Promise New<TCapture>(TCapture captureValue, Action<TCapture, Deferred> resolver, SynchronizationContext synchronizationContext, bool forceAsync = false)
        {
            ValidateArgument(resolver, "resolver", 1);

            Deferred deferred = Deferred.New();
            Run(ValueTuple.Create(deferred, resolver, captureValue), cv =>
            {
                Deferred def = cv.Item1;
                try
                {
                    cv.Item2.Invoke(cv.Item3, def);
                }
                catch (OperationCanceledException)
                {
                    def.TryCancel(); // Don't rethrow cancelation.
                }
                catch (Exception e)
                {
                    if (!def.TryReject(e)) throw;
                }
            }, synchronizationContext, forceAsync)
                .Forget();
            return deferred.Promise;
        }

        [Obsolete("Prefer Promise<T>.New()"), EditorBrowsable(EditorBrowsableState.Never)]
        public static Promise<T> New<TCapture, T>(TCapture captureValue, Action<TCapture, Promise<T>.Deferred> resolver, SynchronizationOption synchronizationOption = SynchronizationOption.Synchronous, bool forceAsync = false)
        {
            return Promise<T>.New(captureValue, resolver, synchronizationOption, forceAsync);
        }

        [Obsolete("Prefer Promise<T>.New()"), EditorBrowsable(EditorBrowsableState.Never)]
        public static Promise<T> New<TCapture, T>(TCapture captureValue, Action<TCapture, Promise<T>.Deferred> resolver, SynchronizationContext synchronizationContext, bool forceAsync = false)
        {
            return Promise<T>.New(captureValue, resolver, synchronizationContext, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="action"/> on the provided <paramref name="synchronizationOption"/> context. Returns a new <see cref="Promise"/> that will be resolved when the <paramref name="action"/> returns successfully.
        /// <para/>If the <paramref name="action"/> throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="action">The delegate that will be invoked.</param>
        /// <param name="synchronizationOption">Indicates on which context the <paramref name="action"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
		public static Promise Run(Action action, SynchronizationOption synchronizationOption = SynchronizationOption.Background, bool forceAsync = true)
        {
            ValidateArgument(action, "action", 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.Run(Internal.PromiseRefBase.DelegateWrapper.Create(action), (Internal.SynchronizationOption) synchronizationOption, null, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="action"/> with <paramref name="captureValue"/> on the provided <paramref name="synchronizationOption"/> context. Returns a new <see cref="Promise"/> that will be resolved when the <paramref name="action"/> returns successfully.
        /// <para/>If the <paramref name="action"/> throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="captureValue">The value that will be passed to <paramref name="action"/>.</param>
        /// <param name="action">The delegate that will be invoked.</param>
        /// <param name="synchronizationOption">Indicates on which context the <paramref name="action"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
		public static Promise Run<TCapture>(TCapture captureValue, Action<TCapture> action, SynchronizationOption synchronizationOption = SynchronizationOption.Background, bool forceAsync = true)
        {
            ValidateArgument(action, "action", 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.Run(Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, action), (Internal.SynchronizationOption) synchronizationOption, null, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="action"/>  on the provided <paramref name="synchronizationContext"/>. Returns a new <see cref="Promise"/> that will be resolved when the <paramref name="action"/> returns successfully.
        /// <para/>If the <paramref name="action"/> throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="action">The delegate that will be invoked.</param>
        /// <param name="synchronizationContext">The context on which the <paramref name="action"/> will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously.</param>
		public static Promise Run(Action action, SynchronizationContext synchronizationContext, bool forceAsync = true)
        {
            ValidateArgument(action, "action", 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.Run(Internal.PromiseRefBase.DelegateWrapper.Create(action), Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="action"/> with <paramref name="captureValue"/> on the provided <paramref name="synchronizationContext"/>. Returns a new <see cref="Promise"/> that will be resolved when the <paramref name="action"/> returns successfully.
        /// <para/>If the <paramref name="action"/> throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="captureValue">The value that will be passed to <paramref name="action"/>.</param>
        /// <param name="action">The delegate that will be invoked.</param>
        /// <param name="synchronizationContext">The context on which the <paramref name="action"/> will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously.</param>
		public static Promise Run<TCapture>(TCapture captureValue, Action<TCapture> action, SynchronizationContext synchronizationContext, bool forceAsync = true)
        {
            ValidateArgument(action, "action", 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.Run(Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, action), Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="function"/> on the provided <paramref name="synchronizationOption"/> context. Returns a new <see cref="Promise{T}"/> that will be resolved with the value returned by the <paramref name="function"/>.
        /// <para/>If the <paramref name="function"/> throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="function">The delegate that will be invoked.</param>
        /// <param name="synchronizationOption">Indicates on which context the <paramref name="function"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
		public static Promise<T> Run<T>(Func<T> function, SynchronizationOption synchronizationOption = SynchronizationOption.Background, bool forceAsync = true)
        {
            ValidateArgument(function, "function", 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.Run(Internal.PromiseRefBase.DelegateWrapper.Create(function), (Internal.SynchronizationOption) synchronizationOption, null, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="function"/> with <paramref name="captureValue"/> on the provided <paramref name="synchronizationOption"/> context. Returns a new <see cref="Promise{T}"/> that will be resolved with the value returned by the <paramref name="function"/>.
        /// <para/>If the <paramref name="function"/> throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="captureValue">The value that will be passed to <paramref name="function"/>.</param>
        /// <param name="function">The delegate that will be invoked.</param>
        /// <param name="synchronizationOption">Indicates on which context the <paramref name="function"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
		public static Promise<T> Run<TCapture, T>(TCapture captureValue, Func<TCapture, T> function, SynchronizationOption synchronizationOption = SynchronizationOption.Background, bool forceAsync = true)
        {
            ValidateArgument(function, "function", 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.Run(Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, function), (Internal.SynchronizationOption) synchronizationOption, null, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="function"/>  on the provided <paramref name="synchronizationContext"/>. Returns a new <see cref="Promise{T}"/> that will be resolved with the value returned by the <paramref name="function"/>.
        /// <para/>If the <paramref name="function"/> throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="function">The delegate that will be invoked.</param>
        /// <param name="synchronizationContext">The context on which the <paramref name="function"/> will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously.</param>
		public static Promise<T> Run<T>(Func<T> function, SynchronizationContext synchronizationContext, bool forceAsync = true)
        {
            ValidateArgument(function, "function", 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.Run(Internal.PromiseRefBase.DelegateWrapper.Create(function), Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="function"/> with <paramref name="captureValue"/> on the provided <paramref name="synchronizationContext"/>. Returns a new <see cref="Promise{T}"/> that will be resolved with the value returned by the <paramref name="function"/>.
        /// <para/>If the <paramref name="function"/> throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="captureValue">The value that will be passed to <paramref name="function"/>.</param>
        /// <param name="function">The delegate that will be invoked.</param>
        /// <param name="synchronizationContext">The context on which the <paramref name="function"/> will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously.</param>
		public static Promise<T> Run<TCapture, T>(TCapture captureValue, Func<TCapture, T> function, SynchronizationContext synchronizationContext, bool forceAsync = true)
        {
            ValidateArgument(function, "function", 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.Run(Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, function), Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="function"/> on the provided <paramref name="synchronizationOption"/> context. Returns a new <see cref="Promise"/> that will adopt the state of the <see cref="Promise"/> returned from the <paramref name="function"/>.
        /// <para/>If the <paramref name="function"/> throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="function">The delegate that will be invoked.</param>
        /// <param name="synchronizationOption">Indicates on which context the <paramref name="function"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
		public static Promise Run(Func<Promise> function, SynchronizationOption synchronizationOption = SynchronizationOption.Background, bool forceAsync = true)
        {
            ValidateArgument(function, "function", 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.RunWait(Internal.PromiseRefBase.DelegateWrapper.Create(function), (Internal.SynchronizationOption) synchronizationOption, null, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="function"/> with <paramref name="captureValue"/> on the provided <paramref name="synchronizationOption"/> context. Returns a new <see cref="Promise"/> that will adopt the state of the <see cref="Promise"/> returned from the <paramref name="function"/>.
        /// <para/>If the <paramref name="function"/> throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="captureValue">The value that will be passed to <paramref name="function"/>.</param>
        /// <param name="function">The delegate that will be invoked.</param>
        /// <param name="synchronizationOption">Indicates on which context the <paramref name="function"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
		public static Promise Run<TCapture>(TCapture captureValue, Func<TCapture, Promise> function, SynchronizationOption synchronizationOption = SynchronizationOption.Background, bool forceAsync = true)
        {
            ValidateArgument(function, "function", 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.RunWait(Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, function), (Internal.SynchronizationOption) synchronizationOption, null, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="function"/>  on the provided <paramref name="synchronizationContext"/>. Returns a new <see cref="Promise"/> that will adopt the state of the <see cref="Promise"/> returned from the <paramref name="function"/>.
        /// <para/>If the <paramref name="function"/> throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="function">The delegate that will be invoked.</param>
        /// <param name="synchronizationContext">The context on which the <paramref name="function"/> will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously.</param>
		public static Promise Run(Func<Promise> function, SynchronizationContext synchronizationContext, bool forceAsync = true)
        {
            ValidateArgument(function, "function", 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.RunWait(Internal.PromiseRefBase.DelegateWrapper.Create(function), Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="function"/> with <paramref name="captureValue"/> on the provided <paramref name="synchronizationContext"/>. Returns a new <see cref="Promise"/> that will adopt the state of the <see cref="Promise"/> returned from the <paramref name="function"/>.
        /// <para/>If the <paramref name="function"/> throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="captureValue">The value that will be passed to <paramref name="function"/>.</param>
        /// <param name="function">The delegate that will be invoked.</param>
        /// <param name="synchronizationContext">The context on which the <paramref name="function"/> will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously.</param>
		public static Promise Run<TCapture>(TCapture captureValue, Func<TCapture, Promise> function, SynchronizationContext synchronizationContext, bool forceAsync = true)
        {
            ValidateArgument(function, "function", 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.RunWait(Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, function), Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="function"/> on the provided <paramref name="synchronizationOption"/> context. Returns a new <see cref="Promise{T}"/> that will adopt the state of the <see cref="Promise{T}"/> returned from the <paramref name="function"/>.
        /// <para/>If the <paramref name="function"/> throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="function">The delegate that will be invoked.</param>
        /// <param name="synchronizationOption">Indicates on which context the <paramref name="function"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
		public static Promise<T> Run<T>(Func<Promise<T>> function, SynchronizationOption synchronizationOption = SynchronizationOption.Background, bool forceAsync = true)
        {
            ValidateArgument(function, "function", 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.RunWait(Internal.PromiseRefBase.DelegateWrapper.Create(function), (Internal.SynchronizationOption) synchronizationOption, null, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="function"/> with <paramref name="captureValue"/> on the provided <paramref name="synchronizationOption"/> context. Returns a new <see cref="Promise{T}"/> that will adopt the state of the <see cref="Promise{T}"/> returned from the <paramref name="function"/>.
        /// <para/>If the <paramref name="function"/> throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="captureValue">The value that will be passed to <paramref name="function"/>.</param>
        /// <param name="function">The delegate that will be invoked.</param>
        /// <param name="synchronizationOption">Indicates on which context the <paramref name="function"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
		public static Promise<T> Run<TCapture, T>(TCapture captureValue, Func<TCapture, Promise<T>> function, SynchronizationOption synchronizationOption = SynchronizationOption.Background, bool forceAsync = true)
        {
            ValidateArgument(function, "function", 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.RunWait(Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, function), (Internal.SynchronizationOption) synchronizationOption, null, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="function"/>  on the provided <paramref name="synchronizationContext"/>. Returns a new <see cref="Promise{T}"/> that will adopt the state of the <see cref="Promise{T}"/> returned from the <paramref name="function"/>.
        /// <para/>If the <paramref name="function"/> throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="function">The delegate that will be invoked.</param>
        /// <param name="synchronizationContext">The context on which the <paramref name="function"/> will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously.</param>
		public static Promise<T> Run<T>(Func<Promise<T>> function, SynchronizationContext synchronizationContext, bool forceAsync = true)
        {
            ValidateArgument(function, "function", 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.RunWait(Internal.PromiseRefBase.DelegateWrapper.Create(function), Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);
        }

        /// <summary>
        /// Run the <paramref name="function"/> with <paramref name="captureValue"/> on the provided <paramref name="synchronizationContext"/>. Returns a new <see cref="Promise{T}"/> that will adopt the state of the <see cref="Promise{T}"/> returned from the <paramref name="function"/>.
        /// <para/>If the <paramref name="function"/> throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>, or rejected with that <see cref="Exception"/>.
        /// </summary>
        /// <param name="captureValue">The value that will be passed to <paramref name="function"/>.</param>
        /// <param name="function">The delegate that will be invoked.</param>
        /// <param name="synchronizationContext">The context on which the <paramref name="function"/> will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the invoke to happen asynchronously.</param>
		public static Promise<T> Run<TCapture, T>(TCapture captureValue, Func<TCapture, Promise<T>> function, SynchronizationContext synchronizationContext, bool forceAsync = true)
        {
            ValidateArgument(function, "function", 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.RunWait(Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, function), Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already resolved.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Promise Resolved()
        {
#if PROMISE_DEBUG
            return Internal.CreateResolved(0);
#else
            return new Promise();
#endif
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already resolved with <paramref name="value"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Promise<T> Resolved<T>(T value)
        {
#if PROMISE_DEBUG
            return Internal.CreateResolved(value, 0);
#else
            return new Promise<T>(value);
#endif
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already rejected with <paramref name="reason"/>.
        /// </summary>
        public static Promise Rejected<TReject>(TReject reason)
        {
            var deferred = NewDeferred();
            deferred.Reject(reason);
            return deferred.Promise;
        }

        [Obsolete("Prefer Promise<T>.Rejected<TReject>(TReject reason)"), EditorBrowsable(EditorBrowsableState.Never)]
        public static Promise<T> Rejected<T, TReject>(TReject reason)
        {
            return Promise<T>.Rejected(reason);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already canceled.
        /// </summary>
        public static Promise Canceled()
        {
            return Internal.CreateCanceled();
        }

        [Obsolete("Cancelation reasons are no longer supported. Use Canceled() instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static Promise Canceled<TCancel>(TCancel reason)
        {
            throw new InvalidOperationException("Cancelation reasons are no longer supported. Use Canceled() instead.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already canceled.
        /// </summary>
        public static Promise<T> Canceled<T>()
        {
            return Promise<T>.Canceled();
        }

        [Obsolete("Cancelation reasons are no longer supported. Use Canceled() instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static Promise<T> Canceled<T, TCancel>(TCancel reason)
        {
            return Promise<T>.Canceled(reason);
        }

        /// <summary>
        /// Returns a new <see cref="Deferred"/> instance that is linked to and controls the state of a new <see cref="Promise"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Deferred NewDeferred()
        {
            return Deferred.New();
        }

        /// <summary>
        /// Returns a new <see cref="Deferred"/> instance that is linked to and controls the state of a new <see cref="Promise"/>.
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while the <see cref="Deferred"/> is pending, it and the <see cref="Promise"/> will be canceled.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("You should instead register a callback on the CancelationToken to cancel the deferred directly.", false), EditorBrowsable(EditorBrowsableState.Never)]
        public static Deferred NewDeferred(CancelationToken cancelationToken)
        {
            return Deferred.New(cancelationToken);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}.Deferred"/> object that is linked to and controls the state of a new <see cref="Promise{T}"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Promise<T>.Deferred NewDeferred<T>()
        {
            return Promise<T>.Deferred.New();
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}.Deferred"/> object that is linked to and controls the state of a new <see cref="Promise{T}"/>.
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while the <see cref="Promise{T}.Deferred"/> is pending, it and the <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("You should instead register a callback on the CancelationToken to cancel the deferred directly.", false), EditorBrowsable(EditorBrowsableState.Never)]
        public static Promise<T>.Deferred NewDeferred<T>(CancelationToken cancelationToken)
        {
            return Promise<T>.Deferred.New(cancelationToken);
        }

        /// <summary>
        /// Get a <see cref="RethrowException"/> that can be thrown inside an onRejected callback to rethrow the caught rejection, preserving the stack trace.
        /// This should be used as "throw Promise.Rethrow;"
        /// This is similar to "throw;" in a synchronous catch clause.
        /// </summary>
        public static RethrowException Rethrow
        {
            get
            {
                return RethrowException.GetOrCreate();
            }
        }

        /// <summary>
        /// Get a <see cref="CanceledException"/> that can be thrown to cancel the promise from an onResolved or onRejected callback, or in an async Promise function.
        /// This should be used as "throw Promise.CancelException();"
        /// </summary>
        public static CanceledException CancelException()
        {
            return Internal.CanceledExceptionInternal.GetOrCreate();
        }

        [Obsolete("Cancelation reasons are no longer supported. Use CancelException() instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static CanceledException CancelException<T>(T value)
        {
            throw new InvalidOperationException("Cancelation reasons are no longer supported. Use CancelException() instead.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Get a <see cref="Promises.RejectException"/> that can be thrown to reject the promise from an onResolved or onRejected callback, or in an async Promise function.
        /// This should be used as "throw Promise.RejectException(value);"
        /// </summary>
        public static RejectException RejectException<T>(T value)
        {
            return new Internal.RejectExceptionInternal(value);
        }
    }
}