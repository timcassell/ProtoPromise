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

	public enum PoolType : sbyte
	{
		/// <summary>
		/// IPoolables must opt in to be re-used, otherwise they will be garbage-collected.
		/// </summary>
		OptIn,
		/// <summary>
		/// IPoolables will automatically be re-used. An IPoolable must opt out to be used past its completion (or unexpected behavior may occur).
		/// </summary>
		OptOut
	}

	public static class ObjectPool
	{
		/// <summary>
		/// Determines whether objects are re-used by default (OptOut) or must be directly flagged for re-use (OptIn)
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

		internal static bool TryTakeInternal<T>(out T item) where T : class, ILinked<T>, IResetable, IPoolable
		{
			object obj;
			if (pool.TryGetValue(typeof(T), out obj))
			{
				LinkedStackClass<T> stack = (LinkedStackClass<T>) obj;
				if (!stack.IsEmpty)
				{
					item = stack.Pop();
					item.Reset();
					if (poolType == PoolType.OptOut)
					{
						item.OptIn();
					}
					return true;
				}
			}
			item = default(T);
			return false;
		}

		internal static void AddInternal<T>(T item) where T : class, ILinked<T>, IResetable, IPoolable
		{
			if (!item.CanPool)
			{
				return;
			}

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