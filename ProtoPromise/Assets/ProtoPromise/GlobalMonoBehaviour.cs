using System;
using System.Collections;
using UnityEngine;

namespace ProtoPromise
{
	public class GlobalMonoBehaviour : MonoBehaviour
	{
		private class Routine : IEnumerator, ILinked<Routine>
		{
			public Routine Next { get; set; }
			public Action onComplete;
			bool _continue = false;

			public object Current { get; set; }

			public bool MoveNext()
			{
				// As a coroutine, this will wait for the Current's yield, then execute this once, then stop.
				if (_continue)
				{
					Current = null;
					if (onComplete != null)
					{
						onComplete.Invoke();
						onComplete = null;
					}
					// Place this back in the pool.
					pool.Push(this);
				}
				return _continue = !_continue;
			}

			void IEnumerator.Reset()
			{
				throw new NotImplementedException();
			}
		}

		private static GlobalMonoBehaviour _instance;
		static GlobalMonoBehaviour Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new GameObject("ProtoPromise.GlobalMonoBehaviour").AddComponent<GlobalMonoBehaviour>();
					_instance.gameObject.hideFlags = HideFlags.HideAndDontSave; // Don't show in hierarchy and don't destroy.
				}
				return _instance;
			}
		}

		private static LinkedStackStruct<Routine> pool; // Pool of routines as linked stack.

		/// <summary>
		/// Waits for <paramref name="yieldInstruction"/> to complete, then calls <paramref name="onComplete"/>.
		/// If <paramref name="yieldInstruction"/> is not a Unity supported <see cref="YieldInstruction"/> or <see cref="CustomYieldInstruction"/>, then this will wait for 1 frame.
		/// If you are using Unity 5.3 or later and <paramref name="yieldInstruction"/> is an <see cref="IEnumerator"/>, it will be started and yielded as a Coroutine by Unity. Earlier versions will simply wait 1 frame.
		/// </summary>
		/// <param name="yieldInstruction">Yield instruction.</param>
		/// <param name="onComplete">Callback</param>
		/// <typeparam name="TYieldInstruction">The type of yield instruction.</typeparam>
		public static void Yield<TYieldInstruction>(TYieldInstruction yieldInstruction, Action onComplete)
		{
			// Grab from pool or create new if pool is empty.
			Routine routine = pool.IsEmpty ? new Routine() : pool.Pop();

			routine.Current = yieldInstruction;
			routine.onComplete = onComplete;
			Instance.StartCoroutine(routine);
		}

		/// <summary>
		/// Waits for one frame, then calls onComplete.
		/// </summary>
		/// <param name="onComplete">Callback</param>
		public static void Yield(Action onComplete)
		{
			Yield<object>(null, onComplete);
		}
	}
}