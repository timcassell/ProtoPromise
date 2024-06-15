#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0270 // Use coalesce expression

using Proto.Promises.CompilerServices;
using System;
using System.Collections.Generic;
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
        /// <remarks>
        /// This function is not recommended. Instead, you should prefer async/await or .Then API.
        /// </remarks>
        public static Promise Sequence(params Func<Promise>[] promiseFuncs)
            => Sequence(default, promiseFuncs.GetGenericEnumerator());

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled before all of the <paramref name="promiseFuncs"/> have been invoked,
        /// the <paramref name="promiseFuncs"/> will stop being invoked, and the returned <see cref="Promise"/> will be canceled.
        /// </summary>
        /// <remarks>
        /// This function is not recommended. Instead, you should prefer async/await or .Then API.
        /// </remarks>
        public static Promise Sequence(CancelationToken cancelationToken, params Func<Promise>[] promiseFuncs)
            => Sequence(cancelationToken, promiseFuncs.GetGenericEnumerator());

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// This function is not recommended. Instead, you should prefer async/await or .Then API.
        /// </remarks>
        public static Promise Sequence(IEnumerable<Func<Promise>> promiseFuncs)
            => Sequence(default, promiseFuncs.GetEnumerator());

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled before all of the <paramref name="promiseFuncs"/> have been invoked,
        /// the <paramref name="promiseFuncs"/> will stop being invoked, and the returned <see cref="Promise"/> will be canceled.
        /// </summary>
        /// <remarks>
        /// This function is not recommended. Instead, you should prefer async/await or .Then API.
        /// </remarks>
        public static Promise Sequence(CancelationToken cancelationToken, IEnumerable<Func<Promise>> promiseFuncs)
            => Sequence(cancelationToken, promiseFuncs.GetEnumerator());

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <remarks>
        /// This function is not recommended. Instead, you should prefer async/await or .Then API.
        /// </remarks>
        public static Promise Sequence<TEnumerator>(TEnumerator promiseFuncs) where TEnumerator : IEnumerator<Func<Promise>>
            => Sequence(default, promiseFuncs);

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled before all of the <paramref name="promiseFuncs"/> have been invoked,
        /// the <paramref name="promiseFuncs"/> will stop being invoked, and the returned <see cref="Promise"/> will be canceled.
        /// </summary>
        /// <remarks>
        /// This function is not recommended. Instead, you should prefer async/await or .Then API.
        /// </remarks>
        public static Promise Sequence<TEnumerator>(CancelationToken cancelationToken, TEnumerator promiseFuncs) where TEnumerator : IEnumerator<Func<Promise>>
        {
            ValidateArgument(promiseFuncs, nameof(promiseFuncs), 2);

            using (promiseFuncs)
            {
                var promise = Resolved();
                while (promiseFuncs.MoveNext())
                {
                    promise = promise.Then(promiseFuncs.Current, cancelationToken);
                }
                return promise;
            }
        }

        private static Promise SwitchToContext(SynchronizationOption synchronizationOption, bool forceAsync)
            => Resolved().WaitAsync(synchronizationOption, forceAsync);

        /// <summary>
        /// Returns a new <see cref="Promise"/> that will resolve on the foreground context.
        /// </summary>
        /// <param name="forceAsync">If true, forces the context switch to happen asynchronously.</param>
        public static Promise SwitchToForeground(bool forceAsync = false)
            => SwitchToContext(SynchronizationOption.Foreground, forceAsync);

        /// <summary>
        /// Returns a new <see cref="Promise"/> that will resolve on the background context.
        /// </summary>
        /// <param name="forceAsync">If true, forces the context switch to happen asynchronously.</param>
        public static Promise SwitchToBackground(bool forceAsync = false)
            => SwitchToContext(SynchronizationOption.Background, forceAsync);

        /// <summary>
        /// Returns a new <see cref="Promise"/> that will resolve on the provided <paramref name="synchronizationContext"/>
        /// </summary>
        /// <param name="synchronizationContext">The context to switch to. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the context switch to happen asynchronously.</param>
        public static Promise SwitchToContext(SynchronizationContext synchronizationContext, bool forceAsync = false)
            => Resolved().WaitAsync(synchronizationContext, forceAsync);

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
        /// This method is equivalent to <see cref="SwitchToBackground(bool)"/>, but is more efficient when used directly with the <see langword="await"/> keyword.
        /// </remarks>
        public static PromiseSwitchToContextAwaiter SwitchToBackgroundAwait(bool forceAsync = false)
            => new PromiseSwitchToContextAwaiter(Config.BackgroundContext, forceAsync);

        /// <summary>
        /// Switch to the provided context in an async method.
        /// </summary>
        /// <param name="synchronizationContext">The context to switch to. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the context switch to happen asynchronously.</param>
        /// <remarks>
        /// This method is equivalent to <see cref="SwitchToContext(SynchronizationContext, bool)"/>, but is more efficient when used directly with the <see langword="await"/> keyword.
        /// </remarks>
        public static PromiseSwitchToContextAwaiter SwitchToContextAwait(SynchronizationContext synchronizationContext, bool forceAsync = false)
            => new PromiseSwitchToContextAwaiter(synchronizationContext, forceAsync);

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
            ValidateArgument(resolver, nameof(resolver), 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.New(Internal.PromiseRefBase.DelegateWrapper.Create(resolver), (Internal.SynchronizationOption) synchronizationOption, null, forceAsync);
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
            ValidateArgument(resolver, nameof(resolver), 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.New(Internal.PromiseRefBase.DelegateWrapper.Create(resolver), Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);
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
            ValidateArgument(resolver, nameof(resolver), 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.New(Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, resolver), (Internal.SynchronizationOption) synchronizationOption, null, forceAsync);
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
            ValidateArgument(resolver, nameof(resolver), 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.New(Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, resolver), Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);
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
            ValidateArgument(action, nameof(action), 1);

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
            ValidateArgument(action, nameof(action), 1);

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
            ValidateArgument(action, nameof(action), 1);

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
            ValidateArgument(action, nameof(action), 1);

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
            ValidateArgument(function, nameof(function), 1);

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
            ValidateArgument(function, nameof(function), 1);

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
            ValidateArgument(function, nameof(function), 1);

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
            ValidateArgument(function, nameof(function), 1);

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
            ValidateArgument(function, nameof(function), 1);

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
            ValidateArgument(function, nameof(function), 1);

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
            ValidateArgument(function, nameof(function), 1);

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
            ValidateArgument(function, nameof(function), 1);

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
            ValidateArgument(function, nameof(function), 1);

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
            ValidateArgument(function, nameof(function), 1);

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
            ValidateArgument(function, nameof(function), 1);

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
            ValidateArgument(function, nameof(function), 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.RunWait(Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, function), Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already resolved.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Promise Resolved()
        {
#if PROMISE_DEBUG
            // Make a promise on the heap to capture causality trace and help with debugging.
            var promise = Internal.PromiseRefBase.DeferredPromise<Internal.VoidResult>.GetOrCreate();
            promise.ResolveDirect(new Internal.VoidResult());
            return new Promise(promise, promise.Id);
#else
            // Make a promise on the stack for efficiency.
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
            // Make a promise on the heap to capture causality trace and help with debugging.
            var promise = Internal.PromiseRefBase.DeferredPromise<T>.GetOrCreate();
            promise.ResolveDirect(value);
            return new Promise<T>(promise, promise.Id);
#else
            // Make a promise on the stack for efficiency.
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

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already canceled.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Promise Canceled()
            => Internal.CreateCanceled();

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already canceled.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Promise<T> Canceled<T>()
            => Promise<T>.Canceled();

        /// <summary>
        /// Returns a new <see cref="Deferred"/> instance that is linked to and controls the state of a new <see cref="Promise"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Deferred NewDeferred()
            => Deferred.New();

        /// <summary>
        /// Returns a <see cref="Promise{T}.Deferred"/> object that is linked to and controls the state of a new <see cref="Promise{T}"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Promise<T>.Deferred NewDeferred<T>()
            => Promise<T>.Deferred.New();

        /// <summary>
        /// Get a <see cref="RethrowException"/> that can be thrown inside an onRejected callback to rethrow the caught rejection, preserving the stack trace.
        /// This should be used as "throw Promise.Rethrow;"
        /// This is similar to "throw;" in a synchronous catch clause.
        /// </summary>
        public static RethrowException Rethrow
            => RethrowException.GetOrCreate();

        /// <summary>
        /// Get a <see cref="CanceledException"/> that can be thrown to cancel the promise from an onResolved or onRejected callback, or in an async Promise function.
        /// This should be used as "throw Promise.CancelException();"
        /// </summary>
        public static CanceledException CancelException()
            => Internal.CanceledExceptionInternal.GetOrCreate();

        /// <summary>
        /// Get a <see cref="Promises.RejectException"/> that can be thrown to reject the promise from an onResolved or onRejected callback, or in an async Promise function.
        /// This should be used as "throw Promise.RejectException(value);"
        /// </summary>
        public static RejectException RejectException<T>(T value)
            => new Internal.RejectExceptionInternal(value);
    }
}