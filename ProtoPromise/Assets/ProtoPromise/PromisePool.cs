namespace ProtoPromise
{
	public partial class Promise
	{
		internal static ObjectPool objectPool = new ObjectPool(); // private protected not supported before c# 7.2, so must use internal.
	}

	public interface IPoolable
	{
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

	public static class PromisePool
	{
		/// <summary>
		/// Determines whether objects are re-used by default (OptOut) or must be directly flagged for re-use (OptIn)
		/// </summary>
		public static PoolType poolType = PoolType.OptIn; // Change this to change the default behavior.

		public static void OptOut<T>(T item) where T : IPoolable
		{
			item.OptOut();
		}

		public static void OptIn<T>(T item) where T : IPoolable
		{
			item.OptIn();
		}
	}
}