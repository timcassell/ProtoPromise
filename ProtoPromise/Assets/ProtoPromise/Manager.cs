// undef CANCEL to disable, or define CANCEL to enable cancelations on promises.
// If CANCEL is defined, it breaks the Promises/A+ spec "2.1. Promise States", but allows breaking promise chains.
#define CANCEL
// undef PROGRESS to disable, or define PROGRESS to enable progress reports on promises.
// If PROGRESS is defined, promises use more memory. If PROGRESS is undefined, there is no limit to the depth of a promise chain.
#define PROGRESS

using System;

namespace ProtoPromise
{
	partial class Promise
	{
		public enum GeneratedStacktrace
		{
			/// <summary>
			/// Don't generate any extra stack traces.
			/// </summary>
			None,
			/// <summary>
			/// Generate stack traces when Deferred.Reject is called.
			/// If Reject is called with an exception, the generated stack trace is appended to the exception's stacktrace.
			/// </summary>
			Rejections,
			/// <summary>
			/// Generate stack traces when Deferred.Reject is called.
			/// Also generate stack traces every time a promise is created (i.e. with .Then). This can help debug where an invalid object was returned from a .Then delegate.
			/// NOTE: This can be extremely expensive, so you should only enable this if you ran into an error and you are not sure where it came from.
			/// </summary>
			All
		}

		public static class Manager
		{
			/// <summary>
			/// If you need to support more whole numbers (longer promise chains), decrease decimalBits. If you need higher precision, increase decimalBits.
			/// Max Whole Number: 2^(32-<see cref="ProgressDecimalBits"/>)
			/// Precision: 1/(2^<see cref="ProgressDecimalBits"/>)
			/// NOTE: promises that don't wait (.Then with an onResolved that simply returns a value or void) don't count towards the promise chain depth limit.
			/// </summary>
			public const int ProgressDecimalBits = 13;

			/// <summary>
			/// Highly recommend to leave this false in DEBUG mode, so that exceptions will fire if/when promises are used incorrectly after they have already completed.
			/// </summary>
			public static bool PoolObjects { get; set; }

			public static void ClearObjectPool()
			{
				Internal.OnClearPool.Invoke();
			}

#if DEBUG
			public static GeneratedStacktrace DebugStacktraceGenerator { get; set; }
#else
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter
			public static GeneratedStacktrace DebugStacktraceGenerator { get { return default(GeneratedStacktrace); } set { } }
#pragma warning restore RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter
#endif
		}



		// Calls to this get compiled away when CANCEL is defined.
		static partial void ValidateCancel();
#if !CANCEL
		static partial void ValidateCancel()
		{
			throw new InvalidOperationException("Define CANCEL in ProtoPromise/Manager.cs to enable cancelations.");
		}
#endif

		// Calls to this get compiled away when PROGRESS is undefined.
		partial void SetDepth(Promise next);
#if !PROGRESS
		private void ProgressPrivate(Action<float> onProgress)
		{
			throw new InvalidOperationException("Define PROGRESS in ProtoPromise/Managers.cs to enable progress reports.");
		}

		protected void ResetDepth() { }

		partial class Internal
		{
			public abstract class PromiseWaitPromise<TPromise> : PoolablePromise<TPromise> where TPromise : PromiseWaitPromise<TPromise>
			{
				protected void SubscribeProgress(Promise other) { }
			}

			public abstract class PromiseWaitPromise<T, TPromise> : PoolablePromise<T, TPromise> where TPromise : PromiseWaitPromise<T, TPromise>
			{
				protected void SubscribeProgress(Promise other) { }
			}

			public abstract class PromiseWaitDeferred<TPromise> : PoolablePromise<TPromise> where TPromise : PromiseWaitDeferred<TPromise> { }
			
			public abstract class PromiseWaitDeferred<T, TPromise> : PoolablePromise<T, TPromise> where TPromise : PromiseWaitDeferred<T, TPromise> { }
		}
#else
		protected Internal.UnsignedFixed32 _waitDepthAndProgress;

		// TODO: Put this under an #if FINALLY symbol.
		private Internal.FinallyProgressContainer _previousFinallyPendCount;

		protected void ResetDepth()
		{
			_waitDepthAndProgress = default(Internal.UnsignedFixed32);
		}

		partial void SetDepth(Promise next)
		{
			next.SetDepth(_waitDepthAndProgress);
		}

		protected virtual void SetDepth(Internal.UnsignedFixed32 previousDepth)
		{
			_waitDepthAndProgress = previousDepth;
		}

		private ulong GetDepth()
		{
			return _waitDepthAndProgress.WholePart + _previousFinallyPendCount.pendCount + 1u;
		}

		// Return true to break the loop.
		protected virtual bool SubscribeProgress(Action<float> onProgress, Promise subscribee)
		{
			switch (_state)
			{
				case PromiseState.Pending:
				{
					return false;
				}
				case PromiseState.Resolved:
				{
					// Invoke immediately if resolved.
					InvokeCurrentProgress(onProgress, subscribee.GetDepth(), _waitDepthAndProgress.WholePart, 1f);
					return true;
				}
				default:
				{
					return true;
				}
			}
		}

		private void ProgressPrivate(Action<float> onProgress)
		{
			// TODO: Simplify this algorithm, it only needs to check if promise._previous is null, do a subscribe and invoke if so, all others simply subscribe.
			Promise promise = this;
			do
			{
				if (promise.SubscribeProgress(onProgress, this))
				{
					break;
				}
				promise = promise._previous;
			} while (promise != null);
		}

		protected static void InvokeCurrentProgress(Action<float> onProgress, float listenerDepth, float reporterDepth, float progress)
		{
			// Calculate the normalized progress for the depth that the listener was added.
			// Divide twice is slower, but gives better precision than single divide.
			onProgress.Invoke((reporterDepth / listenerDepth) + (progress / listenerDepth));
		}

		partial class Internal
		{
			public sealed class FinallyProgressContainer : ILinked<FinallyProgressContainer>
			{
				public FinallyProgressContainer Next { get; set; }

				public UnsignedFixed64 progress;
				public ulong pendCount;
				
				private ulong retainCounter;

				private static ValueLinkedStack<FinallyProgressContainer> pool = new ValueLinkedStack<FinallyProgressContainer>();

				static FinallyProgressContainer()
				{
					OnClearPool += () => pool.Clear();
				}

				private FinallyProgressContainer() { }

				public static FinallyProgressContainer GetOrCreate(ulong depth)
				{
					FinallyProgressContainer fpc = pool.IsNotEmpty ? pool.Pop() : new FinallyProgressContainer();
					fpc.pendCount = depth;
					fpc.progress = default(UnsignedFixed64);
					return fpc;
				}

				public void Retain()
				{
					++retainCounter;
				}

				public void Release()
				{
					if (--retainCounter == 0)
					{
						pool.Push(this);
					}
				}
			}

			/// <summary>
			/// Max Whole Number: 2^(32-<see cref="Manager.ProgressDecimalBits"/>)
			/// Precision: 1/(2^<see cref="Manager.ProgressDecimalBits"/>)
			/// </summary>
			public struct UnsignedFixed32
			{
				public const uint DecimalMax = 1u << Manager.ProgressDecimalBits;
				private const uint DecimalMask = DecimalMax - 1u;
				private const uint WholeMask = ~DecimalMask;

				private uint _value;

				public UnsignedFixed32(uint wholePart)
				{
					_value = wholePart << Manager.ProgressDecimalBits;
				}

				public uint WholePart { get { return _value >> Manager.ProgressDecimalBits; } }
				public float DecimalPart { get { return (float) DecimalPartAsUInt32 / (float) DecimalMax; } }
				public uint DecimalPartAsUInt32 { get { return _value & DecimalMask; } }

				public float ToFloat()
				{
					return WholePart + DecimalPart;
				}

				public uint AssignNewDecimalPartAndGetAsUInt32(float decimalPart)
				{
					// Don't bother rounding, it's more expensive and we don't want to accidentally round to 1.0.
					uint newDecimalPart = (uint) (decimalPart * DecimalMax);
					_value = (_value & WholeMask) | newDecimalPart;
					return newDecimalPart;
				}

				public UnsignedFixed32 GetIncrementedWholeTruncated()
				{
#if DEBUG
					checked
#endif
					{
						return new UnsignedFixed32()
						{
							_value = (_value & WholeMask) + (1u << Manager.ProgressDecimalBits)
						};
					}
				}
			}

			/// <summary>
			/// Max Whole Number: 2^(64-<see cref="Manager.ProgressDecimalBits"/>)
			/// Precision: 1/(2^<see cref="Manager.ProgressDecimalBits"/>)
			/// </summary>
			public struct UnsignedFixed64
			{
				private const ulong DecimalMask = UnsignedFixed32.DecimalMax - 1ul;
				private const ulong WholeMask = ~DecimalMask;

				private ulong _value;

				public UnsignedFixed64(uint wholePart)
				{
					_value = wholePart << Manager.ProgressDecimalBits;
				}

				public ulong WholePart { get { return _value >> Manager.ProgressDecimalBits; } }
				public float DecimalPart { get { return (float) DecimalPartAsUInt64 / (float) UnsignedFixed32.DecimalMax; } }
				public ulong DecimalPartAsUInt64 { get { return _value & DecimalMask; } }

				public float ToFloat()
				{
					return WholePart + DecimalPart;
				}

				public ulong ToUInt64()
				{
					return _value;
				}

				public void Increment(ulong increment)
				{
#if DEBUG
					checked
#endif
					{
						_value += increment;
					}
				}
			}

			public class ProgressDelegate : ILinked<ProgressDelegate>
			{
				public ProgressDelegate Next { get; set; }

				private Promise _subscribee;
				private Action<float> _onProgress;

				private static ValueLinkedStack<ProgressDelegate> pool;

				static ProgressDelegate()
				{
					OnClearPool += () => pool.Clear();
				}

				private ProgressDelegate() { }

				public static ProgressDelegate GetOrCreate(Action<float> onProgress, Promise subscribee)
				{
					var progress = pool.IsNotEmpty ? pool.Pop() : new ProgressDelegate();
					progress._onProgress = onProgress;
					progress._subscribee = subscribee;
					return progress;
				}

				public void Report(ulong reporterDepth, float progress)
				{
					InvokeCurrentProgress(_onProgress, _subscribee.GetDepth(), reporterDepth, progress);
				}

				public void Dispose()
				{
					_onProgress = null;
					pool.Push(this);
				}

				public static bool SubscribeProgress(Action<float> onProgress, Promise subscribee, PromiseState state, UnsignedFixed32 depthAndProgress, ref ValueLinkedQueue<ProgressDelegate> progressListeners)
				{
					switch (state)
					{
						case PromiseState.Pending:
						{
							var del = GetOrCreate(onProgress, subscribee);
							progressListeners.AddLast(del);
							if (depthAndProgress.DecimalPartAsUInt32 > 0u)
							{
								// Invoke immediately if progress is greater than zero.
								InvokeCurrentProgress(onProgress, subscribee.GetDepth(), depthAndProgress.WholePart, depthAndProgress.DecimalPart);
								return true;
							}
							return false;
						}
						case PromiseState.Resolved:
						{
							// Invoke immediately if resolved.
							InvokeCurrentProgress(onProgress, subscribee.GetDepth(), depthAndProgress.WholePart, 1f);
							return true;
						}
						default:
						{
							return true;
						}
					}
				}

				public static void ReportProgress(float progress, uint wholePart, ProgressDelegate progressDelegate)
				{
					while (progressDelegate != null)
					{
						progressDelegate.Report(wholePart, progress);
						progressDelegate = progressDelegate.Next;
					}
				}

				public static void Dispose(ref ValueLinkedQueue<ProgressDelegate> progressDelegates)
				{
					while (progressDelegates.IsNotEmpty)
					{
						progressDelegates.TakeFirst().Dispose();
					}
				}
			}

			public abstract class PromiseWaitPromise<TPromise> : PoolablePromise<TPromise> where TPromise : PromiseWaitPromise<TPromise>
			{
				protected ValueLinkedQueue<ProgressDelegate> _progressListeners;

				// This is used to prevent adding progress listeners to the entire chain of a promise when progress isn't even listened for on this promise.
				private Promise cachedPromise;

				// So that a new delegate doesn't need to be created every time.
				private readonly Action<float> _reportProgress;

				protected PromiseWaitPromise() : base()
				{
					_reportProgress = ReportProgress;
				}

				protected override bool SubscribeProgress(Action<float> onProgress, Promise subscribee)
				{
					bool breakLoop = ProgressDelegate.SubscribeProgress(onProgress, subscribee, _state, _waitDepthAndProgress, ref _progressListeners);
					if (cachedPromise != null)
					{
						cachedPromise.Progress(_reportProgress);
						cachedPromise = null;
					}
					return breakLoop;
				}

				protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
				{
					_waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
				}

				protected override void ReportProgress(float progress)
				{
					_waitDepthAndProgress.AssignNewDecimalPartAndGetAsUInt32(progress);
					ProgressDelegate.ReportProgress(progress, _waitDepthAndProgress.WholePart, _progressListeners.PeekFirst());
				}

				protected void SubscribeProgress(Promise other)
				{
					if (_progressListeners.IsEmpty)
					{
						cachedPromise = other;
					}
					else
					{
						other.Progress(_reportProgress);
					}
				}

				protected override void Dispose()
				{
					base.Dispose();
					cachedPromise = null;
					ProgressDelegate.Dispose(ref _progressListeners);
				}
			}

			public abstract class PromiseWaitPromise<T, TPromise> : PoolablePromise<T, TPromise> where TPromise : PromiseWaitPromise<T, TPromise>
			{
				protected ValueLinkedQueue<ProgressDelegate> _progressListeners;

				// This is used to prevent adding progress listeners to the entire chain of a promise when progress isn't even listened for on this promise.
				private Promise cachedPromise;

				// So that a new delegate doesn't need to be created every time.
				private readonly Action<float> _reportProgress;

				protected PromiseWaitPromise() : base()
				{
					_reportProgress = ReportProgress;
				}

				protected override bool SubscribeProgress(Action<float> onProgress, Promise subscribee)
				{
					bool breakLoop = ProgressDelegate.SubscribeProgress(onProgress, subscribee, _state, _waitDepthAndProgress, ref _progressListeners);
					if (cachedPromise != null)
					{
						cachedPromise.Progress(_reportProgress);
						cachedPromise = null;
					}
					return breakLoop;
				}

				protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
				{
					_waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
				}

				protected override void ReportProgress(float progress)
				{
					_waitDepthAndProgress.AssignNewDecimalPartAndGetAsUInt32(progress);
					ProgressDelegate.ReportProgress(progress, _waitDepthAndProgress.WholePart, _progressListeners.PeekFirst());
				}

				protected void SubscribeProgress(Promise other)
				{
					if (_progressListeners.IsEmpty)
					{
						cachedPromise = other;
					}
					else
					{
						other.Progress(_reportProgress);
					}
				}

				protected override void Dispose()
				{
					base.Dispose();
					cachedPromise = null;
					ProgressDelegate.Dispose(ref _progressListeners);
				}
			}

			public abstract class PromiseWaitDeferred<TPromise> : PoolablePromise<TPromise> where TPromise : PromiseWaitDeferred<TPromise>
			{
				protected ValueLinkedQueue<ProgressDelegate> _progressListeners;

				protected override bool SubscribeProgress(Action<float> onProgress, Promise subscribee)
				{
					return ProgressDelegate.SubscribeProgress(onProgress, subscribee, _state, _waitDepthAndProgress, ref _progressListeners);
				}

				protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
				{
					_waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
				}

				protected override void ReportProgress(float progress)
				{
					_waitDepthAndProgress.AssignNewDecimalPartAndGetAsUInt32(progress);
					ProgressDelegate.ReportProgress(progress, _waitDepthAndProgress.WholePart, _progressListeners.PeekFirst());
				}

				protected override void Dispose()
				{
					base.Dispose();
					ProgressDelegate.Dispose(ref _progressListeners);
				}
			}

			public abstract class PromiseWaitDeferred<T, TPromise> : PoolablePromise<T, TPromise> where TPromise : PromiseWaitDeferred<T, TPromise>
			{
				protected ValueLinkedQueue<ProgressDelegate> _progressListeners;

				protected override bool SubscribeProgress(Action<float> onProgress, Promise subscribee)
				{
					return ProgressDelegate.SubscribeProgress(onProgress, subscribee, _state, _waitDepthAndProgress, ref _progressListeners);
				}

				protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
				{
					_waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
				}

				protected override void ReportProgress(float progress)
				{
					_waitDepthAndProgress.AssignNewDecimalPartAndGetAsUInt32(progress);
					ProgressDelegate.ReportProgress(progress, _waitDepthAndProgress.WholePart, _progressListeners.PeekFirst());
				}

				protected override void Dispose()
				{
					base.Dispose();
					ProgressDelegate.Dispose(ref _progressListeners);
				}
			}
		}
#endif
	}
}