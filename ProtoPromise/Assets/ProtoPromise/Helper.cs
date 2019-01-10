using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoPromise
{
	internal interface ILinked<T> where T : class, ILinked<T>
	{
		T Next { get; set; }
	}

	/// <summary>
	///  This structure is unsuitable for general purpose.
	/// </summary>
	internal class LinkedStackClass<T> where T : class, ILinked<T>
	{
		T first;

		public bool IsEmpty { get { return first == null; } }

		public void Clear()
		{
			first = null;
		}

		public void Push(T item)
		{
			item.Next = first;
			first = item;
		}

		public T Pop()
		{
			T temp = first;
			first = first.Next;
			return temp;
		}

		public T Peek()
		{
			return first;
		}
	}

	/// <summary>
	///  This structure is unsuitable for general purpose.
	/// </summary>
	internal class LinkedStackStruct<T> where T : class, ILinked<T>
	{
		T first;

		public bool IsEmpty { get { return first == null; } }

		public void Clear()
		{
			first = null;
		}

		public void Push(T item)
		{
			item.Next = first;
			first = item;
		}

		public T Pop()
		{
			T temp = first;
			first = first.Next;
			return temp;
		}

		public T Peek()
		{
			return first;
		}
	}

	/// <summary>
	///  This structure is unsuitable for general purpose.
	/// </summary>
	internal class LinkedQueueClass<T> where T : class, ILinked<T>
	{
		T first;
		T last;

		public void Clear()
		{
			first = last = null;
		}

		public void Enqueue(T item)
		{
			item.Next = null;
			if (first == null)
			{
				first = last = item;
			}
			else
			{
				last.Next = item;
				last = item;
			}
		}

		public T Peek()
		{
			return first;
		}
	}

	/// <summary>
	///  This structure is unsuitable for general purpose.
	/// </summary>
	internal struct LinkedQueueStruct<T>  where T : class, ILinked<T>
	{
		T first;
		T last;

		public LinkedQueueStruct(T item)
		{
			item.Next = null;
			first = last = item;
		}

		public void Clear()
		{
			first = last = null;
		}

		public void Enqueue(T item)
		{
			item.Next = null;
			if (first == null)
			{
				first = last = item;
			}
			else
			{
				last.Next = item;
				last = item;
			}
		}

		/// <summary>
		/// Only use this to enqueue if you know this isn't empty.
		/// </summary>
		/// <param name="item">Item.</param>
		public void EnqueueRisky(T item)
		{
			item.Next = null;
			last.Next = item;
			last = item;
		}

		public T Peek()
		{
			return first;
		}
	}

	public static class PromiseExtensions
	{
		/// <summary>
		/// Helper method for promise.Notification{<see cref="float"/>}(onProgress).
		/// </summary>
		public static Promise Progress(this Promise promise, Action<float> onProgress)
		{
			return promise.Notification(onProgress);
		}

		/// <summary>
		/// Helper method for promise.Notification{<see cref="float"/>}(onProgress).
		/// </summary>
		public static Promise<T> Progress<T>(this Promise<T> promise, Action<float> onProgress)
		{
			return promise.Notification(onProgress);
		}

		public static Promise Catch(this Promise promise, Action<Exception> onRejected)
		{
			return promise.Catch(onRejected);
		}
	}
}