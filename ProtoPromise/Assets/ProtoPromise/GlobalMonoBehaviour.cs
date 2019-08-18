#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
using System;
using System.Collections;
using UnityEngine;

namespace ProtoPromise
{
	public interface IYieldIntercept
	{
		/// <summary>
		/// Return <see langword="true"/> if the <paramref name="yieldInstruction"/> was intercepted, <see langword="false"/> otherwise.
		/// If it was intercepted, call <paramref name="onComplete"/> when the yieldInstruction has completed.
		/// If <paramref name="cancel"/> is invoked, it should stop the wait, then immediately call <paramref name="onComplete"/> if the value passed in is <see langword="true"/>
		/// </summary>
		bool InterceptYieldInstruction<TYieldInstruction>(TYieldInstruction yieldInstruction, Action onComplete, out Action<bool> cancel);
		/// <summary>
		/// Return <see langword="true"/> if the call was intercepted, <see langword="false"/> otherwise.
		/// If it was intercepted, call <paramref name="onComplete"/> when a single frame has passed.
		/// </summary>
		bool InterceptSingleFrameWait(Action onComplete);
	}

	public class GlobalMonoBehaviour : MonoBehaviour
	{
		/// <summary>
		/// Assign an object to this to use your own or third party coroutines or other yield methods.
		/// If this is null, or the Intercept calls return false, Unity's coroutines will be used.
		/// </summary>
		public static IYieldIntercept yieldInterceptor;

		private static event Action _onClear;

		public static void ClearPooledRoutines()
		{
			_onClear.Invoke();
		}

		private class Routine : IEnumerator, ILinked<Routine>
		{
			Routine ILinked<Routine>.Next { get; set; }

			private static ValueLinkedStack<Routine> _pool;

			public static Routine GetOrCreate()
			{
				return _pool.IsNotEmpty ? _pool.Pop() : new Routine();
			}

			static Routine()
			{
				_onClear += () => _pool.Clear();
			}

			private Routine() { }

			public Action onComplete;
			public bool _continue;

			public object Current { get; set; }

			public bool MoveNext()
			{
				// As a coroutine, this will wait for the Current's yield, then execute this once, then stop.
				if (_continue)
				{
					Complete();
				}
				return _continue = !_continue; // If the continue flag is flipped from the callback, this will continue to run.
			}

			void Complete()
			{
				Action comp = onComplete;
				onComplete = null;
				Current = null;
				// Place this back in the pool before invoking in case the invocation will re-use this.
				_pool.Push(this);

				try
				{
					comp.Invoke();
				}
				catch
				{
					// Reset the flag if there was an error.
					_continue = false;
					throw;
				}
			}

			public void Cancel(bool invokeOnComplete)
			{
				if (onComplete == null)
				{
					// It was already completed, do nothing.
					return;
				}

				_continue = false;
				_instance._StopCoroutine(this);
				if (invokeOnComplete)
				{
					Complete();
				}
				else
				{
					onComplete = null;
					Current = null;
					_pool.Push(this);
				}
			}

			void IEnumerator.Reset() { }
		}

		private class Routine<T> : IEnumerator, ILinked<Routine<T>>
 		{
			Routine<T> ILinked<Routine<T>>.Next { get; set; }

#pragma warning disable RECS0108 // Warns about static fields in generic types
			private static ValueLinkedStack<Routine<T>> _pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

			public static Routine<T> GetOrCreate()
			{
				return _pool.IsNotEmpty ? _pool.Pop() : new Routine<T>();
			}

			static Routine()
			{
				_onClear += () => _pool.Clear();
			}

			private Routine() { }

			public Action<T> onComplete;
			public bool _continue;

			public T Current { get; set; }
			object IEnumerator.Current { get { return Current; } }

			public bool MoveNext()
			{
				// As a coroutine, this will wait for the Current's yield, then execute this once, then stop.
				if (_continue)
				{
					Complete();
				}
				return _continue = !_continue;
			}

			public void Complete()
			{
				Action<T> comp = onComplete;
				onComplete = null;
				T tempObj = Current;
				Current = default(T);
				// Place this back in the pool before invoking in case the invocation will re-use this.
				_pool.Push(this);

				try
				{
					comp.Invoke(tempObj);
				}
				catch
				{
					// Reset the flag if there was an error.
					_continue = false;
					throw;
				}
			}

			public void Cancel(bool invokeOnComplete)
			{
				if (onComplete == null)
				{
					// It was already completed, do nothing.
					return;
				}

				if (_continue) // If the yieldInstruction was intercepted, _continue will be false, so no need to stop Unity's coroutine.
				{
					_continue = false;
					_instance._StopCoroutine(this);
				}
				if (invokeOnComplete)
				{
					Complete();
				}
				else
				{
					onComplete = null;
					Current = default(T);
					_pool.Push(this);
				}
			}

			void IEnumerator.Reset() { }
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

		/// <summary>
		/// Waits for <paramref name="yieldInstruction"/> to complete, then calls <paramref name="onComplete"/>.
		/// If <paramref name="yieldInstruction"/> is not a Unity supported <see cref="YieldInstruction"/> or <see cref="CustomYieldInstruction"/>, and it is not intercepted, then this will wait for 1 frame.
		/// If you are using Unity 5.3 or later and <paramref name="yieldInstruction"/> is an <see cref="IEnumerator"/>, it will be started and yielded as a Coroutine by Unity. Earlier versions will simply wait 1 frame.
		/// Returns a cancelation delegate, which will stop the waiting if invoked. If the value passed in is <see langword="true"/>, <paramref name="onComplete"/> will be invoked immediately, otherwise it will not be invoked.
		/// Note: the cancelation delegate should be discarded when it is invoked or when <paramref name="onComplete"/> is invoked. 
		/// </summary>
		/// <param name="yieldInstruction">Yield instruction.</param>
		/// <param name="onComplete">Callback</param>
		/// <typeparam name="TYieldInstruction">The type of yield instruction.</typeparam>
		/// <returns>Cancelation delegate to stop waiting. If the value passed in is <see langword="true"/>, <param name="onComplete"/> will be invoked immediately, otherwise it will not be invoked.</returns>
		public static Action<bool> Yield<TYieldInstruction>(TYieldInstruction yieldInstruction, Action<TYieldInstruction> onComplete)
		{
#if DEBUG
			if (onComplete == null)
			{
#pragma warning disable RECS0163 // Suggest the usage of the nameof operator
#pragma warning disable IDE0016 // Use 'throw' expression
                throw new ArgumentNullException("onComplete");
#pragma warning restore IDE0016 // Use 'throw' expression
#pragma warning restore RECS0163 // Suggest the usage of the nameof operator
            }
#endif

            Routine<TYieldInstruction> routine = Routine<TYieldInstruction>.GetOrCreate();
			routine.Current = yieldInstruction;
			routine.onComplete = onComplete;

			// Try to intercept.
			Action<bool> cancel;
			if (yieldInterceptor != null && yieldInterceptor.InterceptYieldInstruction(yieldInstruction, routine.Complete, out cancel))
			{
				cancel += routine.Cancel;
				return cancel;
			}

			if (routine._continue)
			{
				// The routine is already running, so don't start a new one, just set the continue flag. This prevents extra GC allocations from Unity's Coroutine.
				routine._continue = false;
			}
			else
			{
				StartCoroutine(routine);
			}

			return routine.Cancel;
		}

		/// <summary>
		/// Waits for <paramref name="yieldInstruction"/> to complete, then calls <paramref name="onComplete"/>.
		/// If <paramref name="yieldInstruction"/> is not a Unity supported <see cref="YieldInstruction"/> or <see cref="CustomYieldInstruction"/>, and it is not intercepted, then this will wait for 1 frame.
		/// If you are using Unity 5.3 or later and <paramref name="yieldInstruction"/> is an <see cref="IEnumerator"/>, it will be started and yielded as a Coroutine by Unity. Earlier versions will simply wait 1 frame.
		/// Returns a cancelation delegate, which will stop the waiting if invoked. If the value passed in is <see langword="true"/>, <paramref name="onComplete"/> will be invoked immediately, otherwise it will not be invoked.
		/// Note: the cancelation delegate should be discarded when it is invoked or when <paramref name="onComplete"/> is invoked. 
		/// </summary>
		/// <param name="yieldInstruction">Yield instruction.</param>
		/// <param name="onComplete">Callback</param>
		/// <typeparam name="TYieldInstruction">The type of yield instruction.</typeparam>
		/// <returns>Cancelation delegate to stop waiting. If the value passed in is <see langword="true"/>, <param name="onComplete"/> will be invoked immediately, otherwise it will not be invoked.</returns>
		public static Action<bool> Yield<TYieldInstruction>(TYieldInstruction yieldInstruction, Action onComplete)
		{
#if DEBUG
			if (onComplete == null)
			{
#pragma warning disable RECS0163 // Suggest the usage of the nameof operator
				throw new ArgumentNullException("onComplete");
#pragma warning restore RECS0163 // Suggest the usage of the nameof operator
			}
#endif

			// Try to intercept.
			Action<bool> cancel;
			if (yieldInterceptor != null && yieldInterceptor.InterceptYieldInstruction(yieldInstruction, onComplete, out cancel))
			{
				return cancel;
			}

			// Grab from pool or create new if pool is empty.
			Routine routine = Routine.GetOrCreate();
			if (routine._continue)
			{
				// The routine is already running, so don't start a new one, just set the continue flag. This prevents extra GC allocations from Unity's Coroutine.
				routine._continue = false;
				routine.Current = yieldInstruction;
				routine.onComplete = onComplete;
				return routine.Cancel;
			}

			routine.Current = yieldInstruction;
			routine.onComplete = onComplete;
			StartCoroutine(routine);

			return routine.Cancel;
		}

		/// <summary>
		/// Waits for one frame, then calls onComplete.
		/// </summary>
		/// <param name="onComplete">Callback</param>
		public static void Yield(Action onComplete)
		{
#if DEBUG
			if (onComplete == null)
			{
#pragma warning disable RECS0163 // Suggest the usage of the nameof operator
				throw new ArgumentNullException("onComplete");
#pragma warning restore RECS0163 // Suggest the usage of the nameof operator
			}
#endif

			// Try to intercept.
			if (yieldInterceptor != null && yieldInterceptor.InterceptSingleFrameWait(onComplete))
			{
				return;
			}

			Routine routine = Routine.GetOrCreate();
			if (routine._continue)
			{
				// The routine is already running, so don't start a new one, just set the continue flag. This prevents extra GC allocations from Unity's Coroutine.
				routine._continue = false;
				routine.onComplete = onComplete;
				return;
			}

			routine.onComplete = onComplete;
			StartCoroutine(routine);
		}

		public static new Coroutine StartCoroutine(IEnumerator routine)
		{
			return Instance._StartCoroutine(routine);
		}

		private Coroutine _StartCoroutine(IEnumerator routine)
		{
			return base.StartCoroutine(routine);
		}

		public static new void StopCoroutine(IEnumerator routine)
		{
			Instance._StopCoroutine(routine);
		}

		private void _StopCoroutine(IEnumerator routine)
		{
			base.StopCoroutine(routine);
		}

		public static new void StopCoroutine(Coroutine routine)
		{
			Instance._StopCoroutine(routine);
		}

		private void _StopCoroutine(Coroutine routine)
		{
			base.StopCoroutine(routine);
		}
	}
}
#pragma warning restore IDE0034 // Simplify 'default' expression
#pragma warning restore IDE0018 // Inline variable declaration