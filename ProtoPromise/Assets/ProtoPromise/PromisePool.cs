using System;
using System.Collections.Generic;

namespace ProtoPromise
{
	internal interface IResetable
	{
		void Reset();
	}

	public interface IPoolable
	{
		bool CanPool { get; }
		void OptIn();
		void OptOut();
	}

	public static class PromisePool
	{
		public enum PoolType : sbyte
		{
			OptIn,
			OptOut
		}

		/// <summary>
		/// Determines whether promises are re-used by default (OptOut) or promises must be directly flagged for re-use (OptIn)
		/// </summary>
		public static PoolType poolType = PoolType.OptIn; // Change this to change the default behavior.

		private static Dictionary<Type, object> pool = new Dictionary<Type, object>();

		public static void OptOut<T>(T item) where T : IPoolable
		{
			item.OptOut();
		}

		public static void OptIn<T>(T item) where T : IPoolable
		{
			item.OptIn();
		}

		internal static TPromise TakePromiseInternal<TPromise>(ADeferred deferred) where TPromise : Promise, ILinked<TPromise>, IPoolable, IResetable, new()
		{
			TPromise promise = TakePoolableInternal(() => new TPromise());
			promise.DeferredInternal = deferred;
			return promise;
		}

		internal static T TakePoolableInternal<T>(Func<T> creator) where T : class, ILinked<T>, IPoolable, IResetable
		{
			T value = TakeInternal(creator);
			if (poolType == PoolType.OptOut)
			{
				OptIn(value);
			}
			return value;
		}

		internal static T TakeInternal<T>(Func<T> creator) where T : class, ILinked<T>, IResetable
		{
			object obj;
			if (pool.TryGetValue(typeof(T), out obj))
			{
				LinkedStackClass<T> stack = (LinkedStackClass<T>) obj;
				if (!stack.IsEmpty)
				{
					T value = stack.Pop();
					value.Reset();
					return value;
				}
			}
			return creator.Invoke();
		}

		internal static void AddPoolableInternal<T>(T item) where T : class, ILinked<T>, IPoolable
		{
			if (!item.CanPool)
			{
				return;
			}
			AddInternal(item);
		}

		internal static void AddInternal<T>(T item) where T : class, ILinked<T>
		{
			object obj;
			LinkedStackClass<T> stack;
			if (pool.TryGetValue(typeof(T), out obj))
			{
				stack = (LinkedStackClass<T>) obj;
			}
			else
			{
				pool[typeof(T)] = stack = new LinkedStackClass<T>();
			}
			stack.Push(item);
		}
	}
}