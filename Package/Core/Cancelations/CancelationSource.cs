#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Timers;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises
{
    /// <summary>
    /// Cancelation source used to cancel operations.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct CancelationSource : ICancelable, IDisposable, IEquatable<CancelationSource>
    {
        internal readonly Internal.CancelationRef _ref;
        private readonly int _sourceId;
        private readonly int _tokenId;

        private CancelationSource(Internal.CancelationRef cancelationRef)
        {
            _ref = cancelationRef;
            _sourceId = _ref.SourceId;
            _tokenId = _ref.TokenId;
        }

        /// <summary>
        /// Create a new <see cref="CancelationSource"/>.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <returns>A new <see cref="CancelationSource"/>.</returns>
        public static CancelationSource New()
            => new CancelationSource(Internal.CancelationRef.GetOrCreate());

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled after the specified <paramref name="delay"/>.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <param name="delay">The time interval to wait before canceling the <see cref="CancelationSource"/>.</param>
        /// <returns>A new <see cref="CancelationSource"/>.</returns>
        public static CancelationSource New(TimeSpan delay)
            => New(delay, Promise.Config.DefaultTimerFactory);

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled after the specified <paramref name="delay"/>.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <param name="delay">The time interval to wait before canceling the <see cref="CancelationSource"/>.</param>
        /// <param name="timerFactory">The <see cref="TimerFactory"/> with which to interpret <paramref name="delay"/>.</param>
        /// <returns>A new <see cref="CancelationSource"/>.</returns>
        public static CancelationSource New(TimeSpan delay, TimerFactory timerFactory)
            => new CancelationSource(Internal.CancelationRef.GetOrCreate(delay, timerFactory));

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled when the given token is canceled.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <param name="token">The cancelation token to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source token.</returns>
        public static CancelationSource New(CancelationToken token)
        {
            var source = New();
            source._ref.MaybeLinkToken(token);
            return source;
        }

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled when the given token is canceled or after the specified <paramref name="delay"/>.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <param name="delay">The time interval to wait before canceling the <see cref="CancelationSource"/>.</param>
        /// <param name="token">The cancelation token to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source token.</returns>
        public static CancelationSource New(TimeSpan delay, CancelationToken token)
            => New(delay, Promise.Config.DefaultTimerFactory, token);

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled after the specified <paramref name="delay"/> or when the given token is canceled.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <param name="delay">The time interval to wait before canceling the <see cref="CancelationSource"/>.</param>
        /// <param name="timerFactory">The <see cref="TimerFactory"/> with which to interpret <paramref name="delay"/>.</param>
        /// <param name="token">The cancelation token to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source token.</returns>
        public static CancelationSource New(TimeSpan delay, TimerFactory timerFactory, CancelationToken token)
        {
            var source = New(delay, timerFactory);
            source._ref.MaybeLinkToken(token);
            return source;
        }

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled when any of the given tokens are canceled.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <param name="token1">The first cancelation token to observe.</param>
        /// <param name="token2">The second cancelation token to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source tokens.</returns>
        public static CancelationSource New(CancelationToken token1, CancelationToken token2)
        {
            var source = New();
            source._ref.MaybeLinkToken(token1);
            source._ref.MaybeLinkToken(token2);
            return source;
        }

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled after the specified <paramref name="delay"/> or when any of the given tokens are canceled.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <param name="delay">The time interval to wait before canceling the <see cref="CancelationSource"/>.</param>
        /// <param name="token1">The first cancelation token to observe.</param>
        /// <param name="token2">The second cancelation token to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source tokens.</returns>
        public static CancelationSource New(TimeSpan delay, CancelationToken token1, CancelationToken token2)
            => New(delay, Promise.Config.DefaultTimerFactory, token1, token2);

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled after the specified <paramref name="delay"/> or when any of the given tokens are canceled.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <param name="delay">The time interval to wait before canceling the <see cref="CancelationSource"/>.</param>
        /// <param name="timerFactory">The <see cref="TimerFactory"/> with which to interpret <paramref name="delay"/>.</param>
        /// <param name="token1">The first cancelation token to observe.</param>
        /// <param name="token2">The second cancelation token to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source tokens.</returns>
        public static CancelationSource New(TimeSpan delay, TimerFactory timerFactory, CancelationToken token1, CancelationToken token2)
        {
            var source = New(delay, timerFactory);
            source._ref.MaybeLinkToken(token1);
            source._ref.MaybeLinkToken(token2);
            return source;
        }

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled when any of the given tokens are canceled.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <param name="tokens">An array that contains the cancelation token instances to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source tokens.</returns>
        public static CancelationSource New(params CancelationToken[] tokens)
        {
            var source = New();
            for (int i = 0, max = tokens.Length; i < max; ++i)
            {
                source._ref.MaybeLinkToken(tokens[i]);
            }
            return source;
        }

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled after the specified <paramref name="delay"/> or when any of the given tokens are canceled.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <param name="delay">The time interval to wait before canceling the <see cref="CancelationSource"/>.</param>
        /// <param name="tokens">An array that contains the cancelation token instances to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source tokens.</returns>
        public static CancelationSource New(TimeSpan delay, params CancelationToken[] tokens)
            => New(delay, Promise.Config.DefaultTimerFactory, tokens);

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled after the specified <paramref name="delay"/> or when any of the given tokens are canceled.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <param name="delay">The time interval to wait before canceling the <see cref="CancelationSource"/>.</param>
        /// <param name="timerFactory">The <see cref="TimerFactory"/> with which to interpret <paramref name="delay"/>.</param>
        /// <param name="tokens">An array that contains the cancelation token instances to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source tokens.</returns>
        public static CancelationSource New(TimeSpan delay, TimerFactory timerFactory, params CancelationToken[] tokens)
        {
            var source = New(delay, timerFactory);
            for (int i = 0, max = tokens.Length; i < max; ++i)
            {
                source._ref.MaybeLinkToken(tokens[i]);
            }
            return source;
        }

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled when any of the given tokens are canceled.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <param name="tokens">A <see cref="ReadOnlySpan{T}"/> that contains the cancelation token instances to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source tokens.</returns>
        public static CancelationSource New(ReadOnlySpan<CancelationToken> tokens)
        {
            var source = New();
            for (int i = 0, max = tokens.Length; i < max; ++i)
            {
                source._ref.MaybeLinkToken(tokens[i]);
            }
            return source;
        }

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled after the specified <paramref name="delay"/> or when any of the given tokens are canceled.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <param name="delay">The time interval to wait before canceling the <see cref="CancelationSource"/>.</param>
        /// <param name="tokens">A <see cref="ReadOnlySpan{T}"/> that contains the cancelation token instances to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source tokens.</returns>
        public static CancelationSource New(TimeSpan delay, ReadOnlySpan<CancelationToken> tokens)
            => New(delay, Promise.Config.DefaultTimerFactory, tokens);

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled after the specified <paramref name="delay"/> or when any of the given tokens are canceled.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        /// <param name="delay">The time interval to wait before canceling the <see cref="CancelationSource"/>.</param>
        /// <param name="timerFactory">The <see cref="TimerFactory"/> with which to interpret <paramref name="delay"/>.</param>
        /// <param name="tokens">A <see cref="ReadOnlySpan{T}"/> that contains the cancelation token instances to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source tokens.</returns>
        public static CancelationSource New(TimeSpan delay, TimerFactory timerFactory, ReadOnlySpan<CancelationToken> tokens)
        {
            var source = New(delay, timerFactory);
            for (int i = 0, max = tokens.Length; i < max; ++i)
            {
                source._ref.MaybeLinkToken(tokens[i]);
            }
            return source;
        }
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Get the <see cref="CancelationToken"/> associated with this <see cref="CancelationSource"/>.
        /// </summary>
        public CancelationToken Token
        {
            [MethodImpl(Internal.InlineOption)]
            get => new CancelationToken(_ref, _tokenId);
        }

        /// <summary>
        /// Get whether or not this <see cref="CancelationSource"/> is valid.
        /// <para/>A <see cref="CancelationSource"/> is valid if it was created from <see cref="New()"/> and was not disposed.
        /// </summary>
        [Obsolete("Due to object pooling, this property is inherently unsafe. Prefer != default, and remember to set your CancelationSource fields to default when you Dispose.", false), EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsValid
            => _ref?.IsValidSource(_sourceId) == true;

        /// <summary>
        /// Gets whether cancelation has been requested for this source.
        /// </summary>
        public bool IsCancelationRequested
            => _ref?.IsSourceCanceled(_sourceId) == true;

        /// <summary>
        /// Try to communicate a request for cancelation, and invoke all callbacks that are registered to the associated <see cref="Token"/>. Returns true if successful, false otherwise.
        /// </summary>
        /// <returns>True if this is valid and was not already canceled, false otherwise.</returns>
        [Obsolete("Prefer != default and Cancel.", false), EditorBrowsable(EditorBrowsableState.Never)]
        public bool TryCancel()
            => _ref?.TryCancel(_sourceId) == true;

        /// <summary>
        /// Communicate a request for cancelation, and invoke all callbacks that are registered to the associated <see cref="Token"/>.
        /// </summary>
        /// <remarks>
        /// If this was already canceled, a call do this does nothing.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">This was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public void Cancel()
            => _ref.Cancel(_sourceId);

        /// <summary>
        /// Schedules a cancel operation on this <see cref="CancelationSource"/>.
        /// </summary>
        /// <param name="delay">The time interval to wait before canceling this <see cref="CancelationSource"/>.</param>
        /// <exception cref="ObjectDisposedException">This was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public void CancelAfter(TimeSpan delay)
            => _ref.CancelAfter(delay, _sourceId);

        /// <summary>
        /// Try to release all resources used by this <see cref="CancelationSource"/>. This instance will no longer be valid.
        /// </summary>
        /// <returns>True if this is valid and was not already disposed, false otherwise.</returns>
        [Obsolete("Prefer != default and Dispose.", false), EditorBrowsable(EditorBrowsableState.Never)]
        public bool TryDispose()
            => _ref?.TryDispose(_sourceId) == true;

        /// <summary>
        /// Release all resources used by this <see cref="CancelationSource"/>. This instance will no longer be valid.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public void Dispose()
            => _ref.Dispose(_sourceId);

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="CancelationSource"/>.</summary>
        public bool Equals(CancelationSource other)
            => this == other;

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object obj)
            => obj is CancelationSource source && Equals(source);

        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode()
            => HashCode.Combine(_ref, _sourceId, _tokenId);

        /// <summary>Returns a value indicating whether two <see cref="CancelationSource"/> values are equal.</summary>
        public static bool operator ==(CancelationSource c1, CancelationSource c2)
            => c1._ref == c2._ref
            & c1._sourceId == c2._sourceId
            & c1._tokenId == c2._tokenId;

        /// <summary>Returns a value indicating whether two <see cref="CancelationSource"/> values are not equal.</summary>
        public static bool operator !=(CancelationSource c1, CancelationSource c2)
            => !(c1 == c2);
    }
}