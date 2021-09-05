#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;

namespace Proto.Promises
{
    /// <summary>
    /// Cancelation source used to cancel promises.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public
#if CSHARP_7_3_OR_NEWER
        readonly
#endif
        struct CancelationSource : ICancelableAny, IDisposable, IEquatable<CancelationSource>
    {
        private readonly Internal.CancelationRef _ref;
        private readonly short _sourceId;
        private readonly short _tokenId;

        /// <summary>
        /// Create a new <see cref="CancelationSource"/>.
        /// <para/>Note: the new <see cref="CancelationSource"/> must be disposed when you are finished with it.
        /// </summary>
        public static CancelationSource New()
        {
            return new CancelationSource(Internal.CancelationRef.GetOrCreate());
        }

        private CancelationSource(Internal.CancelationRef cancelationRef)
        {
            _ref = cancelationRef;
            _sourceId = _ref.SourceId;
            _tokenId = _ref.TokenId;
        }

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled either when you cancel it, or when the given token is canceled (with the same value), whichever is first.
        /// <para/>Note: the new <see cref="CancelationSource"/> still must be disposed when you are finished with it.
        /// </summary>
        /// <param name="token">The cancelation token to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source token.</returns>
        public static CancelationSource New(CancelationToken token)
        {
            CancelationSource newCancelationSource = New();
            token.MaybeLinkSourceInternal(newCancelationSource._ref);
            return newCancelationSource;
        }

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled either when you cancel it, or when any of the given tokens are canceled (with the same value), whichever is first.
        /// <para/>Note: the new <see cref="CancelationSource"/> still must be disposed when you are finished with it.
        /// </summary>
        /// <param name="token1">The first cancelation token to observe.</param>
        /// <param name="token2">The second cancelation token to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source token.</returns>
        public static CancelationSource New(CancelationToken token1, CancelationToken token2)
        {
            CancelationSource newCancelationSource = New();
            token1.MaybeLinkSourceInternal(newCancelationSource._ref);
            token2.MaybeLinkSourceInternal(newCancelationSource._ref);
            return newCancelationSource;
        }

        /// <summary>
        /// Create a new <see cref="CancelationSource"/> that will be canceled either when you cancel it, or when any of the given tokens are canceled (with the same value), whichever is first.
        /// <para/>Note: the new <see cref="CancelationSource"/> still must be disposed when you are finished with it.
        /// </summary>
        /// <param name="tokens">An array that contains the cancelation token instances to observe.</param>
        /// <returns>A new <see cref="CancelationSource"/> that is linked to the source token.</returns>
        public static CancelationSource New(params CancelationToken[] tokens)
        {
            CancelationSource newCancelationSource = New();
            for (int i = 0, max = tokens.Length; i < max; ++i)
            {
                tokens[i].MaybeLinkSourceInternal(newCancelationSource._ref);
            }
            return newCancelationSource;
        }

        /// <summary>
        /// Get the <see cref="CancelationToken"/> associated with this <see cref="CancelationSource"/>.
        /// </summary>
        public CancelationToken Token
        {
            get
            {
                return new CancelationToken(_ref, _tokenId);
            }
        }

        /// <summary>
        /// Get whether or not this <see cref="CancelationSource"/> is valid.
        /// <para/>A <see cref="CancelationSource"/> is valid if it was created from <see cref="New"/> and was not disposed.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return _ref != null && _ref.SourceId == _sourceId;
            }
        }

        /// <summary>
        /// Gets whether cancelation has been requested for this source.
        /// </summary>
        public bool IsCancelationRequested
        {
            get
            {
                return _ref != null && _ref.IsSourceCanceled(_sourceId);
            }
        }

        /// <summary>
        /// Try to communicate a request for cancelation without providing a reason, and invoke all callbacks that are registered to the associated <see cref="Token"/>. Returns true if successful, false otherwise.
        /// </summary>
        /// <returns>True if this is valid and was not already canceled, false otherwise.</returns>
        public bool TryCancel()
        {
            return _ref != null && _ref.TrySetCanceled(_sourceId);
        }

        /// <summary>
        /// Try to communicate a request for cancelation with the provided reason, and invoke all callbacks that are registered to the associated <see cref="Token"/>.
        /// </summary>
        /// <returns>True if this is valid and was not already canceled, false otherwise.</returns>
        public bool TryCancel<TCancel>(TCancel reason)
        {
            return _ref != null && _ref.TrySetCanceled(ref reason, _sourceId);
        }

        /// <summary>
        /// Communicate a request for cancelation without providing a reason, and invoke all callbacks that are registered to the associated <see cref="Token"/>.
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
        /// Communicate a request for cancelation with the provided reason, and invoke all callbacks that are registered to the associated <see cref="Token"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void Cancel<TCancel>(TCancel reason)
        {
            if (!TryCancel(reason))
            {
                throw new InvalidOperationException("CancelationSource.Cancel: source is not valid or was already canceled.", Internal.GetFormattedStacktrace(1));
            }
        }

        /// <summary>
        /// Try to release all resources used by this <see cref="CancelationSource"/>. This instance will no longer be valid.
        /// </summary>
        /// <returns>True if this is valid and was not already disposed, false otherwise.</returns>
        public bool TryDispose()
        {
            return _ref != null && _ref.TryDispose(_sourceId);
        }

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

        public bool Equals(CancelationSource other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
#if CSHARP_7_3_OR_NEWER
            return obj is CancelationSource source && Equals(source);
#else
            return obj is CancelationSource && Equals((CancelationSource) obj);
#endif
        }

        public override int GetHashCode()
        {
            if (_ref == null)
            {
                return 0;
            }
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + _sourceId.GetHashCode();
                hash = hash * 31 + _ref.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(CancelationSource c1, CancelationSource c2)
        {
            return c1._ref == c2._ref & c1._sourceId == c2._sourceId;
        }

        public static bool operator !=(CancelationSource c1, CancelationSource c2)
        {
            return !(c1 == c2);
        }
    }
}