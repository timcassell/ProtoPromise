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

	internal struct LinkedStackStruct<T> where T : class, ILinked<T>
	{
		T first;

		//public bool IsNotEmpty { get { return first != null; } }

		//public LinkedEnumerator<T> GetEnumerator()
		//{
		//	return new LinkedEnumerator<T>(first);
		//}

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

		//public bool TryPop(out T item)
		//{
		//	if (IsNotEmpty)
		//	{
		//		item = Pop();
		//		return true;
		//	}
		//	item = default(T);
		//	return false;
		//}

		public T Peek()
		{
			return first;
		}
	}

	internal struct LinkedListStruct<T>  where T : class, ILinked<T>
	{
		T first;
		T last;

		//public bool IsNotEmpty { get { return first != null; } }

		//public LinkedEnumerator<T> GetEnumerator()
		//{
		//	return new LinkedEnumerator<T>(first);
		//}

		public void Clear()
		{
			first = last = null;
		}

		public void AddFirst(T item)
		{
			item.Next = first;
			if (first == null)
			{
				first = last = item;
			}
			else
			{
				first = item;
			}
		}

		public void AddLast(T item)
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

		public T TakeFirst()
		{
			T temp = first;
			first = first.Next;
			return temp;
		}

		//public bool TryTakeFirst(out T item)
		//{
		//	if (IsNotEmpty)
		//	{
		//		item = TakeFirst();
		//		return true;
		//	}
		//	item = default(T);
		//	return false;
		//}

		public T PeekFirst()
		{
			return first;
		}
	}

	//internal struct LinkedEnumerator<T> where T : class, ILinked<T>
	//{
	//	public T Current { get; private set; }
	//	T next;

	//	public LinkedEnumerator(T first)
	//	{
	//		Current = next = first;
	//	}

	//	public bool MoveNext()
	//	{
	//		if (next != null)
	//		{
	//			Current = next;
	//			next = next.Next;
	//			return true;
	//		}
	//		Current = null;
	//		return false;
	//	}
	//}

	public static class PromiseHelper
	{
		/// <summary>
		/// Helper method for promise.Notification&lt;float&gt;(onProgress).
		/// </summary>
		public static Promise Progress(this Promise promise, Action<float> onProgress)
		{
			return promise.Notification(onProgress);
		}

		/// <summary>
		/// Helper method for promise.Notification&lt;float&gt;(onProgress).
		/// </summary>
		public static Promise<T> Progress<T>(this Promise<T> promise, Action<float> onProgress)
		{
			return promise.Notification(onProgress);
		}
	}
}