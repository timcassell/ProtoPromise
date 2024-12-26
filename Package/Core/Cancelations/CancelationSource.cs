#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
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
        private readonly Internal.CancelationRef _ref;
        private readonly int _sourceId;
        private readonly int _tokenId;

        /// <summary>
        /// Create a new <see cref="CancelationSource"/>.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        public static CancelationSource New()
            => new CancelationSource(Internal.CancelationRef.GetOrCreate());

        private CancelationSource(Internal.CancelationRef cancelationRef)
        {
            _ref = cancelationRef;
            _sourceId = _ref.SourceId;
            _tokenId = _ref.TokenId;
        }

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled either when you cancel it, or when the given token is canceled, whichever is first.
        /// <para/>Note: the new <see cref="CancelationSource"/> still must be disposed when you are finished with it.
        /// </summary>
        /// <param name="token">The cancelation token to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source token.</returns>
        public static CancelationSource New(CancelationToken token)
        {
            CancelationSource newCancelationSource = New();
            newCancelationSource._ref.MaybeLinkToken(token);
            return newCancelationSource;
        }

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled either when you cancel it, or when any of the given tokens are canceled, whichever is first.
        /// <para/>Note: the new <see cref="CancelationSource"/> still must be disposed when you are finished with it.
        /// </summary>
        /// <param name="token1">The first cancelation token to observe.</param>
        /// <param name="token2">The second cancelation token to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source token.</returns>
        public static CancelationSource New(CancelationToken token1, CancelationToken token2)
        {
            CancelationSource newCancelationSource = New();
            newCancelationSource._ref.MaybeLinkToken(token1);
            newCancelationSource._ref.MaybeLinkToken(token2);
            return newCancelationSource;
        }

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled either when you cancel it, or when any of the given tokens are canceled, whichever is first.
        /// <para/>Note: the new <see cref="CancelationSource"/> still must be disposed when you are finished with it.
        /// </summary>
        /// <param name="tokens">An array that contains the cancelation token instances to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source token.</returns>
        public static CancelationSource New(params CancelationToken[] tokens)
        {
            CancelationSource newCancelationSource = New();
            for (int i = 0, max = tokens.Length; i < max; ++i)
            {
                newCancelationSource._ref.MaybeLinkToken(tokens[i]);
            }
            return newCancelationSource;
        }

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled either when you cancel it, or when any of the given tokens are canceled, whichever is first.
        /// <para/>Note: the new <see cref="CancelationSource"/> still must be disposed when you are finished with it.
        /// </summary>
        /// <param name="tokens">A <see cref="ReadOnlySpan{T}"/> that contains the cancelation token instances to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source token.</returns>
        public static CancelationSource New(ReadOnlySpan<CancelationToken> tokens)
        {
            CancelationSource newCancelationSource = New();
            for (int i = 0, max = tokens.Length; i < max; ++i)
            {
                newCancelationSource._ref.MaybeLinkToken(tokens[i]);
            }
            return newCancelationSource;
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
        public bool IsValid
            => Internal.CancelationRef.IsValidSource(_ref, _sourceId);

        /// <summary>
        /// Gets whether cancelation has been requested for this source.
        /// </summary>
        public bool IsCancelationRequested
            => Internal.CancelationRef.IsSourceCanceled(_ref, _sourceId);

        /// <summary>
        /// Try to communicate a request for cancelation, and invoke all callbacks that are registered to the associated <see cref="Token"/>. Returns true if successful, false otherwise.
        /// </summary>
        /// <returns>True if this is valid and was not already canceled, false otherwise.</returns>
        public bool TryCancel()
            => Internal.CancelationRef.TrySetCanceled(_ref, _sourceId);

        /// <summary>
        /// Communicate a request for cancelation, and invoke all callbacks that are registered to the associated <see cref="Token"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void Cancel()
        {
            if (!TryCancel())
            {
                throw new InvalidOperationException("CancelationSource.Cancel: source is not valid or was already canceled.", Internal.GetFormattedStacktrace(1));
            }
        }

        /// <summary>
        /// Try to release all resources used by this <see cref="CancelationSource"/>. This instance will no longer be valid.
        /// </summary>
        /// <returns>True if this is valid and was not already disposed, false otherwise.</returns>
        public bool TryDispose()
            => Internal.CancelationRef.TryDispose(_ref, _sourceId);

        /// <summary>
        /// Release all resources used by this <see cref="CancelationSource"/>. This instance will no longer be valid.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void Dispose()
        {
            if (!TryDispose())
            {
                throw new InvalidOperationException("CancelationSource.Dispose: source is not valid.", Internal.GetFormattedStacktrace(1));
            }
        }

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