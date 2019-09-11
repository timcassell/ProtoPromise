using System;
using System.Collections.Generic;

namespace Proto.Promises
{
    public interface IPromiseYielder
    {
        /// <summary>
        /// Returns a promise that resolves after 1 frame.
        /// </summary>
        Promise Yield();

        /// <summary>
        /// Returns a promise that resolves with the <paramref name="yieldInstruction"/> after the <paramref name="yieldInstruction"/> has completed.
        /// </summary>
        /// <param name="yieldInstruction">Yield instruction.</param>
        /// <typeparam name="TYieldInstruction">The type of yieldInstruction.</typeparam>
        Promise<TYieldInstruction> Yield<TYieldInstruction>(TYieldInstruction yieldInstruction);
    }

    public partial class Promise
	{
		public static Promise All(params Promise[] promises)
		{
            return Internal.AllPromise.GetOrCreate(new ArrayEnumerator<Promise>(promises), 1);
		}

		public static Promise All(IEnumerable<Promise> promises)
		{
            return Internal.AllPromise.GetOrCreate(promises.GetEnumerator(), 1);
        }

        public static Promise AllNonAlloc<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            return Internal.AllPromise.GetOrCreate(promises, 1);
        }

        public static Promise<IList<T>> All<T>(params Promise<T>[] promises)
        {
            return Internal.AllPromise<T>.GetOrCreate(new ArrayEnumerator<Promise>(promises), new List<T>(promises.Length), 1);
        }

        public static Promise<IList<T>> All<T>(IEnumerable<Promise<T>> promises)
        {
            return Internal.AllPromise<T>.GetOrCreate(promises.GetEnumerator(), new List<T>(), 1);
        }

        public static Promise<IList<T>> AllNonAlloc<T, TEnumerator>(TEnumerator promises, IList<T> valueContainer) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Internal.AllPromise<T>.GetOrCreate(promises, valueContainer, 1);
        }

        // TODO
        //public static Promise<T1> All<T1>(Promise<T1> promise1, Promise promise2)
        //{

        //}
        //public static Promise<ValueTuple<T1, T2>> All<T1, T2>(Promise<T1> promise1, Promise<T2> promise2, Promise promise3)
        //{

        //}

        public static Promise Race(params Promise[] promises)
        {
            return Internal.RacePromise.GetOrCreate(new ArrayEnumerator<Promise>(promises), 1);
        }

        public static Promise Race(IEnumerable<Promise> promises)
        {
            return Internal.RacePromise.GetOrCreate(promises.GetEnumerator(), 1);
        }

        public static Promise RaceNonAlloc<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            return Internal.RacePromise.GetOrCreate(promises, 1);
        }

        public static Promise<T> Race<T>(params Promise<T>[] promises)
        {
            return Internal.RacePromise<T>.GetOrCreate(new ArrayEnumerator<Promise>(promises), 1);
        }

        public static Promise<T> Race<T>(IEnumerable<Promise<T>> promises)
        {
            return Internal.RacePromise<T>.GetOrCreate(promises.GetEnumerator(), 1);
        }

        public static Promise<T> RaceNonAlloc<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Internal.RacePromise<T>.GetOrCreate(promises, 1);
        }

        public static Promise Sequence(params Func<Promise>[] funcs)
		{
            return SequenceNonAlloc(new ArrayEnumerator<Func<Promise>>(funcs));
		}

		public static Promise Sequence(IEnumerable<Func<Promise>> funcs)
		{
            return SequenceNonAlloc(funcs.GetEnumerator());
		}

        public static Promise SequenceNonAlloc<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Func<Promise>>
        {
            if (!promises.MoveNext())
            {
                // If promises is empty, just return a resolved promise.
                return Resolved();
            }
            Promise promise = promises.Current.Invoke();
            while (promises.MoveNext())
            {
                promise = promise.Then(promises.Current.Invoke);
            }
            return promise;
        }

        /// <summary>
        /// Returns a promise that resolves with the <paramref name="yieldInstruction"/> after the <paramref name="yieldInstruction"/> has completed.
        /// Uses Unity's coroutines by default, unless a different yielder is provided.
        /// </summary>
        /// <param name="yieldInstruction">Yield instruction.</param>
        /// <typeparam name="TYieldInstruction">The type of yieldInstruction.</typeparam>
        public static Promise<TYieldInstruction> Yield<TYieldInstruction>(TYieldInstruction yieldInstruction)
		{
            return Config.Yielder.Yield(yieldInstruction);
		}

		/// <summary>
		/// Returns a promise that resolves after 1 frame.
		/// </summary>
		public static Promise Yield()
		{
            return Config.Yielder.Yield();
		}

		public static Promise New(Action<Deferred> resolver)
        {
            var promise = Internal.DeferredPromise.GetOrCreate(1);
            try
            {
                resolver.Invoke(promise.Deferred);
            }
            catch (Exception e)
            {
                promise.Deferred.Reject(e);
            }
            return promise;
		}

		public static Promise<T> New<T>(Action<Promise<T>.Deferred> resolver)
        {
            var promise = Internal.DeferredPromise<T>.GetOrCreate(1);
            try
            {
                resolver.Invoke(promise.Deferred);
            }
            catch (Exception e)
            {
                promise.Deferred.Reject(e);
            }
            return promise;
        }

		public static Promise Resolved()
		{
			var promise = Internal.LitePromise.GetOrCreate(1);
			promise.Resolve();
			return promise;
		}

		public static Promise<T> Resolved<T>(T value)
		{
			var promise = Internal.LitePromise<T>.GetOrCreate(1);
			promise.Resolve(value);
			return promise;
		}

		public static Promise<T> Rejected<T, TReject>(TReject reason)
		{
			var promise = Internal.LitePromise<T>.GetOrCreate(1);
			promise.Reject(reason, 1);
			return promise;
		}

		public static Promise Rejected<TReject>(TReject reason)
		{
			var promise = Internal.LitePromise.GetOrCreate(1);
			promise.Reject(reason, 1);
			return promise;
		}

		public static Promise<T> Rejected<T>()
		{
			var promise = Internal.LitePromise<T>.GetOrCreate(1);
			promise.Reject(1);
			return promise;
		}

		public static Promise Rejected()
		{
			var promise = Internal.LitePromise.GetOrCreate(1);
			promise.Reject(1);
			return promise;
		}

		public static Deferred NewDeferred()
		{
            return Internal.DeferredPromise.GetOrCreate(1).Deferred;
		}

		public static Promise<T>.Deferred NewDeferred<T>()
		{
            return Internal.DeferredPromise<T>.GetOrCreate(1).Deferred;
		}
	}
}