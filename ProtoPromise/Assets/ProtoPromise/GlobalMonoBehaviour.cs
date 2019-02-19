using System;
using System.Collections;
using UnityEngine;

namespace ProtoPromise
{
	public class GlobalMonoBehaviour : MonoBehaviour
	{
		private class Routine : IEnumerator, ILinked<Routine>, IResetable, IPoolable
		{
			public Routine Next { get; set; }
			public Action onComplete;
			public bool _continue = false;

			public object Current { get; set; }

			public bool CanPool { get { return true; } }

			public bool MoveNext()
			{
				bool cont = _continue;
				// As a coroutine, this will wait for the Current's yield, then execute this once, then stop.
				if (cont)
				{
					Action comp = onComplete;
					onComplete = null;
					Current = null;
					// Place this back in the pool before invoking in case the invocation will re-use this.
					ObjectPool.AddInternal(this);
					comp.Invoke();
				}
				return _continue = !_continue;
			}

			public void Cancel(bool invokeOnComplete)
			{
				_instance.StopCoroutine(this);
				if (invokeOnComplete)
				{
					onComplete.Invoke();
				}
				Current = null;
				onComplete = null;
			}

			public void Reset() { }

			public void OptIn()
			{
				throw new NotImplementedException();
			}

			public void OptOut()
			{
				throw new NotImplementedException();
			}
		}

		private class Routine<T> : IEnumerator, ILinked<Routine<T>>, IResetable, IPoolable
		{
			public Routine<T> Next { get; set; }
			public Action<T> onComplete;
			public bool _continue = false;

			public T Current { get; set; }
			object IEnumerator.Current { get { return Current; } }

			public bool CanPool { get { return true; } }

			public bool MoveNext()
			{
				// As a coroutine, this will wait for the Current's yield, then execute this once, then stop.
				bool cont = _continue;
				_continue = !_continue;
				if (cont)
				{
					Action<T> comp = onComplete;
					onComplete = null;
					T tempObj = Current;
					Current = default(T);
					// Place this back in the pool before invoking in case the invocation will re-use this.
					ObjectPool.AddInternal(this);
					comp.Invoke(tempObj);
					return false;
				}
				return true;
			}

			public void Cancel(bool invokeOnComplete)
			{
				_instance.StopCoroutine(this);
				if (invokeOnComplete)
				{
					onComplete.Invoke(Current);
				}
				Current = default(T);
				onComplete = null;
			}

			public void Reset() { }

			public void OptIn()
			{
				throw new NotImplementedException();
			}

			public void OptOut()
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

		//private static LinkedStackStruct<Routine> pool; // Pool of routines as linked stack.

		/// <summary>
		/// Waits for <paramref name="yieldInstruction"/> to complete, then calls <paramref name="onComplete"/>.
		/// If <paramref name="yieldInstruction"/> is not a Unity supported <see cref="YieldInstruction"/> or <see cref="CustomYieldInstruction"/>, then this will wait for 1 frame.
		/// If you are using Unity 5.3 or later and <paramref name="yieldInstruction"/> is an <see cref="IEnumerator"/>, it will be started and yielded as a Coroutine by Unity. Earlier versions will simply wait 1 frame.
		/// Returns a cancelation delegate, which will stop the waiting if invoked. If the value passed in is true, <paramref name="onComplete"/> will be invoked immediately, otherwise it will not be invoked.
		/// </summary>
		/// <param name="yieldInstruction">Yield instruction.</param>
		/// <param name="onComplete">Callback</param>
		/// <typeparam name="TYieldInstruction">The type of yield instruction.</typeparam>
		/// <returns>Cancelation delegate to stop waiting. If the value passed in is true, <param name="onComplete"/> will be invoked immediately, otherwise it will not be invoked.</returns>
		public static Action<bool> Yield<TYieldInstruction>(TYieldInstruction yieldInstruction, Action<TYieldInstruction> onComplete)
		{
			if (onComplete == null)
			{
				throw new ArgumentNullException("onComplete");
			}

			// Grab from pool or create new if pool is empty.
			Routine<TYieldInstruction> routine;
			if (ObjectPool.TryTakeInternal(out routine))
			{
				if (routine._continue)
				{
					// The routine is already running, so don't start a new one, just set the continue flag. This prevents extra GC allocations from Unity's Coroutine.
					routine._continue = false;
					routine.Current = yieldInstruction;
					routine.onComplete = onComplete;
					return routine.Cancel;
				}
			}
			else
			{
				routine = new Routine<TYieldInstruction>();
			}

			routine.Current = yieldInstruction;
			routine.onComplete = onComplete;
			Instance.StartCoroutine(routine);

			return routine.Cancel;
		}

		/// <summary>
		/// Waits for <paramref name="yieldInstruction"/> to complete, then calls <paramref name="onComplete"/>.
		/// If <paramref name="yieldInstruction"/> is not a Unity supported <see cref="YieldInstruction"/> or <see cref="CustomYieldInstruction"/>, then this will wait for 1 frame.
		/// If you are using Unity 5.3 or later and <paramref name="yieldInstruction"/> is an <see cref="IEnumerator"/>, it will be started and yielded as a Coroutine by Unity. Earlier versions will simply wait 1 frame.
		/// Returns a cancelation delegate, which will stop the waiting if invoked. If the value passed in is true, <paramref name="onComplete"/> will be invoked immediately, otherwise it will not be invoked.
		/// </summary>
		/// <param name="yieldInstruction">Yield instruction.</param>
		/// <param name="onComplete">Callback</param>
		/// <typeparam name="TYieldInstruction">The type of yield instruction.</typeparam>
		/// <returns>Cancelation delegate to stop waiting. If the value passed in is true, <param name="onComplete"/> will be invoked immediately, otherwise it will not be invoked.</returns>
		public static Action<bool> Yield<TYieldInstruction>(TYieldInstruction yieldInstruction, Action onComplete)
		{
			if (onComplete == null)
			{
				throw new ArgumentNullException("onComplete");
			}

			// Grab from pool or create new if pool is empty.
			Routine routine;
			if (ObjectPool.TryTakeInternal(out routine))
			{
				if (routine._continue)
				{
					// The routine is already running, so don't start a new one, just set the continue flag. This prevents extra GC allocations from Unity's Coroutine.
					routine._continue = false;
					routine.Current = yieldInstruction;
					routine.onComplete = onComplete;
					return routine.Cancel;
				}
			}
			else
			{
				routine = new Routine();
			}

			routine.Current = yieldInstruction;
			routine.onComplete = onComplete;
			Instance.StartCoroutine(routine);

			return routine.Cancel;
		}

		/// <summary>
		/// Waits for one frame, then calls onComplete.
		/// </summary>
		/// <param name="onComplete">Callback</param>
		public static void Yield(Action onComplete)
		{
			if (onComplete == null)
			{
				throw new ArgumentNullException("onComplete");
			}

			// Grab from pool or create new if pool is empty.
			Routine routine;
			if (ObjectPool.TryTakeInternal(out routine))
			{
				if (routine._continue)
				{
					// The routine is already running, so don't start a new one, just set the continue flag. This prevents extra GC allocations from Unity's Coroutine.
					routine._continue = false;
					routine.onComplete = onComplete;
					return;
				}
			}
			else
			{
				routine = new Routine();
			}

			routine.onComplete = onComplete;
			Instance.StartCoroutine(routine);
		}
	}
}